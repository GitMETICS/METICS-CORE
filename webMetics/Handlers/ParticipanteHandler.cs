using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace webMetics.Handlers
{
    public class ParticipanteHandler : BaseDeDatosHandler
    {
        public ParticipanteHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
        {

        }

        // Verificar la existencia de un participante en la base de datos
        public bool ExisteParticipante(string id)
        {
            bool existe = false;

            using (var command = new SqlCommand("ExistsParticipante", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@id", id);
                var existeParam = command.Parameters.Add("@existe", SqlDbType.Int);
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
                command.Parameters.AddWithValue("@tipoIdentificacion", participante.tipoIdentificacion);
                // command.Parameters.AddWithValue("@numeroIdentificacion", participante.numeroIdentificacion);
                command.Parameters.AddWithValue("@correo", participante.correo);
                command.Parameters.AddWithValue("@nombre", participante.nombre);
                command.Parameters.AddWithValue("@apellido1", participante.primerApellido);
                command.Parameters.AddWithValue("@apellido2", participante.segundoApellido);
                command.Parameters.AddWithValue("@condicion", participante.condicion);
                command.Parameters.AddWithValue("@unidadAcademica", participante.unidadAcademica);
                command.Parameters.AddWithValue("@tipoParticipante", participante.tipoParticipante);
                command.Parameters.AddWithValue("@telefonos", participante.telefonos);
                command.Parameters.AddWithValue("@area", participante.area);
                command.Parameters.AddWithValue("@departamento", participante.departamento);
                command.Parameters.AddWithValue("@seccion", participante.seccion);
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
                command.Parameters.AddWithValue("@tipoIdentificacion", participante.tipoIdentificacion);
                // command.Parameters.AddWithValue("@numeroIdentificacion", participante.numeroIdentificacion);
                command.Parameters.AddWithValue("@correo", participante.correo);
                command.Parameters.AddWithValue("@nombre", participante.nombre);
                command.Parameters.AddWithValue("@apellido1", participante.primerApellido);
                command.Parameters.AddWithValue("@apellido2", participante.segundoApellido);
                command.Parameters.AddWithValue("@condicion", participante.condicion);
                command.Parameters.AddWithValue("@unidadAcademica", participante.unidadAcademica);
                command.Parameters.AddWithValue("@tipoParticipante", participante.tipoParticipante);
                command.Parameters.AddWithValue("@telefonos", participante.telefonos);
                command.Parameters.AddWithValue("@area", participante.area);
                command.Parameters.AddWithValue("@departamento", participante.departamento);
                command.Parameters.AddWithValue("@seccion", participante.seccion);
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

        // Método para editar las horas matriculadas de un participante existente en la base de datos
        public bool ActualizarHorasMatriculadasParticipante(string idParticipante, int horasParticipante)
        {
            bool exito;
            string consulta = "UPDATE participante SET horas_matriculadas = @horasMatriculadas " +
                              "WHERE id_participante_PK = @idParticipante";

            ConexionMetics.Open();

            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoParaConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);
            comandoParaConsulta.Parameters.AddWithValue("@horasMatriculadas", horasParticipante);

            exito = comandoParaConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }

        // Método para editar las horas aprobadas de un participante existente en la base de datos
        public bool ActualizarHorasAprobadasParticipante(string idParticipante, int horasParticipante)
        {
            bool exito;
            string consulta = "UPDATE participante SET horas_aprobadas = @horasAprobadas " +
                              "WHERE id_participante_PK = @idParticipante";

            ConexionMetics.Open();

            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoParaConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);
            comandoParaConsulta.Parameters.AddWithValue("@horasAprobadas", horasParticipante);

            exito = comandoParaConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

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

                ConexionMetics.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        participante = new ParticipanteModel
                        {
                            idParticipante = reader.GetString(reader.GetOrdinal("id_participante_PK")),
                            nombre = reader.GetString(reader.GetOrdinal("nombre")),
                            primerApellido = reader.GetString(reader.GetOrdinal("apellido_1")),
                            segundoApellido = reader.GetString(reader.GetOrdinal("apellido_2")),
                            correo = reader.GetString(reader.GetOrdinal("correo")),
                            tipoIdentificacion = reader.GetString(reader.GetOrdinal("tipo_identificacion")),
                            // command.Parameters.AddWithValue("@numeroIdentificacion", participante.numeroIdentificacion);
                            tipoParticipante = reader.GetString(reader.GetOrdinal("tipo_participante")),
                            unidadAcademica = reader.GetString(reader.GetOrdinal("unidad_academica")),
                            area = reader.GetString(reader.GetOrdinal("area")),
                            departamento = reader.GetString(reader.GetOrdinal("departamento")),
                            seccion = reader.GetString(reader.GetOrdinal("seccion")),
                            condicion = reader.GetString(reader.GetOrdinal("condicion")),
                            telefonos = reader.GetString(reader.GetOrdinal("telefonos")),
                            horasAprobadas = reader.GetInt32(reader.GetOrdinal("horas_aprobadas")),
                            horasMatriculadas = reader.GetInt32(reader.GetOrdinal("horas_matriculadas")),
                            gruposInscritos = new List<GrupoModel>()
                        };
                    }
                }

                ConexionMetics.Close();
            }

            return participante;
        }

        // Método para obtener información detallada del participante a partir de una fila de datos
        public ParticipanteModel ObtenerInfoParticipante(DataRow filaParticipante)
        {
            // Crear un objeto ParticipanteModel y asignar valores desde la fila de datos
            ParticipanteModel info = new ParticipanteModel
            {
                idParticipante = Convert.ToString(filaParticipante["id_participante_PK"]),
                tipoIdentificacion = Convert.ToString(filaParticipante["tipo_identificacion"]),
                correo = Convert.ToString(filaParticipante["correo"]),
                nombre = Convert.ToString(filaParticipante["nombre"]),
                primerApellido = Convert.ToString(filaParticipante["apellido_1"]),
                segundoApellido = Convert.ToString(filaParticipante["apellido_2"]),
                condicion = Convert.ToString(filaParticipante["condicion"]),
                tipoParticipante = Convert.ToString(filaParticipante["tipo_participante"]),
                area = Convert.ToString(filaParticipante["area"]),
                unidadAcademica = Convert.ToString(filaParticipante["unidad_academica"]),
                departamento = Convert.ToString(filaParticipante["departamento"]),
                seccion = Convert.ToString(filaParticipante["seccion"]),
                telefonos = Convert.ToString(filaParticipante["telefonos"]),
                horasMatriculadas = Convert.ToInt32(filaParticipante["horas_matriculadas"]),
                horasAprobadas = Convert.ToInt32(filaParticipante["horas_aprobadas"]),
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
                                    string seccion = (string)seccionObject["name"];
                                    seccionesList.Add(seccion);
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
