namespace NotasApp.Services;

/// <summary>
/// Pide a la interfaz Blazor que navegue a una ruta desde fuera de ella (bandeja, avisos).
/// Si la ventana aún no se creó, la ruta queda pendiente hasta que el layout se inicie.
/// </summary>
public class NavegacionService
{
    private string? pendiente;

    public event Action<string>? Solicitud;

    public void Ir(string ruta)
    {
        if (Solicitud is null) pendiente = ruta;
        else Solicitud(ruta);
    }

    public string? TomarPendiente()
    {
        var p = pendiente;
        pendiente = null;
        return p;
    }
}
