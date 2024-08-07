﻿using System.Data;
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
                estado = Convert.ToString(fila["estado"]),
                observaciones = Convert.ToString(fila["observaciones"]),
                horasAprobadas = Convert.ToInt32(fila["horas_aprobadas"]),
                horasMatriculadas = Convert.ToInt32(fila["horas_matriculadas"])
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
                estado = Convert.ToString(fila["estado"]),
                observaciones = Convert.ToString(fila["observaciones"]),
                horasAprobadas = Convert.ToInt32(fila["horas_aprobadas"]),
                horasMatriculadas = Convert.ToInt32(fila["horas_matriculadas"])
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
                estado = Convert.ToString(fila["estado"]),
                observaciones = Convert.ToString(fila["observaciones"]),
                horasAprobadas = Convert.ToInt32(fila["horas_aprobadas"]),
                horasMatriculadas = Convert.ToInt32(fila["horas_matriculadas"])
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
            estado = Convert.ToString(fila["estado"]),
            observaciones = Convert.ToString(fila["observaciones"]),
            horasAprobadas = Convert.ToInt32(fila["horas_aprobadas"]),
            horasMatriculadas = Convert.ToInt32(fila["horas_matriculadas"])
        };

        return inscripcion;
    }

    public bool InsertarInscripcion(InscripcionModel inscripcion)
    {
        string consulta = "INSERT INTO inscripcion (id_grupo_FK, id_participante_FK, estado, observaciones, horas_aprobadas, horas_matriculadas)" +
                          "VALUES (@idGrupo, @idParticipante, @estado, @observaciones, @horasAprobadas, @horasMatriculadas);";

        ConexionMetics.Open();

        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
        comandoConsulta.Parameters.AddWithValue("@idGrupo", inscripcion.idGrupo);
        comandoConsulta.Parameters.AddWithValue("@idParticipante", inscripcion.idParticipante);
        comandoConsulta.Parameters.AddWithValue("@horasAprobadas", inscripcion.horasAprobadas);
        comandoConsulta.Parameters.AddWithValue("@horasMatriculadas", inscripcion.horasMatriculadas);
        comandoConsulta.Parameters.AddWithValue("@estado", "Cursando");
        comandoConsulta.Parameters.AddWithValue("@observaciones", "");

        bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

        ConexionMetics.Close();

        return exito;
    }

    public bool EditarInscripcion(InscripcionModel inscripcion)
    {
        string consulta = "UPDATE inscripcion SET id_grupo_FK = @idGrupo, id_participante_FK = @idParticipante, " +
                          "estado = @estado, observaciones = @observaciones, " +
                          "horas_aprobadas = @horasAprobadas, horas_matriculadas = @horasMatriculadas " +
                          "WHERE id_inscripcion_PK = @idInscripcion";

        ConexionMetics.Open();

        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
        comandoConsulta.Parameters.AddWithValue("@idInscripcion", inscripcion.idInscripcion);
        comandoConsulta.Parameters.AddWithValue("@idGrupo", inscripcion.idGrupo);
        comandoConsulta.Parameters.AddWithValue("@idParticipante", inscripcion.idParticipante);
        comandoConsulta.Parameters.AddWithValue("@horasAprobadas", inscripcion.horasAprobadas);
        comandoConsulta.Parameters.AddWithValue("@horasMatriculadas", inscripcion.horasMatriculadas);
        comandoConsulta.Parameters.AddWithValue("@estado", "Cursando");
        comandoConsulta.Parameters.AddWithValue("@observaciones", "");

        bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

        ConexionMetics.Close();

        return exito;
    }

    public bool EliminarInscripcion(string idGrupo, string idParticipante)
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
}

