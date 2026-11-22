using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace NotasApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        // Evita el destello blanco mientras carga WebView2.
        Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "FF000000");
        InitializeComponent();
        WebView.Services = App.Servicios;
        SourceInitialized += (_, _) => BarraTituloOscura();
#if !DEBUG
        // Sin atajos de navegador (F5/Ctrl+R recargarían la página, Ctrl+P imprimiría…).
        // En desarrollo se dejan para poder abrir las herramientas con F12.
        WebView.BlazorWebViewInitialized += (_, e) =>
            e.WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
#endif
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Cerrar la ventana no cierra la app: queda en la bandeja para los avisos.
        if (!App.Saliendo)
        {
            e.Cancel = true;
            Hide();
            ((App)Application.Current).AvisarSegundoPlano();
        }
        base.OnClosing(e);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int atributo, ref int valor, int tamano);

    private void BarraTituloOscura()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        int si = 1;
        DwmSetWindowAttribute(hwnd, 20, ref si, sizeof(int)); // DWMWA_USE_IMMERSIVE_DARK_MODE
        int color = 0x001E1C1C; // #1C1C1E en formato COLORREF (0x00BBGGRR)
        DwmSetWindowAttribute(hwnd, 35, ref color, sizeof(int)); // DWMWA_CAPTION_COLOR
    }
}
