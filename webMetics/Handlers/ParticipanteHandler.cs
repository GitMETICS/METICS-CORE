using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace webMetics.Handlers
{
    public class ParticipanteHandler : BaseDeDatosHandler
    {
        private protected InscripcionHandler accesoAInscripcion;

        public ParticipanteHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
        {
            accesoAInscripcion = new InscripcionHandler(environment, configuration);
        }

        // Verificar la existencia de un participante en la base de datos
        public bool ExisteParticipante(string id)
        {
            bool existe = false;

            using (var command = new SqlCommand("ExistsParticipante", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@id", id);
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
                    Console.WriteLine($"Error in ExisteParticipante: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return existe;
        }

        // Insertar un nuevo participante en la base de datos
        public bool CrearParticipante(ParticipanteModel participante)
        {
            bool exito = false;
            using (var command = new SqlCommand("InsertParticipante", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idUsuario", participante.idParticipante);
                command.Parameters.AddWithValue("@idParticipante", participante.idParticipante);
                command.Parameters.AddWithValue("@nombre", participante.nombre);
                command.Parameters.AddWithValue("@apellido1", participante.primerApellido);
                command.Parameters.AddWithValue("@apellido2", participante.segundoApellido);
                command.Parameters.AddWithValue("@tipoIdentificacion", participante.tipoIdentificacion);
                command.Parameters.AddWithValue("@numeroIdentificacion", participante.numeroIdentificacion);
                command.Parameters.AddWithValue("@correo", participante.correo);
                command.Parameters.AddWithValue("@tipoParticipante", participante.tipoParticipante);
                command.Parameters.AddWithValue("@condicion", participante.condicion);
                command.Parameters.AddWithValue("@telefono", participante.telefono);
                command.Parameters.AddWithValue("@area", participante.area);
                command.Parameters.AddWithValue("@departamento", participante.departamento);
                command.Parameters.AddWithValue("@unidadAcademica", participante.unidadAcademica);
                command.Parameters.AddWithValue("@sede", participante.sede);
                command.Parameters.AddWithValue("@horasMatriculadas", participante.horasMatriculadas);
                command.Parameters.AddWithValue("@horasAprobadas", participante.horasAprobadas);

                try
                {
                    ConexionMetics.Open();
                    exito = command.ExecuteNonQuery() >= 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CrearParticipante: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return exito;
        }

        // Actualizar la información de un participante en la base de datos
        public bool EditarParticipante(ParticipanteModel participante)
        {
            bool exito = false;
            using (var command = new SqlCommand("UpdateParticipante", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idUsuario", participante.idParticipante);
                command.Parameters.AddWithValue("@idParticipante", participante.idParticipante);
                command.Parameters.AddWithValue("@nombre", participante.nombre);
                command.Parameters.AddWithValue("@apellido1", participante.primerApellido);
                command.Parameters.AddWithValue("@apellido2", participante.segundoApellido);
                command.Parameters.AddWithValue("@tipoIdentificacion", participante.tipoIdentificacion);
                command.Parameters.AddWithValue("@numeroIdentificacion", participante.numeroIdentificacion);
                command.Parameters.AddWithValue("@correo", participante.correo);
                command.Parameters.AddWithValue("@tipoParticipante", participante.tipoParticipante);
                command.Parameters.AddWithValue("@condicion", participante.condicion);
                command.Parameters.AddWithValue("@telefono", participante.telefono);
                command.Parameters.AddWithValue("@area", participante.area);
                command.Parameters.AddWithValue("@departamento", participante.departamento);
                command.Parameters.AddWithValue("@unidadAcademica", participante.unidadAcademica);
                command.Parameters.AddWithValue("@sede", participante.sede);
                command.Parameters.AddWithValue("@horasMatriculadas", participante.horasMatriculadas);
                command.Parameters.AddWithValue("@horasAprobadas", participante.horasAprobadas);

                try
                {
                    ConexionMetics.Open();
                    exito = command.ExecuteNonQuery() >= 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in EditarParticipante: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return exito;
        }

        // Método para obtener un participante específico según su ID
        public ParticipanteModel ObtenerParticipante(string idParticipante)
        {
            ParticipanteModel participante = null;

            using (SqlCommand command = new SqlCommand("SelectParticipante", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@id", idParticipante);

                try
                {
                    ConexionMetics.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            participante = new ParticipanteModel
                            {
                                idParticipante = idParticipante,
                                nombre = reader["nombre"].ToString(),
                                primerApellido = reader["apellido_1"].ToString(),
                                segundoApellido = reader["apellido_2"].ToString(),
                                tipoIdentificacion = reader["tipo_identificacion"].ToString(),
                                numeroIdentificacion = reader["numero_identificacion"].ToString(),
                                correo = reader["correo"].ToString(),
                                tipoParticipante = reader["tipo_participante"].ToString(),
                                condicion = reader["condicion"].ToString(),
                                telefono = reader["telefono"].ToString(),
                                area = reader["area"].ToString(),
                                departamento = reader["departamento"].ToString(),
                                unidadAcademica = reader["unidad_academica"].ToString(),
                                sede = reader["sede"].ToString(),
                                horasAprobadas = reader.GetInt32(reader.GetOrdinal("total_horas_aprobadas")),
                                horasMatriculadas = reader.GetInt32(reader.GetOrdinal("total_horas_matriculadas")),
                                correoNotificacionEnviado = reader.GetInt32(reader.GetOrdinal("correo_notificacion_enviado")),
                                gruposInscritos = new List<GrupoModel>()
                            };
                        }
                    }

                    ConexionMetics.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al obtener el participante: {ex.Message}");
                    participante = null;
                }
            }

            return participante;
        }












        /*
         
         TODO: Pasar estos métodos a stored procedures en la base de datos.
         
         */

        // Se encarga de eliminar un participante de la base de datos.
        public bool EliminarParticipante(string idParticipante)
        {
            bool comprobacionConsultaExitosa;

            string consulta = "DELETE FROM participante WHERE id_participante_PK = @idParticipante";

            ConexionMetics.Open(); // Abre la conexión con la base de datos.

            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            comandoParaConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);

            comprobacionConsultaExitosa = comandoParaConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close(); // Cierra la conexión con la base de datos.

            return comprobacionConsultaExitosa;
        }

        private int CalcularHorasMatriculadasParticipante(string idParticipante)
        {
            int total = 0;
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesParticipante(idParticipante);

            foreach (InscripcionModel inscripcion in inscripciones)
            {
                if (inscripcion.estado == "Inscrito")
                {
                    total += inscripcion.horasMatriculadas;
                }

            }
            return total;
        }

        private int CalcularHorasAprobadasParticipante(string idParticipante)
        {
            int total = 0;
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesParticipante(idParticipante);

            foreach (InscripcionModel inscripcion in inscripciones)
            {
                total += inscripcion.horasAprobadas;
            }
            return total;
        }

        // Método para editar las horas matriculadas de un participante existente en la base de datos
        public bool ActualizarHorasMatriculadasParticipante(string idParticipante)
        {
            bool exito;
            string consulta = "UPDATE participante SET total_horas_matriculadas = @horasMatriculadas " +
                              "WHERE id_participante_PK = @idParticipante";

            ConexionMetics.Open();

            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoParaConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);
            comandoParaConsulta.Parameters.AddWithValue("@horasMatriculadas", CalcularHorasMatriculadasParticipante(idParticipante));

            exito = comandoParaConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }

        // Método para editar las horas aprobadas de un participante existente en la base de datos
        public bool ActualizarHorasAprobadasParticipante(string idParticipante)
        {
            bool exito;
            string consulta = "UPDATE participante SET total_horas_aprobadas = @horasAprobadas " +
                              "WHERE id_participante_PK = @idParticipante";

            ConexionMetics.Open();

            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoParaConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);
            comandoParaConsulta.Parameters.AddWithValue("@horasAprobadas", CalcularHorasAprobadasParticipante(idParticipante));

            exito = comandoParaConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }

        // Método para obtener información detallada del participante a partir de una fila de datos
        public ParticipanteModel ObtenerInfoParticipante(DataRow filaParticipante)
        {
            // Crear un objeto ParticipanteModel y asignar valores desde la fila de datos
            ParticipanteModel info = new ParticipanteModel
            {
                idParticipante = Convert.ToString(filaParticipante["id_participante_PK"]),
                nombre = Convert.ToString(filaParticipante["nombre"]),
                primerApellido = Convert.ToString(filaParticipante["apellido_1"]),
                segundoApellido = Convert.ToString(filaParticipante["apellido_2"]),
                tipoIdentificacion = Convert.ToString(filaParticipante["tipo_identificacion"]),
                numeroIdentificacion = Convert.ToString(filaParticipante["numero_identificacion"]),
                correo = Convert.ToString(filaParticipante["correo"]),
                tipoParticipante = Convert.ToString(filaParticipante["tipo_participante"]),
                condicion = Convert.ToString(filaParticipante["condicion"]),
                telefono = Convert.ToString(filaParticipante["telefono"]),
                area = Convert.ToString(filaParticipante["area"]),
                departamento = Convert.ToString(filaParticipante["departamento"]),
                unidadAcademica = Convert.ToString(filaParticipante["unidad_academica"]),
                sede = Convert.ToString(filaParticipante["sede"]),
                horasMatriculadas = Convert.ToInt32(filaParticipante["total_horas_matriculadas"]),
                horasAprobadas = Convert.ToInt32(filaParticipante["total_horas_aprobadas"]),
                correoNotificacionEnviado = Convert.ToInt32(filaParticipante["correo_notificacion_enviado"]),
                gruposInscritos = new List<GrupoModel>()
            };

            return info;
        }


        // Método para obtener una lista de todos los participantes
        public List<ParticipanteModel> ObtenerListaParticipantes()
        {
            string consulta = "SELECT * FROM participante";
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);
            List<ParticipanteModel> listaParticipantes = new List<ParticipanteModel>();

            // Iterar sobre las filas de resultado y agregar la información del participante a la lista
            foreach (DataRow filaParticipante in tablaResultado.Rows)
            {
                listaParticipantes.Add(ObtenerInfoParticipante(filaParticipante));
            }

            // Si la lista está vacía, devolver null
            if (listaParticipantes.Count == 0)
            {
                return null;
            }

            return listaParticipantes;
        }

        // Método para obtener una lista de participantes asociados a un grupo específico
        public List<ParticipanteModel> ObtenerParticipantesDelGrupo(int idGrupo)
        {
            string consulta = "SELECT P.* FROM participante P " +
                              "JOIN inscripcion I ON P.id_participante_PK = I.id_participante_FK " +
                              "JOIN grupo G ON I.id_grupo_FK = G.id_grupo_PK " +
                              "WHERE G.id_grupo_PK =" + idGrupo;
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);
            List<ParticipanteModel> infoParticipante = new List<ParticipanteModel>();

            // Iterar sobre las filas de resultado y agregar la información del participante a la lista
            foreach (DataRow filaParticipante in tablaResultado.Rows)
            {
                infoParticipante.Add(ObtenerInfoParticipante(filaParticipante));
            }

            // Si la lista está vacía, devolver null
            if (infoParticipante.Count == 0)
            {
                return null;
            }

            return infoParticipante;
        }

        // Método para verificar si un usuario está inscrito en la base de datos
        public bool UsuarioEstaInscrito(string identificacion)
        {
            // Consulta para contar la cantidad de veces que aparece la identificación en la tabla de participantes
            string consulta = "SELECT CAST(COUNT(1) AS BIT) AS estaInscrito" +
                              "FROM participante " +
                              "WHERE EXISTS (SELECT * FROM participante WHERE id_participante_PK = " + identificacion + ");";
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

            // Obtener la respuesta de la consulta
            DataRow respuesta = tablaResultado.Rows[0];
            int inscrito = Convert.ToInt32(respuesta["estaInscrito"]);

            // Si la identificación no aparece en la tabla de participantes (inscrito = 0), entonces el usuario no está registrado
            if (inscrito == 0)
            {
                // No se encontró al usuario en la base de datos
                return false;
            }
            else
            {
                // El usuario está registrado en la base de datos
                return true;
            }
        }

        public bool ObtenerCorreoNotificacionEnviadoParticipante(string idParticipante)
        {
            int enviado = 0;
            string consulta = "SELECT correo_notificacion_enviado FROM participante WHERE id_participante_PK = @idParticipante;";

            using (SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics))
            {
                comandoConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);

                DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);

                if (tablaResultado.Rows.Count > 0)
                {
                    DataRow fila = tablaResultado.Rows[0];

                    if (fila["correo_notificacion_enviado"] != DBNull.Value)
                    {
                        enviado = Convert.ToInt32(fila["correo_notificacion_enviado"]);
                    }
                }

                return enviado != 0;
            }
        }

        public bool ActualizarCorreoNotificacionEnviadoParticipante(string idParticipante)
        {
            string consulta = "UPDATE participante SET correo_notificacion_enviado = @valor " +
                "WHERE id_participante_PK = @idParticipante;";

            ConexionMetics.Open();

            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);

            comandoConsulta.Parameters.AddWithValue("@valor", 1);
            comandoConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);

            bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }

        // Método para leer un archivo JSON y devolver su contenido como una cadena
        public string ReadJsonFile(string filePath)
        {
            using (StreamReader r = new StreamReader(filePath))
            {
                string json = r.ReadToEnd();
                return json;
            }
        }

        // Método para obtener el contenido de un archivo JSON y deserializarlo en un objeto genérico
        public object GetJsonFile()
        {
            string path = Path.Combine(_environment.WebRootPath, "data/dataAreas.json");
            // Leer todo el contenido del archivo como una cadena
            string allText = File.ReadAllText(path);
            // Deserializar el contenido del archivo JSON en un objeto genérico
            object jsonObject = JsonConvert.DeserializeObject(allText);
            return jsonObject;
        }






        // TODO: Unidad academica, area, sede...


        // Método para obtener una lista de todas las áreas disponibles del archivo JSON
        public List<string> GetAllAreas()
        {
            // Obtener el objeto JSON del archivo
            JObject jsonObject = (JObject)GetJsonFile();
            // Obtener la lista de áreas del objeto JSON
            JArray areasArray = (JArray)jsonObject["areas"];
            List<string> areasList = new List<string>();
            // Iterar sobre las áreas y agregar sus nombres a la lista de áreas
            foreach (JObject areaObject in areasArray)
            {
                string area = (string)areaObject["name"];
                areasList.Add(area);
            }
            return areasList;
        }

        // Método para obtener una lista de departamentos por nombre de área del archivo JSON
        public List<string> GetDepartamentosByArea(string areaName)
        {
            // Obtener el objeto JSON del archivo
            JObject jsonObject = (JObject)GetJsonFile();
            // Obtener la lista de áreas del objeto JSON
            JArray areasArray = (JArray)jsonObject["areas"];
            List<string> departamentosList = new List<string>();
            // Iterar sobre las áreas para encontrar la correspondiente al nombre de área proporcionado
            foreach (JObject areaObject in areasArray)
            {
                string currentAreaName = (string)areaObject["name"];
                // Si se encuentra el área, obtener la lista de departamentos y agregar sus nombres a la lista de departamentos
                if (currentAreaName.Equals(areaName))
                {
                    JArray departamentosArray = (JArray)areaObject["departamentos"];
                    foreach (JObject departamentoObject in departamentosArray)
                    {
                        string departamento = (string)departamentoObject["name"];
                        departamentosList.Add(departamento);
                    }
                    break;
                }
            }
            return departamentosList;
        }

        // Método para obtener una lista de secciones por nombre de área y nombre de departamento del archivo JSON
        public List<string> GetSeccionesByDepartamento(string areaName, string departamentoName)
        {
            // Obtener el objeto JSON del archivo
            JObject jsonObject = (JObject)GetJsonFile();
            // Obtener la lista de áreas del objeto JSON
            JArray areasArray = (JArray)jsonObject["areas"];
            List<string> seccionesList = new List<string>();
            // Iterar sobre las áreas para encontrar la correspondiente al nombre de área proporcionado
            foreach (JObject areaObject in areasArray)
            {
                string currentAreaName = (string)areaObject["name"];
                // Si se encuentra el área, buscar el departamento dentro del área y obtener la lista de secciones si existe
                if (currentAreaName.Equals(areaName))
                {
                    JArray departamentosArray = (JArray)areaObject["departamentos"];
                    foreach (JObject departamentoObject in departamentosArray)
                    {
                        string currentDepartamentoName = (string)departamentoObject["name"];
                        // Si se encuentra el departamento, obtener la lista de secciones si existe y agregar sus nombres a la lista de secciones
                        if (currentDepartamentoName.Equals(departamentoName))
                        {
                            JArray seccionesArray = (JArray)departamentoObject["secciones"];
                            if (seccionesArray != null)
                            {
                                foreach (JObject seccionObject in seccionesArray)
                                {
                                    string unidadAcademica = (string)seccionObject["name"];
                                    seccionesList.Add(unidadAcademica);
                                }
                                break;
                            }
                            else
                            {
                                // Si no hay secciones, agregar una cadena vacía a la lista de secciones
                                seccionesList.Add("");
                                break;
                            }
                        }
                    }
                    break;
                }
            }
            return seccionesList;
        }
    }
}
