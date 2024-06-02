using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace webMetics.Handlers
{
    public class BaseDeDatosHandler
    {
        protected readonly SqlConnection ConexionMetics;
        protected readonly IWebHostEnvironment _environment;
        protected readonly IConfiguration _configuration;

        public BaseDeDatosHandler(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            ConexionMetics = new SqlConnection(connectionString);
        }

        // Método para ejecutar una consulta SELECT y retornar los resultados en una DataTable
        public DataTable CrearTablaConsulta(SqlCommand comandoConsulta)
        {
            // Crea un SqlDataAdapter con el comando de consulta proporcionado
            SqlDataAdapter adaptadorTabla = new SqlDataAdapter(comandoConsulta);

            // Crea una DataTable para almacenar los resultados de la consulta
            DataTable consultaFormatoTabla = new DataTable();

            // Abre la conexión con la base de datos
            ConexionMetics.Open();

            // Rellena la DataTable con los resultados de la consulta
            int _ = adaptadorTabla.Fill(consultaFormatoTabla);

            // Cierra la conexión con la base de datos
            ConexionMetics.Close();

            // Retorna la DataTable con los resultados de la consulta
            return consultaFormatoTabla;
        }
    }
}