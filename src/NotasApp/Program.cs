using CommunityToolkit.WinUI.Notifications;
using NotasApp.Services;
using Velopack;

namespace NotasApp;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        // Velopack va primero: atiende la instalación, las actualizaciones y la desinstalación
        // (en esos casos hace lo suyo y termina el proceso) y aplica una actualización que
        // quedó descargada si no se reinició la app.
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ =>
            {
                // Lo que la app dejó fuera de su carpeta. Los datos de %LOCALAPPDATA%\NotasApp se conservan.
                InicioWindows.Fijar(false);
                ToastNotificationManagerCompat.Uninstall();
            })
            .Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
