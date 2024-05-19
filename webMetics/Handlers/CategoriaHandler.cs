using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

/*
 * Handler para las categorias
 * En esta clase se encuentran los metodos backend para realizar las consultas a la base de datos
 */
namespace webMetics.Handlers
{
    public class CategoriaHandler : BaseDeDatosHandler
    {
        public CategoriaHandler(IWebHostEnvironment environment) : base(environment)
        {
        }

        // Método para obtener una instancia de la clase CategoriaModel a partir de un DataRow
        public CategoriaModel ObtenerCategorias(DataRow filaCategoria)
        {
            // Crea una nueva instancia de CategoriaModel y asigna los valores desde el DataRow
            CategoriaModel categoria = new CategoriaModel
            {
                nombre = Convert.ToString(filaCategoria["nombre"]),
                descripcion = Convert.ToString(filaCategoria["descripcion"]),
                idGenerado = Convert.ToString(filaCategoria["id_categoria_PK"])
            };
            return categoria;
        }

        // Método para crear una nueva categoría en la base de datos
        public bool CrearCategoria(CategoriaModel categoria)
        {
            bool consultaExitosa;
            // Consulta SQL para insertar una nueva categoría en la tabla 'categoria'
            string consulta = "INSERT INTO categoria (nombre, descripcion) VALUES (@nombre, @descripcion)";

            // Llama al método AgregarCategoria para ejecutar la consulta
            consultaExitosa = AgregarCategoria(consulta, categoria);
            return consultaExitosa;
        }

        // Método para agregar una nueva categoría en la base de datos
        public bool AgregarCategoria(string consulta, CategoriaModel categoria)
        {
            bool exito;
            // Abre la conexión con la base de datos
            ConexionMetics.Open();
            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            // Asigna los parámetros de la consulta con los valores de la categoría
            comandoConsulta.Parameters.AddWithValue("@nombre", categoria.nombre);
            comandoConsulta.Parameters.AddWithValue("@descripcion", categoria.descripcion);
            // Ejecuta la consulta y comprueba si al menos una fila fue afectada
            exito = comandoConsulta.ExecuteNonQuery() >= 1;
            // Cierra la conexión con la base de datos
            ConexionMetics.Close();
            return exito;
        }

        // Método para recuperar una lista de todas las categorías desde la base de datos
        public List<CategoriaModel> RecuperarCategorias()
        {
            // Consulta SQL para seleccionar todas las categorías de la tabla 'categoria'
            string consulta = "SELECT * FROM categoria";
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            // Obtiene los resultados de la consulta en una DataTable
            DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
            // Crea una lista para almacenar las categorías recuperadas
            List<CategoriaModel> listaCategorias = new List<CategoriaModel>();

            // Recorre cada fila de la DataTable y agrega una instancia de CategoriaModel a la lista
            foreach (DataRow filaCategoria in tablaResultado.Rows)
            {
                listaCategorias.Add(ObtenerCategoriaIndividual(filaCategoria));
            }

            // Si no se encontraron categorías, devuelve null; de lo contrario, devuelve la lista
            if (listaCategorias.Count == 0)
            {
                return null;
            }
            return listaCategorias;
        }

        public CategoriaModel ObtenerCategoriaIndividual(DataRow filaCategoria)
        {
            // Crea una nueva instancia de CategoriaModel y asigna los valores desde el DataRow
            CategoriaModel categoria = new CategoriaModel
            {
                nombre = Convert.ToString(filaCategoria["nombre"]),
                descripcion = Convert.ToString(filaCategoria["descripcion"]),
                idGenerado = Convert.ToString(filaCategoria["id_categoria_PK"])
            };
            return categoria;
        }

        public bool EliminarCategoria(string nombreCategoria)
        {
            bool consultaExitosa;
            // Consulta SQL para eliminar una categoría con el nombre proporcionado
            string consulta = "DELETE FROM categoria WHERE nombre = @nombre";

            // Abre la conexión con la base de datos
            ConexionMetics.Open();

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@nombre", nombreCategoria);

            // Ejecuta la consulta y comprueba si al menos una fila fue afectada
            consultaExitosa = comandoConsulta.ExecuteNonQuery() >= 1;

            // Cierra la conexión con la base de datos
            ConexionMetics.Close();

            return consultaExitosa;
        }

        public bool EditarCategoria(CategoriaModel categoria)
        {
            bool consultaExitosa;
            // Consulta SQL para actualizar una categoría con los nuevos valores proporcionados
            string consulta = "UPDATE categoria SET nombre = @nombre, descripcion = @descripcion WHERE id_categoria_PK = @idGenerado";

            // Abre la conexión con la base de datos
            ConexionMetics.Open();

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@nombre", categoria.nombre);
            comandoConsulta.Parameters.AddWithValue("@idGenerado", categoria.idGenerado);
            comandoConsulta.Parameters.AddWithValue("@descripcion", categoria.descripcion);

            // Ejecuta la consulta y comprueba si al menos una fila fue afectada
            consultaExitosa = comandoConsulta.ExecuteNonQuery() >= 1;

            // Cierra la conexión con la base de datos
            ConexionMetics.Close();

            return consultaExitosa;
        }

