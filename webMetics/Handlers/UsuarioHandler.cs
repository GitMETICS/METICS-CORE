using System.Data;
using Microsoft.Data.SqlClient;
using NPOI.SS.Formula.Functions;
using NPOI.Util;
using webMetics.Models;

namespace webMetics.Handlers
{
    public class UsuarioHandler : BaseDeDatosHandler

    {
        public UsuarioHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
        {

        }

        public bool CrearUsuario(string id, string contrasena, int rol = 0)
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

        public bool ActualizarContrasena(string correo, string contrasena)
        {
            int rol = 0;

            // Obtener rol_FK de la tabla usuario usando el correo dado de participante
            string consulta = "SELECT rol_FK FROM usuario WHERE id_usuario_PK = @correo;";

            ConexionMetics.Open();

            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@correo", correo);

            using (comandoConsulta)
            {
                using (var reader = comandoConsulta.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        rol = Convert.ToInt32(reader["rol_FK"]);
                    }
                    else
                    {
                        ConexionMetics.Close();
                        return false;
                    }
                }
            }
            ConexionMetics.Close();

            bool exito = EditarUsuario(correo, rol, contrasena);
            // Usamos el procedimiento ya guardado para actualizar la contraseña
            return exito;
        }

        public void InsertarAccesoUsuarioBitacora(string idUsuario, string estadoAcceso) // SUCCESS or FAILED
        {
            try
            {
                using (SqlCommand command = new SqlCommand("InsertBitacoraAcceso", ConexionMetics))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    ConexionMetics.Open();

                    command.Parameters.AddWithValue("@id_usuario", idUsuario);
                    command.Parameters.AddWithValue("@estado_acceso", estadoAcceso);

                    command.ExecuteNonQuery();

                    // Console.WriteLine("Procedimiento almacenado 'InsertBitacoraAcceso' ejecutado correctamente.");
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Error al ejecutar el procedimiento almacenado 'InsertBitacoraAcceso': {ex.Message}");
            }
            finally
            {
                if (ConexionMetics.State == ConnectionState.Open)
                {
                    ConexionMetics.Close();
                }
            }
        }

