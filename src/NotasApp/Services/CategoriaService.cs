using Microsoft.EntityFrameworkCore;
using NotasApp.Data;

namespace NotasApp.Services;

public record CategoriaResumen(Guid Id, string Nombre, string Lenguaje, int Archivos);

public class CategoriaService(IDbContextFactory<LocalDbContext> fabrica)
{
    public async Task<List<CategoriaResumen>> ListarAsync()
    {
        await using var db = await fabrica.CreateDbContextAsync();
        return await db.Categorias.AsNoTracking()
            .Where(c => !c.Eliminado)
            .OrderBy(c => c.Orden)
            .Select(c => new CategoriaResumen(c.Id, c.Nombre, c.Lenguaje, c.Archivos.Count(a => !a.Eliminado)))
            .ToListAsync();
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
}
