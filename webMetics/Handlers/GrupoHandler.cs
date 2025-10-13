using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using webMetics.Helpers;


/*
 * Handler para los grupos
 * En esta clase se encuentran los metodos backend para realizar las consultas a la base de datos
 */

namespace webMetics.Handlers
{
    public class GrupoHandler : BaseDeDatosHandler
    {
        private TemaHandler accesoATema;
        private CategoriaHandler accesoACategoria;
        private AsesorHandler accesoAAsesor;
        private GrupoTemaHandler accesoAGrupoTema;

        public GrupoHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
        {
            accesoATema = new TemaHandler(environment, configuration);
            accesoACategoria = new CategoriaHandler(environment, configuration);
            accesoAAsesor = new AsesorHandler(environment, configuration);
            accesoAGrupoTema = new GrupoTemaHandler(environment, configuration, accesoATema);
        }

        public bool EditarIdGruposAsesor(string idUsuario, string oldIdUsuario)
        {
            bool exito = false;

            string query = "UPDATE grupo SET id_asesor_FK = @idUsuario WHERE id_asesor_FK = @idAnterior;";

            ConexionMetics.Open();

            SqlCommand command = new SqlCommand(query, ConexionMetics);

            command.Parameters.AddWithValue("@idUsuario", idUsuario);
            command.Parameters.AddWithValue("@idAnterior", oldIdUsuario);

            exito = command.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }

        // Crea un grupo en la base de datos.
        public bool CrearGrupo(GrupoModel grupo, int[] idTemas)
        {
            bool exito = false;

            using (var command = new SqlCommand("InsertGrupo", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;

                string idAsesor = grupo.modalidad == "Autogestionado" ? null : grupo.idAsesor;
                command.Parameters.AddWithValue("@idCategoria", grupo.idCategoria);
                command.Parameters.AddWithValue("@idAsesor", idAsesor);
                command.Parameters.AddWithValue("@nombre", grupo.nombre);
                command.Parameters.AddWithValue("@horario", grupo.horario);
                command.Parameters.AddWithValue("@modalidad", grupo.modalidad);
                command.Parameters.AddWithValue("@fecha_inicio_grupo", grupo.fechaInicioGrupo);
                command.Parameters.AddWithValue("@fecha_finalizacion_grupo", grupo.fechaFinalizacionGrupo);
                command.Parameters.AddWithValue("@fecha_inicio_inscripcion", grupo.fechaInicioInscripcion);
                command.Parameters.AddWithValue("@fecha_finalizacion_inscripcion", grupo.fechaFinalizacionInscripcion);
                command.Parameters.AddWithValue("@cantidad_horas", grupo.cantidadHoras);
                command.Parameters.AddWithValue("@cupo", grupo.cupo);
                command.Parameters.AddWithValue("@numeroGrupo", grupo.numeroGrupo);
                command.Parameters.AddWithValue("@descripcion", grupo.descripcion);
                command.Parameters.AddWithValue("@lugar", grupo.lugar);
                command.Parameters.AddWithValue("@enlace", grupo.enlace);
                command.Parameters.AddWithValue("@clave_inscripcion", grupo.claveInscripcion);


                if (grupo.archivoAdjunto != null)
                {
                    byte[] adjunto = ObtenerAdjunto(grupo);
                    string nombreArchivoAdjunto = ObtenerNombreAdjunto(grupo);
                    command.Parameters.AddWithValue("@adjunto", adjunto);
                    command.Parameters.AddWithValue("@nombre_archivo", nombreArchivoAdjunto);
                }
                else
                {
                    command.Parameters.AddWithValue("@adjunto", grupo.archivoAdjunto);
                    command.Parameters.AddWithValue("@nombre_archivo", grupo.nombreArchivo);
                }

                try
                {
                    ConexionMetics.Open();
                    // Ejecutar el comando y obtener el ID del grupo recién creado
                    int idGrupo = Convert.ToInt32(command.ExecuteScalar());

                    // Insertar los temas asociados al grupo
                    foreach (var idTema in idTemas)
                    {
                        using (var commandTema = new SqlCommand("InsertGrupoTema", ConexionMetics))
                        {
                            commandTema.CommandType = CommandType.StoredProcedure;
                            commandTema.Parameters.AddWithValue("@idGrupo", idGrupo);
                            commandTema.Parameters.AddWithValue("@idTema", idTema);
                            commandTema.ExecuteNonQuery();
                        }
                    }

                    exito = true;
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

        /// <summary>
        /// Se editan los datos de un grupo en la base de datos.
        /// </summary>
        /// <param name="grupo">Modelo del grupo con los datos actualizados.</param>
        /// <returns> True si la operación fue exitosa, de lo contrario false.</returns>
        public bool EditarGrupo(GrupoModel grupo)
        {
            bool exito = false;

            using (var command = new SqlCommand("UpdateGrupo", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;

                string idAsesor = grupo.modalidad == "Autogestionado" ? null : grupo.idAsesor;

                command.Parameters.AddWithValue("@idGrupo", grupo.idGrupo);
                command.Parameters.AddWithValue("@idCategoria", grupo.idCategoria);
                command.Parameters.AddWithValue("@idAsesor", idAsesor);
                command.Parameters.AddWithValue("@nombre", grupo.nombre);
                command.Parameters.AddWithValue("@horario", grupo.horario);
                command.Parameters.AddWithValue("@modalidad", grupo.modalidad);
                command.Parameters.AddWithValue("@fecha_inicio_grupo", grupo.fechaInicioGrupo);
                command.Parameters.AddWithValue("@fecha_finalizacion_grupo", grupo.fechaFinalizacionGrupo);
                command.Parameters.AddWithValue("@fecha_inicio_inscripcion", grupo.fechaInicioInscripcion);
                command.Parameters.AddWithValue("@fecha_finalizacion_inscripcion", grupo.fechaFinalizacionInscripcion);
                command.Parameters.AddWithValue("@cantidad_horas", grupo.cantidadHoras);
                command.Parameters.AddWithValue("@cupo", grupo.cupo);
                command.Parameters.AddWithValue("@numeroGrupo", grupo.numeroGrupo);
                command.Parameters.AddWithValue("@descripcion", grupo.descripcion);
                command.Parameters.AddWithValue("@lugar", grupo.lugar);
                command.Parameters.AddWithValue("@es_visible", grupo.esVisible);
                command.Parameters.AddWithValue("@enlace", grupo.enlace);
                command.Parameters.AddWithValue("@clave_inscripcion", grupo.claveInscripcion);

                if (grupo.archivoAdjunto != null)
                {
                    byte[] adjunto = ObtenerAdjunto(grupo);
                    string nombreArchivoAdjunto = ObtenerNombreAdjunto(grupo);
                    command.Parameters.AddWithValue("@adjunto", adjunto);
                    command.Parameters.AddWithValue("@nombre_archivo", nombreArchivoAdjunto);
                }
                else
                {
                    command.Parameters.AddWithValue("@adjunto", grupo.archivoAdjunto);
                    command.Parameters.AddWithValue("@nombre_archivo", grupo.nombreArchivo);
                }

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

        // Convierte el archivo adjunto en un arreglo de bytes para almacenarlo en la base de datos.
        public byte[] ObtenerAdjunto(GrupoModel grupo)
        {
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

            return buffer;
        }

        private string ObtenerNombreAdjunto(GrupoModel grupo)
        {
            string fileNombre = Path.GetFileNameWithoutExtension(grupo.archivoAdjunto.FileName) == null ? "empty" : Path.GetFileNameWithoutExtension(grupo.archivoAdjunto.FileName);
            string fileExtension = Path.GetExtension(grupo.archivoAdjunto.FileName) == null ? ".pdf" : Path.GetExtension(grupo.archivoAdjunto.FileName);
            string fileNombreExtension = fileNombre + fileExtension;

            return fileNombreExtension;
        }

        // Obtiene la información detallada de todos los grupos en la base de datos.
        public List<GrupoModel> ObtenerListaGrupos()
        {
            List<GrupoModel> grupos = new List<GrupoModel>();

            SqlCommand command = new SqlCommand("SelectAllGrupos", ConexionMetics);

            DataTable result = CrearTablaConsulta(command);

            List<int> idsGrupos = result.AsEnumerable().Select(row => Convert.ToInt32(row["id_grupo_PK"])).ToList();

            // Recorre cada fila de la tabla y agrega una instancia de GrupoModel a la lista.
            foreach (DataRow row in result.Rows)
            {
                int idGrupo = Convert.ToInt32(row["id_grupo_PK"]);

                GrupoModel grupo = new GrupoModel
                {
                    idGrupo = Convert.ToInt32(row["id_grupo_PK"]),
                    idCategoria = Convert.ToInt32(row["id_categoria_FK"]),
                    nombreCategoria = Convert.ToString(row["nombre_categoria"]),
                    idAsesor = Convert.ToString(row["id_asesor_FK"]),
                    nombreAsesor = Convert.ToString(row["nombre_asesor"]),
                    modalidad = Convert.ToString(row["modalidad"]),
                    cupo = Convert.ToInt32(row["cupo"]),
                    numeroGrupo = Convert.ToInt32(row["numero_grupo"]),
                    descripcion = Convert.ToString(row["descripcion"]),
                    esVisible = Convert.ToBoolean(row["es_visible"]),
                    lugar = Convert.ToString(row["lugar"]),
                    nombre = Convert.ToString(row["nombre"]),
                    horario = Convert.ToString(row["horario"]),
                    fechaInicioGrupo = Convert.ToDateTime(row["fecha_inicio_grupo"]),
                    fechaFinalizacionGrupo = Convert.ToDateTime(row["fecha_finalizacion_grupo"]),
                    fechaInicioInscripcion = Convert.ToDateTime(row["fecha_inicio_inscripcion"]),
                    fechaFinalizacionInscripcion = Convert.ToDateTime(row["fecha_finalizacion_inscripcion"]),
                    cantidadHoras = Convert.ToInt32(row["cantidad_horas"]),
                    cupoActual = ObtenerCupoActual(Convert.ToInt32(row["id_grupo_PK"])),
                    nombreArchivo = Convert.ToString(row["nombre_archivo"]),
                    TemasSeleccionadosNombres = accesoAGrupoTema.ObtenerNombresTemasDelGrupo(idGrupo),
                    enlace = Convert.ToString(row["enlace"]),
                    claveInscripcion = Convert.ToString(row["clave_inscripcion"]),



                };

                grupos.Add(grupo);
            }

            // Si no se encontraron grupos, devuelve null; de lo contrario, devuelve la lista.
            if (grupos.Count == 0)
            {
                return null;
            }

            return grupos;
        }

        // Obtiene la información detallada de un solo un grupo en la base de datos según su id.
        public GrupoModel ObtenerGrupo(int idGrupo)
        {
            GrupoModel grupo = null;

            using (SqlCommand command = new SqlCommand("SelectGrupo", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idGrupo", idGrupo);

                try
                {
                    ConexionMetics.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            grupo = new GrupoModel
                            {
                                idGrupo = reader.GetInt32(reader.GetOrdinal("id_grupo_PK")),
                                idCategoria = reader.GetInt32(reader.GetOrdinal("id_categoria_FK")),
                                nombreCategoria = reader.GetString(reader.GetOrdinal("nombre_categoria")),
                                idAsesor = reader.IsDBNull(reader.GetOrdinal("id_asesor_FK")) ? null : reader.GetString(reader.GetOrdinal("id_asesor_FK")),
                                nombreAsesor = reader.IsDBNull(reader.GetOrdinal("nombre_asesor")) ? null : reader.GetString(reader.GetOrdinal("nombre_asesor")),
                                nombre = reader.IsDBNull(reader.GetOrdinal("nombre")) ? "Sin nombre" : reader.GetString(reader.GetOrdinal("nombre")),
                                numeroGrupo = reader.IsDBNull(reader.GetOrdinal("numero_grupo")) ? 0 : reader.GetInt32(reader.GetOrdinal("numero_grupo")),
                                descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? "Sin descripción" : reader.GetString(reader.GetOrdinal("descripcion")),
                                modalidad = reader.IsDBNull(reader.GetOrdinal("modalidad")) ? "Sin modalidad" : reader.GetString(reader.GetOrdinal("modalidad")),
                                cupo = reader.IsDBNull(reader.GetOrdinal("cupo")) ? 0 : reader.GetInt32(reader.GetOrdinal("cupo")),
                                esVisible = reader.IsDBNull(reader.GetOrdinal("es_visible")) ? false : reader.GetBoolean(reader.GetOrdinal("es_visible")),
                                lugar = reader.IsDBNull(reader.GetOrdinal("lugar")) ? "Sin lugar" : reader.GetString(reader.GetOrdinal("lugar")),
                                horario = reader.IsDBNull(reader.GetOrdinal("horario")) ? "Sin horario definido" : reader.GetString(reader.GetOrdinal("horario")),
                                fechaInicioGrupo = reader.IsDBNull(reader.GetOrdinal("fecha_inicio_grupo")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("fecha_inicio_grupo")),
                                fechaFinalizacionGrupo = reader.IsDBNull(reader.GetOrdinal("fecha_finalizacion_grupo")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("fecha_finalizacion_grupo")),
                                fechaInicioInscripcion = reader.IsDBNull(reader.GetOrdinal("fecha_inicio_inscripcion")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("fecha_inicio_inscripcion")),
                                fechaFinalizacionInscripcion = reader.IsDBNull(reader.GetOrdinal("fecha_finalizacion_inscripcion")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("fecha_finalizacion_inscripcion")),
                                cantidadHoras = reader.IsDBNull(reader.GetOrdinal("cantidad_horas")) ? (byte)0 : reader.GetByte(reader.GetOrdinal("cantidad_horas")),
                                nombreArchivo = reader.IsDBNull(reader.GetOrdinal("nombre_archivo")) ? "Sin archivo adjunto" : reader.GetString(reader.GetOrdinal("nombre_archivo")),
                                TemasSeleccionadosNombres = accesoAGrupoTema.ObtenerNombresTemasDelGrupo(idGrupo),
                                enlace = reader.IsDBNull(reader.GetOrdinal("enlace")) ? "Sin enlace del curso" : reader.GetString(reader.GetOrdinal("enlace")),
                                claveInscripcion = reader.IsDBNull(reader.GetOrdinal("clave_inscripcion")) ? "Sin clave de inscripción" : reader.GetString(reader.GetOrdinal("clave_inscripcion")),


                            };
                        }
                    }

                    ConexionMetics.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al obtener el grupo: {ex.Message}");
                }
            }

            return grupo;
        }

        public GrupoModel ObtenerGrupoPorNombre(string nombreGrupo, int numeroGrupo)
        {
            List<GrupoModel> grupos = ObtenerListaGrupos();

            GrupoModel grupo = grupos.FirstOrDefault(g => g.nombre == nombreGrupo && g.numeroGrupo == numeroGrupo);

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
                listaGrupos.Add(ObtenerGrupo(idGrupo));
            }

            if (listaGrupos.Count == 0)
            {
                return null;
            }

            return listaGrupos;
        }

        public List<GrupoModel> ObtenerListaGruposAsesor(string idAsesor)
        {
            List<GrupoModel> grupos = new List<GrupoModel>();

            SqlCommand command = new SqlCommand("SelectGruposAsesor", ConexionMetics);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@idAsesor", idAsesor);

            DataTable result = CrearTablaConsulta(command);

            List<int> idsGrupos = result.AsEnumerable().Select(row => Convert.ToInt32(row["id_grupo_PK"])).ToList();

            foreach (DataRow row in result.Rows)
            {
                int idGrupo = Convert.ToInt32(row["id_grupo_PK"]);
                GrupoModel grupo = new GrupoModel
                {
                    idGrupo = Convert.ToInt32(row["id_grupo_PK"]),
                    idCategoria = Convert.ToInt32(row["id_categoria_FK"]),
                    nombreCategoria = Convert.ToString(row["nombre_categoria"]),
                    idAsesor = Convert.ToString(row["id_asesor_FK"]),
                    nombreAsesor = Convert.ToString(row["nombre_asesor"]),
                    modalidad = Convert.ToString(row["modalidad"]),
                    cupo = Convert.ToInt32(row["cupo"]),
                    numeroGrupo = Convert.ToInt32(row["numero_grupo"]),
                    descripcion = Convert.ToString(row["descripcion"]),
                    esVisible = Convert.ToBoolean(row["es_visible"]),
                    lugar = Convert.ToString(row["lugar"]),
                    nombre = Convert.ToString(row["nombre"]),
                    horario = Convert.ToString(row["horario"]),
                    fechaInicioGrupo = Convert.ToDateTime(row["fecha_inicio_grupo"]),
                    fechaFinalizacionGrupo = Convert.ToDateTime(row["fecha_finalizacion_grupo"]),
                    fechaInicioInscripcion = Convert.ToDateTime(row["fecha_inicio_inscripcion"]),
                    fechaFinalizacionInscripcion = Convert.ToDateTime(row["fecha_finalizacion_inscripcion"]),
                    cantidadHoras = Convert.ToInt32(row["cantidad_horas"]),
                    cupoActual = ObtenerCupoActual(Convert.ToInt32(row["id_grupo_PK"])),
                    nombreArchivo = Convert.ToString(row["nombre_archivo"]),
                    TemasSeleccionadosNombres = accesoAGrupoTema.ObtenerNombresTemasDelGrupo(idGrupo),
                    enlace = Convert.ToString(row["enlace"]),
                    claveInscripcion = Convert.ToString(row["clave_inscripcion"]),

                };

                grupos.Add(grupo);
            }

            if (grupos.Count == 0)
            {
                return null;
            }

            return grupos;
        }

        // Método para eliminar un grupo de la base de datos mediante su ID
        public bool EliminarGrupo(int grupoId)
        {
            bool consultaExitosa;

            string consulta = "DELETE FROM grupo WHERE id_grupo_PK = @grupoId";

            ConexionMetics.Open();

            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@grupoId", grupoId);
            consultaExitosa = comandoConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return consultaExitosa;
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



        // Método para obtener el estado visible de un grupo mediante su ID
        public bool EsVisible(int grupoId)
        {
            string consulta = "SELECT es_visible FROM grupo WHERE id_grupo_PK = " + grupoId;

            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

            DataRow esVisible = tablaResultado.Rows[0];
            int estadoActual = Convert.ToInt32(esVisible[0]);

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
    }
}