        public List<BitacoraAcceso> SelectBitacoraAccesoUsuario(string idUsuario, int diasAtras)
        {
            // Se usa una lista para almacenar todos los objetos de bitácora leídos.
            List<BitacoraAcceso> accesos = new List<BitacoraAcceso>();

            try
            {
                // Se usa un bloque 'using' para asegurar que los recursos se liberen.
                using (SqlCommand command = new SqlCommand("SelectBitacoraAccesoUsuario", ConexionMetics))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Agregamos los parámetros necesarios para el procedimiento almacenado.
                    command.Parameters.AddWithValue("@id_usuario", idUsuario);
                    command.Parameters.AddWithValue("@dias_atras", diasAtras);

                    ConexionMetics.Open();

                    // Usamos SqlDataReader para leer los datos del resultado.
                    using (var reader = command.ExecuteReader())
                    {
                        // Se itera a través de cada fila del resultado.
                        while (reader.Read())
                        {
                            // Se crea una instancia del modelo BitacoraAcceso para cada registro.
                            var bitacoraAcceso = new BitacoraAcceso
                            {
                                IdAccesoPK = reader.IsDBNull(reader.GetOrdinal("id_acceso_PK")) ? 0 : reader.GetInt64(reader.GetOrdinal("id_acceso_PK")),
                                IdUsuarioFK = reader.IsDBNull(reader.GetOrdinal("id_usuario_FK")) ? "" : reader.GetString(reader.GetOrdinal("id_usuario_FK")),
                                FechaHoraAcceso = reader.IsDBNull(reader.GetOrdinal("fecha_hora_acceso")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("fecha_hora_acceso")),
                                EstadoAcceso = reader.IsDBNull(reader.GetOrdinal("estado_acceso")) ? "" : reader.GetString(reader.GetOrdinal("estado_acceso"))
                            };
                            // Se añade el objeto a la lista.
                            accesos.Add(bitacoraAcceso);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Error al ejecutar el procedimiento almacenado 'SelectBitacoraAccesoUsuario': {ex.Message}");
            }
            finally
            {
                if (ConexionMetics.State == ConnectionState.Open)
                {
                    ConexionMetics.Close();
                }
            }

            return accesos;
        }

        public List<BitacoraAcceso> SelectBitacoraAccesosPorFecha(string fechaDesde, string fechaHasta, string estadoAcceso)
        {
            // Se usa una lista para almacenar todos los objetos de bitácora leídos.
            List<BitacoraAcceso> accesos = new List<BitacoraAcceso>();

            try
            {
                // Se usa un bloque 'using' para asegurar que los recursos se liberen.
                using (SqlCommand command = new SqlCommand("SelectBitacoraAccesosPorFecha", ConexionMetics))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Agregamos los parámetros necesarios para el procedimiento almacenado.
                    command.Parameters.AddWithValue("@fecha_desde", fechaDesde);
                    command.Parameters.AddWithValue("@fecha_hasta", fechaHasta);
                    command.Parameters.AddWithValue("@estado_filtro", estadoAcceso);

                    ConexionMetics.Open();

                    // Usamos SqlDataReader para leer los datos del resultado.
                    using (var reader = command.ExecuteReader())
                    {
                        // Se itera a través de cada fila del resultado.
                        while (reader.Read())
                        {
                            // Se crea una instancia del modelo BitacoraAcceso para cada registro.
                            var bitacoraAcceso = new BitacoraAcceso
                            {
                                IdAccesoPK = reader.IsDBNull(reader.GetOrdinal("id_acceso_PK")) ? 0 : reader.GetInt64(reader.GetOrdinal("id_acceso_PK")),
                                IdUsuarioFK = reader.IsDBNull(reader.GetOrdinal("id_usuario_FK")) ? "" : reader.GetString(reader.GetOrdinal("id_usuario_FK")),
                                FechaHoraAcceso = reader.IsDBNull(reader.GetOrdinal("fecha_hora_acceso")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("fecha_hora_acceso")),
                                EstadoAcceso = reader.IsDBNull(reader.GetOrdinal("estado_acceso")) ? "" : reader.GetString(reader.GetOrdinal("estado_acceso"))
                            };
                            // Se añade el objeto a la lista.
                            accesos.Add(bitacoraAcceso);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Error al ejecutar el procedimiento almacenado 'SelectBitacoraAccesosPorFecha': {ex.Message}");
                // En caso de error, devuelve una lista vacía para evitar fallos.
                return new List<BitacoraAcceso>();
            }
            finally
            {
                if (ConexionMetics.State == ConnectionState.Open)
                {
                    ConexionMetics.Close();
                }
            }

            // Devuelve la lista de objetos de bitácora encontrados.
            return accesos;
        }


        public BitacoraAcceso SelectUltimoAccesoUsuario(string idUsuario)
        {
            // Declara una variable para almacenar el resultado. La inicializamos en null.
            BitacoraAcceso ultimoAcceso = null;

            try
            {
                // Se usa un bloque 'using' para asegurar que el comando se libere correctamente.
                using (SqlCommand command = new SqlCommand("SelectUltimoAccesoUsuario", ConexionMetics))
                {
                    // Especifica que el tipo de comando es un procedimiento almacenado.
                    command.CommandType = CommandType.StoredProcedure;

                    // Agrega el parámetro para el ID de usuario.
                    command.Parameters.AddWithValue("@id_usuario", idUsuario);

                    // Asegúrate de que la conexión esté abierta antes de ejecutar el comando.
                    ConexionMetics.Open();

                    // Usa SqlDataReader para leer los datos del resultado, ya que el SP devuelve una fila.
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // Solo lee la primera fila del resultado (ya que el SP usa TOP 1).
                        if (reader.Read())
                        {
                            // Crea una nueva instancia del objeto BitacoraAcceso y mapea los datos del lector.
                            // Usamos reader.IsDBNull para manejar valores nulos de forma segura.
                            ultimoAcceso = new BitacoraAcceso
                            {
                                IdAccesoPK = reader.IsDBNull(reader.GetOrdinal("id_acceso_PK")) ? 0 : reader.GetInt64(reader.GetOrdinal("id_acceso_PK")),
                                IdUsuarioFK = reader.IsDBNull(reader.GetOrdinal("id_usuario_FK")) ? "" : reader.GetString(reader.GetOrdinal("id_usuario_FK")),
                                FechaHoraAcceso = reader.IsDBNull(reader.GetOrdinal("fecha_hora_acceso")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("fecha_hora_acceso")),
                                EstadoAcceso = reader.IsDBNull(reader.GetOrdinal("estado_acceso")) ? "" : reader.GetString(reader.GetOrdinal("estado_acceso"))
                            };
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Error al ejecutar el procedimiento almacenado 'SelectUltimoAccesoUsuario': {ex.Message}");
                // En caso de error, devuelve null para indicar que la operación falló.
                return null;
            }
            finally
            {
                // Se asegura de que la conexión se cierre, incluso si ocurre un error.
                if (ConexionMetics.State == ConnectionState.Open)
                {
                    ConexionMetics.Close();
                }
            }

            // Devuelve el objeto de bitácora encontrado, o null si no se encontró ninguno.
            return ultimoAcceso;
        }
    }
}
