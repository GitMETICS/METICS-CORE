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
        public CategoriaHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
        {
        }

        public List<CategoriaModel> ObtenerCategorias()
        {
            List<CategoriaModel> categorias = new List<CategoriaModel>();
            string consulta = "SELECT * FROM categoria;";

            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);

            foreach (DataRow fila in tablaResultado.Rows)
            {
                CategoriaModel categoria = new CategoriaModel
                {
                    idCategoria = Convert.ToInt32(fila["id_categoria_PK"]),
                    nombre = Convert.ToString(fila["nombre"]),
                    descripcion = Convert.ToString(fila["descripcion"]),
                };

                categorias.Add(categoria);
            }
            return categorias;
        }

        public CategoriaModel ObtenerCategoria(int idCategoria)
        {
            string consulta = "SELECT * FROM categoria WHERE id_categoria_PK = @idCategoria;";

            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@idCategoria", idCategoria);

            DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
            DataRow fila = tablaResultado.Rows[0];

            CategoriaModel categoria = new CategoriaModel
            {
                idCategoria = Convert.ToInt32(fila["id_categoria_PK"]),
                nombre = Convert.ToString(fila["nombre"]),
                descripcion = Convert.ToString(fila["descripcion"]),
            };

            return categoria;
        }

        // Método para obtener una lista de objetos SelectListItem que representan las categorias.
        public List<SelectListItem> ObtenerListaSeleccionCategorias()
        {
            List<CategoriaModel> categorias = ObtenerCategorias();
            List<SelectListItem> categoriasParseadas = new List<SelectListItem>();

            foreach (CategoriaModel categoria in categorias)
            {
                categoriasParseadas.Add(new SelectListItem { Text = categoria.nombre, Value = Convert.ToString(categoria.idCategoria) });
            }

            return categoriasParseadas;
        }

        public bool CrearCategoria(CategoriaModel categoria)
        {
            string consulta =
                "INSERT INTO categoria (nombre, descripcion) " +
                "VALUES (@nombre, @descripcion)";

            ConexionMetics.Open();

            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@nombre", categoria.nombre);
            comandoConsulta.Parameters.AddWithValue("@descripcion", categoria.descripcion);

            bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }

        public bool EditarCategoria(CategoriaModel categoria)
        {
            string consulta =
                "UPDATE tema SET nombre = @nombre, descripcion = @descripcion" +
                "WHERE id_categoria_PK, @idCategoria";

            ConexionMetics.Open();

            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@nombre", categoria.nombre);
            comandoConsulta.Parameters.AddWithValue("@descripcion", categoria.descripcion);
            comandoConsulta.Parameters.AddWithValue("@idCategoria", categoria.idCategoria);

            bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }

        public bool EliminarCategoria(int idCategoria)
        {
            string consulta = "DELETE FROM categoria WHERE id_categoria_PK = @idCategoria";

            ConexionMetics.Open();

            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@idCategoria", idCategoria);

            bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }
    }
}