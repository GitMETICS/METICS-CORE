using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

/*
 * Handler para los grupos
 * En esta clase se encuentran los metodos backend para realizar las consultas a la base de datos
 */

namespace webMetics.Handlers
{
    public class GrupoHandler : BaseDeDatosHandler
    {
        public GrupoHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
        {

        }

        // Método para editar un grupo en la base de datos
        public bool CrearGrupo(GrupoModel grupo)
        {
            bool exito = false;

            using (var command = new SqlCommand("InsertGrupo", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idTema", int.Parse(grupo.temaAsociado));
                command.Parameters.AddWithValue("@nombre", grupo.nombre);
                command.Parameters.AddWithValue("@horario", grupo.horario);
                command.Parameters.AddWithValue("@fecha_inicio_grupo", grupo.fechaInicioGrupo);
                command.Parameters.AddWithValue("@fecha_finalizacion_grupo", grupo.fechaFinalizacionGrupo);
                command.Parameters.AddWithValue("@fecha_inicio_inscripcion", grupo.fechaInicioInscripcion);
                command.Parameters.AddWithValue("@fecha_finalizacion_inscripcion", grupo.fechaFinalizacionInscripcion);
                command.Parameters.AddWithValue("@cantidad_horas", grupo.cantidadHoras);
                command.Parameters.AddWithValue("@modalidad", grupo.modalidad);
                command.Parameters.AddWithValue("@cupo", grupo.cupo);
                command.Parameters.AddWithValue("@descripcion", grupo.descripcion);
                command.Parameters.AddWithValue("@lugar", grupo.lugar);
                command.Parameters.AddWithValue("@nombre_archivo", grupo.nombreArchivo);

                // Convierte el archivo adjunto en un arreglo de bytes para almacenarlo en la base de datos
                byte[] buffer = new byte[0];

                if (grupo.archivoAdjunto != null)
                {
                    long fileLength = grupo.archivoAdjunto.Length;
                    buffer = new byte[fileLength];

                    using (var stream = grupo.archivoAdjunto.OpenReadStream())
                    {
                        stream.Read(buffer, 0, (int)fileLength);
                    }
                }

                command.Parameters.AddWithValue("@adjunto", buffer);

                try
                {
                    ConexionMetics.Open();
                    exito = command.ExecuteNonQuery() >= 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CrearGrupo: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return exito;
        }

        // Método para obtener la lista de grupos desde la base de datos con información detallada
        public List<GrupoModel> ObtenerListaGrupos()
        {
            // Consulta SQL para obtener la información de los grupos y sus asociaciones
            string consulta = "SELECT G.id_grupo_PK, G.id_tema_FK, G.modalidad, G.cupo, G.descripcion, G.es_visible, G.lugar, G.nombre, G.horario, G.fecha_inicio_grupo, G.fecha_finalizacion_grupo, G.fecha_inicio_inscripcion, G.fecha_finalizacion_inscripcion, G.cantidad_horas, G.nombre_archivo, T.nombre AS tema_asociado, A.nombre + ' ' + A.apellido_1 AS asesor, TA.nombre AS tipo_actividad FROM grupo G JOIN tema T ON T.id_tema_PK = G.id_tema_FK JOIN asesor_da_tema ADT ON T.id_tema_PK = ADT.id_tema_FK JOIN asesor A ON ADT.id_asesor_FK = A.id_asesor_PK JOIN tipos_actividad TA ON T.id_tipos_actividad_FK = TA.id_tipos_actividad_PK";

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            // Obtiene una tabla con los resultados de la consulta
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

            // Crea una lista para almacenar los objetos GrupoModel con la información de los grupos
            List<GrupoModel> listaGrupos = new List<GrupoModel>();

            // Recorre cada fila de la tabla y agrega una instancia de GrupoModel a la lista
            foreach (DataRow filaGrupo in tablaResultado.Rows)
            {
                listaGrupos.Add(ObtenerGrupo(filaGrupo));
            }

            // Si no se encontraron grupos, devuelve null; de lo contrario, devuelve la lista
            if (listaGrupos.Count == 0)
            {
                return null;
            }

            return listaGrupos;
        }

        // Método para obtener la información detallada de un grupo específico desde la base de datos
        public GrupoModel ObtenerInfoGrupo(int idGrupo)
        {
            // Consulta SQL para obtener la información de un grupo específico y sus asociaciones mediante su ID
            string consulta = "SELECT G.id_grupo_PK, G.id_tema_FK, G.modalidad, G.cupo, G.descripcion, G.es_visible, G.lugar, G.nombre, G.horario, G.fecha_inicio_grupo, G.fecha_finalizacion_grupo, G.fecha_inicio_inscripcion, G.fecha_finalizacion_inscripcion, G.cantidad_horas, G.nombre_archivo, T.nombre AS tema_asociado, A.nombre + ' ' + A.apellido_1 AS asesor, TA.nombre AS tipo_actividad FROM grupo G JOIN tema T ON T.id_tema_PK = G.id_tema_FK JOIN asesor_da_tema ADT ON T.id_tema_PK = ADT.id_tema_FK JOIN asesor A ON ADT.id_asesor_FK = A.id_asesor_PK JOIN tipos_actividad TA ON T.id_tipos_actividad_FK = TA.id_tipos_actividad_PK WHERE G.id_grupo_PK = " + idGrupo;
;
            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            // Obtiene una tabla con los resultados de la consulta
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

            // Obtiene la primera fila de la tabla (la información del grupo) y la convierte en un objeto GrupoModel
            DataRow info = tablaResultado.Rows[0];
            GrupoModel infoGrupo = ObtenerGrupo(info);

            // Retorna el objeto GrupoModel con la información del grupo específico
            return infoGrupo;
        }

        // Método para convertir una fila de la tabla 'grupo' en un objeto GrupoModel
        public GrupoModel ObtenerGrupo(DataRow filaGrupo)
        {
            // Crea un nuevo objeto GrupoModel y asigna los valores desde el DataRow
            GrupoModel grupo = new GrupoModel
            {
                idGrupo = Convert.ToInt32(filaGrupo["id_grupo_PK"]),
                modalidad = Convert.ToString(filaGrupo["modalidad"]),
                cupo = Convert.ToInt32(filaGrupo["cupo"]),
                descripcion = Convert.ToString(filaGrupo["descripcion"]),
                esVisible = Convert.ToByte(filaGrupo["es_visible"]),
                lugar = Convert.ToString(filaGrupo["lugar"]),
                nombre = Convert.ToString(filaGrupo["nombre"]),
                horario = Convert.ToString(filaGrupo["horario"]),
                fechaInicioGrupo = Convert.ToDateTime(filaGrupo["fecha_inicio_grupo"]),
                fechaFinalizacionGrupo = Convert.ToDateTime(filaGrupo["fecha_finalizacion_grupo"]),
                fechaInicioInscripcion = Convert.ToDateTime(filaGrupo["fecha_inicio_inscripcion"]),
                fechaFinalizacionInscripcion = Convert.ToDateTime(filaGrupo["fecha_finalizacion_inscripcion"]),
                cantidadHoras = Convert.ToInt32(filaGrupo["cantidad_horas"]),
                temaAsociado = Convert.ToString(filaGrupo[15]),
                nombreAsesorAsociado = Convert.ToString(filaGrupo[16]),
                tipoActividadAsociado = Convert.ToString(filaGrupo[17]),
                cupoActual = ObtenerCupoActual(Convert.ToInt32(filaGrupo["id_grupo_PK"])),
                nombreArchivo = Convert.ToString(filaGrupo["nombre_archivo"])
            };

            // Retorna el objeto GrupoModel creado
            return grupo;
        }

        // Consulta para obtener el cupo actual del grupo desde la tabla 'inscripcion'
        public int ObtenerCupoActual(int idGrupo)
        {
            // Consulta SQL para obtener el número de inscripciones en un grupo específico mediante su ID
            string consulta = "SELECT COUNT(id_grupo_FK) FROM inscripcion WHERE id_grupo_FK =" + idGrupo;

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            // Obtiene una tabla con los resultados de la consulta
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

            // Obtiene el valor del cupo actual (el número de inscripciones) desde la primera fila de la tabla
            DataRow filaCupoActual = tablaResultado.Rows[0];
            int cupoActual = Convert.ToInt32(filaCupoActual[0]);

            // Retorna el valor del cupo actual
            return cupoActual;
        }

        public List<GrupoModel> ObtenerListaGruposParticipante(string idParticipante)
        {
            string consulta = "SELECT * FROM grupo G JOIN inscripcion I ON G.id_grupo_PK = I.id_grupo_FK " +
                              "WHERE I.id_participante_FK = @id";

            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoParaConsulta.Parameters.AddWithValue("@id", idParticipante);

            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);
            List<GrupoModel> listaGrupos = new List<GrupoModel>();

            foreach (DataRow filaGrupo in tablaResultado.Rows)
            {
                int idGrupo = Convert.ToInt32(filaGrupo["id_grupo_PK"]);
                listaGrupos.Add(ObtenerInfoGrupo(idGrupo));
            }

            // Si no hay grupos en la lista, retornar nulo
            if (listaGrupos.Count == 0)
            {
                return null;
            }

            // Retornar la lista de grupos obtenida de la base de datos
            return listaGrupos;
        }

        // Método para eliminar un grupo de la base de datos mediante su ID
        public bool EliminarGrupo(int grupoId)
        {
            bool consultaExitosa;

            // Consulta SQL para eliminar un grupo de la tabla 'grupo' utilizando su ID
            string consulta = "DELETE FROM grupo WHERE id_grupo_PK = @grupoId";

            // Abre la conexión con la base de datos
            ConexionMetics.Open();

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@grupoId", grupoId);

            // Ejecuta la consulta y comprueba si al menos una fila fue afectada
            consultaExitosa = comandoConsulta.ExecuteNonQuery() >= 1;

            // Cierra la conexión con la base de datos
            ConexionMetics.Close();

            // Retorna true si la consulta se ejecutó correctamente, de lo contrario, retorna false
            return consultaExitosa;
        }

        // Método para obtener el estado visible de un grupo mediante su ID
        public bool EsVisible(int grupoId)
        {
            // Consulta SQL para obtener el estado 'es_visible' de un grupo específico mediante su ID
            string consulta = "SELECT es_visible FROM grupo WHERE id_grupo_PK = " + grupoId;

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            // Obtiene una tabla con los resultados de la consulta
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

            // Obtiene el valor del estado 'es_visible' desde la primera fila de la tabla
            DataRow esVisible = tablaResultado.Rows[0];
            int estadoActual = Convert.ToInt32(esVisible[0]);

            // Retorna true si el estado es 1 (visible), de lo contrario, retorna false (no visible)
            return estadoActual == 1;
        }

        /* Método para cambiar el estado visible de un grupo
         * Si el estado es 0, el grupo no está visible.
         * Si el estado es 1, el grupo está visible. */
        public bool CambiarEstadoVisible(int idGrupo)
        {
            string consulta;
            bool consultaExitosa;

            // Verifica el estado actual del grupo mediante el método esVisible
            if (EsVisible(idGrupo))
            {
                // Si el grupo está visible, se cambia su estado a no visible (es_visible = 0)
                consulta = "UPDATE grupo SET es_visible = 0 WHERE id_grupo_PK = " + idGrupo;
            }
            else
            {
                // Si el grupo no está visible, se cambia su estado a visible (es_visible = 1)
                consulta = "UPDATE grupo SET es_visible = 1 WHERE id_grupo_PK = " + idGrupo;
            }

            // Abre la conexión con la base de datos
            ConexionMetics.Open();

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);

            // Ejecuta la consulta y comprueba si al menos una fila fue afectada
            consultaExitosa = comandoConsulta.ExecuteNonQuery() >= 1;

            // Cierra la conexión con la base de datos
            ConexionMetics.Close();

            // Retorna true si la consulta se ejecutó correctamente, de lo contrario, retorna false
            return consultaExitosa;
        }

        // Método para obtener el nombre del archivo adjunto de un grupo mediante su ID
        public string ObtenerNombreArchivo(GrupoModel grupo)
        {
            // Consulta SQL para obtener el nombre del archivo adjunto de un grupo específico mediante su ID
            string consulta = "SELECT nombre_archivo FROM grupo WHERE id_grupo_PK = " + grupo.idGrupo;

            // Utiliza la sentencia 'using' para asegurar que los recursos sean liberados correctamente
            using (SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics))
            {
                using (DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta))
                {
                    // Obtiene la primera fila de la tabla (el nombre del archivo) y lo convierte a una cadena
                    DataRow archivo = tablaResultado.Rows[0];
                    string nombre = Convert.ToString(archivo[0]);
                    return nombre;
                }
            }
        }

