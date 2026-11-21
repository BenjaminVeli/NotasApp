using Microsoft.EntityFrameworkCore;
using NotasApp.Data;

namespace NotasApp.Services;

public record CategoriaResumen(Guid Id, string Nombre, string Lenguaje, string Extension, int Archivos);

public class CategoriaService(IDbContextFactory<LocalDbContext> fabrica)
{
    /// <summary>Se dispara al crear, editar, eliminar o reordenar categorías.</summary>
    public event Action? Cambio;

    public async Task<List<CategoriaResumen>> ListarAsync()
    {
        await using var db = await fabrica.CreateDbContextAsync();
        return await db.Categorias.AsNoTracking()
            .Where(c => !c.Eliminado)
            .OrderBy(c => c.Orden)
            .Select(c => new CategoriaResumen(c.Id, c.Nombre, c.Lenguaje, c.Extension, c.Archivos.Count(a => !a.Eliminado)))
            .ToListAsync();
    }

    public async Task<Categoria> CrearAsync(string nombre, string lenguaje, string extension)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var maximo = await db.Categorias.Where(c => !c.Eliminado).MaxAsync(c => (int?)c.Orden) ?? -1;
        var categoria = new Categoria { Nombre = nombre, Lenguaje = lenguaje, Extension = NormalizarExtension(extension), Orden = maximo + 1 };
        db.Categorias.Add(categoria);
        Pendiente(db, categoria.Id);
        await db.SaveChangesAsync();
        Cambio?.Invoke();
        return categoria;
    }

    public Task ActualizarAsync(Guid id, string nombre, string lenguaje, string extension) =>
        ModificarAsync(id, c => { c.Nombre = nombre; c.Lenguaje = lenguaje; c.Extension = NormalizarExtension(extension); });

    /// <summary>Borrado lógico de la categoría y de todos sus archivos.</summary>
    public async Task EliminarAsync(Guid id)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var categoria = await db.Categorias.Include(c => c.Archivos).FirstOrDefaultAsync(c => c.Id == id);
        if (categoria is null) return;
        var ahora = DateTime.UtcNow;
        categoria.Eliminado = true;
        categoria.ActualizadoEn = ahora;
        Pendiente(db, id);
        foreach (var a in categoria.Archivos.Where(a => !a.Eliminado))
        {
            a.Eliminado = true;
            a.ActualizadoEn = ahora;
            db.SyncPendientes.Add(new SyncPendiente { Tabla = "archivos", RegistroId = a.Id });
        }
        await db.SaveChangesAsync();
        Cambio?.Invoke();
    }

    public async Task ReordenarAsync(IReadOnlyList<Guid> ids)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var categorias = await db.Categorias.Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id);
        var ahora = DateTime.UtcNow;
        for (int n = 0; n < ids.Count; n++)
        {
            if (!categorias.TryGetValue(ids[n], out var c) || c.Orden == n) continue;
            c.Orden = n;
            c.ActualizadoEn = ahora;
            Pendiente(db, c.Id);
        }
        await db.SaveChangesAsync();
        Cambio?.Invoke();
    }

    /// <summary>Crea SQL, CSS y C# la primera vez que se abre la app.</summary>
    public async Task SembrarAsync()
    {
        await using var db = await fabrica.CreateDbContextAsync();
        if (await db.Categorias.AnyAsync()) return;
        db.Categorias.AddRange(
            new Categoria { Nombre = "SQL", Lenguaje = "sql", Extension = ".sql", Orden = 0 },
            new Categoria { Nombre = "CSS", Lenguaje = "css", Extension = ".css", Orden = 1 },
            new Categoria { Nombre = "C#", Lenguaje = "csharp", Extension = ".cs", Orden = 2 });
        await db.SaveChangesAsync();
    }

    /// <summary>".sql", "sql" o " .SQL " → ".sql".</summary>
    public static string NormalizarExtension(string extension)
    {
        var e = extension.Trim().ToLowerInvariant().TrimStart('.');
        return e.Length == 0 ? ".txt" : "." + e;
    }

    private async Task ModificarAsync(Guid id, Action<Categoria> cambio)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var categoria = await db.Categorias.FindAsync(id);
        if (categoria is null) return;
        cambio(categoria);
        categoria.ActualizadoEn = DateTime.UtcNow;
        Pendiente(db, id);
        await db.SaveChangesAsync();
        Cambio?.Invoke();
    }

    private static void Pendiente(LocalDbContext db, Guid id) =>
        db.SyncPendientes.Add(new SyncPendiente { Tabla = "categorias", RegistroId = id });
}
