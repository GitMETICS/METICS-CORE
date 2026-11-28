using System.Data;
using Microsoft.Data.SqlClient;

namespace webMetics.Services;

/// <summary>Guarda horas, estado, nota y acumulados como una sola operación.</summary>
public sealed class CalificacionParticipanteService
{
    private readonly string connectionString;
    public CalificacionParticipanteService(IConfiguration configuration) => connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Falta DefaultConnection.");

    // null significa conservar el campo; cero es un valor válido.
    public bool Guardar(int groupId, string participantId, int? hours, int? grade)
    {
        if (hours is < 0) throw new ArgumentException("Las horas aprobadas no pueden ser negativas.");
        if (grade is < 0 or > 100) throw new ArgumentException("La calificación debe estar entre 0 y 100.");
        if (groupId <= 0 || string.IsNullOrWhiteSpace(participantId))
            throw new ArgumentException("El módulo o participante indicado no es válido.");
        if (hours == null && grade == null) return false;

        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using var participant = new SqlCommand("""
            SELECT id_participante_PK FROM dbo.participante WITH (UPDLOCK, HOLDLOCK)
            WHERE id_participante_PK = @participante;
            """, connection, transaction);
        participant.Parameters.Add("@participante", SqlDbType.NVarChar, 64).Value = participantId;
        if (participant.ExecuteScalar() == null) throw new ArgumentException("No se encontró el participante.");

        using var read = new SqlCommand("""
            SELECT i.id_inscripcion_PK, i.horas_aprobadas, i.calificacion, g.cantidad_horas
            FROM dbo.inscripcion AS i WITH (UPDLOCK, HOLDLOCK)
            INNER JOIN dbo.grupo AS g ON g.id_grupo_PK = i.id_grupo_FK
            WHERE i.id_grupo_FK = @grupo AND i.id_participante_FK = @participante;
            """, connection, transaction);
        read.Parameters.Add("@grupo", SqlDbType.Int).Value = groupId;
        read.Parameters.Add("@participante", SqlDbType.NVarChar, 64).Value = participantId;
        int enrollmentId, totalHours;
        int? previousHours;
        double? previousGrade;
        using (var reader = read.ExecuteReader())
        {
            if (!reader.Read()) throw new ArgumentException("No se encontró la inscripción en ese módulo.");
            enrollmentId = reader.GetInt32(0);
            previousHours = reader.IsDBNull(1) ? null : Convert.ToInt32(reader.GetValue(1));
            previousGrade = reader.IsDBNull(2) ? null : Convert.ToDouble(reader.GetValue(2));
            totalHours = Convert.ToInt32(reader.GetValue(3));
            if (reader.Read()) throw new ArgumentException("Hay inscripciones duplicadas; revise los datos antes de guardar.");
        }
        if (hours > totalHours)
            throw new ArgumentException($"Las horas aprobadas no pueden ser mayores a {totalHours}, la duración del módulo.");
        var hoursChanged = hours.HasValue && hours != previousHours;
        var gradeChanged = grade.HasValue && grade != previousGrade;
        if (!hoursChanged && !gradeChanged) return false;

        using var save = new SqlCommand("""
            UPDATE dbo.inscripcion SET
                horas_aprobadas = CASE WHEN @cambiarHoras = 1 THEN @horas ELSE horas_aprobadas END,
                horas_matriculadas = CASE WHEN @cambiarHoras = 1 THEN @duracion - @horas ELSE horas_matriculadas END,
                estado = CASE WHEN @cambiarHoras = 0 THEN estado
                    WHEN @horas > 0 AND @horas = @duracion THEN N'Aprobado'
                    WHEN @horas > 0 THEN N'Incompleto' ELSE N'Inscrito' END,
                calificacion = CASE WHEN @cambiarNota = 1 THEN @nota ELSE calificacion END
            WHERE id_inscripcion_PK = @inscripcion;
            """, connection, transaction);
        save.Parameters.Add("@cambiarHoras", SqlDbType.Bit).Value = hoursChanged;
        save.Parameters.Add("@cambiarNota", SqlDbType.Bit).Value = gradeChanged;
        save.Parameters.Add("@horas", SqlDbType.Int).Value = (object?)hours ?? DBNull.Value;
        save.Parameters.Add("@nota", SqlDbType.Int).Value = (object?)grade ?? DBNull.Value;
        save.Parameters.Add("@duracion", SqlDbType.Int).Value = totalHours;
        save.Parameters.Add("@inscripcion", SqlDbType.Int).Value = enrollmentId;
        if (save.ExecuteNonQuery() != 1) throw new InvalidOperationException("No se pudo guardar la inscripción.");
        if (hoursChanged)
        {
            // Conserva la regla existente: solo Aprobado suma horas aprobadas;
            // solo Inscrito suma horas matriculadas; Incompleto no suma acumulados.
            using var totals = new SqlCommand("""
                UPDATE dbo.participante SET
                    total_horas_aprobadas = (
                        SELECT COALESCE(SUM(CASE WHEN horas_aprobadas > 0 AND COALESCE(horas_matriculadas, 0) <= 0
                            THEN horas_aprobadas ELSE 0 END), 0)
                        FROM dbo.inscripcion WHERE id_participante_FK = @participante),
                    total_horas_matriculadas = (
                        SELECT COALESCE(SUM(CASE WHEN COALESCE(horas_aprobadas, 0) <= 0
                            THEN COALESCE(horas_matriculadas, 0) ELSE 0 END), 0)
                        FROM dbo.inscripcion WHERE id_participante_FK = @participante)
                WHERE id_participante_PK = @participante;
                """, connection, transaction);
            totals.Parameters.Add("@participante", SqlDbType.NVarChar, 64).Value = participantId;
            if (totals.ExecuteNonQuery() != 1) throw new InvalidOperationException("No se pudieron actualizar los acumulados.");
        }
        transaction.Commit();
        return true;
    }
}
