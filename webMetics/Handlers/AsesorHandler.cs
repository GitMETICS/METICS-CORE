using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace webMetics.Handlers
{
    public class AsesorHandler : BaseDeDatosHandler
    {
        public AsesorHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
        {
        }

        public List<AsesorModel> ObtenerAsesores()
        {
            List<AsesorModel> asesores = new List<AsesorModel>();
            string consulta = "SELECT * FROM asesor;";
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);

            DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
            foreach (DataRow fila in tablaResultado.Rows)
            {
                AsesorModel asesor = new AsesorModel
                {
                    idAsesor = Convert.ToString(fila["id_asesor_PK"]),
                    nombre = Convert.ToString(fila["nombre"]),
                    primerApellido = Convert.ToString(fila["apellido_1"]),
                    segundoApellido = Convert.ToString(fila["apellido_2"]),
                    tipoIdentificacion = Convert.ToString(fila["tipo_identificacion"]),
                    numeroIdentificacion = Convert.ToString(fila["numero_identificacion"]),
                    correo = Convert.ToString(fila["correo"]),
                    descripcion = Convert.ToString(fila["descripcion"]),
                    telefono = Convert.ToString(fila["telefono"])
                };

                asesores.Add(asesor);
            }
            return asesores;
        }

        public AsesorModel ObtenerAsesor(string idAsesor)
        {
            AsesorModel asesor = null;

            using (SqlCommand command = new SqlCommand("SelectAsesor", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idAsesor", idAsesor);

                ConexionMetics.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        asesor = new AsesorModel
                        {
                            idAsesor = reader.GetString(reader.GetOrdinal("id_asesor_PK")),
                            nombre = reader.GetString(reader.GetOrdinal("nombre")),
                            primerApellido = reader.GetString(reader.GetOrdinal("apellido_1")),
                            segundoApellido = reader.GetString(reader.GetOrdinal("apellido_2")),
                            tipoIdentificacion = reader.GetString(reader.GetOrdinal("tipo_identificacion")),
                            numeroIdentificacion = reader.GetString(reader.GetOrdinal("numero_identificacion")),
                            correo = reader.GetString(reader.GetOrdinal("correo")),
                            descripcion = reader.GetString(reader.GetOrdinal("descripcion")),
                            telefono = reader.GetString(reader.GetOrdinal("telefono"))
                        };
                    }
                }

                ConexionMetics.Close();
            }

            return asesor;
        }

        public List<SelectListItem> ObtenerListaSeleccionAsesores()
        {
            List<AsesorModel> asesores = ObtenerAsesores();
            List<SelectListItem> asesoresParseados = new List<SelectListItem>();

            foreach (AsesorModel asesor in asesores)
            {
                string primerApellido = asesor.primerApellido == null ? "" : asesor.primerApellido;
                string segundoApellido = asesor.segundoApellido == null ? "" : asesor.segundoApellido;

                asesoresParseados.Add(new SelectListItem { Text = asesor.nombre + " " + primerApellido + " " + segundoApellido, Value = Convert.ToString(asesor.idAsesor) });
            }

            return asesoresParseados;
        }

        /* public string ObtenerNombreAsesor(string nombreAsesor)
        {
            string consulta = "SELECT id_asesor_PK FROM asesor WHERE " +
                "nombre = @nombre AND apellido_1 = @apellido_1 AND apellido_2 = @apellido_2 ";

            // crear el comando de consulta con la consulta sql y la conexión establecida
            SqlCommand comandoparaconsulta = new SqlCommand(consulta, ConexionMetics);

            string[] temp = nombreAsesor.Split(' ');

            if (temp.Length == 4)
            {
                comandoparaconsulta.Parameters.AddWithValue("@nombre", temp[0] + " " + temp[1]);
            }
            else
            {
                comandoparaconsulta.Parameters.AddWithValue("@nombre", temp[0]);
            }

            comandoparaconsulta.Parameters.AddWithValue("@apellido_1", temp[^2]);
            comandoparaconsulta.Parameters.AddWithValue("@apellido_2", temp[^1]);

            DataTable tablaResultado = CrearTablaConsulta(comandoparaconsulta);


            return tablaResultado.Rows[0][0].ToString();
        } */

        // Verificar la existencia de un asesor en la base de datos
        public bool ExisteAsesor(string idAsesor)
        {
            bool existe = false;

            using (var command = new SqlCommand("ExistsAsesor", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idAsesor", idAsesor);
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
                    Console.WriteLine($"Error in ExisteAsesor: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return existe;
        }

        public bool CrearAsesor(AsesorModel asesor)
        {
            bool exito = false;
            using (var command = new SqlCommand("InsertAsesor", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idUsuario", asesor.idAsesor);
                command.Parameters.AddWithValue("@idAsesor", asesor.idAsesor);
                command.Parameters.AddWithValue("@nombre", asesor.nombre);
                command.Parameters.AddWithValue("@apellido1", asesor.primerApellido);
                command.Parameters.AddWithValue("@apellido2", asesor.segundoApellido);
                command.Parameters.AddWithValue("@tipoIdentificacion", asesor.tipoIdentificacion);
                command.Parameters.AddWithValue("@numeroIdentificacion", asesor.numeroIdentificacion);
                command.Parameters.AddWithValue("@correo", asesor.correo);
                command.Parameters.AddWithValue("@descripcion", asesor.descripcion);
                command.Parameters.AddWithValue("@telefono", asesor.telefono);

                try
                {
                    ConexionMetics.Open();
                    exito = command.ExecuteNonQuery() >= 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CrearAsesor: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return exito;
        }

        public bool EditarAsesor(AsesorModel asesor)
        {
            bool exito = false;
            using (var command = new SqlCommand("UpdateAsesor", ConexionMetics))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@idUsuario", asesor.idAsesor);
                command.Parameters.AddWithValue("@idAsesor", asesor.idAsesor);
                command.Parameters.AddWithValue("@nombre", asesor.nombre);
                command.Parameters.AddWithValue("@apellido1", asesor.primerApellido);
                command.Parameters.AddWithValue("@apellido2", asesor.segundoApellido);
                command.Parameters.AddWithValue("@tipoIdentificacion", asesor.tipoIdentificacion);
                command.Parameters.AddWithValue("@numeroIdentificacion", asesor.numeroIdentificacion);
                command.Parameters.AddWithValue("@correo", asesor.correo);
                command.Parameters.AddWithValue("@descripcion", asesor.descripcion);
                command.Parameters.AddWithValue("@telefono", asesor.telefono);

                try
                {
                    ConexionMetics.Open();
                    exito = command.ExecuteNonQuery() >= 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in EditarAsesor: {ex.Message}");
                }
                finally
                {
                    ConexionMetics.Close();
                }
            }

            return exito;
        }
        
        public bool EliminarAsesor(string idAsesor)
        {
            string consulta = "DELETE FROM asesor WHERE id_asesor_PK = @idAsesor";

            SqlCommand comandoParaConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoParaConsulta.Parameters.AddWithValue("@idAsesor", idAsesor);

            ConexionMetics.Open();

            bool exito = comandoParaConsulta.ExecuteNonQuery() >= 1;

            ConexionMetics.Close();

            return exito;
        }
    }
}