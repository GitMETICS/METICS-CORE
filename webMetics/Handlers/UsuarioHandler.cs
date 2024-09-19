using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;

namespace webMetics.Handlers
{
    public class UsuarioHandler : BaseDeDatosHandler
        
    {
        public UsuarioHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
        {

        }

        public bool CrearUsuario(string id, string contrasena, int rol=0)
        {
            bool exito = false;

            using (var command = new SqlCommand("InsertUsuario", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@rol", rol);
                command.Parameters.AddWithValue("@contrasena", contrasena);

                try
                {
                    ConexionMetics.Open();
                    exito = command.ExecuteNonQuery() >= 1;
                }
                catch
                {
                    exito = false;
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return exito;
        }

        public LoginModel ObtenerUsuario(string id)
        {
            LoginModel usuario = null;

            using (var command = new SqlCommand("SelectUsuario", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@id", id);

                try
                {
                    ConexionMetics.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            usuario = new LoginModel
                            {
                                id = Convert.ToString(reader["id_usuario_PK"]),
                                rol = Convert.ToInt32(reader["rol_FK"])
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ObtenerUsuario: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return usuario;
        }

        public bool ExisteUsuario(string id)
        {
            bool existe = false;

            using (var command = new SqlCommand("ExistsUsuario", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@id", id);
                var existeParam = command.Parameters.Add("@exists", SqlDbType.Int);
                existeParam.Direction = ParameterDirection.Output;

                try
                {
                    ConexionMetics.Open();
                    command.ExecuteNonQuery();
                    existe = Convert.ToInt32(existeParam.Value) == 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ExisteUsuario: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return existe;
        }

        public bool EditarUsuario(string id, int rol, string contrasena)
        {
            bool exito = false;

            using (var command = new SqlCommand("UpdateUsuario", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@rol", rol);
                command.Parameters.AddWithValue("@contrasena", contrasena);

                try
                {
                    ConexionMetics.Open();
                    exito = command.ExecuteNonQuery() >= 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in EditarUsuario: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return exito;
        }

        public bool EliminarUsuario(string idUsuario)
        {
            string consulta = "DELETE FROM usuario WHERE id_usuario_PK = @idUsuario";

            ConexionMetics.Open();

            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);

            comandoParaConsulta.Parameters.AddWithValue("@idUsuario", idUsuario);

            bool exito = comandoParaConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }

        public bool AutenticarUsuario(string id, string contrasena)
        {
            bool autenticado = false;

            using (var command = new SqlCommand("AuthUsuario", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@contrasena", contrasena);
                var authParam = command.Parameters.Add("@auth", SqlDbType.Int);
                authParam.Direction = ParameterDirection.Output;

                try
                {
                    ConexionMetics.Open();
                    command.ExecuteNonQuery();
                    autenticado = Convert.ToInt32(authParam.Value) == 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in AutenticarUsuario: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return autenticado;
        }

        public bool ObtenerRegistradoPorUsuario(string idUsuario)
        {
            string consulta = "SELECT registrado_por_usuario FROM usuario WHERE id_usuario_PK = @idUsuario;";
            int registradoPorUsuario = 1;

            using (SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics))
            {
                comandoConsulta.Parameters.AddWithValue("@idUsuario", idUsuario);

                DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);

                if (tablaResultado.Rows.Count > 0)
                {
                    DataRow fila = tablaResultado.Rows[0];
                    registradoPorUsuario = Convert.ToInt32(fila["registrado_por_usuario"]);
                }
            }

            return registradoPorUsuario == 1;
        }

        public bool ActualizarRegistradoPorUsuario(string idUsuario)
        {
            string consulta = "UPDATE usuario SET registrado_por_usuario = 1 WHERE id_usuario_PK = @idUsuario;";

            ConexionMetics.Open();

            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@idUsuario", idUsuario);

            bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }
    }
}
