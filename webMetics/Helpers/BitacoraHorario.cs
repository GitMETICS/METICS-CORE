using System.Globalization;

namespace webMetics.Helpers;

/// <summary>
/// Conversión de horas para la bitácora de accesos.
/// La base de datos guarda la hora con GETDATE(), que depende de la zona configurada en el servidor SQL
/// (por ejemplo UTC). El desfase real del servidor se consulta en tiempo de ejecución, por lo que la
/// conversión es correcta sin importar cómo esté configurado ese servidor.
/// </summary>
public static class BitacoraHorario
{
    private static readonly TimeZoneInfo ZonaCostaRica = ObtenerZonaCostaRica();

    private static TimeZoneInfo ObtenerZonaCostaRica()
    {
        foreach (var id in new[] { "America/Costa_Rica", "Central America Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        // Costa Rica no usa horario de verano: UTC-6 todo el año.
        return TimeZoneInfo.CreateCustomTimeZone("Costa Rica", TimeSpan.FromHours(-6), "Costa Rica", "Costa Rica");
    }

    /// <summary>Convierte una hora guardada por el servidor SQL a la hora de Costa Rica.</summary>
    /// <param name="fechaServidor">Hora tal como la devuelve la base de datos.</param>
    /// <param name="desfaseServidorMinutos">Minutos que el servidor SQL está adelantado respecto a UTC (GETDATE - GETUTCDATE).</param>
    public static DateTime ServidorACostaRica(DateTime fechaServidor, int desfaseServidorMinutos)
    {
        if (fechaServidor == DateTime.MinValue)
        {
            return fechaServidor;
        }

        DateTime utc = DateTime.SpecifyKind(fechaServidor.AddMinutes(-desfaseServidorMinutos), DateTimeKind.Utc);
        return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(utc, ZonaCostaRica), DateTimeKind.Unspecified);
    }

    /// <summary>Convierte una hora de Costa Rica a la hora equivalente del servidor SQL.</summary>
    public static DateTime CostaRicaAServidor(DateTime fechaCostaRica, int desfaseServidorMinutos)
    {
        DateTime local = DateTime.SpecifyKind(fechaCostaRica, DateTimeKind.Unspecified);
        DateTime utc = TimeZoneInfo.ConvertTimeToUtc(local, ZonaCostaRica);
        return DateTime.SpecifyKind(utc.AddMinutes(desfaseServidorMinutos), DateTimeKind.Unspecified);
    }

    /// <summary>Interpreta una fecha del filtro con formato yyyy-MM-dd (el que envía el campo de tipo fecha).</summary>
    public static bool IntentarLeerFecha(string? texto, out DateTime fecha)
    {
        return DateTime.TryParseExact(texto?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
    }

    /// <summary>Formato que SQL Server convierte sin ambigüedad a DATETIME2.</summary>
    public static string FormatearParaServidor(DateTime fecha)
    {
        return fecha.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }
}
