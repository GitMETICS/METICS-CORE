using Microsoft.Data.SqlClient;
using System.Data;

namespace webMetics.Handlers
{
    public class BaseDeDatosHandler
    {
        protected readonly SqlConnection ConexionMetics;

        // Constructor de la clase BaseDeDatosHandler
        public BaseDeDatosHandler()
        {
            // Obtiene la cadena de conexión de la configuración    // TODO
            string RutaConexion = "Server=(localdb)\\mssqllocaldb;Database=aspnet-webMetics-2793fa9b-a054-47cf-8443-da5f8bca4169;Trusted_Connection=True;MultipleActiveResultSets=true";

            // Inicializa la conexión con la base de datos usando la cadena de conexión
            ConexionMetics = new SqlConnection(RutaConexion);
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