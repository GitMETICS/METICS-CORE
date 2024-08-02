using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace webMetics.Handlers
{
    public class TemaHandler : BaseDeDatosHandler
    {
        private CategoriaHandler categoriaHandler;
        private TipoActividadHandler tipoActividadHandler;

        // Constructor de la clase TemaHandler
        public TemaHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
        {
            // Se inicializan los objetos categoriaHandler y tipoActividadHandler.
            categoriaHandler = new CategoriaHandler(environment, configuration);
            tipoActividadHandler = new TipoActividadHandler(environment, configuration);
        }

        // Método para obtener una lista de objetos SelectListItem que representan los temas.
        public List<SelectListItem> ObtenerListaSeleccionTemas()
        {
            // Se obtiene una lista de nombres de temas junto con sus identificadores.
            List<TemaModel> temas = ObtenerTemas();
            List<SelectListItem> temasParseados = new List<SelectListItem>();

            // Se recorre la lista de temas obtenida para parsearlos a objetos SelectListItem.
            foreach (TemaModel tema in temas)
            {
                temasParseados.Add(new SelectListItem { Text = tema.nombre, Value = tema.nombre });
            }

            return temasParseados;
        }

        // Método para obtener una lista de nombres de temas junto con sus identificadores desde la base de datos.
        public List<TemaModel> ObtenerTemas()
        {
            List<TemaModel> temas = new List<TemaModel>();

            // Se obtiene la tabla de temas mediante una consulta SQL a la base de datos.
            DataTable tablaResultado = ObtenerTablaTemas();

            // Se recorre la tabla para obtener los nombres de temas junto con sus identificadores y agregarlos a la lista.
            foreach (DataRow fila in tablaResultado.Rows)
            {
                TemaModel tema = new TemaModel 
                { 
                    nombre = Convert.ToString(fila["nombre"]),
                    idTema = Convert.ToInt32(fila["id_tema_PK"])
                };

                temas.Add(tema);
            }
            return temas;
        }

        // Método para obtener la tabla de temas desde la base de datos.
        public DataTable ObtenerTablaTemas()
        {
            // Se define la consulta SQL para obtener todos los datos de la tabla "tema".
            string consulta = "SELECT * FROM tema";
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            // Se ejecuta la consulta y se obtiene el resultado como un objeto DataTable.
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);
            return tablaResultado;
        }

        public List<TemaModel> RecuperarTemas()
        {
            // Se define la consulta SQL para obtener todos los temas de la base de datos.
            string consulta = "SELECT * FROM tema";
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            // Se ejecuta la consulta y se obtiene el resultado como un objeto DataTable.
            DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
            // Se crea una lista para almacenar los temas recuperados.
            List<TemaModel> listaTemas = new List<TemaModel>();
            // Se recorre cada fila del resultado de la consulta para obtener la información de cada tema.
            foreach (DataRow filaTema in tablaResultado.Rows)
            {
                // Se agrega el tema individual a la lista de temas.
                listaTemas.Add(ObtenerTemaIndividual(filaTema));
            }
            // Si no se encontraron temas, se devuelve null; de lo contrario, se devuelve la lista de temas.
            if (listaTemas.Count == 0)
            {
                return null;
            }
            return listaTemas;
        }

        public TemaModel ObtenerTemaIndividual(DataRow filaTema)
        {
            // Se crea un objeto TemaModel y se asignan los valores de cada columna de la fila a sus propiedades correspondientes.
            TemaModel tema = new TemaModel
            {
                idTema = Convert.ToInt32(filaTema["id_tema_PK"]),
                nombre = Convert.ToString(filaTema["nombre"]),
                categoria = Convert.ToString(filaTema["id_categoria_FK"]),
                tipoActividad = Convert.ToString(filaTema["id_tipos_actividad_FK"])
            };
            return tema;
        }

        public bool AgregarTema(string consulta, TemaModel tema, string idCategoria, string idTiposActividad)
        {
            bool exito;
            ConexionMetics.Open();
            // Se crea un comando con la consulta SQL y se asignan los parámetros con los valores del tema y las claves foráneas.
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@nombre", tema.nombre);
            comandoConsulta.Parameters.AddWithValue("@id_categoria", idCategoria);
            comandoConsulta.Parameters.AddWithValue("@id_tipo_actividad", idTiposActividad);
            // Se ejecuta el comando para agregar el nuevo tema a la base de datos.
            exito = comandoConsulta.ExecuteNonQuery() >= 1;
            ConexionMetics.Close();
            return exito;
        }

        public bool CrearTema(TemaModel tema)
        {
            bool consultaExitosa;
            // Se define la consulta SQL para insertar un nuevo tema en la tabla "tema".
            string consulta = "INSERT INTO tema (nombre,id_categoria_FK,id_tipos_actividad_FK) " +
                " values (@nombre,@id_categoria,@id_tipo_actividad) ";
            // Se obtienen los identificadores de la categoría y el tipo de actividad del tema.
            string idCategoria = categoriaHandler.ObtenerIDCategoria(tema.categoria);
            string idTiposActividad = tipoActividadHandler.ObtenerIDTipoActividad(tema.tipoActividad);
            // Se agrega el nuevo tema a la base de datos.
            consultaExitosa = AgregarTema(consulta, tema, idCategoria, idTiposActividad);
            return consultaExitosa;
        }

        public string ObtenerUltimoTemaAgregado()
        {
            // Se define la consulta SQL para obtener el identificador del último tema agregado en la tabla "tema".
            string consulta = "SELECT IDENT_CURRENT('tema')";
            ConexionMetics.Open();
            // Se ejecuta la consulta para obtener el identificador.
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            ConexionMetics.Close();
            // Se obtiene el resultado como un objeto DataTable.
            DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
            string tema = "";
            // Se recorre la tabla para obtener el identificador del último tema agregado.
            foreach (DataRow filaResultado in tablaResultado.Rows)
            {
                tema = Convert.ToString(filaResultado[0]);
            }

            return tema;
        }

        public string ParsearListaAsesoresAsistentes(IEnumerable<string> asesores)
        {
            // Verifica si la lista de asesores no es nula.
            if (asesores != null)
            {
                string lista = "";
                // Recorre cada asesor en la lista y los concatena en una cadena separada por "/".
                foreach (string asesor in asesores)
                {
                    lista += asesor + "/";
                }
                return lista;
            }
            else
            {
                // Si la lista de asesores es nula, devuelve una cadena vacía.
                return "";
            }
        }

        public bool AgregarRelacionTemaAsesor(string id_asesor, string id_tema, string asesores, string consulta)
        {
            bool exito;
            ConexionMetics.Open();
            // Crea un comando con la consulta SQL y asigna los parámetros con los valores del tema, id del asesor y lista de asistentes.
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@id_tema", id_tema);
            comandoConsulta.Parameters.AddWithValue("@id_asesor", id_asesor);
            comandoConsulta.Parameters.AddWithValue("@asesores_apoyo", asesores);
            // Ejecuta el comando para agregar la relación tema-asesor en la base de datos.
            exito = comandoConsulta.ExecuteNonQuery() >= 1;
            ConexionMetics.Close();
            return exito;
        }

        public string ObtenerIdAsesor(string asesor)
        {
            string id_asesor = "";
            // Divide el nombre completo del asesor en sus partes: nombre, primer apellido y segundo apellido.
            string[] nombre = asesor.Split(' ');
            // Define la consulta SQL para obtener el identificador del asesor en base a su nombre completo.
            string consulta = "SELECT id_asesor_PK FROM asesor WHERE " +
                "nombre = @nombre AND apellido_1 = @primerApellido AND apellido_2 = @segundoApellido ";

            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@nombre", nombre[0]);
            comandoConsulta.Parameters.AddWithValue("@primerApellido", nombre[1]);
            // Verifica si el asesor tiene segundo apellido o no y agrega el parámetro correspondiente.
            if (nombre[2] != "")
            {
                comandoConsulta.Parameters.AddWithValue("@segundoApellido", nombre[2]);
            }
            else
            {
                comandoConsulta.Parameters.AddWithValue("@segundoApellido", "-");
            }
            DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
            // Recorre el resultado de la consulta y obtiene el identificador del asesor.
            foreach (DataRow filaResultado in tablaResultado.Rows)
            {
                id_asesor = Convert.ToString(filaResultado[0]);
            }
            return id_asesor;
        }

        public List<TemaModel> RecuperarTemasDeCategoria(string nombreCategoria)
        {
            // Obtiene el identificador de la categoría en base a su nombre.
            string idCategoria = categoriaHandler.ObtenerIDCategoria(nombreCategoria);
            // Define la consulta SQL para obtener la información de los temas relacionados con la categoría.
            string consulta = " SELECT T.id_tema_PK, T.nombre, A.nombre, A.apellido_1, A.apellido_2 , DT.asesores_asistentes, TA.nombre  " +
                " FROM Tema T JOIN" +
                " JOIN tipos_actividad TA ON TA.id_tipos_actividad_PK = T.id_tipos_actividad_FK " +
                " WHERE T.id_categoria_FK = @idCategoria ";
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoParaConsulta.Parameters.AddWithValue("@idCategoria", idCategoria);
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);
            List<TemaModel> listaDeTemas = new List<TemaModel>();
            foreach (DataRow temas in tablaResultado.Rows)
            {
                // Parsea los asesores de apoyo en una cadena separada por comas.
                // Crea un objeto TemaModel con la información del tema y sus asesores.
                TemaModel tema = new TemaModel
                {
                    nombre = Convert.ToString(temas["nombre"]),
                    idTema = Convert.ToInt32(temas["id_tema_PK"]),
                    tipoActividad = Convert.ToString(temas["nombre2"])
                };

                listaDeTemas.Add(tema);
            }
            // Si no se encontraron temas relacionados con la categoría, devuelve una lista vacía.
            if (listaDeTemas.Count == 0)
            {
                return null;
            }
            return listaDeTemas;
        }

        public bool EliminarTema(string nombreTema)
        {
            bool consultaExitosa;
            // Define la consulta SQL para eliminar el tema con el nombre especificado.
            string consulta = " DELETE FROM tema  " +
                " WHERE id_tema_PK = @idTema";
            ConexionMetics.Open();
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@idTema", nombreTema);
            // Ejecuta el comando para eliminar el tema de la base de datos.
            consultaExitosa = comandoConsulta.ExecuteNonQuery() >= 1;
            ConexionMetics.Close();
            return consultaExitosa;
        }
    }
}