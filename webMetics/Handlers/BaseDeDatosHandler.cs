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
            DataTable consultaFormatoTabla = new DataTable();

            try
            {
                if (ConexionMetics.State != ConnectionState.Open)
                {
                    ConexionMetics.Open();
                }

                using SqlDataAdapter adaptadorTabla = new SqlDataAdapter(comandoConsulta);
                adaptadorTabla.Fill(consultaFormatoTabla);
                return consultaFormatoTabla;
            }
            finally
            {
                if (ConexionMetics.State != ConnectionState.Closed)
                {
                    ConexionMetics.Close();
                }
            }
        }
    }
}
