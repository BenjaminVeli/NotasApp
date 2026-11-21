using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CommunityToolkit.WinUI.Notifications;
using H.NotifyIcon;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotasApp.Data;
using NotasApp.Services;

namespace NotasApp;

public partial class App : Application
{
    private const string NombreMutex = @"Local\NotasApp.Instancia";
    private const string NombreEventoMostrar = @"Local\NotasApp.Mostrar";

    public static IServiceProvider Servicios { get; private set; } = null!;
    /// <summary>True cuando el usuario eligió "Salir": la ventana deja de ocultarse al cerrar.</summary>
    public static bool Saliendo { get; private set; }

    private Mutex? mutex;
    private EventWaitHandle? eventoMostrar;
    private TaskbarIcon? bandeja;
    private MainWindow? ventana;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var segundoPlano = e.Args.Contains(InicioWindows.ArgumentoSegundoPlano);

        // Instancia única: si ya está corriendo, se le pide que muestre su ventana y se sale.
        eventoMostrar = new EventWaitHandle(false, EventResetMode.AutoReset, NombreEventoMostrar);
        mutex = new Mutex(true, NombreMutex, out var esPrimera);
        if (!esPrimera)
        {
            if (!segundoPlano)
            {
                AllowSetForegroundWindow(-1); // ASFW_ANY: deja que la otra instancia pase al frente
                eventoMostrar.Set();
            }
            Shutdown();
            return;
        }
        new Thread(EsperarPedidosDeMostrar) { IsBackground = true, Name = "Instancia única" }.Start();

        var config = new ConfigurationBuilder()
            .AddUserSecrets<App>(optional: true)
            .AddEnvironmentVariables("NOTASAPP_")
            .Build();

        var servicios = new ServiceCollection();
        servicios.AddWpfBlazorWebView();
#if DEBUG
        servicios.AddBlazorWebViewDeveloperTools();
#endif
        servicios.AddDbContextFactory<LocalDbContext>(o => o.UseSqlite(RutasDatos.CadenaSqlite));

        // La cadena de Neon nunca va en el código: User Secrets en desarrollo.
        var neon = config.GetConnectionString("Neon");
        if (!string.IsNullOrWhiteSpace(neon))
            servicios.AddDbContextFactory<CloudDbContext>(o => o.UseNpgsql(neon));

        servicios.AddSingleton<ItemService>();
        servicios.AddSingleton<CategoriaService>();
        servicios.AddSingleton<ArchivoService>();
        servicios.AddSingleton<ConexionService>();
        servicios.AddSingleton<ConfiguracionService>();
        servicios.AddSingleton<AvisosService>();
        servicios.AddSingleton<NavegacionService>();
        Servicios = servicios.BuildServiceProvider();

        var fabrica = Servicios.GetRequiredService<IDbContextFactory<LocalDbContext>>();
        await using (var db = await fabrica.CreateDbContextAsync())
            await db.Database.MigrateAsync();
        await Servicios.GetRequiredService<CategoriaService>().SembrarAsync();

        // Mantiene la entrada del registro al día (por si el ejecutable cambió de lugar).
        InicioWindows.Fijar(Servicios.GetRequiredService<ConfiguracionService>().IniciarConWindows);

        ToastNotificationManagerCompat.OnActivated += AlActivarAviso;
        CrearBandeja();
        Servicios.GetRequiredService<ConexionService>().Iniciar();
        Servicios.GetRequiredService<AvisosService>().Iniciar();

        // Al arrancar con Windows queda oculta en la bandeja. Si la abrió un clic en un
        // aviso, AlActivarAviso decide qué mostrar.
        if (!segundoPlano && !ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
            MostrarVentana();
    }

    /// <summary>Muestra (o crea) la ventana principal y opcionalmente navega a una ruta.</summary>
    public void MostrarVentana(string? ruta = null)
    {
        ventana ??= new MainWindow();
        if (ruta is not null) Servicios.GetRequiredService<NavegacionService>().Ir(ruta);
        ventana.Show();
        if (ventana.WindowState == WindowState.Minimized) ventana.WindowState = WindowState.Normal;
        ventana.Activate();
        // Truco habitual para traerla al frente cuando otra app tiene el foco.
        ventana.Topmost = true;
        ventana.Topmost = false;
        ventana.Focus();
    }

    /// <summary>La primera vez que se cierra la ventana, explica que la app sigue en la bandeja.</summary>
    public void AvisarSegundoPlano()
    {
        var config = Servicios.GetRequiredService<ConfiguracionService>();
        if (config.Actual.AvisoBandejaMostrado) return;
        config.Guardar(c => c.AvisoBandejaMostrado = true);
        AvisosService.MostrarMensaje("Mi espacio sigue funcionando",
            "La app queda en la bandeja, junto al reloj, para avisarte de tus reuniones. Para cerrarla del todo usa Salir en su ícono.");
    }

    public void Salir()
    {
        Saliendo = true;
        bandeja?.Dispose();
        ventana?.Close();
        Shutdown();
    }

    private void CrearBandeja()
    {
        var menu = new ContextMenu();
        var abrir = new MenuItem { Header = "Abrir Mi espacio", FontWeight = FontWeights.SemiBold };
        abrir.Click += (_, _) => MostrarVentana();
        var salir = new MenuItem { Header = "Salir" };
        salir.Click += (_, _) => Salir();
        menu.Items.Add(abrir);
        menu.Items.Add(new Separator());
        menu.Items.Add(salir);

        bandeja = new TaskbarIcon
        {
            ToolTipText = "Mi espacio",
            IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/app.ico")),
            ContextMenu = menu,
            NoLeftClickDelay = true,
        };
        bandeja.TrayLeftMouseUp += (_, _) => MostrarVentana();
        bandeja.ForceCreate(enablesEfficiencyMode: false);
    }

    private void EsperarPedidosDeMostrar()
    {
        while (eventoMostrar!.WaitOne())
        {
            if (Saliendo) return;
            Dispatcher.BeginInvoke(() => MostrarVentana());
        }
    }

    /// <summary>Clic en una notificación o en uno de sus botones (llega en otro hilo).</summary>
    private async void AlActivarAviso(ToastNotificationActivatedEventArgsCompat e)
    {
        var args = ToastArguments.Parse(e.Argument);
        args.TryGetValue("accion", out string? accion);
        Guid.TryParse(args.TryGetValue("id", out string? id) ? id : null, out var itemId);

        switch (accion)
        {
            case "posponer":
                var minutos = args.TryGetValue("min", out string? m) && int.TryParse(m, out var n) ? n : 5;
                await Servicios.GetRequiredService<ItemService>().PosponerAsync(itemId, minutos);
                break;
            case "descartar":
                break;
            case "abrir" when itemId != Guid.Empty:
                // El parámetro "n" hace que se resalte otra vez aunque ya estuviera abierta.
                await Dispatcher.InvokeAsync(() => MostrarVentana($"reuniones?id={itemId}&n={DateTime.Now.Ticks}"));
                break;
            default:
                await Dispatcher.InvokeAsync(() => MostrarVentana());
                break;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        bandeja?.Dispose();
        (Servicios as IDisposable)?.Dispose();
        mutex?.Dispose();
        base.OnExit(e);
    }

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int procesoId);
}
