using System.IO;
using System.Text.Json;
using CommunityToolkit.WinUI.Notifications;
using Microsoft.EntityFrameworkCore;
using NotasApp.Data;

namespace NotasApp.Services;

public enum TipoAviso { Previo, Ahora, Pospuesto }

/// <summary>
/// Revisa las reuniones pendientes (en SQLite, así funciona sin internet) y muestra
/// notificaciones de Windows: un aviso previo, otro a la hora y los avisos pospuestos.
/// Corre mientras la app esté abierta, aunque la ventana esté oculta en la bandeja.
/// </summary>
public class AvisosService(IDbContextFactory<LocalDbContext> fabrica, ItemService items, ConfiguracionService config) : IDisposable
{
    /// <summary>Margen para el aviso "a la hora" si la app estaba cerrada justo en ese momento.</summary>
    public static readonly TimeSpan Gracia = TimeSpan.FromMinutes(10);

    private static readonly string RutaEstado = Path.Combine(RutasDatos.Carpeta, "avisos.json");
    private readonly CancellationTokenSource cts = new();
    private readonly SemaphoreSlim revisando = new(1, 1);
    // Avisos ya mostrados: "{id}:{inicio:yyyyMMddHHmm}:{tipo}". Persisten para no repetirlos al reiniciar.
    private readonly HashSet<string> mostrados = CargarEstado();

    public void Iniciar()
    {
        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(20));
            do
            {
                try { await RevisarAsync(); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine($"Avisos: {ex}");
                }
            }
            while (await timer.WaitForNextTickAsync(cts.Token).ConfigureAwait(false));
        });
    }

    /// <summary>
    /// Decide qué aviso corresponde a una reunión en este momento, o null si ninguno.
    /// <paramref name="ahora"/> es hora local; PosponerHasta se guarda en UTC.
    /// </summary>
    public static TipoAviso? Decidir(Item r, DateTime ahora, DateTime ahoraUtc, int minutosPrevio, Func<TipoAviso, bool> yaMostrado)
    {
        if (r.Completado || r.Eliminado || r.Hora is not { } hora) return null;
        if (r.PosponerHasta is { } hasta)
            return hasta <= ahoraUtc ? TipoAviso.Pospuesto : null; // pospuesta: nada hasta esa hora

        var inicio = r.Fecha.ToDateTime(hora);
        if (ahora >= inicio && ahora < inicio + Gracia && !yaMostrado(TipoAviso.Ahora))
            return TipoAviso.Ahora;
        if (ahora >= inicio.AddMinutes(-minutosPrevio) && ahora < inicio
            && !yaMostrado(TipoAviso.Previo) && !yaMostrado(TipoAviso.Ahora))
            return TipoAviso.Previo;
        return null;
    }

    public async Task RevisarAsync()
    {
        if (!await revisando.WaitAsync(0)) return;
        try
        {
            var ahora = DateTime.Now;
            var hoy = DateOnly.FromDateTime(ahora);
            List<Item> reuniones;
            await using (var db = await fabrica.CreateDbContextAsync(cts.Token))
            {
                reuniones = await db.Items.AsNoTracking()
                    .Where(i => i.Tipo == TipoItem.Reunion && !i.Eliminado && !i.Completado && i.Hora != null
                                && i.Fecha >= hoy.AddDays(-1) && i.Fecha <= hoy.AddDays(1))
                    .ToListAsync(cts.Token);
            }

            var cambio = false;
            foreach (var r in reuniones)
            {
                var clave = $"{r.Id:N}:{r.Fecha.ToDateTime(r.Hora!.Value):yyyyMMddHHmm}";
                var aviso = Decidir(r, ahora, DateTime.UtcNow, config.Actual.MinutosAviso,
                    t => mostrados.Contains($"{clave}:{t}"));
                if (aviso is null) continue;

                Mostrar(r, ahora);
                if (aviso == TipoAviso.Pospuesto)
                    await items.QuitarPosponerAsync(r.Id);
                else
                {
                    mostrados.Add($"{clave}:{aviso}");
                    cambio = true;
                }
            }
            if (cambio) GuardarEstado();
        }
        finally { revisando.Release(); }
    }

    private static void Mostrar(Item r, DateTime ahora)
    {
        var inicio = r.Fecha.ToDateTime(r.Hora!.Value);
        var faltan = (int)Math.Ceiling((inicio - ahora).TotalMinutes);
        var hora = inicio.ToString("HH:mm");
        var linea = faltan switch
        {
            > 0 => $"Tu reunión empieza en {faltan} {(faltan == 1 ? "minuto" : "minutos")} · {hora}",
            0 => $"Tu reunión empieza ahora · {hora}",
            _ => $"Tu reunión empezó a las {hora}", // p. ej. la app estaba cerrada a esa hora
        };

        var id = r.Id.ToString();
        var builder = new ToastContentBuilder()
            .AddArgument("accion", "abrir")
            .AddArgument("id", id)
            .AddText(r.Titulo)
            .AddText(linea);
        if (!string.IsNullOrWhiteSpace(r.Contenido)) builder.AddText(r.Contenido);

        builder
            .SetToastScenario(ToastScenario.Reminder)
            .AddButton(BotonPosponer(id, 5))
            .AddButton(BotonPosponer(id, 10))
            .AddButton(new ToastButton().SetContent("Descartar")
                .AddArgument("accion", "descartar").AddArgument("id", id).SetBackgroundActivation())
            .Show(t =>
            {
                // Un aviso nuevo de la misma reunión reemplaza al anterior.
                t.Tag = r.Id.ToString("N");
                t.Group = "reuniones";
            });
    }

    private static ToastButton BotonPosponer(string id, int minutos) =>
        new ToastButton().SetContent($"Recordar en {minutos} min")
            .AddArgument("accion", "posponer").AddArgument("id", id).AddArgument("min", minutos)
            .SetBackgroundActivation();

    /// <summary>Notificación simple sin acciones de reunión (prueba, aviso de bandeja).</summary>
    public static void MostrarMensaje(string titulo, string texto) =>
        new ToastContentBuilder()
            .AddArgument("accion", "abrir-app")
            .AddText(titulo)
            .AddText(texto)
            .Show();

    private static HashSet<string> CargarEstado()
    {
        try
        {
            if (File.Exists(RutaEstado))
                return JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(RutaEstado)) ?? [];
        }
        catch (JsonException) { }
        return [];
    }

    private void GuardarEstado()
    {
        // Descarta avisos de reuniones de hace más de 3 días.
        var limite = DateTime.Now.AddDays(-3).ToString("yyyyMMddHHmm");
        mostrados.RemoveWhere(k => k.Split(':') is [_, var fecha, _] && string.CompareOrdinal(fecha, limite) < 0);
        File.WriteAllText(RutaEstado, JsonSerializer.Serialize(mostrados));
    }

    public void Dispose() => cts.Cancel();
}
