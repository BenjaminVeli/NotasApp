using System.Globalization;

namespace NotasApp.Services;

/// <summary>Formatos de fecha en español para la interfaz.</summary>
public static class Fechas
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");

    public static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>"lun 23 nov"</summary>
    public static string Corta(DateOnly f) => f.ToString("ddd d MMM", Es).Replace(".", "");

    /// <summary>"23 nov"</summary>
    public static string DiaMes(DateOnly f) => f.ToString("d MMM", Es).Replace(".", "");

    /// <summary>"Hoy", "Mañana", "Ayer" o "lunes 23 de noviembre".</summary>
    public static string Larga(DateOnly f)
    {
        var dias = f.DayNumber - Hoy.DayNumber;
        return dias switch
        {
            0 => "Hoy",
            1 => "Mañana",
            -1 => "Ayer",
            _ => Capitalizar(f.ToString(f.Year == Hoy.Year ? "dddd d 'de' MMMM" : "dddd d 'de' MMMM 'de' yyyy", Es)),
        };
    }

    /// <summary>Lunes de la semana de la fecha dada.</summary>
    public static DateOnly InicioSemana(DateOnly f) => f.AddDays(-(((int)f.DayOfWeek + 6) % 7));

    private static string Capitalizar(string s) => s.Length == 0 ? s : char.ToUpper(s[0], Es) + s[1..];
}
