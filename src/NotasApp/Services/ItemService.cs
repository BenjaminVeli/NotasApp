using Microsoft.EntityFrameworkCore;
using NotasApp.Data;

namespace NotasApp.Services;

/// <summary>CRUD de notas y reuniones. Siempre trabaja contra SQLite.</summary>
public class ItemService(IDbContextFactory<LocalDbContext> fabrica)
{
    /// <summary>Se dispara tras cualquier cambio (para refrescar contadores del sidebar).</summary>
    public event Action? Cambio;

    public async Task<List<Item>> ListarAsync(string tipo)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var consulta = db.Items.AsNoTracking().Where(i => i.Tipo == tipo && !i.Eliminado);
        // Las reuniones se agrupan por día; dentro del día manda el orden manual.
        var ordenada = tipo == TipoItem.Reunion
            ? consulta.OrderBy(i => i.Fecha).ThenBy(i => i.Orden)
            : consulta.OrderBy(i => i.Orden);
        return await ordenada.ThenByDescending(i => i.CreadoEn).ToListAsync();
    }

    public async Task<Dictionary<string, int>> ContarPendientesAsync()
    {
        await using var db = await fabrica.CreateDbContextAsync();
        return await db.Items.Where(i => !i.Eliminado && !i.Completado)
            .GroupBy(i => i.Tipo)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public async Task<Item> CrearAsync(Item item)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        item.CreadoEn = item.ActualizadoEn = DateTime.UtcNow;
        if (item.Tipo == TipoItem.Reunion)
            await ColocarEnDiaAsync(db, item);
        else
        {
            // Las notas nuevas van arriba de la lista.
            var minimo = await db.Items.Where(i => i.Tipo == item.Tipo && !i.Eliminado)
                .MinAsync(i => (int?)i.Orden) ?? 0;
            item.Orden = minimo - 1;
        }
        db.Items.Add(item);
        Pendiente(db, item.Id);
        await db.SaveChangesAsync();
        Cambio?.Invoke();
        return item;
    }

    public Task ActualizarNotaAsync(Guid id, string titulo, string contenido, DateOnly fecha) =>
        ModificarAsync(id, i => { i.Titulo = titulo; i.Contenido = contenido; i.Fecha = fecha; });

    /// <summary>Actualiza una reunión. Si cambia el día o la hora, se recoloca por hora.</summary>
    public async Task<Item?> ActualizarReunionAsync(Guid id, string titulo, string contenido, DateOnly fecha, TimeOnly hora)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var item = await db.Items.FindAsync(id);
        if (item is null) return null;
        var recolocar = item.Fecha != fecha || item.Hora != hora;
        item.Titulo = titulo;
        item.Contenido = contenido;
        item.Fecha = fecha;
        item.Hora = hora;
        item.ActualizadoEn = DateTime.UtcNow;
        if (recolocar) await ColocarEnDiaAsync(db, item);
        Pendiente(db, id);
        await db.SaveChangesAsync();
        Cambio?.Invoke();
        return item;
    }

    /// <summary>Inserta la reunión en su día antes de la primera que empiece más tarde y renumera el día.</summary>
    private static async Task ColocarEnDiaAsync(LocalDbContext db, Item item)
    {
        var delDia = await db.Items
            .Where(i => i.Tipo == TipoItem.Reunion && !i.Eliminado && i.Fecha == item.Fecha && i.Id != item.Id)
            .OrderBy(i => i.Orden)
            .ToListAsync();
        var pos = delDia.FindIndex(i => i.Hora > item.Hora);
        delDia.Insert(pos < 0 ? delDia.Count : pos, item);
        var ahora = DateTime.UtcNow;
        for (int n = 0; n < delDia.Count; n++)
        {
            if (delDia[n].Orden == n && delDia[n] != item) continue;
            delDia[n].Orden = n;
            if (delDia[n] == item) continue;
            delDia[n].ActualizadoEn = ahora;
            Pendiente(db, delDia[n].Id);
        }
    }

    public Task AlternarCompletadoAsync(Guid id) => ModificarAsync(id, i => i.Completado = !i.Completado);

    public Task EliminarAsync(Guid id) => ModificarAsync(id, i => i.Eliminado = true);

    public Task AlternarFavoritoAsync(Guid id) => ModificarAsync(id, i => i.Favorito = !i.Favorito);

    /// <summary>"Recordar en N minutos" desde el aviso de una reunión.</summary>
    public Task PosponerAsync(Guid id, int minutos) =>
        ModificarAsync(id, i => i.PosponerHasta = DateTime.UtcNow.AddMinutes(minutos));

    public Task QuitarPosponerAsync(Guid id) => ModificarAsync(id, i => i.PosponerHasta = null);

    public async Task ReordenarAsync(IReadOnlyList<Guid> ids)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var items = await db.Items.Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id);
        var ahora = DateTime.UtcNow;
        for (int n = 0; n < ids.Count; n++)
        {
            if (!items.TryGetValue(ids[n], out var item) || item.Orden == n) continue;
            item.Orden = n;
            item.ActualizadoEn = ahora;
            Pendiente(db, item.Id);
        }
        await db.SaveChangesAsync();
    }

    private async Task ModificarAsync(Guid id, Action<Item> cambio)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var item = await db.Items.FindAsync(id);
        if (item is null) return;
        cambio(item);
        item.ActualizadoEn = DateTime.UtcNow;
        Pendiente(db, id);
        await db.SaveChangesAsync();
        Cambio?.Invoke();
    }

    private static void Pendiente(LocalDbContext db, Guid id) =>
        db.SyncPendientes.Add(new SyncPendiente { Tabla = "items", RegistroId = id });
}
