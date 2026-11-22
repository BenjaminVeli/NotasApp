namespace NotasApp;

/// <summary>
/// Separa la versión de desarrollo (Debug) de la instalada (Release): cada una tiene su carpeta de
/// datos, su instancia única, su atajo global y su entrada de inicio con Windows. Así, probar desde
/// el código nunca toca los datos reales ni choca con la app instalada.
/// </summary>
public static class Entorno
{
#if DEBUG
    public const bool Desarrollo = true;
#else
    public const bool Desarrollo = false;
#endif

    /// <summary>"-dev" en desarrollo; vacío en la versión instalada.</summary>
    public const string Sufijo = Desarrollo ? "-dev" : "";

    public const string Nombre = Desarrollo ? "Mi espacio (desarrollo)" : "Mi espacio";

    /// <summary>Versión del ensamblado (la de la etiqueta vX.Y.Z al publicar).</summary>
    public static string Version =>
        typeof(Entorno).Assembly.GetName().Version is { } v ? $"{v.Major}.{v.Minor}.{v.Build}" : "0.0.0";
}
