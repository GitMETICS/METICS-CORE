using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using webMetics.Handlers;

public class InscripcionHandler : BaseDeDatosHandler
{
    public InscripcionHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
    {
    }

    // Método para insertar una nueva inscripción en un grupo
    public bool InsertarInscripcion(InscripcionModel inscripcion)
    {
        // Consulta SQL para insertar una nueva inscripción
        string consulta = "INSERT INTO inscripcion (id_grupo_FK, id_participante_FK, estado, observaciones) " +
                            "VALUES (@id_grupo_FK, @id_participante_FK, @estado, @observaciones)";

        // Llama al método AgregarInscripcion para realizar la inserción y devuelve el resultado
        return AgregarInscripcion(consulta, inscripcion);
    }

    // Método para agregar una inscripción en la base de datos
    public bool AgregarInscripcion(string consulta, InscripcionModel inscripcion)
    {
        // Variables para verificar si la consulta fue exitosa
        bool exito;
        ConexionMetics.Open();

        // Crea un nuevo comando SQL con la consulta y la conexión establecida
        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
        comandoConsulta.Parameters.AddWithValue("@id_grupo_FK", inscripcion.idGrupo);
        comandoConsulta.Parameters.AddWithValue("@id_participante_FK", inscripcion.idParticipante);
        comandoConsulta.Parameters.AddWithValue("@estado", "Cursando");
        comandoConsulta.Parameters.AddWithValue("@observaciones", "");

        // Ejecuta la consulta y verifica si se insertó al menos un registro
        exito = comandoConsulta.ExecuteNonQuery() >= 1;

        // Cierra la conexión a la base de datos
        ConexionMetics.Close();

        return exito;
    }

    // Método para obtener una lista de modelos de InscripcionModel con información de las inscripciones en un grupo específico
    public List<InscripcionModel> ObtenerInscripcionesDelGrupo(int idGrupo)
    {
        // Consulta SQL para obtener las inscripciones en un grupo específico
        string consulta = "SELECT P.nombre+' '+P.apellido_1 AS " + @"""NombreCompleto""," +
                            " I.* " +
                            " FROM inscripcion I " +
                            " JOIN participante P " +
                            " ON P.id_participante_PK = I.id_participante_FK " +
                            " WHERE I.id_grupo_FK = " + idGrupo;

        // Crea un nuevo comando SQL con la consulta y la conexión establecida
        SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

        // Crea una tabla con los resultados de la consulta
        DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

        // Crea una lista para almacenar la información de las inscripciones en el grupo
        List<InscripcionModel> infoInscripcion = new List<InscripcionModel>();

        // Recorre cada fila de la tabla de resultados y agrega la información de cada inscripción al grupo a la lista
        foreach (DataRow filaInscripcion in tablaResultado.Rows)
        {
            infoInscripcion.Add(ObtenerInfoInscripcion(filaInscripcion));
        }

        // Si no hay información de inscripciones en el grupo, retorna null, de lo contrario, retorna la lista de información
        if (infoInscripcion.Count == 0)
        {
            return null;
        }
        return infoInscripcion;
    }

    // Método para obtener un modelo de InscripcionModel con la información de una inscripción en un grupo
    public InscripcionModel ObtenerInfoInscripcion(DataRow filaInscripcion)
    {
        // Crea un nuevo modelo de InscripcionModel y asigna los valores de la fila correspondiente
        InscripcionModel info = new InscripcionModel
        {
            idInscripcion = Convert.ToInt32(filaInscripcion["id_inscripcion_PK"]),
            idParticipante = Convert.ToString(filaInscripcion["id_participante_FK"]),
            idGrupo = Convert.ToInt32(filaInscripcion["id_grupo_FK"]),
            participanteAsociado = Convert.ToString(filaInscripcion["NombreCompleto"]),
            estado = Convert.ToString(filaInscripcion["estado"]),
            observaciones = Convert.ToString(filaInscripcion["observaciones"]),
        };
        return info;
    }

    // Se encarga de eliminar una inscripción de un grupo específico en la base de datos.
    public bool EliminarInscripcion(string idGenerado, string idGrupo)
    {
        bool comprobacionConsultaExitosa;

        string consulta = " DELETE FROM inscripcion  " + // Consulta SQL para eliminar la inscripción del grupo.
                            " WHERE id_participante_FK = @idGenerado AND id_grupo_FK = @idGrupo"; // Condiciones para la eliminación basadas en los parámetros idGenerado e idGrupo.

        ConexionMetics.Open(); // Abre la conexión con la base de datos.

        SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics); 

        comandoParaConsulta.Parameters.AddWithValue("@idGenerado", idGenerado); 
        comandoParaConsulta.Parameters.AddWithValue("@idGrupo", idGrupo); 

        comprobacionConsultaExitosa = comandoParaConsulta.ExecuteNonQuery() >= 1;

        ConexionMetics.Close(); // Cierra la conexión con la base de datos.

        return comprobacionConsultaExitosa;
    }

}

