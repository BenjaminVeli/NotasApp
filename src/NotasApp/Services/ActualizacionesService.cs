using System.IO;
using CommunityToolkit.WinUI.Notifications;
using Velopack;
using Velopack.Sources;

namespace NotasApp.Services;

public enum EstadoActualizacion { NoDisponible, AlDia, Buscando, Descargando, Lista, Error }

/// <summary>
/// Actualizaciones automáticas con Velopack desde GitHub Releases: revisa al iniciar y cada
/// 4 horas, descarga en segundo plano y avisa para reiniciar. Si no se reinicia, la versión
/// descargada se aplica sola la próxima vez que se abra la app (VelopackApp en Program.cs).
/// Solo funciona en la app instalada con el Setup (no en desarrollo ni en la versión portable).
/// </summary>
public sealed class ActualizacionesService : IDisposable
{
    public const string Repositorio = "https://github.com/BenjaminVeli/NotasApp";

    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(4);
    private readonly UpdateManager manager;
    private readonly CancellationTokenSource cts = new();
    private readonly SemaphoreSlim revisando = new(1, 1);
    private VelopackAsset? pendiente;
    private string? avisada;

    public ActualizacionesService()
    {
        // NOTASAPP_ACTUALIZACIONES=<carpeta> permite probar con paquetes locales en vez de GitHub.
        var carpeta = Environment.GetEnvironmentVariable("NOTASAPP_ACTUALIZACIONES");
        IUpdateSource fuente = string.IsNullOrWhiteSpace(carpeta)
            ? new GithubSource(Repositorio, null, false)
            : new SimpleFileSource(new DirectoryInfo(carpeta));
        manager = new UpdateManager(fuente);
    }

    public bool Instalada => manager.IsInstalled;
    public string Version => manager.CurrentVersion?.ToString() ?? Entorno.Version;
    public EstadoActualizacion Estado { get; private set; } = EstadoActualizacion.NoDisponible;
    public string? NuevaVersion { get; private set; }
    public int Progreso { get; private set; }
    public string? Error { get; private set; }
    public DateTime? UltimaRevision { get; private set; }

    /// <summary>Se dispara en otro hilo cuando cambia el estado.</summary>
    public event Action? Cambio;

    public void Iniciar()
    {
        if (!Instalada) return;
        Estado = EstadoActualizacion.AlDia;
        if (manager.UpdatePendingRestart is { } yaDescargada) MarcarLista(yaDescargada);
        _ = Task.Run(async () =>
        {
            try
            {
                // Deja arrancar tranquila a la app antes de la primera revisión.
                await Task.Delay(TimeSpan.FromSeconds(20), cts.Token);
                using var timer = new PeriodicTimer(Intervalo);
                do { await RevisarAsync(); }
                while (await timer.WaitForNextTickAsync(cts.Token));
            }
            catch (OperationCanceledException) { }
        });
    }

    public async Task RevisarAsync()
    {
        if (!Instalada || Estado == EstadoActualizacion.Lista || !await revisando.WaitAsync(0)) return;
        try
        {
            Fijar(EstadoActualizacion.Buscando);
            var info = await manager.CheckForUpdatesAsync();
            UltimaRevision = DateTime.Now;
            if (info is null)
            {
                Fijar(EstadoActualizacion.AlDia);
                return;
            }
            NuevaVersion = info.TargetFullRelease.Version.ToString();
            Progreso = 0;
            Fijar(EstadoActualizacion.Descargando);
            await manager.DownloadUpdatesAsync(info, p => { Progreso = p; Cambio?.Invoke(); }, cts.Token);
            MarcarLista(info.TargetFullRelease);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Error = ex.Message;
            Fijar(EstadoActualizacion.Error);
        }
        finally { revisando.Release(); }
    }

    /// <summary>Cierra la app, instala la versión descargada y la vuelve a abrir.</summary>
    public void AplicarYReiniciar()
    {
        if (pendiente is not null) manager.ApplyUpdatesAndRestart(pendiente);
    }

    private void MarcarLista(VelopackAsset version)
    {
        pendiente = version;
        NuevaVersion = version.Version.ToString();
        Error = null;
        Fijar(EstadoActualizacion.Lista);
        if (avisada == NuevaVersion) return;
        avisada = NuevaVersion;
        new ToastContentBuilder()
            .AddArgument("accion", "abrir-app")
            .AddText("Hay una actualización lista")
            .AddText($"Mi espacio {NuevaVersion} ya se descargó. Reinicia para usarla, o se instalará la próxima vez que abras la app.")
            .AddButton(new ToastButton().SetContent("Reiniciar").AddArgument("accion", "actualizar"))
            .AddButton(new ToastButton().SetContent("Más tarde").AddArgument("accion", "descartar").SetBackgroundActivation())
            .Show();
    }

    private void Fijar(EstadoActualizacion estado)
    {
        Estado = estado;
        Cambio?.Invoke();
    }

    public void Dispose() => cts.Cancel();
}
