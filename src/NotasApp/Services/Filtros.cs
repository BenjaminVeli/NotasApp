using NotasApp.Data;

namespace NotasApp.Services;

public enum FiltroFecha { Todo, Hoy, Manana, Semana, Rango }
public enum FiltroEstado { Todas, Pendientes, Completadas }

/// <summary>Filtros combinables de la lista de notas/reuniones (Fase 4).</summary>
public class FiltroItems
{
    public FiltroFecha Fecha { get; set; }
    /// <summary>Solo con <see cref="FiltroFecha.Rango"/> (un día si Desde == Hasta).</summary>
    public DateOnly Desde { get; set; } = Fechas.Hoy;
    public DateOnly Hasta { get; set; } = Fechas.Hoy;
    public FiltroEstado Estado { get; set; }
    /// <summary>Incluye también el otro tipo (notas + reuniones).</summary>
    public bool AmbosTipos { get; set; }

    /// <summary>Rango de días del filtro, o null si no filtra por fecha.</summary>
    public (DateOnly Desde, DateOnly Hasta)? Dias => Fecha switch
    {
        FiltroFecha.Hoy => (Fechas.Hoy, Fechas.Hoy),
        FiltroFecha.Manana => (Fechas.Hoy.AddDays(1), Fechas.Hoy.AddDays(1)),
        FiltroFecha.Semana => (Fechas.InicioSemana(Fechas.Hoy), Fechas.InicioSemana(Fechas.Hoy).AddDays(6)),
        FiltroFecha.Rango => (Desde, Hasta),
        _ => null,
    };

    public bool VariosDias => Dias is not { } d || d.Desde != d.Hasta;

    public bool CumpleFecha(DateOnly f) => Dias is not { } d || (f >= d.Desde && f <= d.Hasta);

    public bool CumpleEstado(bool completado) => Estado switch
    {
        FiltroEstado.Pendientes => !completado,
        FiltroEstado.Completadas => completado,
        _ => true,
    };

    public bool Cumple(Item i) => CumpleFecha(i.Fecha) && CumpleEstado(i.Completado);

    /// <summary>Filtra por un solo día, usando Hoy/Mañana cuando corresponde.</summary>
    public void FijarDia(DateOnly f)
    {
        if (f == Fechas.Hoy) Fecha = FiltroFecha.Hoy;
        else if (f == Fechas.Hoy.AddDays(1)) Fecha = FiltroFecha.Manana;
        else { Fecha = FiltroFecha.Rango; Desde = Hasta = f; }
    }

    /// <summary>"lun 23 nov" o "23 nov – 27 nov".</summary>
    public string EtiquetaRango => Desde == Hasta ? Fechas.Corta(Desde) : $"{Fechas.DiaMes(Desde)} – {Fechas.DiaMes(Hasta)}";

    public string Periodo => Fecha switch
    {
        FiltroFecha.Hoy => " para hoy",
        FiltroFecha.Manana => " para mañana",
        FiltroFecha.Semana => " esta semana",
        FiltroFecha.Rango when Desde == Hasta => $" el {Fechas.Corta(Desde)}",
        FiltroFecha.Rango => $" entre el {Fechas.DiaMes(Desde)} y el {Fechas.DiaMes(Hasta)}",
        _ => "",
    };

    public string EstadoTexto => Estado switch
    {
        FiltroEstado.Pendientes => " pendientes",
        FiltroEstado.Completadas => " completadas",
        _ => "",
    };

    public bool Activo => Fecha != FiltroFecha.Todo || Estado != FiltroEstado.Todas || AmbosTipos;
}
