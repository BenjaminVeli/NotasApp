using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NotasApp.Data;

namespace NotasApp.Services;

/// <summary>
/// Un resultado de la búsqueda global. <see cref="Titulo"/> y <see cref="Fragmento"/> marcan el
/// texto encontrado entre <see cref="BusquedaService.InicioMarca"/> y <see cref="BusquedaService.FinMarca"/>.
/// </summary>
public record ResultadoBusqueda(
    string Tipo, Guid Id, string Titulo, string Fragmento, string Detalle, bool Completado, string Ruta);

/// <summary>Búsqueda de texto completo (FTS5 de SQLite): funciona sin internet.</summary>
public partial class BusquedaService(IDbContextFactory<LocalDbContext> fabrica)
{
    public const char InicioMarca = '\u0001';
    public const char FinMarca = '\u0002';
    public const string TipoArchivo = "archivo";

    private const int Limite = 60;

    // Fila tal como la devuelve la consulta FTS5.
    private sealed class Fila
    {
        public string Tipo { get; set; } = "";
        public string RegistroId { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string Fragmento { get; set; } = "";
    }

    [GeneratedRegex(@"[\p{L}\p{N}_]+")]
    private static partial Regex Palabras();

    /// <summary>"reunion equi" → "reunion"* "equi"* (todas las palabras, cada una como prefijo).</summary>
    public static string? ConsultaFts(string texto)
    {
        var palabras = Palabras().Matches(texto).Select(m => m.Value).ToList();
        return palabras.Count == 0 ? null : string.Join(" ", palabras.Select(p => $"\"{p}\"*"));
    }

    /// <summary>Resultados ordenados: notas, reuniones y archivos; dentro de cada grupo, por relevancia.</summary>
    public async Task<List<ResultadoBusqueda>> BuscarAsync(string texto, CancellationToken token = default)
    {
        if (ConsultaFts(texto) is not { } consulta) return [];
        await using var db = await fabrica.CreateDbContextAsync(token);

        // El título pesa más que las etiquetas, y éstas más que el contenido.
        var filas = await db.Database.SqlQueryRaw<Fila>("""
            SELECT tipo AS Tipo, registro_id AS RegistroId,
                   highlight(busqueda, 2, char(1), char(2)) AS Titulo,
                   snippet(busqueda, 3, char(1), char(2), '…', 14) AS Fragmento
            FROM busqueda
            WHERE busqueda MATCH {0}
            ORDER BY bm25(busqueda, 0, 0, 10.0, 1.0, 5.0)
            LIMIT {1}
            """, consulta, Limite).ToListAsync(token);
        if (filas.Count == 0) return [];

        var ids = filas.Select(f => Guid.Parse(f.RegistroId)).ToList();
        var items = await db.Items.AsNoTracking().Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id, token);
        var archivos = await db.Archivos.AsNoTracking().Include(a => a.Categoria)
            .Where(a => ids.Contains(a.Id)).ToDictionaryAsync(a => a.Id, token);

        var marca = DateTime.Now.Ticks; // hace que la página resalte otra vez aunque ya estuviera abierta
        var q = Uri.EscapeDataString(texto.Trim());
        var resultados = new List<ResultadoBusqueda>();
        foreach (var f in filas)
        {
            var id = Guid.Parse(f.RegistroId);
            var fragmento = f.Fragmento.ReplaceLineEndings(" ");
            if (f.Tipo == TipoArchivo && archivos.TryGetValue(id, out var a))
            {
                resultados.Add(new(f.Tipo, id, f.Titulo, fragmento, a.Categoria?.Nombre ?? "", false,
                    $"codigo/{a.CategoriaId}/{id}?q={q}&n={marca}"));
            }
            else if (items.TryGetValue(id, out var i))
            {
                var esReunion = i.Tipo == TipoItem.Reunion;
                var detalle = Fechas.Corta(i.Fecha) + (esReunion && i.Hora is { } h ? $" · {h:HH:mm}" : "");
                resultados.Add(new(f.Tipo, id, f.Titulo, fragmento, detalle, i.Completado,
                    $"{(esReunion ? "reuniones" : "")}?id={id}&n={marca}"));
            }
        }

        return resultados.OrderBy(r => r.Tipo switch { TipoItem.Nota => 0, TipoItem.Reunion => 1, _ => 2 }).ToList();
    }
}
