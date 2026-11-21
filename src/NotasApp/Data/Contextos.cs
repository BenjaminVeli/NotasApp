using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NotasApp.Data;

/// <summary>Modelo compartido entre SQLite (local) y Neon (nube).</summary>
public abstract class BaseDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Archivo> Archivos => Set<Archivo>();

    protected abstract bool EsPostgres { get; }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Item>(e =>
        {
            e.ToTable("items");
            e.HasIndex(x => new { x.Tipo, x.Fecha });
            e.HasIndex(x => x.ActualizadoEn);
        });
        b.Entity<Categoria>(e =>
        {
            e.ToTable("categorias");
            e.HasIndex(x => x.ActualizadoEn);
        });
        b.Entity<Archivo>(e =>
        {
            e.ToTable("archivos");
            e.HasOne(x => x.Categoria).WithMany(c => c.Archivos).HasForeignKey(x => x.CategoriaId);
            e.HasIndex(x => x.ActualizadoEn);
        });

        if (EsPostgres)
        {
            b.Entity<Item>().Property(x => x.Etiquetas).HasColumnType("jsonb");
            b.Entity<Archivo>().Property(x => x.Etiquetas).HasColumnType("jsonb");
        }

        // Columnas en snake_case, como en el plan: actualizado_en, categoria_id, ...
        foreach (var entidad in b.Model.GetEntityTypes())
            foreach (var prop in entidad.GetProperties())
                prop.SetColumnName(SnakeCase(prop.Name));
    }

    private static string SnakeCase(string nombre)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < nombre.Length; i++)
        {
            if (char.IsUpper(nombre[i]) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(nombre[i]));
        }
        return sb.ToString();
    }
}

public class LocalDbContext(DbContextOptions<LocalDbContext> options) : BaseDbContext(options)
{
    public DbSet<SyncPendiente> SyncPendientes => Set<SyncPendiente>();

    protected override bool EsPostgres => false;

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<SyncPendiente>().ToTable("sync_pendientes");
        base.OnModelCreating(b);
    }
}

public class CloudDbContext(DbContextOptions<CloudDbContext> options) : BaseDbContext(options)
{
    protected override bool EsPostgres => true;
}

public static class RutasDatos
{
    public static string Carpeta
    {
        get
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NotasApp");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string CadenaSqlite => $"Data Source={Path.Combine(Carpeta, "notas.db")}";
}

// Fábricas para `dotnet ef migrations add` (no necesitan conexión real).
public class LocalDesignFactory : IDesignTimeDbContextFactory<LocalDbContext>
{
    public LocalDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite(RutasDatos.CadenaSqlite).Options);
}

public class CloudDesignFactory : IDesignTimeDbContextFactory<CloudDbContext>
{
    public CloudDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CloudDbContext>().UseNpgsql("Host=localhost;Database=notas").Options);
}
