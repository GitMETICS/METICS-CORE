using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace webMetics.Handlers
{
    public class TipoActividadHandler : BaseDeDatosHandler
    {
        public bool CrearTipoActividad(TipoActividadModel tipoActividad)
        {
            bool consultaExitosa;
            // Consulta SQL para insertar un nuevo tipo de actividad en la base de datos.
            string consulta = " INSERT INTO tipos_actividad " +
                " (nombre, descripcion) " +
                " VALUES (@nombre, @descripcion) ";

            // Llama al método AgregarTipoActividad para ejecutar la consulta y agregar el tipo de actividad.
            consultaExitosa = AgregarTipoActividad(consulta, tipoActividad);
            return consultaExitosa;
        }

        public bool AgregarTipoActividad(string consulta, TipoActividadModel tipoActividad)
        {
            bool consultaExitosa;
            ConexionMetics.Open();
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@nombre", tipoActividad.nombre);
            comandoConsulta.Parameters.AddWithValue("@descripcion", tipoActividad.descripcion);
            // Ejecuta el comando para agregar el tipo de actividad en la base de datos.
            consultaExitosa = comandoConsulta.ExecuteNonQuery() >= 1;
            ConexionMetics.Close();
            return consultaExitosa;
        }

        public List<TipoActividadModel> RecuperarTiposDeActividades()
        {
            // Consulta SQL para obtener todos los tipos de actividades de la base de datos.
            string consulta = "SELECT * FROM tipos_actividad";
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
            List<TipoActividadModel> listaTiposActividad = new List<TipoActividadModel>();
            foreach (DataRow filaTipoActividad in tablaResultado.Rows)
            {
                // Agrega cada tipo de actividad a la lista.
                listaTiposActividad.Add(ObtenerTipoActividadIndividual(filaTipoActividad));
            }
            // Si no se encontraron tipos de actividades, devuelve una lista vacía.
            if (listaTiposActividad.Count == 0)
            {
                return null;
            }
            return listaTiposActividad;
        }

        public TipoActividadModel ObtenerTipoActividadIndividual(DataRow filaTipoActividad)
        {
            // Crea un objeto TipoActividadModel con la información del tipo de actividad.
            TipoActividadModel tipoActividad = new TipoActividadModel
            {
                nombre = Convert.ToString(filaTipoActividad["nombre"]),
                descripcion = Convert.ToString(filaTipoActividad["descripcion"]),
                idGenerado = Convert.ToString(filaTipoActividad["id_tipos_actividad_PK"])
            };
            return tipoActividad;
        }

        public bool EliminarTipoActividad(string nombreTipoActividad)
        {
            bool consultaExitosa;
            // Consulta SQL para eliminar un tipo de actividad de la base de datos por su nombre.
            string consulta = " DELETE FROM tipos_actividad  " +
                " WHERE nombre = @nombre";
            ConexionMetics.Open();
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@nombre", nombreTipoActividad);
            // Ejecuta el comando para eliminar el tipo de actividad de la base de datos.
            consultaExitosa = comandoConsulta.ExecuteNonQuery() >= 1;
            ConexionMetics.Close();
            return consultaExitosa;
        }

        public bool EditarTipoActividad(TipoActividadModel tipoActividad)
        {
            bool consultaExitosa;
            // Consulta SQL para actualizar la información de un tipo de actividad en la base de datos.
            string consulta = " UPDATE tipos_actividad SET nombre = @nombre , descripcion = @descripcion " +
                " WHERE id_tipos_actividad_PK = @idGenerado";
            ConexionMetics.Open();
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoParaConsulta.Parameters.AddWithValue("@nombre", tipoActividad.nombre);
            comandoParaConsulta.Parameters.AddWithValue("@idGenerado", tipoActividad.idGenerado);
            comandoParaConsulta.Parameters.AddWithValue("@descripcion", tipoActividad.descripcion);
            // Ejecuta el comando para actualizar el tipo de actividad en la base de datos.
            consultaExitosa = comandoParaConsulta.ExecuteNonQuery() >= 1;
            ConexionMetics.Close();
            return consultaExitosa;
        }

        public List<SelectListItem> RecuperarTiposDeActividadesSeleccionables()
        {
            List<string> tiposActividades = ObtenerNombresTipoActividad();
            List<SelectListItem> tiposActividadParseados = new List<SelectListItem>();
            foreach (string tipoActividad in tiposActividades)
            {
                // Convierte cada nombre de tipo de actividad en un objeto SelectListItem
                // para utilizarlo en elementos seleccionables (por ejemplo, en una lista desplegable).
                tiposActividadParseados.Add(new SelectListItem { Text = tipoActividad, Value = tipoActividad });
            }
            return tiposActividadParseados;
        }

        public List<string> ObtenerNombresTipoActividad()
        {
            List<string> lista = new List<string>();
            DataTable tablaResultado = ObtenerTablaTipoActividad();
            foreach (DataRow fila in tablaResultado.Rows)
            {
                // Obtiene el nombre de cada tipo de actividad desde la tabla de resultados
                // y lo agrega a la lista de nombres.
                string nombre = Convert.ToString(fila["nombre"]);
                lista.Add(nombre);
            }
            return lista;
        }

        public DataTable ObtenerTablaTipoActividad()
        {
            // Consulta SQL para obtener los nombres de los tipos de actividad desde la base de datos.
            string consulta = "SELECT nombre FROM tipos_actividad ";
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            // Crea una tabla de resultados para almacenar los datos obtenidos.
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);
            return tablaResultado;
        }

        public string ObtenerIDTipoActividad(string nombre)
        {
            string consulta = "SELECT id_tipos_actividad_PK FROM tipos_actividad WHERE nombre= @nombre";
            ConexionMetics.Open();
            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoParaConsulta.Parameters.AddWithValue("@nombre", nombre);
            // Ejecutar la consulta y obtener los resultados en una tabla
            DataTable tablaResultado = CrearTablaConsulta(comandoParaConsulta);
            ConexionMetics.Close(); // Se cierra la conexión después de obtener los resultados

            string id = "";
            foreach (DataRow filaId in tablaResultado.Rows)
            {
                // Obtener el ID del tipo de actividad desde la tabla de resultados
                id = Convert.ToString(filaId["id_tipos_actividad_PK"]);
            }
            return id;
        }

        public TipoActividadModel ObtenerTipoActividadLowerCase(DataRow filaTipoActividad)
        {
            // Obtiene el nombre del tipo de actividad desde la fila de resultados
            string nombre = Convert.ToString(filaTipoActividad["nombre"]);
            // Crea un objeto TipoActividadModel con el nombre en minúsculas y sin espacios
            TipoActividadModel tipoActividad = new TipoActividadModel
            {
                nombre = nombre.Replace(" ", "").ToLower(),
                idGenerado = Convert.ToString(filaTipoActividad["id_tipos_actividad_PK"])
            };
            return tipoActividad;
        }

        public List<TipoActividadModel> RecuperarTiposDeActividadesLowerCase()
        {
            string consulta = "SELECT nombre, id_tipos_actividad_PK FROM tipos_actividad";
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            // Ejecutar la consulta y obtener los resultados en una tabla
            DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
            List<TipoActividadModel> listaTiposActividad = new List<TipoActividadModel>();
            foreach (DataRow filaTipoActividad in tablaResultado.Rows)
            {
                // Agregar cada tipo de actividad (en minúsculas y sin espacios) a la lista
                listaTiposActividad.Add(ObtenerTipoActividadLowerCase(filaTipoActividad));
            }
            if (listaTiposActividad.Count == 0)
            {
                return null;
            }
            return listaTiposActividad;
        }
    }
}