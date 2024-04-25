using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;
using webMetics.Models;

/*
 * Handler de los asesores
 * En esta clase se encuentran los metodos backend para realizar las consultas a la base de datos
 */
namespace webMetics.Handlers
{
    public class AsesorHandler : BaseDeDatosHandler
    {
        private GrupoHandler accesoAGrupo = new GrupoHandler();

        // Crear la consulta para crear el asesor en la base de datos
        public bool CrearAsesor(AsesorModel asesor)
        {
            bool exito;
            string consulta = "INSERT INTO asesor" +
                             "(id_usuario_FK, id_asesor_PK, nombre, apellido_1, apellido_2, telefonos, descripcion) " +
                             "VALUES (@idUsuario, @idAsesor, @nombre, @apellido1, @apellido2, @telefonos, @descripcion)";
            try
            {
                ConexionMetics.Open();

                SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);

                comandoConsulta.Parameters.AddWithValue("@idUsuario", asesor.identificacion);
                comandoConsulta.Parameters.AddWithValue("@idAsesor", asesor.identificacion);
                comandoConsulta.Parameters.AddWithValue("@nombre", asesor.nombre);
                comandoConsulta.Parameters.AddWithValue("@apellido1", asesor.apellido1);
                comandoConsulta.Parameters.AddWithValue("@apellido2", asesor.apellido2);
                comandoConsulta.Parameters.AddWithValue("@telefonos", asesor.telefonos);
                comandoConsulta.Parameters.AddWithValue("@descripcion", asesor.descripcion);

                exito = comandoConsulta.ExecuteNonQuery() >= 1;

                ConexionMetics.Close();
            } 
            catch
            {
                exito = false;
            }

            return exito;
        }

        /*Consulta en la base de datos para obtener todos los asesores en la tabla asesor
         * Retorna una lista de participantes
         */
        public List<AsesorModel> ObtenerListaAsesores()
        {
            string consulta = "SELECT * FROM asesor";
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);
            List<AsesorModel> listaAsesores = new List<AsesorModel>();

            // Iterar sobre las filas de la tabla resultado para convertirlas en objetos AsesorModel
            foreach (DataRow filaAsesor in tablaResultado.Rows)
            {
                // Obtener un objeto AsesorModel a partir de la fila actual
                listaAsesores.Add(ObtenerAsesor(filaAsesor));
            }

            // Si no hay asesores en la lista, retornar nulo
            if (listaAsesores.Count == 0)
            {
                return null;
            }

