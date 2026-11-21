using Microsoft.EntityFrameworkCore;
using NotasApp.Data;

namespace NotasApp.Services;

public enum EstadoConexion { SinConexion, EnLinea, Sincronizando }

/// <summary>
/// Vigila la conexión con Neon. Si no hay cadena de conexión configurada, la app
/// trabaja solo con SQLite y el estado queda en "Sin conexión".
/// </summary>
public class ConexionService(IServiceProvider servicios) : IDisposable
{
    private readonly IDbContextFactory<CloudDbContext>? nube =
        servicios.GetService(typeof(IDbContextFactory<CloudDbContext>)) as IDbContextFactory<CloudDbContext>;
    private readonly CancellationTokenSource cts = new();
    private bool migrada;

    public EstadoConexion Estado { get; private set; } = EstadoConexion.SinConexion;
    public bool Configurada => nube is not null;
    public string? UltimoError { get; private set; }
    public event Action? Cambio;

    public void Iniciar()
    {
        if (nube is null) return;
        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            do { await RevisarAsync(); }
            while (await timer.WaitForNextTickAsync(cts.Token).ConfigureAwait(false));
        });
    }

    private async Task RevisarAsync()
    {
        try
        {
            await using var db = await nube!.CreateDbContextAsync(cts.Token);
            if (!migrada)
            {
                Fijar(EstadoConexion.Sincronizando);
                await db.Database.MigrateAsync(cts.Token);
                migrada = true;
            }
            var ok = await db.Database.CanConnectAsync(cts.Token);
            UltimoError = ok ? null : "No se pudo conectar con Neon";
            Fijar(ok ? EstadoConexion.EnLinea : EstadoConexion.SinConexion);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            UltimoError = ex.Message;
            Fijar(EstadoConexion.SinConexion);
        }
    }

    private void Fijar(EstadoConexion estado)
    {
        if (Estado == estado) return;
        Estado = estado;
        Cambio?.Invoke();
    }

    public void Dispose() => cts.Cancel();
}
