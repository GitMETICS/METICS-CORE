using System.Configuration;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;

namespace webMetics.Handlers
{
    public class CalificacionesHandler : BaseDeDatosHandler
    {
        public CalificacionesHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
        {
        }

        // Método para obtener la calificación de un participante
        public CalificacionModel ObtenerCalificacionParticipante(DataRow filaCalificacion)
        {
            string idParticipante = Convert.ToString(filaCalificacion["id_participante_FK"]);
            ParticipanteHandler participanteHandler = new ParticipanteHandler(_environment, _configuration);

            ParticipanteModel participante = participanteHandler.ObtenerParticipante(idParticipante);

            // Crear un objeto CalificacionModel y asignar valores desde la fila de datos
            CalificacionModel calificacion = new CalificacionModel
            {
                idGrupo = Convert.ToInt32(filaCalificacion["id_grupo_FK"]),
                participante = participante,
                calificacion = Convert.ToDouble(filaCalificacion["calificacion"])
            };
            return calificacion;
        }

        // Método para obtener una lista de todas las calificaciones de un grupo
        public List<CalificacionModel> ObtenerListaCalificaciones(int idGrupo)
        {
            string consulta = "SELECT * FROM inscripcion WHERE id_grupo_FK = " + idGrupo;
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);
            List<CalificacionModel> listaCalificaciones = new List<CalificacionModel>();

            // Iterar sobre las filas de resultado y agregar la información del participante a la lista
            foreach (DataRow filaCalificacion in tablaResultado.Rows)
            {
                listaCalificaciones.Add(ObtenerCalificacionParticipante(filaCalificacion));
            }

            // Si la lista está vacía, devolver null
            if (listaCalificaciones.Count == 0)
            {
                return null;
            }

            return listaCalificaciones;
        }

        // Método para agregar nota a un participante en la base de datos
        public bool IngresarNota(int idGrupo, string idParticipante, int calificacion)
        {
            bool comprobacionConsultaExitosa;
            string consulta = "UPDATE inscripcion SET calificacion = @calificacion WHERE id_grupo_FK = @idGrupo AND id_participante_FK = @idParticipante;";

            ConexionMetics.Open();
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoParaConsulta.Parameters.AddWithValue("@calificacion", calificacion);
            comandoParaConsulta.Parameters.AddWithValue("@idGrupo", idGrupo);
            comandoParaConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);
            
            comprobacionConsultaExitosa = comandoParaConsulta.ExecuteNonQuery() >= 1;
            ConexionMetics.Close();
            return comprobacionConsultaExitosa;
        }
    }
}