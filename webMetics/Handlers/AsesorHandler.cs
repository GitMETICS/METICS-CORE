using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;

/*
 * Handler de los asesores
 * En esta clase se encuentran los metodos backend para realizar las consultas a la base de datos
 */
namespace webMetics.Handlers
{
    public class AsesorHandler : BaseDeDatosHandler
    {
        private GrupoHandler accesoAGrupo;

        public AsesorHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
        {
            accesoAGrupo = new GrupoHandler(environment, configuration);
        }

        // Verificar la existencia de un asesor en la base de datos
        public bool ExisteAsesor(string idAsesor)
        {
            bool existe = false;

            using (var command = new SqlCommand("ExistsAsesor", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idAsesor", idAsesor);
                var existeParam = command.Parameters.Add("@exists", SqlDbType.Int);
                existeParam.Direction = ParameterDirection.Output;

                try
                {
                    ConexionMetics.Open();
                    command.ExecuteNonQuery();
                    existe = Convert.ToInt32(existeParam.Value) == 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ExisteAsesor: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return existe;
        }

        // Insertar un nuevo asesor en la base de datos
        public bool CrearAsesor(AsesorModel asesor)
        {
            bool exito = false;
            using (var command = new SqlCommand("InsertAsesor", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idUsuario", asesor.idAsesor);
                command.Parameters.AddWithValue("@idAsesor", asesor.idAsesor);
                command.Parameters.AddWithValue("@nombre", asesor.nombre);
                command.Parameters.AddWithValue("@apellido1", asesor.primerApellido);
                command.Parameters.AddWithValue("@apellido2", asesor.segundoApellido);
                command.Parameters.AddWithValue("@tipoIdentificacion", asesor.tipoIdentificacion);
                command.Parameters.AddWithValue("@numeroIdentificacion", asesor.numeroIdentificacion);
                command.Parameters.AddWithValue("@correo", asesor.correo);
                command.Parameters.AddWithValue("@descripcion", asesor.descripcion);
                command.Parameters.AddWithValue("@telefono", asesor.telefono);

                try
                {
                    ConexionMetics.Open();
                    exito = command.ExecuteNonQuery() >= 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CrearAsesor: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return exito;
        }

        // Actualizar la información de un asesor en la base de datos
        public bool EditarAsesor(AsesorModel asesor)
        {
            bool exito = false;
            using (var command = new SqlCommand("UpdateAsesor", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idUsuario", asesor.idAsesor);
                command.Parameters.AddWithValue("@idAsesor", asesor.idAsesor);
                command.Parameters.AddWithValue("@nombre", asesor.nombre);
                command.Parameters.AddWithValue("@apellido1", asesor.primerApellido);
                command.Parameters.AddWithValue("@apellido2", asesor.segundoApellido);
                command.Parameters.AddWithValue("@tipoIdentificacion", asesor.tipoIdentificacion);
                command.Parameters.AddWithValue("@numeroIdentificacion", asesor.numeroIdentificacion);
                command.Parameters.AddWithValue("@correo", asesor.correo);
                command.Parameters.AddWithValue("@descripcion", asesor.descripcion);
                command.Parameters.AddWithValue("@telefono", asesor.telefono);

                try
                {
                    ConexionMetics.Open();
                    exito = command.ExecuteNonQuery() >= 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in EditarAsesor: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return exito;
        }

        // Método para obtener un asesor específico según su ID
        public AsesorModel ObtenerAsesor(string idAsesor)
        {
            AsesorModel asesor = null;

            using (SqlCommand command = new SqlCommand("SelectAsesor", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idAsesor", idAsesor);

                ConexionMetics.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        asesor = new AsesorModel
                        {
                            idAsesor = reader.GetString(reader.GetOrdinal("id_asesor_PK")),
                            nombre = reader["nombre"].ToString(),
                            primerApellido = reader["apellido_1"].ToString(),
                            segundoApellido = reader["apellido_2"].ToString(),
                            tipoIdentificacion = reader["tipo_identificacion"].ToString(),
                            numeroIdentificacion = reader["numero_identificacion"].ToString(),
                            correo = reader["correo"].ToString(),
                            descripcion = reader["descripcion"].ToString(),
                            telefono = reader["telefono"].ToString()
                        };
                    }
                }

                ConexionMetics.Close();
            }

            return asesor;
        }







        /*
         
         TODO: Pasar estos métodos a stored procedures en la base de datos.
         
         */

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
                "G.cantidad_horas, G.nombre_archivo, T.nombre, A.nombre + ' ' + A.apellido_1 as nombreAsesor, TA.nombre as tipo_actividad " +
                "FROM grupo G JOIN tema T ON T.id_tema_PK = G.id_tema_FK "+
                "JOIN asesor A ON A.id_asesor_PK = G.id_asesor_FK " +
                "JOIN tipos_actividad TA ON T.id_tipos_actividad_FK = TA.id_tipos_actividad_PK " +
                "WHERE A.id_asesor_PK = @id";

            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoParaConsulta.Parameters.AddWithValue("@id", identificacion);

            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);
            List<GrupoModel> listaGrupos = new List<GrupoModel>();

            foreach (DataRow filaGrupo in tablaResultado.Rows)
            {
                GrupoModel grupo = new GrupoModel
                {
                    idGrupo = Convert.ToInt32(filaGrupo["id_grupo_PK"]),
                    modalidad = Convert.ToString(filaGrupo["modalidad"]),
                    cupo = Convert.ToInt32(filaGrupo["cupo"]),
                    descripcion = Convert.ToString(filaGrupo["descripcion"]),
                    esVisible = Convert.ToBoolean(filaGrupo["es_visible"]),
                    lugar = Convert.ToString(filaGrupo["lugar"]),
                    nombre = Convert.ToString(filaGrupo["nombre"]),
                    horario = Convert.ToString(filaGrupo["horario"]),
                    fechaInicioGrupo = Convert.ToDateTime(filaGrupo["fecha_inicio_grupo"]),
                    fechaFinalizacionGrupo = Convert.ToDateTime(filaGrupo["fecha_finalizacion_grupo"]),
                    fechaInicioInscripcion = Convert.ToDateTime(filaGrupo["fecha_inicio_inscripcion"]),
                    fechaFinalizacionInscripcion = Convert.ToDateTime(filaGrupo["fecha_finalizacion_inscripcion"]),
                    cantidadHoras = Convert.ToInt32(filaGrupo["cantidad_horas"]),
                    temaAsociado = Convert.ToString(filaGrupo["id_tema_FK"]),
                    nombreAsesorAsociado = Convert.ToString(filaGrupo[17]), /// TODO: CAMBIAAAAAAAAR de numero a nombre de columna
                    tipoActividadAsociado = Convert.ToString(filaGrupo["tipo_actividad"]),
                    cupoActual = accesoAGrupo.ObtenerCupoActual(Convert.ToInt32(filaGrupo["id_grupo_PK"])),
                    nombreArchivo = Convert.ToString(filaGrupo["nombre_archivo"])
                };

                listaGrupos.Add(grupo);
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
                idAsesor = Convert.ToString(filaAsesor["id_asesor_PK"]),
                nombre = Convert.ToString(filaAsesor["nombre"]),
                primerApellido = Convert.ToString(filaAsesor["apellido_1"]),
                segundoApellido = Convert.ToString(filaAsesor["apellido_2"]),
                correo = Convert.ToString(filaAsesor["id_asesor_PK"]),
                descripcion = Convert.ToString(filaAsesor["descripcion"]),
                telefono = Convert.ToString(filaAsesor["telefono"])
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
                if (gruposAsesor != null && gruposAsesor.Any(g => g.esVisible == true))
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

        /*// Método para editar un asesor en la base de datos
        public bool EditarAsesor(AsesorModel asesor)
        {
            bool comprobacionConsultaExitosa;
            string consulta = "UPDATE asesor SET nombre = @nombre , apellido_1 = @primerApellido , apellido_2 = @segundoApellido, " +
                              "telefono = @telefono,  descripcion= @descripcion " +
                              "WHERE id_asesor_PK = @id";

            // Si el segundoApellido es nulo, reemplazarlo con "-"
            if (asesor.segundoApellido == null)
            {
                asesor.segundoApellido = "-";
            }

            // Abrir la conexión a la base de datos
            ConexionMetics.Open();

            // Crear el comando de consulta con la consulta SQL y la conexión establecida
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            // Asignar los valores de los parámetros con los atributos del objeto AsesorModel

            comandoParaConsulta.Parameters.AddWithValue("@id", asesor.idAsesor);
            comandoParaConsulta.Parameters.AddWithValue("@nombre", asesor.nombre);
            comandoParaConsulta.Parameters.AddWithValue("@primerApellido", asesor.primerApellido);
            comandoParaConsulta.Parameters.AddWithValue("@segundoApellido", asesor.segundoApellido);
            comandoParaConsulta.Parameters.AddWithValue("@telefono", asesor.telefono);
            comandoParaConsulta.Parameters.AddWithValue("@descripcion", asesor.descripcion);

            // Ejecutar la consulta y obtener el número de filas afectadas (mayor o igual a 1 si se actualizó correctamente)
            comprobacionConsultaExitosa = comandoParaConsulta.ExecuteNonQuery() >= 1;

            // Cerrar la conexión a la base de datos
            ConexionMetics.Close();

            // Retornar un valor booleano que indica si la consulta se ejecutó exitosamente
            return comprobacionConsultaExitosa;
        }*/


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
                string primerApellido = Convert.ToString(fila["apellido_1"]);
                string segundoApellido = Convert.ToString(fila["apellido_2"]);

                // Crear el nombre completo y agregarlo a la lista
                string nombreCompleto = nombre + " " + primerApellido + " " + segundoApellido;
                lista.Add(nombreCompleto);
            }

            // Retornar la lista de nombres completos de asesores
            return lista;
        }
    }
}