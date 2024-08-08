using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace webMetics.Handlers
{
    public class TemaHandler : BaseDeDatosHandler
    {
        private CategoriaHandler accesoACategoria;
        // private TipoActividadHandler tipoActividadHandler;

        public TemaHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
        {
            accesoACategoria = new CategoriaHandler(environment, configuration);
            // tipoActividadHandler = new TipoActividadHandler(environment, configuration);
        }

        public List<TemaModel> ObtenerTemas()
        {
            List<TemaModel> temas = new List<TemaModel>();
            string consulta = "SELECT * FROM tema;";
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);
            foreach (DataRow fila in tablaResultado.Rows)
            {
                TemaModel tema = new TemaModel 
                {
                    idTema = Convert.ToInt32(fila["id_tema_PK"]),
                    nombre = Convert.ToString(fila["nombre"]),
                };

                temas.Add(tema);
            }
            return temas;
        }

        public TemaModel ObtenerTema(int idTema)
        {
            string consulta = "SELECT * FROM tema WHERE id_tema_PK = @idTema;";
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@idTema", idTema);

            DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
            DataRow fila = tablaResultado.Rows[0];

            TemaModel tema = new TemaModel
            {
                idTema = Convert.ToInt32(fila["id_tema_PK"]),
                nombre = Convert.ToString(fila["nombre"]),
            };

            return tema;
        }

        // Método para obtener una lista de objetos SelectListItem de los temas.
        public List<SelectListItem> ObtenerListaSeleccionTemas()
        {
            List<TemaModel> temas = ObtenerTemas();
            List<SelectListItem> temasParseados = new List<SelectListItem>();

            foreach (TemaModel tema in temas)
            {
                temasParseados.Add(new SelectListItem { Text = tema.nombre, Value = Convert.ToString(tema.idTema) });
            }

            return temasParseados;
        }

        public bool CrearTema(TemaModel tema)
        {
            string consulta = "INSERT INTO tema(nombre) VALUES (@nombre);";

            ConexionMetics.Open();

            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@nombre", tema.nombre);

            bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }

        public bool EditarTema(TemaModel tema)
        {
            string consulta = "UPDATE tema SET nombre = @nombre WHERE id_tema_PK = @idTema;";

            ConexionMetics.Open();

            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@nombre", tema.nombre);
            comandoConsulta.Parameters.AddWithValue("@idTema", tema.idTema);

            bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }

        public bool EliminarTema(int idTema)
        {
            string consulta = "DELETE FROM tema WHERE id_tema_PK = @idTema;";

            ConexionMetics.Open();

            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@idTema", idTema);

            bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }
    }
}