        // Método para obtener el archivo adjunto de un grupo mediante su ID
        public byte[] ObtenerArchivo(int idGrupo)
        {
            // Consulta SQL para obtener el archivo adjunto de un grupo específico mediante su ID
            string consulta = "SELECT adjunto FROM grupo WHERE id_grupo_PK = " + idGrupo;

            // Utiliza la sentencia 'using' para asegurar que los recursos sean liberados correctamente
            using (SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics))
            {
                using (DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta))
                {
                    byte[] stream;
                    try
                    {
                        // Obtiene la primera fila de la tabla (los datos del archivo) y los convierte a un arreglo de bytes
                        DataRow archivo = tablaResultado.Rows[0];
                        stream = (byte[])archivo[0];
                    } 
                    catch
                    {
                        stream = new byte[10];
                    }
                    
                    return stream;
                }
            }
        }

        // Método para editar un grupo en la base de datos
        public bool EditarGrupo(GrupoModel grupo)
        {
            bool exito = false;

            using (var command = new SqlCommand("UpdateGrupo", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idGrupo", grupo.idGrupo);
                command.Parameters.AddWithValue("@idTema", int.Parse(grupo.temaAsociado));
                command.Parameters.AddWithValue("@nombre", grupo.nombre);
                command.Parameters.AddWithValue("@horario", grupo.horario);
                command.Parameters.AddWithValue("@fecha_inicio_grupo", grupo.fechaInicioGrupo);
                command.Parameters.AddWithValue("@fecha_finalizacion_grupo", grupo.fechaFinalizacionGrupo);
                command.Parameters.AddWithValue("@fecha_inicio_inscripcion", grupo.fechaInicioInscripcion);
                command.Parameters.AddWithValue("@fecha_finalizacion_inscripcion", grupo.fechaFinalizacionInscripcion);
                command.Parameters.AddWithValue("@cantidad_horas", grupo.cantidadHoras);
                command.Parameters.AddWithValue("@modalidad", grupo.modalidad);
                command.Parameters.AddWithValue("@cupo", grupo.cupo);
                command.Parameters.AddWithValue("@descripcion", grupo.descripcion);
                command.Parameters.AddWithValue("@lugar", grupo.lugar);
                command.Parameters.AddWithValue("@es_visible", grupo.esVisible);
                command.Parameters.AddWithValue("@nombre_archivo", grupo.nombreArchivo);
                command.Parameters.AddWithValue("@adjunto", grupo.archivoAdjunto);

                try
                {
                    ConexionMetics.Open();
                    exito = command.ExecuteNonQuery() >= 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in EditarGrupo: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return exito;
        }

        // Método para obtener el tema seleccionado del grupo mediante su ID
        public SelectListItem ObtenerTemaSeleccionado(GrupoModel grupo)
        {
            // Consulta SQL para obtener el ID del tema del grupo específico mediante su ID
            string consulta = "SELECT id_tema_FK FROM grupo WHERE id_grupo_PK = " + grupo.idGrupo;

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            // Obtiene una tabla con los resultados de la consulta
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

            // Obtiene el valor del ID del tema desde la primera fila de la tabla
            DataRow idTemaRow = tablaResultado.Rows[0];
            string tema = grupo.temaAsociado;
            string idTema = Convert.ToString(idTemaRow[0]);

            // Crea y retorna un objeto SelectListItem con el texto y valor del tema seleccionado
            SelectListItem temaSeleccionado = new SelectListItem { Text = tema, Value = idTema };
            return temaSeleccionado;
        }

        // Método para editar el archivo adjunto de un grupo en la base de datos
        public bool EditarAdjunto(GrupoModel grupo)
        {
            bool consultaExitosa;

            // Consulta SQL para actualizar el archivo adjunto y su nombre en la tabla 'grupo' mediante su ID
            string consulta =
                "UPDATE grupo SET " +
                "adjunto = Convert(varbinary(max),@adjunto), " +
                "nombre_archivo = @nombre_archivo " +
                "WHERE id_grupo_PK = @idGrupo";

            // Abre la conexión con la base de datos
            ConexionMetics.Open();

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@idGrupo", grupo.idGrupo);

            // Convierte el archivo adjunto a un arreglo de bytes y lo agrega como parámetro en el comando
            long fileLength = grupo.archivoAdjunto.Length;
            byte[] buffer = new byte[fileLength];
            using (var stream = grupo.archivoAdjunto.OpenReadStream())
            {
                stream.Read(buffer, 0, (int)fileLength);
            }

            comandoConsulta.Parameters.AddWithValue("@adjunto", buffer);

            // Obtiene el nombre y la extensión del archivo adjunto y lo agrega como parámetro en el comando
            string fileNombre = Path.GetFileNameWithoutExtension(grupo.archivoAdjunto.FileName);
            string fileExtension = Path.GetExtension(grupo.archivoAdjunto.FileName);
            string fileNombreExtension = fileNombre + fileExtension;
            comandoConsulta.Parameters.AddWithValue("@nombre_archivo", fileNombreExtension);

            // Ejecuta la consulta y comprueba si al menos una fila fue afectada
            consultaExitosa = comandoConsulta.ExecuteNonQuery() >= 1;

            // Cierra la conexión con la base de datos
            ConexionMetics.Close();

            // Retorna true si la consulta se ejecutó correctamente, de lo contrario, retorna false
            return consultaExitosa;
        }

        // Método para obtener una lista de grupos junto con sus temas y los IDs de los temas asociados
        public List<(string grupo, string tema, int temaId)> ObtenerGrupoTemas()
        {
            // Lista para almacenar tuplas con la información de grupo, tema y temaId
            List<(string grupo, string tema, int temaId)> grupoTemas = new List<(string grupo, string tema, int temaId)>();

            // Abre la conexión con la base de datos
            ConexionMetics.Open();

            // Consulta SQL para obtener el nombre del grupo, el nombre del tema y el ID del tema asociado a cada grupo
            string consulta = "SELECT g.nombre AS grupo, t.nombre AS tema, t.id_tema_PK AS temaId " +
                              "FROM grupo g JOIN tema t ON g.id_tema_FK = t.id_tema_PK";

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);

            // Ejecuta la consulta y obtiene un lector para leer los resultados
            SqlDataReader reader = comandoConsulta.ExecuteReader();

            // Lee los resultados del lector y agrega las tuplas a la lista
            while (reader.Read())
            {
                string grupo = reader.GetString(reader.GetOrdinal("grupo"));
                string tema = reader.GetString(reader.GetOrdinal("tema"));
                int temaId = reader.GetInt32(reader.GetOrdinal("temaId"));

                grupoTemas.Add((grupo, tema, temaId));
            }

            // Cierra el lector y la conexión con la base de datos
            reader.Close();
            ConexionMetics.Close();

            // Retorna la lista de tuplas con la información de grupo, tema y temaId
            return grupoTemas;
        }

        // Método para verificar si un tema puede ser eliminado
        public bool CanEliminarTema(int idTema)
        {
            // Obtiene la lista de grupos junto con sus temas y los IDs de los temas asociados
            List<(string grupo, string tema, int temaId)> grupoTemas = ObtenerGrupoTemas();

            // Verifica si existe algún grupo que tenga asociado el ID del tema a eliminar
            bool temaExists = grupoTemas.Any(x => x.temaId == idTema);

            // Retorna true si el tema no está asociado a ningún grupo, de lo contrario, retorna false
            return !temaExists;
        }

        // Método para obtener una lista de tuplas con información de los temas, incluyendo ID del tema, ID de la categoría y ID de la actividad
        public List<(string nombre, int idTema, int idCategoria, int idActividad)> TemasParaValidaciones()
        {
            List<(string nombre, int idTema, int idCategoria, int idActividad)> temas = new List<(string nombre, int idTema, int idCategoria, int idActividad)>();

            // Abre la conexión con la base de datos
            ConexionMetics.Open();

            // Consulta SQL para obtener el nombre del tema, el ID del tema, el ID de la categoría y el ID de la actividad para cada tema
            string consulta = "SELECT nombre AS tema, id_tema_PK AS temaId, id_categoria_FK AS catId, id_tipos_actividad_FK AS actividadId " +
                              "FROM tema";

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);

            // Ejecuta la consulta y obtiene un lector para leer los resultados
            SqlDataReader reader = comandoConsulta.ExecuteReader();

            // Lee los resultados del lector y agrega las tuplas a la lista
            while (reader.Read())
            {
                string tema = reader.GetString(reader.GetOrdinal("tema"));
                int temaId = reader.GetInt32(reader.GetOrdinal("temaId"));
                int catId = reader.GetInt32(reader.GetOrdinal("catId"));
                int actividadId = reader.GetInt32(reader.GetOrdinal("actividadId"));

                temas.Add((tema, temaId, catId, actividadId));
            }

            // Cierra el lector y la conexión con la base de datos
            reader.Close();
            ConexionMetics.Close();

            // Retorna la lista de tuplas con información de los temas
            return temas;
        }

        // Método para obtener una lista de temas que coincidan con los IDs de temas asociados a grupos
        public List<(string nombre, int idTema, int idCategoria, int idActividad)> ObtenerTemasConMatchingIds(List<(string nombre, int idTema, int idCategoria, int idActividad)> temas,
            List<(string grupo, string tema, int temaId)> grupoTemas)
        {
            // Obtiene una lista de IDs de temas asociados a grupos
            List<int> matchingIds = grupoTemas.Select(x => x.temaId).ToList();

            // Filtra la lista de temas para obtener solo aquellos que tienen IDs coincidentes con los IDs de temas asociados a grupos
            List<(string nombre, int idTema, int idCategoria, int idActividad)> result = temas.Where(x => matchingIds.Contains(x.idTema)).ToList();

            // Retorna la lista de temas que coinciden con los IDs de temas asociados a grupos
            return result;
        }

        // Método para verificar si una actividad puede ser eliminada
        public bool CanEliminarActividad(int idActividadActual)
        {
            // Obtiene la lista de temas con sus IDs de categoría y actividad que coinciden con los IDs de temas asociados a grupos
            List<(string nombreTema, int temaId, int tipoActividadId, int categoriaId)> result = ObtenerTemasConMatchingIds(TemasParaValidaciones(), ObtenerGrupoTemas());

            // Verifica si existe alguna actividad que tenga asociado el ID de la actividad a eliminar
            bool actividadExists = result.Any(x => x.tipoActividadId == idActividadActual);

            // Retorna true si la actividad no está asociada a ningún tema, de lo contrario, retorna false
            return !actividadExists;
        }

        // Método para verificar si una categoría puede ser eliminada
        public bool CanEliminarCategoria(int idCategoria)
        {
            // Obtiene la lista de temas con sus IDs de categoría y actividad que coinciden con los IDs de temas asociados a grupos
            List<(string nombreTema, int temaId, int tipoActividadId, int categoriaId)> result = ObtenerTemasConMatchingIds(TemasParaValidaciones(), ObtenerGrupoTemas());

            // Verifica si existe alguna categoría que tenga asociado el ID de la categoría a eliminar
            bool categoriaExists = result.Any(x => x.categoriaId == idCategoria);

            // Retorna true si la categoría no está asociada a ningún tema, de lo contrario, retorna false
            return !categoriaExists;
        }

        // Método para obtener una lista de modelos de InscripcionModel con información de los participantes en grupos
        public List<InscripcionModel> ParticipantesEnGrupos()
        {
            // Consulta SQL para obtener los registros de inscripción de participantes en grupos
            string consulta = "SELECT id_inscripcion_PK, estado, id_grupo_FK, id_participante_FK FROM inscripcion";

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            // Crea una tabla con los resultados de la consulta
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

            // Crea una lista para almacenar la información de los participantes en grupos
            List<InscripcionModel> infoParticipantesGrupos = new List<InscripcionModel>();

            // Recorre cada fila de la tabla de resultados y agrega la información de cada participante en grupos a la lista
            foreach (DataRow filaGrupoParticipantes in tablaResultado.Rows)
            {
                infoParticipantesGrupos.Add(ObtenerInfoGrupoParticipante(filaGrupoParticipantes));
            }

            // Si no hay información de participantes en grupos, retorna null, de lo contrario, retorna la lista de información
            if (infoParticipantesGrupos.Count == 0)
            {
                return null;
            }
            return infoParticipantesGrupos;
        }

        // Método para obtener un modelo de InscripcionModel con la información de un participante en un grupo
        public InscripcionModel ObtenerInfoGrupoParticipante(DataRow filaGrupoParticipantes)
        {
            // Crea un nuevo modelo de InscripcionModel y asigna los valores de la fila correspondiente
            InscripcionModel info = new InscripcionModel
            {
                idInscripcion = Convert.ToInt32(filaGrupoParticipantes["id_inscripcion_PK"]),
                idParticipante = Convert.ToString(filaGrupoParticipantes["id_participante_FK"]),
                idGrupo = Convert.ToInt32(filaGrupoParticipantes["id_grupo_FK"]),
                estado = Convert.ToString(filaGrupoParticipantes["estado"])
            };
            return info;
        }
    }
}
