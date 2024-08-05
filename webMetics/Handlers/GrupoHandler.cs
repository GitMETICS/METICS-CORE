﻿using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;
using System.Collections.Generic;
using System;
using NPOI.SS.Formula.Functions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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

        public GrupoHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
        {
            accesoATema = new TemaHandler(environment, configuration);
            accesoACategoria = new CategoriaHandler(environment, configuration);
        }

        public string ObtenerIdTema(string nombreTema)
        {
            string consulta = "SELECT id_tema_PK FROM tema WHERE nombre = @nombre";

            // crear el comando de consulta con la consulta sql y la conexión establecida
            SqlCommand comandoparaconsulta = new SqlCommand(consulta, ConexionMetics);
            comandoparaconsulta.Parameters.AddWithValue("@nombre", nombreTema);

            DataTable tablaResultado = CrearTablaConsulta(comandoparaconsulta);

            return tablaResultado.Rows[0][0].ToString();
        }



        // Crea un grupo en la base de datos.
        public bool CrearGrupo(GrupoModel grupo)
        {
            bool exito = false;

            using (var command = new SqlCommand("InsertGrupo", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;

                string idAsesor = grupo.modalidad == "Autogestionado" ? "" : ObtenerIdAsesor(grupo.nombreAsesorAsociado);

                command.Parameters.AddWithValue("@idTema", grupo.idTema);
                command.Parameters.AddWithValue("@idCategoria", grupo.idCategoria);
                command.Parameters.AddWithValue("@nombre", grupo.nombre);
                command.Parameters.AddWithValue("@horario", grupo.horario);
                command.Parameters.AddWithValue("@modalidad", grupo.modalidad);
                command.Parameters.AddWithValue("@fecha_inicio_grupo", grupo.fechaInicioGrupo);
                command.Parameters.AddWithValue("@fecha_finalizacion_grupo", grupo.fechaFinalizacionGrupo);
                command.Parameters.AddWithValue("@fecha_inicio_inscripcion", grupo.fechaInicioInscripcion);
                command.Parameters.AddWithValue("@fecha_finalizacion_inscripcion", grupo.fechaFinalizacionInscripcion);
                command.Parameters.AddWithValue("@cantidad_horas", grupo.cantidadHoras);
                command.Parameters.AddWithValue("@idAsesor", idAsesor);
                command.Parameters.AddWithValue("@nombreAsesor", grupo.nombreAsesorAsociado);
                command.Parameters.AddWithValue("@cupo", grupo.cupo);
                command.Parameters.AddWithValue("@descripcion", grupo.descripcion);
                command.Parameters.AddWithValue("@lugar", grupo.lugar);
                command.Parameters.AddWithValue("@nombre_archivo", grupo.nombreArchivo);

                // Convierte el archivo adjunto en un arreglo de bytes para almacenarlo en la base de datos.
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

        // Obtiene la información detallada de todos los grupos en la base de datos.
        public List<GrupoModel> ObtenerListaGrupos()
        {
            List<GrupoModel> grupos = new List<GrupoModel>();

            SqlCommand command = new SqlCommand("SelectAllGrupos", ConexionMetics);

            DataTable result = CrearTablaConsulta(command);

            // Recorre cada fila de la tabla y agrega una instancia de GrupoModel a la lista.
            foreach (DataRow row in result.Rows)
            {
                int idTema = Convert.ToInt32(row["id_tema_FK"]);
                string nombreTema = accesoATema.ObtenerTema(idTema).nombre;

                int idCategoria = Convert.ToInt32(row["id_categoria_FK"]);
                string nombreCategoria = accesoACategoria.ObtenerCategoria(idCategoria).nombre;

                GrupoModel grupo = new GrupoModel
                {
                    idGrupo = Convert.ToInt32(row["id_grupo_PK"]),
                    idTema = idTema,
                    nombreTema = nombreTema,
                    idCategoria = idCategoria,
                    nombreCategoria = nombreCategoria,
                    idAsesor = Convert.ToString(row["id_asesor_FK"]),
                    modalidad = Convert.ToString(row["modalidad"]),
                    cupo = Convert.ToInt32(row["cupo"]),
                    descripcion = Convert.ToString(row["descripcion"]),
                    esVisible = Convert.ToBoolean(row["es_visible"]),
                    lugar = Convert.ToString(row["lugar"]),
                    nombreAsesorAsociado = Convert.ToString(row["nombreAsesor"]),
                    nombre = Convert.ToString(row["nombre"]),
                    horario = Convert.ToString(row["horario"]),
                    fechaInicioGrupo = Convert.ToDateTime(row["fecha_inicio_grupo"]),
                    fechaFinalizacionGrupo = Convert.ToDateTime(row["fecha_finalizacion_grupo"]),
                    fechaInicioInscripcion = Convert.ToDateTime(row["fecha_inicio_inscripcion"]),
                    fechaFinalizacionInscripcion = Convert.ToDateTime(row["fecha_finalizacion_inscripcion"]),
                    cantidadHoras = Convert.ToInt32(row["cantidad_horas"]),
                    cupoActual = ObtenerCupoActual(Convert.ToInt32(row["id_grupo_PK"])),
                    nombreArchivo = Convert.ToString(row["nombre_archivo"])
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
                            int idTema = reader.GetInt32(reader.GetOrdinal("id_tema_FK"));
                            string nombreTema = accesoATema.ObtenerTema(idTema).nombre;

                            int idCategoria = reader.GetInt32(reader.GetOrdinal("id_categoria_FK"));
                            string nombreCategoria = accesoACategoria.ObtenerCategoria(idCategoria).nombre;

                            grupo = new GrupoModel
                            {
                                idGrupo = reader.GetInt32(reader.GetOrdinal("id_grupo_PK")),
                                idTema = idTema,
                                nombreTema = nombreTema,
                                idCategoria = idCategoria,
                                nombreCategoria = nombreCategoria,
                                idAsesor = reader.IsDBNull(reader.GetOrdinal("id_asesor_FK")) ? "Sin archivo" : reader.GetString(reader.GetOrdinal("id_asesor_FK")),
                                nombre = reader.IsDBNull(reader.GetOrdinal("nombre")) ? "Sin nombre" : reader.GetString(reader.GetOrdinal("nombre")),
                                descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? "Sin descripción" : reader.GetString(reader.GetOrdinal("descripcion")),
                                modalidad = reader.IsDBNull(reader.GetOrdinal("modalidad")) ? "Sin modalidad" : reader.GetString(reader.GetOrdinal("modalidad")),
                                cupo = reader.IsDBNull(reader.GetOrdinal("cupo")) ? 0 : reader.GetInt32(reader.GetOrdinal("cupo")),
                                esVisible = reader.IsDBNull(reader.GetOrdinal("es_visible")) ? false : reader.GetBoolean(reader.GetOrdinal("es_visible")),
                                lugar = reader.IsDBNull(reader.GetOrdinal("lugar")) ? "Sin lugar" : reader.GetString(reader.GetOrdinal("lugar")),
                                nombreAsesorAsociado = reader.IsDBNull(reader.GetOrdinal("nombreAsesor")) ? "Sin asesor" : reader.GetString(reader.GetOrdinal("nombreAsesor")),
                                horario = reader.IsDBNull(reader.GetOrdinal("horario")) ? "Sin horario" : reader.GetString(reader.GetOrdinal("horario")),
                                fechaInicioGrupo = reader.IsDBNull(reader.GetOrdinal("fecha_inicio_grupo")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("fecha_inicio_grupo")),
                                fechaFinalizacionGrupo = reader.IsDBNull(reader.GetOrdinal("fecha_finalizacion_grupo")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("fecha_finalizacion_grupo")),
                                fechaInicioInscripcion = reader.IsDBNull(reader.GetOrdinal("fecha_inicio_inscripcion")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("fecha_inicio_inscripcion")),
                                fechaFinalizacionInscripcion = reader.IsDBNull(reader.GetOrdinal("fecha_finalizacion_inscripcion")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("fecha_finalizacion_inscripcion")),
                                cantidadHoras = reader.IsDBNull(reader.GetOrdinal("cantidad_horas")) ? (byte)0 : reader.GetByte(reader.GetOrdinal("cantidad_horas")),
                                nombreArchivo = reader.IsDBNull(reader.GetOrdinal("nombre_archivo")) ? "Sin archivo" : reader.GetString(reader.GetOrdinal("nombre_archivo"))
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


        public string ObtenerIdAsesor(string nombreAsesor)
        {
            string consulta = "SELECT id_asesor_PK FROM asesor WHERE " +
                "nombre = @nombre AND apellido_1 = @apellido_1 AND apellido_2 = @apellido_2 ";

            // crear el comando de consulta con la consulta sql y la conexión establecida
            SqlCommand comandoparaconsulta = new SqlCommand(consulta, ConexionMetics);

            string[] temp = nombreAsesor.Split(' ');

            if (temp.Length == 4)
            {
                comandoparaconsulta.Parameters.AddWithValue("@nombre", temp[0] + " " + temp[1]);
            }
            else {
                comandoparaconsulta.Parameters.AddWithValue("@nombre", temp[0]);
            }
            
            comandoparaconsulta.Parameters.AddWithValue("@apellido_1", temp[^2]);
            comandoparaconsulta.Parameters.AddWithValue("@apellido_2", temp[^1]);

            DataTable tablaResultado = CrearTablaConsulta(comandoparaconsulta);


            return tablaResultado.Rows[0][0].ToString();
        }

        // Método para editar un grupo en la base de datos
        public bool EditarGrupo(GrupoModel grupo)
        {
            bool exito = false;

            using (var command = new SqlCommand("UpdateGrupo", ConexionMetics))
            {
                string idAsesor = ObtenerIdAsesor(grupo.nombreAsesorAsociado);

                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idGrupo", grupo.idGrupo);
                command.Parameters.AddWithValue("@idTema", grupo.idTema);
                command.Parameters.AddWithValue("@idCategoria", grupo.idCategoria);
                command.Parameters.AddWithValue("@idAsesor", idAsesor);
                command.Parameters.AddWithValue("@nombreAsesor", grupo.nombreAsesorAsociado);
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

                if (grupo.archivoAdjunto != null)
                {
                    exito = EditarAdjunto(grupo);
                }
                else
                {

                    command.Parameters.AddWithValue("@nombre_archivo", "");
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

        // Método para obtener el tema seleccionado del grupo mediante su ID
        public SelectListItem ObtenerTemaSeleccionado(GrupoModel grupo)
        {
            // Consulta SQL para obtener el ID del tema del grupo específico mediante su ID
            string consulta = "SELECT nombre FROM tema WHERE id_tema_PK = " + Convert.ToString(grupo.idTema);

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            // Obtiene una tabla con los resultados de la consulta
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

            // Obtiene el valor del ID del tema desde la primera fila de la tabla
            DataRow TemaRow = tablaResultado.Rows[0];
            string tema = Convert.ToString(TemaRow[0]);

            // Crea y retorna un objeto SelectListItem con el texto y valor del tema seleccionado
            SelectListItem temaSeleccionado = new SelectListItem { Text = tema, Value = Convert.ToString(grupo.idTema)};
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
