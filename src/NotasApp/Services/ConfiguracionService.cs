using System.IO;
using System.Text.Json;
using NotasApp.Data;

namespace NotasApp.Services;

public class Configuracion
{
    /// <summary>Arrancar con Windows (oculta en la bandeja). Null = aún sin decidir: se usa el valor por defecto.</summary>
    public bool? IniciarConWindows { get; set; }
    /// <summary>Minutos de anticipación del aviso previo de una reunión.</summary>
    public int MinutosAviso { get; set; } = 10;
    /// <summary>Ya se avisó que cerrar la ventana deja la app en la bandeja.</summary>
    public bool AvisoBandejaMostrado { get; set; }
    /// <summary>Atajo global para mostrar u ocultar la app ("Ctrl+Alt+N"). Null = el de por defecto.</summary>
    public string? AtajoGlobal { get; set; }
}

/// <summary>Configuración local del equipo, guardada en %LOCALAPPDATA%\NotasApp\config.json.</summary>
public class ConfiguracionService
{
#if DEBUG
    // En desarrollo no se registra el ejecutable de bin\Debug para arrancar con Windows,
    // salvo que se active a mano desde Configuración.
    private const bool IniciarConWindowsPorDefecto = false;
#else
    private const bool IniciarConWindowsPorDefecto = true;
#endif

    private static readonly string Ruta = Path.Combine(RutasDatos.Carpeta, "config.json");
    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        // Legible si se abre a mano: "Ctrl+Alt+N" en vez de "Ctrl+Alt+N".
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public Configuracion Actual { get; private set; } = Cargar();

    public bool IniciarConWindows => Actual.IniciarConWindows ?? IniciarConWindowsPorDefecto;

    public void Guardar(Action<Configuracion> cambio)
    {
        cambio(Actual);
        File.WriteAllText(Ruta, JsonSerializer.Serialize(Actual, Opciones));
    }

    private static Configuracion Cargar()
    {
        try
        {
            if (File.Exists(Ruta))
                return JsonSerializer.Deserialize<Configuracion>(File.ReadAllText(Ruta)) ?? new();
        }
        catch (JsonException) { }
        return new();
    }
}
