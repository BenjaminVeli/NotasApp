namespace NotasApp.Data;

public static class TipoItem
{
    public const string Nota = "nota";
    public const string Reunion = "reunion";
}

/// <summary>Nota o reunión.</summary>
public class Item
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Tipo { get; set; } = TipoItem.Nota;
    public string Titulo { get; set; } = "";
    public string Contenido { get; set; } = "";
    public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public TimeOnly? Hora { get; set; }
    public bool Completado { get; set; }
    public bool Favorito { get; set; }
    /// <summary>Lista en JSON: ["trabajo","urgente"].</summary>
    public string Etiquetas { get; set; } = "[]";
    public DateTime? PosponerHasta { get; set; }
    public int Orden { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
    public bool Eliminado { get; set; }
}

/// <summary>Categoría de código (SQL, CSS, C#, ...).</summary>
public class Categoria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = "";
    /// <summary>Id de lenguaje en Monaco: 'sql', 'css', 'csharp'.</summary>
    public string Lenguaje { get; set; } = "plaintext";
    public string Extension { get; set; } = ".txt";
    public int Orden { get; set; }
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
    public bool Eliminado { get; set; }

    public List<Archivo> Archivos { get; set; } = [];
}

public class Archivo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = "";
    public Guid CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
    public string Contenido { get; set; } = "";
    public bool Favorito { get; set; }
    public string Etiquetas { get; set; } = "[]";
    public int Orden { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
    public bool Eliminado { get; set; }
}

/// <summary>Cambio local pendiente de subir a Neon (solo SQLite).</summary>
public class SyncPendiente
{
    public int Id { get; set; }
    public string Tabla { get; set; } = "";
    public Guid RegistroId { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
