using Microsoft.Win32;

namespace NotasApp.Services;

/// <summary>Entrada en HKCU\...\CurrentVersion\Run para arrancar la app al iniciar sesión.</summary>
public static class InicioWindows
{
    public const string ArgumentoSegundoPlano = "--segundo-plano";

    private const string Clave = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string Nombre = "NotasApp" + Entorno.Sufijo;

    private static string Comando => $"\"{Environment.ProcessPath}\" {ArgumentoSegundoPlano}";

    public static bool Registrado
    {
        get
        {
            using var k = Registry.CurrentUser.OpenSubKey(Clave);
            return k?.GetValue(Nombre) is string;
        }
    }

    /// <summary>Registra o quita la app. Al registrar, actualiza la ruta por si el ejecutable cambió de lugar.</summary>
    public static void Fijar(bool activo)
    {
        using var k = Registry.CurrentUser.CreateSubKey(Clave);
        if (activo) k.SetValue(Nombre, Comando);
        else k.DeleteValue(Nombre, throwOnMissingValue: false);
    }
}
