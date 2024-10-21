using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using webMetics.Handlers;

public class InscripcionHandler : BaseDeDatosHandler
{
    public InscripcionHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
    {
    }

    public List<InscripcionModel> ObtenerInscripciones()
    {
        List<InscripcionModel> inscripciones = new List<InscripcionModel>();

        string consulta = "SELECT * FROM inscripcion;";

        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);

        DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
        foreach (DataRow fila in tablaResultado.Rows)
        {
            InscripcionModel inscripcion = new InscripcionModel
            {
                idInscripcion = Convert.ToInt32(fila["id_inscripcion_PK"]),
                idParticipante = Convert.ToString(fila["id_participante_FK"]),
                idGrupo = Convert.ToInt32(fila["id_grupo_FK"]),
                numeroGrupo = Convert.ToInt32(fila["numero_grupo"]),
                nombreGrupo = Convert.ToString(fila["nombre_grupo"]),
                estado = Convert.ToString(fila["estado"]),
                observaciones = Convert.ToString(fila["observaciones"]),
                horasAprobadas = Convert.ToInt32(fila["horas_aprobadas"]),
                horasMatriculadas = Convert.ToInt32(fila["horas_matriculadas"]),
                calificacion = Convert.ToDouble(fila["calificacion"])
            };

            inscripciones.Add(inscripcion);
        }

