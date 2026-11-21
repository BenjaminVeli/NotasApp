using System.Runtime.InteropServices;
using System.Windows;

namespace NotasApp.Services;

/// <summary>Portapapeles de Windows (más fiable que navigator.clipboard dentro de WebView2).</summary>
public static class Portapapeles
{
    public static async Task<bool> CopiarAsync(string texto)
    {
        // Otra app puede tener el portapapeles abierto un instante: se reintenta.
        for (int intento = 0; intento < 5; intento++)
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() => Clipboard.SetText(texto));
                return true;
            }
            catch (COMException)
            {
                await Task.Delay(60);
            }
        }
        return false;
    }
}
