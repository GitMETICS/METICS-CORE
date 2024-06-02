using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Hosting;


namespace webMetics.Handlers
{
    public class ParticipanteHandler : BaseDeDatosHandler
    {
        public ParticipanteHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
        {
        }

        // Método para verificar si existe un nuevo participante en la base de datos
        public bool ExisteParticipante(string identificacion)
        {
            bool existeEnBaseDatos = false;

            try
            {
                string consulta = "SELECT * FROM participante WHERE id_participante_PK = " + identificacion;
                SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
                DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

                // Crear un objeto ParticipanteModel y obtener la información del participante desde la fila de resultados
                DataRow filaParticipante = tablaResultado.Rows[0];
                ParticipanteModel infoParticipante = ObtenerInfoParticipante(filaParticipante);

                if (infoParticipante != null)
                {
                    existeEnBaseDatos = true;
                }
            }
            catch
            {
                existeEnBaseDatos = false;
            }

            return existeEnBaseDatos;
        }

        // Método para insertar un nuevo participante en la base de datos
        public bool CrearParticipante(ParticipanteModel participante)
        {
            bool comprobacionConsultaExitosa;
            string consulta = "INSERT INTO participante " +
                              "(id_usuario_FK, id_participante_PK, tipo_identificacion, correo, nombre, apellido_1, apellido_2, condicion, " +
                              "unidad_academica, tipo_participante, telefonos, area, departamento, seccion) " +
                              "VALUES (@idUsuario, @idParticipante, @tipoIdentificacion, @correo, @nombre, @apellido1, @apellido2, " +
                              "@condicion, @unidadAcademica, @tipoParticipante, @telefonos, @area, @departamento, @seccion)";
            comprobacionConsultaExitosa = AgregarParticipante(consulta, participante);


            return comprobacionConsultaExitosa;
        }

        // Método auxiliar para agregar un participante en la base de datos
        public bool AgregarParticipante(string consulta, ParticipanteModel participante)
        {
            bool exito;
            ConexionMetics.Open();

            // Crear el comando de consulta y establecer los parámetros
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@idUsuario", participante.idParticipante);
            comandoConsulta.Parameters.AddWithValue("@idParticipante", participante.idParticipante);
            comandoConsulta.Parameters.AddWithValue("@nombre", participante.nombre);
            comandoConsulta.Parameters.AddWithValue("@apellido1", participante.apellido_1);
            comandoConsulta.Parameters.AddWithValue("@apellido2", participante.apellido_2);
            comandoConsulta.Parameters.AddWithValue("@correo", participante.correo);
            comandoConsulta.Parameters.AddWithValue("@tipoIdentificacion", participante.tipoIdentificacion);
            comandoConsulta.Parameters.AddWithValue("@unidadAcademica", participante.unidadAcademica);
            comandoConsulta.Parameters.AddWithValue("@tipoParticipante", participante.tipoParticipante);
            comandoConsulta.Parameters.AddWithValue("@telefonos", participante.telefonos);
            comandoConsulta.Parameters.AddWithValue("@condicion", participante.condicion);
            comandoConsulta.Parameters.AddWithValue("@area", participante.area);
            comandoConsulta.Parameters.AddWithValue("@departamento", participante.departamento);
            comandoConsulta.Parameters.AddWithValue("@seccion", participante.seccion);

            // Ejecutar la consulta y verificar si se realizó con éxito
            exito = comandoConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }

        // Método para editar la información de un participante existente en la base de datos
        public bool EditarParticipante(ParticipanteModel participante)
        {
            bool comprobacionConsultaExitosa;
            string consulta = "UPDATE participante SET tipo_identificacion = @tipoIdentificacion, " +
                              "correo = @correo, nombre = @nombre, apellido_1 = @apellido_1, apellido_2 = @apellido_2, " +
                              "condicion = @condicion, unidad_academica = @unidadAcademica, tipo_participante = @tipoParticipante, " +
                              "telefonos = @telefonos, area = @area, departamento = @departamento, seccion = @seccion, " +
                              "horas_matriculadas = @horasMatriculadas, horas_aprobadas = @horasAprobadas " +
                              "WHERE id_participante_PK = @idParticipante";

            // Verificar si los campos opcionales son nulos y establecerlos como cadenas vacías si es necesario
            if (participante.apellido_2 == null)
            {
                participante.apellido_2 = "";
            }
            if (participante.seccion == null)
            {
                participante.seccion = "";
            }

            ConexionMetics.Open();
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoParaConsulta.Parameters.AddWithValue("@nombre", participante.nombre);
            comandoParaConsulta.Parameters.AddWithValue("@apellido_1", participante.apellido_1);
            comandoParaConsulta.Parameters.AddWithValue("@apellido_2", participante.apellido_2);
            comandoParaConsulta.Parameters.AddWithValue("@correo", participante.correo);
            comandoParaConsulta.Parameters.AddWithValue("@tipoIdentificacion", participante.tipoIdentificacion);
            comandoParaConsulta.Parameters.AddWithValue("@idParticipante", participante.idParticipante);
            comandoParaConsulta.Parameters.AddWithValue("@unidadAcademica", participante.unidadAcademica);
            comandoParaConsulta.Parameters.AddWithValue("@tipoParticipante", participante.tipoParticipante);
            comandoParaConsulta.Parameters.AddWithValue("@telefonos", participante.telefonos);
            comandoParaConsulta.Parameters.AddWithValue("@condicion", participante.condicion);
            comandoParaConsulta.Parameters.AddWithValue("@area", participante.area);
            comandoParaConsulta.Parameters.AddWithValue("@departamento", participante.departamento);
            comandoParaConsulta.Parameters.AddWithValue("@seccion", participante.seccion);
            comandoParaConsulta.Parameters.AddWithValue("@horasMatriculadas", participante.horasMatriculadas);
            comandoParaConsulta.Parameters.AddWithValue("@horasAprobadas", participante.horasAprobadas);

            comprobacionConsultaExitosa = comandoParaConsulta.ExecuteNonQuery() >= 1;
            ConexionMetics.Close();
            return comprobacionConsultaExitosa;
        }

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
            string consulta = "SELECT * FROM participante WHERE id_participante_PK = " + idParticipante;
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

            // Crear un objeto ParticipanteModel y obtener la información del participante desde la fila de resultados
            ParticipanteModel participante = null;

            foreach (DataRow fila in tablaResultado.Rows)
            {
                participante = ObtenerInfoParticipante(fila);
            }

            return participante;
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
                apellido_1 = Convert.ToString(filaParticipante["apellido_1"]),
                apellido_2 = Convert.ToString(filaParticipante["apellido_2"]),
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

        // Método para verificar si un usuario está inscrito en la base de datos
        public bool UsuarioEstaInscrito(string identificacion)
        {
            // Consulta para contar la cantidad de veces que aparece la identificación en la tabla de participantes
            string consulta = "SELECT CAST(COUNT(1) AS BIT) AS estaInscrito" +
                              "FROM participante " +
                              "WHERE EXISTS (SELECT * FROM participante WHERE id_participante_PK = " + identificacion + ");";
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);
            ParticipanteModel infoParticipante = new ParticipanteModel();

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
