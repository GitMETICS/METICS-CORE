using System.Data;
using Microsoft.Data.SqlClient;

namespace webMetics.Handlers
{
    public class BaseDeDatosHandler
    {
        protected readonly SqlConnection ConexionMetics;

        // Constructor de la clase BaseDeDatosHandler
        public BaseDeDatosHandler()
        {
            // Obtiene la cadena de conexión de la configuración
            string connectionString = "Data Source=localhost;Initial Catalog=METICS;User ID=sa;Password=*******;Encrypt=False;Trust Server Certificate=True";

            // string RutaConexion = ConfigurationManager.ConnectionStrings["MeticsConnection"].ToString();

            // Inicializa la conexión con la base de datos usando la cadena de conexión
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