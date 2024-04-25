using System.Data;
using webMetics.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Data.SqlClient;


namespace webMetics.Handlers
{
    public class UsuarioHandler : BaseDeDatosHandler
        
    {
        public bool CrearUsuario(string id, string contrasena)
        {
            int exito;
            var command = new SqlCommand("InsertarUsuario", ConexionMetics);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@contrasena", contrasena);
            command.Parameters.Add("@exito", SqlDbType.Int).Direction = ParameterDirection.Output;

            ConexionMetics.Open();

            try
            {
                command.ExecuteNonQuery();

                exito = Convert.ToInt32(command.Parameters["@exito"].Value);
            }
            catch
            {
                exito = 0;
            }

            ConexionMetics.Close();

            return exito == 1;
        }

        public LoginModel ObtenerUsuario(string id)
        {
            LoginModel usuario;
            var command = new SqlCommand("ObtenerUsuario", ConexionMetics);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@id", id);
            DataTable tablaResultado = CrearTablaConsulta(command);

            ConexionMetics.Open();
            try
            {
                command.ExecuteNonQuery();

                DataRow datosUsuario = tablaResultado.Rows[0];
                usuario = new LoginModel
                {
                    identificacion = Convert.ToString(datosUsuario["id_usuario_PK"]),
                    rol = Convert.ToInt32(datosUsuario["rol_FK"])
                };
            }
            catch 
            {
                usuario = null;
            }

            ConexionMetics.Close();

            return usuario;
        }

        public bool ExisteUsuario(string id)
        {
            int exito;
            var command = new SqlCommand("ExisteUsuario", ConexionMetics);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.Add("@existe", SqlDbType.Int).Direction = ParameterDirection.Output;

            ConexionMetics.Open();
            try
            {
                command.ExecuteNonQuery();

                exito = Convert.ToInt32(command.Parameters["@existe"].Value);
            }
            catch
            {
                exito = 0;
            }

            ConexionMetics.Close();

            return exito == 1;
        }

        public bool EditarUsuario(string id, string contrasena)
        {
            int exito;
            var command = new SqlCommand("EditarUsuario", ConexionMetics);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@contrasena", contrasena);
            command.Parameters.Add("@exito", SqlDbType.Int).Direction = ParameterDirection.Output;

            ConexionMetics.Open();

            try
            {
                command.ExecuteNonQuery();

                exito = Convert.ToInt32(command.Parameters["@exito"].Value);
            }
            catch
            {
                exito = 0;
            }

            ConexionMetics.Close();

            return exito == 1;
        }

        public bool Login(string id, string contrasena)
        {
            int exito;
            var command = new SqlCommand("ValidarUsuario", ConexionMetics);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@contrasena", contrasena);
            command.Parameters.Add("@exito", SqlDbType.Int).Direction = ParameterDirection.Output;

            ConexionMetics.Open();

            try
            {
                command.ExecuteNonQuery();

                exito = Convert.ToInt32(command.Parameters["@exito"].Value);
            }
            catch
            {
                exito = 0;
            }

            ConexionMetics.Close();

            return exito == 1;
        }
    }
}