        // Metodo para obtener los nombres de las categorias con un index asociado para el multiselect de los formularios
        public List<SelectListItem> RecuperarCategoriasIndexadas()
        {
            // Obtiene una lista de los nombres de las categorías
            List<string> categorias = ObtenerNombresCategoria();

            // Crea una nueva lista de SelectListItem para contener los nombres de categoría indexados
            List<SelectListItem> categoriasParseadas = new List<SelectListItem>();

            // Itera sobre la lista de nombres de categorías y los agrega a la lista de SelectListItem
            foreach (string categoria in categorias)
            {
                categoriasParseadas.Add(new SelectListItem { Text = categoria, Value = categoria });
            }

            // Retorna la lista de nombres de categorías indexados
            return categoriasParseadas;
        }

        // Método para obtener una lista de los nombres de las categorías desde la base de datos
        public List<string> ObtenerNombresCategoria()
        {
            // Crea una nueva lista para almacenar los nombres de las categorías
            List<string> lista = new List<string>();

            // Obtiene una tabla con los resultados de la consulta de nombres de categorías
            DataTable tablaResultado = ObtenerTablaCategoria();

            // Recorre cada fila de la tabla y agrega el nombre de categoría a la lista
            foreach (DataRow fila in tablaResultado.Rows)
            {
                string nombre = Convert.ToString(fila["nombre"]);
                lista.Add(nombre);
            }

            // Retorna la lista de nombres de categorías
            return lista;
        }

        // Método para obtener una tabla con los nombres de las categorías desde la base de datos
        public DataTable ObtenerTablaCategoria()
        {
            // Consulta SQL para seleccionar los nombres de categorías desde la tabla 'categoria'
            string consulta = "SELECT nombre FROM categoria";

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            // Obtiene una tabla con los resultados de la consulta
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

            // Retorna la tabla con los resultados de la consulta
            return tablaResultado;
        }

        // Método para obtener el ID de una categoría a partir de su nombre
        public string ObtenerIDCategoria(string nombre)
        {
            // Consulta SQL para seleccionar el ID de la categoría con el nombre proporcionado
            string consulta = "SELECT id_categoria_PK FROM categoria WHERE nombre= @nombre";

            // Abre la conexión con la base de datos
            ConexionMetics.Open();

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@nombre", nombre);

            // Cierra la conexión con la base de datos
            ConexionMetics.Close();

            // Obtiene una tabla con los resultados de la consulta
            DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);

            // Almacena el ID de la categoría encontrado
            string id = "";

            // Recorre los resultados de la tabla para obtener el ID
            foreach (DataRow filaId in tablaResultado.Rows)
            {
                id = Convert.ToString(filaId["id_categoria_PK"]);
            }

            // Retorna el ID de la categoría
            return id;
        }

        // Método para recuperar una lista de todas las categorías desde la base de datos, con los nombres en minúsculas y sin espacios
        public List<CategoriaModel> RecuperarCategoriasLowerCase()
        {
            // Consulta SQL para seleccionar los nombres y los IDs de todas las categorías desde la tabla 'categoria'
            string consulta = "SELECT nombre, id_categoria_PK FROM categoria";

            // Crea un nuevo comando SQL con la consulta y la conexión establecida
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);

            // Obtiene una tabla con los resultados de la consulta
            DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);

            // Crea una lista para almacenar las categorías recuperadas con los nombres en minúsculas y sin espacios
            List<CategoriaModel> listaCategorias = new List<CategoriaModel>();

            // Recorre cada fila de la tabla y agrega una instancia de CategoriaModel a la lista
            foreach (DataRow filaCategoria in tablaResultado.Rows)
            {
                listaCategorias.Add(ObtenerCategoriaIndividualLowerCase(filaCategoria));
            }

            // Si no se encontraron categorías, devuelve null; de lo contrario, devuelve la lista
            if (listaCategorias.Count == 0)
            {
                return null;
            }
            return listaCategorias;
        }

        // Método para obtener una instancia de la clase CategoriaModel a partir de un DataRow con el nombre en minúsculas y sin espacios
        public CategoriaModel ObtenerCategoriaIndividualLowerCase(DataRow filaCategoria)
        {
            // Obtiene el nombre y el ID de la categoría desde el DataRow
            string nombre = Convert.ToString(filaCategoria["nombre"]);
            string id = Convert.ToString(filaCategoria["id_categoria_PK"]);

            // Crea una nueva instancia de CategoriaModel con el nombre en minúsculas y sin espacios
            CategoriaModel categoria = new CategoriaModel
            {
                nombre = nombre.Replace(" ", "").ToLower(),
                idGenerado = id
            };

            // Retorna la instancia de CategoriaModel creada
            return categoria;
        }

    }
}