using System.IO;
using Microsoft.EntityFrameworkCore;
using NotasApp.Data;

namespace NotasApp.Services;

/// <summary>Archivos de código dentro de una categoría. Siempre trabaja contra SQLite.</summary>
public class ArchivoService(IDbContextFactory<LocalDbContext> fabrica)
{
    /// <summary>Se dispara al crear, eliminar o mover archivos (cambian los contadores del sidebar).</summary>
    public event Action? Cambio;

    public async Task<List<Archivo>> ListarAsync(Guid categoriaId)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        return await db.Archivos.AsNoTracking()
            .Where(a => a.CategoriaId == categoriaId && !a.Eliminado)
            .OrderBy(a => a.Orden).ThenByDescending(a => a.CreadoEn)
            .ToListAsync();
    }

    /// <summary>Agrega la extensión de la categoría si el nombre no trae ninguna.</summary>
    public static string CompletarNombre(string nombre, string extension)
    {
        nombre = nombre.Trim();
        return Path.HasExtension(nombre) ? nombre : nombre + extension;
    }

    public async Task<bool> ExisteNombreAsync(Guid categoriaId, string nombre, Guid? excluir = null)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var minusculas = nombre.ToLower();
        return await db.Archivos.AnyAsync(a => a.CategoriaId == categoriaId && !a.Eliminado
                                               && a.Nombre.ToLower() == minusculas && a.Id != excluir);
    }

    public async Task<Archivo> CrearAsync(Guid categoriaId, string nombre)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        // Los nuevos van arriba de la lista.
        var minimo = await db.Archivos.Where(a => a.CategoriaId == categoriaId && !a.Eliminado)
            .MinAsync(a => (int?)a.Orden) ?? 0;
        var archivo = new Archivo { CategoriaId = categoriaId, Nombre = nombre, Orden = minimo - 1 };
        db.Archivos.Add(archivo);
        Pendiente(db, archivo.Id);
        await db.SaveChangesAsync();
        Cambio?.Invoke();
        return archivo;
    }

    public Task RenombrarAsync(Guid id, string nombre) => ModificarAsync(id, a => a.Nombre = nombre);

    /// <summary>Autoguardado del editor (no dispara <see cref="Cambio"/>: no afecta al sidebar).</summary>
    public Task GuardarContenidoAsync(Guid id, string contenido) =>
        ModificarAsync(id, a => a.Contenido = contenido, avisar: false);

    public Task EliminarAsync(Guid id) => ModificarAsync(id, a => a.Eliminado = true);

    public Task AlternarFavoritoAsync(Guid id) => ModificarAsync(id, a => a.Favorito = !a.Favorito);

    public async Task ReordenarAsync(IReadOnlyList<Guid> ids)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var archivos = await db.Archivos.Where(a => ids.Contains(a.Id)).ToDictionaryAsync(a => a.Id);
        var ahora = DateTime.UtcNow;
        for (int n = 0; n < ids.Count; n++)
        {
            if (!archivos.TryGetValue(ids[n], out var a) || a.Orden == n) continue;
            a.Orden = n;
            a.ActualizadoEn = ahora;
            Pendiente(db, a.Id);
        }
        await db.SaveChangesAsync();
    }

    private async Task ModificarAsync(Guid id, Action<Archivo> cambio, bool avisar = true)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var archivo = await db.Archivos.FindAsync(id);
        if (archivo is null) return;
        cambio(archivo);
        archivo.ActualizadoEn = DateTime.UtcNow;
        Pendiente(db, id);
        await db.SaveChangesAsync();
        if (avisar) Cambio?.Invoke();
    }

    private static void Pendiente(LocalDbContext db, Guid id) =>
        db.SyncPendientes.Add(new SyncPendiente { Tabla = "archivos", RegistroId = id });
}