            // Retornar la lista de asesores obtenida de la base de datos
            return listaAsesores;
        }

        /* Metodo para obtener los grupos que da un asesor
         */
        public List<GrupoModel> ObtenerListaGruposAsesor(string identificacion)
        {
            string consulta = "SELECT G.id_grupo_PK, G.id_tema_FK, G.modalidad, G.cupo, " +
                "G.descripcion, G.es_visible, G.lugar, G.nombre, G.horario, G.fecha_inicio_grupo, " +
                "G.fecha_finalizacion_grupo, G.fecha_inicio_inscripcion, G.fecha_finalizacion_inscripcion, " +
                "G.cantidad_horas, G.nombre_archivo, T.nombre, A.nombre + ' ' + A.apellido_1, TA.nombre " +
                "FROM grupo G JOIN tema T ON T.id_tema_PK = G.id_tema_FK " +
                "JOIN asesor_da_tema ADT ON ADT.id_tema_FK = T.id_tema_PK " +
                "JOIN asesor A ON A.id_asesor_PK = ADT.id_asesor_FK " +
                "JOIN tipos_actividad TA ON T.id_tipos_actividad_FK = TA.id_tipos_actividad_PK " +
                "WHERE A.id_asesor_PK = @id";

            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoParaConsulta.Parameters.AddWithValue("@id", identificacion);

            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);
            List<GrupoModel> listaGrupos = new List<GrupoModel>();

            foreach (DataRow filaGrupo in tablaResultado.Rows)
            {
                listaGrupos.Add(accesoAGrupo.ObtenerGrupo(filaGrupo));
            }

            // Si no hay grupos en la lista, retornar nulo
            if (listaGrupos.Count == 0)
            {
                return null;
            }

            // Retornar la lista de grupos obtenida de la base de datos
            return listaGrupos;
        }


        // Método para convertir una fila de la tabla asesor en un objeto AsesorModel
        public AsesorModel ObtenerAsesor(DataRow filaAsesor)
        {
            AsesorModel asesor = new AsesorModel
            {
                // Asignar los valores de las columnas de la fila a las propiedades del objeto AsesorModel
                identificacion = Convert.ToString(filaAsesor["id_asesor_PK"]),
                nombre = Convert.ToString(filaAsesor["nombre"]),
                apellido1 = Convert.ToString(filaAsesor["apellido_1"]),
                apellido2 = Convert.ToString(filaAsesor["apellido_2"]),
                descripcion = Convert.ToString(filaAsesor["descripcion"]),
                telefonos = Convert.ToString(filaAsesor["telefonos"])
            };

            // Retornar el objeto AsesorModel creado a partir de la fila de la tabla asesor
            return asesor;
        }

        // Método para eliminar un asesor de la base de datos mediante su ID generado
        public bool EliminarAsesor(string idAsesor)
        {
            bool consultaExitosa;
            string consulta = "DELETE FROM asesor WHERE id_asesor_PK = @idAsesor";

            // Crear el comando de consulta con la consulta SQL y la conexión establecida
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            comandoParaConsulta.Parameters.AddWithValue("@idAsesor", idAsesor);

            ConexionMetics.Open();

            // Ejecutar la consulta y obtener el número de filas afectadas (mayor o igual a 1 si se eliminó correctamente)
            consultaExitosa = comandoParaConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            // Retornar un valor booleano que indica si la consulta se ejecutó exitosamente
            return consultaExitosa;
        }

        // Método para verificar si un asesor puede ser eliminado
        public bool PuedeEliminarAsesor(string idAsesor)
        {
            bool eliminar = true;

            try
            {
                List<GrupoModel> gruposAsesor = ObtenerListaGruposAsesor(idAsesor);
                if (gruposAsesor != null && gruposAsesor.Any(g => g.esVisible == 1))
                {
                    eliminar = false;
                }
            }
            catch
            {
                eliminar = false;
            }

            return eliminar;
        }

        // Método para editar un asesor en la base de datos
        public bool EditarAsesor(AsesorModel asesor)
        {
            bool comprobacionConsultaExitosa;
            string consulta = "UPDATE asesor SET nombre = @nombre , apellido_1 = @apellido1 , apellido_2 = @apellido2, " +
                              "telefonos = @telefonos,  descripcion= @descripcion " +
                              "WHERE id_asesor_PK = @id";

            // Si el apellido2 es nulo, reemplazarlo con "-"
            if (asesor.apellido2 == null)
            {
                asesor.apellido2 = "-";
            }

            // Abrir la conexión a la base de datos
            ConexionMetics.Open();

            // Crear el comando de consulta con la consulta SQL y la conexión establecida
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            // Asignar los valores de los parámetros con los atributos del objeto AsesorModel

            comandoParaConsulta.Parameters.AddWithValue("@id", asesor.identificacion);
            comandoParaConsulta.Parameters.AddWithValue("@nombre", asesor.nombre);
            comandoParaConsulta.Parameters.AddWithValue("@apellido1", asesor.apellido1);
            comandoParaConsulta.Parameters.AddWithValue("@apellido2", asesor.apellido2);
            comandoParaConsulta.Parameters.AddWithValue("@telefonos", asesor.telefonos);
            comandoParaConsulta.Parameters.AddWithValue("@descripcion", asesor.descripcion);

            // Ejecutar la consulta y obtener el número de filas afectadas (mayor o igual a 1 si se actualizó correctamente)
            comprobacionConsultaExitosa = comandoParaConsulta.ExecuteNonQuery() >= 1;

            // Cerrar la conexión a la base de datos
            ConexionMetics.Close();

            // Retornar un valor booleano que indica si la consulta se ejecutó exitosamente
            return comprobacionConsultaExitosa;
        }


        /*
         * Metodo para obtener una lista de SelectListItem con los nombres de los asesores indexados para multiselect de formularios
         */
        public List<SelectListItem> ObtenerNombresDeAsesoresIndexados()
        {
            // Obtener la lista de nombres de asesores
            List<string> asesores = ObtenerNombreAsesores();

            // Lista de SelectListItem que contendrá los nombres de asesores indexados
            List<SelectListItem> asesoresParseados = new List<SelectListItem>();

            // Iterar sobre los nombres de asesores para crear objetos SelectListItem y agregarlos a la lista
            foreach (string asesor in asesores)
            {
                asesoresParseados.Add(new SelectListItem { Text = asesor, Value = asesor });
            }

            // Retornar la lista de SelectListItem con nombres de asesores indexados
            return asesoresParseados;
        }

        // Método para obtener la tabla de asesores desde la base de datos
        public DataTable ObtenerTablaAsesores()
        {
            string consulta = "SELECT * FROM asesor";

            // Crear el comando de consulta con la consulta SQL y la conexión establecida
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            // Obtener la tabla resultado de la consulta
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

            // Retornar la tabla de asesores obtenida de la base de datos
            return tablaResultado;
        }

        // Método para obtener una lista de nombres completos de asesores para los formularios
        public List<string> ObtenerNombreAsesores()
        {
            List<string> lista = new List<string>();

            // Obtener la tabla de asesores desde la base de datos
            DataTable tablaResultado = this.ObtenerTablaAsesores();

            // Iterar sobre las filas de la tabla de asesores para obtener los nombres completos
            foreach (DataRow fila in tablaResultado.Rows)
            {
                string nombre = Convert.ToString(fila["nombre"]);
                string apellido1 = Convert.ToString(fila["apellido_1"]);
                string apellido2 = Convert.ToString(fila["apellido_2"]);

                // Crear el nombre completo y agregarlo a la lista
                string nombreCompleto = nombre + " " + apellido1 + " " + apellido2;
                lista.Add(nombreCompleto);
            }

            // Retornar la lista de nombres completos de asesores
            return lista;
        }
    }
}