        return inscripciones;
    }

    public List<InscripcionModel> ObtenerInscripcionesDelGrupo(int idGrupo)
    {
        List<InscripcionModel> inscripciones = new List<InscripcionModel>();

        string consulta = "SELECT * FROM inscripcion WHERE id_grupo_FK = @idGrupo;";
        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
        comandoConsulta.Parameters.AddWithValue("@idGrupo", idGrupo);

        DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
        foreach (DataRow fila in tablaResultado.Rows)
        {
            InscripcionModel inscripcion = new InscripcionModel
            {
                idInscripcion = Convert.ToInt32(fila["id_inscripcion_PK"]),
                idParticipante = Convert.ToString(fila["id_participante_FK"]),
                idGrupo = Convert.ToInt32(fila["id_grupo_FK"]),
                numeroGrupo = Convert.ToInt32(fila["numero_grupo"]),
                nombreGrupo = Convert.ToString(fila["nombre_grupo"]),
                estado = Convert.ToString(fila["estado"]),
                observaciones = Convert.ToString(fila["observaciones"]),
                horasAprobadas = Convert.ToInt32(fila["horas_aprobadas"]),
                horasMatriculadas = Convert.ToInt32(fila["horas_matriculadas"]),
                calificacion = Convert.ToDouble(fila["calificacion"]),
            };

            inscripciones.Add(inscripcion);
        }
        return inscripciones;
    }

    public List<InscripcionModel> ObtenerInscripcionesParticipante(string idParticipante)
    {
        List<InscripcionModel> inscripciones = new List<InscripcionModel>();

        string consulta = "SELECT * FROM inscripcion WHERE id_participante_FK = @idParticipante;";
        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
        comandoConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);

        DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
        foreach (DataRow fila in tablaResultado.Rows)
        {
            InscripcionModel inscripcion = new InscripcionModel
            {
                idInscripcion = Convert.ToInt32(fila["id_inscripcion_PK"]),
                idParticipante = Convert.ToString(fila["id_participante_FK"]),
                idGrupo = Convert.ToInt32(fila["id_grupo_FK"]),
                numeroGrupo = Convert.ToInt32(fila["numero_grupo"]),
                nombreGrupo = Convert.ToString(fila["nombre_grupo"]),
                estado = Convert.ToString(fila["estado"]),
                observaciones = Convert.ToString(fila["observaciones"]),
                horasAprobadas = Convert.ToInt32(fila["horas_aprobadas"]),
                horasMatriculadas = Convert.ToInt32(fila["horas_matriculadas"]),
                calificacion = Convert.ToDouble(fila["calificacion"])
            };

            inscripciones.Add(inscripcion);
        }
        return inscripciones;
    }

    public InscripcionModel ObtenerInscripcionParticipante(int idGrupo, string idParticipante)
    {
        string consulta = "SELECT * FROM inscripcion WHERE id_grupo_FK = @idGrupo AND id_participante_FK = @idParticipante;";
        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
        comandoConsulta.Parameters.AddWithValue("@idGrupo", idGrupo);
        comandoConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);

        DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
        DataRow fila = tablaResultado.Rows[0];

        InscripcionModel inscripcion = new InscripcionModel
        {
            idInscripcion = Convert.ToInt32(fila["id_inscripcion_PK"]),
            idParticipante = Convert.ToString(fila["id_participante_FK"]),
            idGrupo = Convert.ToInt32(fila["id_grupo_FK"]),
            numeroGrupo = Convert.ToInt32(fila["numero_grupo"]),
            nombreGrupo = Convert.ToString(fila["nombre_grupo"]),
            estado = Convert.ToString(fila["estado"]),
            observaciones = Convert.ToString(fila["observaciones"]),
            horasAprobadas = Convert.ToInt32(fila["horas_aprobadas"]),
            horasMatriculadas = Convert.ToInt32(fila["horas_matriculadas"]),
            calificacion = Convert.ToDouble(fila["calificacion"])
        };

        return inscripcion;
    }

    public bool InsertarInscripcion(InscripcionModel inscripcion)
    {
        string consulta = "INSERT INTO inscripcion (id_grupo_FK, id_participante_FK, numero_grupo, nombre_grupo, estado, observaciones, horas_aprobadas, horas_matriculadas)" +
                          "VALUES (@idGrupo, @idParticipante, @numeroGrupo, @nombreGrupo, @estado, @observaciones, @horasAprobadas, @horasMatriculadas);";

        ConexionMetics.Open();

        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
        comandoConsulta.Parameters.AddWithValue("@idGrupo", inscripcion.idGrupo);
        comandoConsulta.Parameters.AddWithValue("@idParticipante", inscripcion.idParticipante);
        comandoConsulta.Parameters.AddWithValue("@numeroGrupo", inscripcion.numeroGrupo);
        comandoConsulta.Parameters.AddWithValue("@nombreGrupo", inscripcion.nombreGrupo);
        comandoConsulta.Parameters.AddWithValue("@horasAprobadas", inscripcion.horasAprobadas);
        comandoConsulta.Parameters.AddWithValue("@horasMatriculadas", inscripcion.horasMatriculadas);
        comandoConsulta.Parameters.AddWithValue("@estado", inscripcion.estado);
        comandoConsulta.Parameters.AddWithValue("@observaciones", "");

        bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

        ConexionMetics.Close();

        return exito;
    }

    public bool EditarInscripcion(InscripcionModel inscripcion)
    {
        string consulta = "UPDATE inscripcion SET id_grupo_FK = @idGrupo, numero_grupo = @numeroGrupo, nombre_grupo = @nombreGrupo, " +
            "id_participante_FK = @idParticipante, estado = @estado, observaciones = @observaciones, " +
            "horas_aprobadas = @horasAprobadas, horas_matriculadas = @horasMatriculadas " +
            "WHERE id_inscripcion_PK = @idInscripcion";

        ConexionMetics.Open();

        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
        comandoConsulta.Parameters.AddWithValue("@idInscripcion", inscripcion.idInscripcion);
        comandoConsulta.Parameters.AddWithValue("@idGrupo", inscripcion.idGrupo);
        comandoConsulta.Parameters.AddWithValue("@numeroGrupo", inscripcion.numeroGrupo);
        comandoConsulta.Parameters.AddWithValue("@nombreGrupo", inscripcion.nombreGrupo);
        comandoConsulta.Parameters.AddWithValue("@idParticipante", inscripcion.idParticipante);
        comandoConsulta.Parameters.AddWithValue("@horasAprobadas", inscripcion.horasAprobadas);
        comandoConsulta.Parameters.AddWithValue("@horasMatriculadas", inscripcion.horasMatriculadas);
        comandoConsulta.Parameters.AddWithValue("@estado", inscripcion.estado);
        comandoConsulta.Parameters.AddWithValue("@observaciones", "");

        bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

        ConexionMetics.Close();

        return exito;
    }

    public bool EliminarInscripcion(int idGrupo, string idParticipante)
    {
        string consulta = "DELETE FROM inscripcion WHERE id_grupo_FK = @idGrupo AND id_participante_FK = @idParticipante;";

        ConexionMetics.Open();

        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);

        comandoConsulta.Parameters.AddWithValue("@idGrupo", idGrupo);
        comandoConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);

        bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

        ConexionMetics.Close();

        return exito;
    }

    public string ObtenerCorreoLimiteHoras()
    {
        string consulta = "SELECT correo FROM correo_notificacion;";
        string correo = string.Empty;

        using (SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics))
        {
            DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);

            if (tablaResultado.Rows.Count > 0)
            {
                DataRow fila = tablaResultado.Rows[0];
                correo = Convert.ToString(fila["correo"]);
            }
        }

        return correo;
    }

    public bool IngresarCorreoLimiteHoras(string correo)
    {
        string consulta = "INSERT INTO correo_notificacion (correo) VALUES (@correo);";

        ConexionMetics.Open();

        bool exito;
        try
        {
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@correo", correo);

            exito = comandoConsulta.ExecuteNonQuery() >= 1;
        }
        catch (Exception ex)
        {
            exito = false; // No viene ningún correo en la consulta.
        }

        ConexionMetics.Close();

        return exito;
    }

    public bool ActualizarCorreoLimiteHoras(string correo)
    {
        string consulta = "UPDATE correo_notificacion SET correo = @correo;";

        ConexionMetics.Open();

        bool exito;
        try
        {
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@correo", correo);

            exito = comandoConsulta.ExecuteNonQuery() >= 1;
        } 
        catch (Exception ex) 
        {
            exito = false; // No viene ningún correo en la consulta.
        }

        ConexionMetics.Close();

        return exito;
    }

    public bool NoEstaInscritoEnGrupo(int idGrupo, string idParticipante)
    {
        bool noEstaInscrito = false;

        List<InscripcionModel> listaInscritos = ObtenerInscripcionesDelGrupo(idGrupo);

        if (listaInscritos == null ||
            listaInscritos.Find(inscripcionModel => inscripcionModel.idParticipante == idParticipante) == null)
        {
            noEstaInscrito = true;
        }

        return noEstaInscrito;
    }

    public int CalcularNumeroHorasAlInscribirse(int horasGrupo, int horasParticipante)
    {
        return horasParticipante + horasGrupo;
    }

    public int CalcularNumeroHorasAprobadas(int horasActuales, int horasNuevas)
    {
        return horasActuales + horasNuevas;
    }

    public int CalcularNumeroHorasAlDesinscribirse(int horasGrupo, int horasParticipante)
    {
        int numeroHoras = horasParticipante - horasGrupo;
        if (numeroHoras <= 0)
        {
            numeroHoras = 0;
        }

        return numeroHoras;
    }

    public InscripcionModel CambiarEstadoDeInscripcion(InscripcionModel inscripcion, GrupoModel grupo)
    {
        if (inscripcion.horasAprobadas >= inscripcion.horasMatriculadas)
        {
            inscripcion.estado = "Aprobado";
        }
        else
        {
            if (grupo.fechaFinalizacionGrupo == DateTime.MinValue)
            {
                inscripcion.estado = "Desconocido";
            }
            else if (grupo.fechaFinalizacionGrupo < DateTime.Now)
            {
                inscripcion.estado = "Incompleto";
            }
            else
            {
                inscripcion.estado = "Inscrito";
            }
        }

        try
        {
            EditarInscripcion(inscripcion);
        }
        catch (Exception ex)
        {
            // Handle exception, log it, or add more specific actions here.
            // For now, assuming it might not exist, consider logging the error.
            Console.WriteLine($"Error editing Inscription: {ex.Message}");
        }

        return inscripcion;
    }

}

