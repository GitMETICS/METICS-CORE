using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Models;
using webMetics.Handlers;

public class InscripcionHandler : BaseDeDatosHandler
{
    private protected GrupoHandler accesoAGrupo;

    public InscripcionHandler(IWebHostEnvironment environment, IConfiguration configuration) : base(environment, configuration)
    {
        accesoAGrupo = new GrupoHandler(environment, configuration);
    }

    public List<InscripcionModel> ObtenerInscripciones()
    {
        List<InscripcionModel> inscripciones = new List<InscripcionModel>();

        string consulta = "SELECT * FROM inscripcion;";

        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);

        DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
        foreach (DataRow fila in tablaResultado.Rows)
        {
            InscripcionModel inscripcion = new InscripcionModel
            {
                idInscripcion = Convert.ToInt32(fila["id_inscripcion_PK"]),
                idParticipante = Convert.ToString(fila["id_participante_FK"]),
                idGrupo = Convert.ToInt32(fila["id_grupo_FK"]),
                numeroGrupo = Convert.ToInt32(fila["numero_grupo"]),
                nombreGrupo = Convert.ToString(fila["nombre_grupo"]),
                estado = Convert.ToString(fila["estado"]),
                observaciones = Convert.ToString(fila["observaciones"]),
                horasAprobadas = Convert.ToInt32(fila["horas_aprobadas"]),
                horasMatriculadas = Convert.ToInt32(fila["horas_matriculadas"]),
                calificacion = Convert.ToDouble(fila["calificacion"])
            };

            inscripcion.estado = CambiarEstadoDeInscripcion(inscripcion);
            inscripciones.Add(inscripcion);
        }

        return inscripciones;
    }

    public async Task<PagedResult<InscripcionListadoModel>> ObtenerInscripcionesPaginadasAsync(
        int offset,
        int pageSize,
        string? searchTerm,
        string sortColumn,
        string sortDirection)
    {
        PagedResult<InscripcionListadoModel> resultado = new PagedResult<InscripcionListadoModel>();

        using SqlCommand comando = new SqlCommand("SelectInscripcionesPaginadas", ConexionMetics)
        {
            CommandType = CommandType.StoredProcedure
        };

        comando.Parameters.Add("@Offset", SqlDbType.Int).Value = offset;
        comando.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
        comando.Parameters.Add("@SearchTerm", SqlDbType.NVarChar, 512).Value =
            string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm.Trim();
        comando.Parameters.Add("@SortColumn", SqlDbType.NVarChar, 32).Value = sortColumn;
        comando.Parameters.Add("@SortDirection", SqlDbType.NVarChar, 4).Value = sortDirection;

        try
        {
            await ConexionMetics.OpenAsync();
            using SqlDataReader reader = await comando.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                resultado.TotalRecords = reader.GetInt32(reader.GetOrdinal("TotalRecords"));
            }

            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                resultado.FilteredRecords = reader.GetInt32(reader.GetOrdinal("FilteredRecords"));
            }

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    resultado.Items.Add(new InscripcionListadoModel
                    {
                        IdInscripcion = reader.GetInt32(reader.GetOrdinal("IdInscripcion")),
                        IdParticipante = reader.GetString(reader.GetOrdinal("IdParticipante")),
                        Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                        PrimerApellido = reader.GetString(reader.GetOrdinal("PrimerApellido")),
                        SegundoApellido = reader.IsDBNull(reader.GetOrdinal("SegundoApellido"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("SegundoApellido")),
                        Correo = reader.GetString(reader.GetOrdinal("Correo")),
                        UnidadAcademica = reader.IsDBNull(reader.GetOrdinal("UnidadAcademica"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("UnidadAcademica")),
                        NombreGrupo = reader.IsDBNull(reader.GetOrdinal("NombreGrupo"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("NombreGrupo")),
                        NumeroGrupo = reader.IsDBNull(reader.GetOrdinal("NumeroGrupo"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("NumeroGrupo")),
                        Estado = reader.GetString(reader.GetOrdinal("Estado")),
                        HorasMatriculadas = reader.GetInt32(reader.GetOrdinal("HorasMatriculadas")),
                        HorasAprobadas = reader.GetInt32(reader.GetOrdinal("HorasAprobadas"))
                    });
                }
            }
        }
        finally
        {
            ConexionMetics.Close();
        }

        return resultado;
    }

    public async Task<List<InscripcionModel>> ObtenerInscripcionesAsync()
    {
        List<InscripcionModel> inscripciones = new List<InscripcionModel>();
        string consulta = "SELECT * FROM inscripcion;";

        using (SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics))
        {
            try
            {
                await ConexionMetics.OpenAsync();

                using (SqlDataReader reader = await comandoConsulta.ExecuteReaderAsync())
                {
                    InscripcionModel inscripcion = new InscripcionModel
                    {
                        idInscripcion = reader.GetInt32(reader.GetOrdinal("id_inscripcion_PK")),
                        idParticipante = reader.GetString(reader.GetOrdinal("id_participante_FK")),
                        idGrupo = reader.GetInt32(reader.GetOrdinal("id_grupo_FK")),
                        numeroGrupo = reader.GetInt32(reader.GetOrdinal("numero_grupo")),
                        nombreGrupo = reader.GetString(reader.GetOrdinal("nombre_grupo")),
                        estado = reader.GetString(reader.GetOrdinal("estado")),
                        observaciones = !reader.IsDBNull(reader.GetOrdinal("observaciones")) ? reader.GetString(reader.GetOrdinal("observaciones")) : null,
                        horasAprobadas = reader.GetInt32(reader.GetOrdinal("horas_aprobadas")),
                        horasMatriculadas = reader.GetInt32(reader.GetOrdinal("horas_matriculadas")),
                        calificacion = reader.GetDouble(reader.GetOrdinal("calificacion"))
                    };

                    inscripcion.estado = CambiarEstadoDeInscripcion(inscripcion);
                    inscripciones.Add(inscripcion);
                }
            }
            catch (Exception ex)
            {
                
            }
            finally
            {
                ConexionMetics.Close();
            }
        }

        return inscripciones;
    }

    public List<InscripcionModel> ObtenerInscripcionesDelGrupo(int idGrupo)
    {
        List<InscripcionModel> inscripciones = new List<InscripcionModel>();

        string consulta = "SELECT * FROM inscripcion WHERE id_grupo_FK = @idGrupo;";
        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
        comandoConsulta.Parameters.AddWithValue("@idGrupo", idGrupo);

        DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
        foreach (DataRow fila in tablaResultado.Rows)
        {
            InscripcionModel inscripcion = new InscripcionModel
            {
                idInscripcion = Convert.ToInt32(fila["id_inscripcion_PK"]),
                idParticipante = Convert.ToString(fila["id_participante_FK"]),
                idGrupo = Convert.ToInt32(fila["id_grupo_FK"]),
                numeroGrupo = Convert.ToInt32(fila["numero_grupo"]),
                nombreGrupo = Convert.ToString(fila["nombre_grupo"]),
                estado = Convert.ToString(fila["estado"]),
                observaciones = Convert.ToString(fila["observaciones"]),
                horasAprobadas = Convert.ToInt32(fila["horas_aprobadas"]),
                horasMatriculadas = Convert.ToInt32(fila["horas_matriculadas"]),
                calificacion = Convert.ToDouble(fila["calificacion"]),
            };

            inscripcion.estado = CambiarEstadoDeInscripcion(inscripcion);
            inscripciones.Add(inscripcion);
        }
        return inscripciones;
    }

    public List<InscripcionModel> ObtenerInscripcionesParticipante(string idParticipante)
    {
        List<InscripcionModel> inscripciones = new List<InscripcionModel>();

        string consulta = "SELECT * FROM inscripcion WHERE id_participante_FK = @idParticipante;";
        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
        comandoConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);

        DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
        foreach (DataRow fila in tablaResultado.Rows)
        {
            InscripcionModel inscripcion = new InscripcionModel
            {
                idInscripcion = Convert.ToInt32(fila["id_inscripcion_PK"]),
                idParticipante = Convert.ToString(fila["id_participante_FK"]),
                idGrupo = Convert.ToInt32(fila["id_grupo_FK"]),
                numeroGrupo = Convert.ToInt32(fila["numero_grupo"]),
                nombreGrupo = Convert.ToString(fila["nombre_grupo"]),
                estado = Convert.ToString(fila["estado"]),
                observaciones = Convert.ToString(fila["observaciones"]),
                horasAprobadas = Convert.ToInt32(fila["horas_aprobadas"]),
                horasMatriculadas = Convert.ToInt32(fila["horas_matriculadas"]),
                calificacion = Convert.ToDouble(fila["calificacion"])
            };

            inscripcion.estado = CambiarEstadoDeInscripcion(inscripcion);
            inscripciones.Add(inscripcion);
        }
        return inscripciones;
    }

    public InscripcionModel ObtenerInscripcionParticipante(int idGrupo, string idParticipante)
    {
        string consulta = "SELECT * FROM inscripcion WHERE id_grupo_FK = @idGrupo AND id_participante_FK = @idParticipante;";
        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
        comandoConsulta.Parameters.AddWithValue("@idGrupo", idGrupo);
        comandoConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);

        DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
        DataRow fila = tablaResultado.Rows[0];

        InscripcionModel inscripcion = new InscripcionModel
        {
            idInscripcion = Convert.ToInt32(fila["id_inscripcion_PK"]),
            idParticipante = Convert.ToString(fila["id_participante_FK"]),
            idGrupo = Convert.ToInt32(fila["id_grupo_FK"]),
            numeroGrupo = Convert.ToInt32(fila["numero_grupo"]),
            nombreGrupo = Convert.ToString(fila["nombre_grupo"]),
            estado = Convert.ToString(fila["estado"]),
            observaciones = Convert.ToString(fila["observaciones"]),
            horasAprobadas = Convert.ToInt32(fila["horas_aprobadas"]),
            horasMatriculadas = Convert.ToInt32(fila["horas_matriculadas"]),
            calificacion = Convert.ToDouble(fila["calificacion"])
        };

        inscripcion.estado = CambiarEstadoDeInscripcion(inscripcion);

        return inscripcion;
    }

    public InscripcionModel ObtenerInscripcionDeGrupoInexistenteParticipante(string nombreGrupo, int numeroGrupo, string idParticipante)
    {
        string consulta = "SELECT * FROM inscripcion WHERE nombre_grupo = @nombreGrupo AND numero_grupo = @numeroGrupo AND id_participante_FK = @idParticipante;";
        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
        comandoConsulta.Parameters.AddWithValue("@nombreGrupo", nombreGrupo);
        comandoConsulta.Parameters.AddWithValue("@numeroGrupo", numeroGrupo);
        comandoConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);

        DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);
        DataRow fila = tablaResultado.Rows[0];

        InscripcionModel inscripcion = new InscripcionModel
        {
            idInscripcion = Convert.ToInt32(fila["id_inscripcion_PK"]),
            idParticipante = Convert.ToString(fila["id_participante_FK"]),
            idGrupo = Convert.ToInt32(fila["id_grupo_FK"]),
            numeroGrupo = Convert.ToInt32(fila["numero_grupo"]),
            nombreGrupo = Convert.ToString(fila["nombre_grupo"]),
            estado = Convert.ToString(fila["estado"]),
            observaciones = Convert.ToString(fila["observaciones"]),
            horasAprobadas = Convert.ToInt32(fila["horas_aprobadas"]),
            horasMatriculadas = Convert.ToInt32(fila["horas_matriculadas"]),
            calificacion = Convert.ToDouble(fila["calificacion"])
        };

        inscripcion.estado = CambiarEstadoDeInscripcion(inscripcion);

        return inscripcion;
    }

    public bool InsertarInscripcion(InscripcionModel inscripcion)
    {
        string consulta = "INSERT INTO inscripcion (id_grupo_FK, id_participante_FK, numero_grupo, nombre_grupo, estado, observaciones, horas_aprobadas, horas_matriculadas)" +
                          "VALUES (@idGrupo, @idParticipante, @numeroGrupo, @nombreGrupo, @estado, @observaciones, @horasAprobadas, @horasMatriculadas);";

        ConexionMetics.Open();

        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
        comandoConsulta.Parameters.AddWithValue("@idGrupo", inscripcion.idGrupo);
        comandoConsulta.Parameters.AddWithValue("@idParticipante", inscripcion.idParticipante);
        comandoConsulta.Parameters.AddWithValue("@numeroGrupo", inscripcion.numeroGrupo);
        comandoConsulta.Parameters.AddWithValue("@nombreGrupo", inscripcion.nombreGrupo);
        comandoConsulta.Parameters.AddWithValue("@horasAprobadas", inscripcion.horasAprobadas);
        comandoConsulta.Parameters.AddWithValue("@horasMatriculadas", inscripcion.horasMatriculadas);
        comandoConsulta.Parameters.AddWithValue("@estado", inscripcion.estado);
        comandoConsulta.Parameters.AddWithValue("@observaciones", "");

        bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

        ConexionMetics.Close();

        return exito;
    }

    public async Task<bool> InsertarInscripcionAsync(InscripcionModel inscripcion)
    {
        string consulta = "INSERT INTO inscripcion (id_grupo_FK, id_participante_FK, numero_grupo, nombre_grupo, estado, observaciones, horas_aprobadas, horas_matriculadas)" +
                          "VALUES (@idGrupo, @idParticipante, @numeroGrupo, @nombreGrupo, @estado, @observaciones, @horasAprobadas, @horasMatriculadas);";

        using (SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics))
        {
            comandoConsulta.Parameters.AddWithValue("@idGrupo", inscripcion.idGrupo);
            comandoConsulta.Parameters.AddWithValue("@idParticipante", inscripcion.idParticipante);
            comandoConsulta.Parameters.AddWithValue("@numeroGrupo", inscripcion.numeroGrupo);
            comandoConsulta.Parameters.AddWithValue("@nombreGrupo", inscripcion.nombreGrupo);
            comandoConsulta.Parameters.AddWithValue("@horasAprobadas", inscripcion.horasAprobadas);
            comandoConsulta.Parameters.AddWithValue("@horasMatriculadas", inscripcion.horasMatriculadas);
            comandoConsulta.Parameters.AddWithValue("@estado", inscripcion.estado);
            comandoConsulta.Parameters.AddWithValue("@observaciones", "");

            try
            {
                await ConexionMetics.OpenAsync(); // Asynchronously open the connection
                int rowsAffected = await comandoConsulta.ExecuteNonQueryAsync(); // Asynchronously execute the command
                return rowsAffected >= 1;
            }
            finally
            {
                ConexionMetics.Close(); // Ensure the connection is closed even if an exception occurs
            }
        }
    }

    public bool EditarInscripcion(InscripcionModel inscripcion)
    {
        string consulta = "UPDATE inscripcion SET id_grupo_FK = @idGrupo, numero_grupo = @numeroGrupo, nombre_grupo = @nombreGrupo, " +
            "id_participante_FK = @idParticipante, estado = @estado, observaciones = @observaciones, " +
            "horas_aprobadas = @horasAprobadas, horas_matriculadas = @horasMatriculadas " +
            "WHERE id_inscripcion_PK = @idInscripcion";

        ConexionMetics.Open();

        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
        comandoConsulta.Parameters.AddWithValue("@idInscripcion", inscripcion.idInscripcion);
        comandoConsulta.Parameters.AddWithValue("@idGrupo", inscripcion.idGrupo);
        comandoConsulta.Parameters.AddWithValue("@numeroGrupo", inscripcion.numeroGrupo);
        comandoConsulta.Parameters.AddWithValue("@nombreGrupo", inscripcion.nombreGrupo);
        comandoConsulta.Parameters.AddWithValue("@idParticipante", inscripcion.idParticipante);
        comandoConsulta.Parameters.AddWithValue("@horasAprobadas", inscripcion.horasAprobadas);
        comandoConsulta.Parameters.AddWithValue("@horasMatriculadas", inscripcion.horasMatriculadas);
        comandoConsulta.Parameters.AddWithValue("@estado", inscripcion.estado);
        comandoConsulta.Parameters.AddWithValue("@observaciones", "");

        bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

        ConexionMetics.Close();

        return exito;
    }

    public async Task<bool> EditarInscripcionAsync(InscripcionModel inscripcion)
    {
        string consulta = "UPDATE inscripcion SET id_grupo_FK = @idGrupo, numero_grupo = @numeroGrupo, nombre_grupo = @nombreGrupo, " +
                          "id_participante_FK = @idParticipante, estado = @estado, observaciones = @observaciones, " +
                          "horas_aprobadas = @horasAprobadas, horas_matriculadas = @horasMatriculadas, " +
                          "calificacion = @calificacion " +
                          "WHERE id_inscripcion_PK = @idInscripcion";

        using (SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics))
        {
            comandoConsulta.Parameters.AddWithValue("@idInscripcion", inscripcion.idInscripcion);
            comandoConsulta.Parameters.AddWithValue("@idGrupo", inscripcion.idGrupo);
            comandoConsulta.Parameters.AddWithValue("@numeroGrupo", inscripcion.numeroGrupo);
            comandoConsulta.Parameters.AddWithValue("@nombreGrupo", inscripcion.nombreGrupo);
            comandoConsulta.Parameters.AddWithValue("@idParticipante", inscripcion.idParticipante);
            comandoConsulta.Parameters.AddWithValue("@horasAprobadas", inscripcion.horasAprobadas);
            comandoConsulta.Parameters.AddWithValue("@horasMatriculadas", inscripcion.horasMatriculadas);
            comandoConsulta.Parameters.AddWithValue("@estado", inscripcion.estado);
            comandoConsulta.Parameters.AddWithValue("@calificacion", inscripcion.calificacion);
            comandoConsulta.Parameters.AddWithValue("@observaciones", "");

            try
            {
                await ConexionMetics.OpenAsync();
                int rowsAffected = await comandoConsulta.ExecuteNonQueryAsync();
                return rowsAffected >= 1;
            }
            finally
            {
                ConexionMetics.Close();
            }
        }
    }

    public bool EliminarInscripcion(string nombreGrupo, int numeroGrupo, string idParticipante)
    {
        string consulta = "DELETE FROM inscripcion WHERE nombre_grupo = @nombreGrupo AND numero_grupo = @numeroGrupo AND id_participante_FK = @idParticipante;";

        ConexionMetics.Open();

        SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);

        comandoConsulta.Parameters.AddWithValue("@nombreGrupo", nombreGrupo);
        comandoConsulta.Parameters.AddWithValue("@numeroGrupo", numeroGrupo);
        comandoConsulta.Parameters.AddWithValue("@idParticipante", idParticipante);

        bool exito = comandoConsulta.ExecuteNonQuery() >= 1;

        ConexionMetics.Close();

        return exito;
    }

    public string ObtenerCorreoLimiteHoras()
    {
        string consulta = "SELECT correo FROM correo_notificacion;";
        string correo = string.Empty;

        using (SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics))
        {
            DataTable tablaResultado = CrearTablaConsulta(comandoConsulta);

            if (tablaResultado.Rows.Count > 0)
            {
                DataRow fila = tablaResultado.Rows[0];
                correo = Convert.ToString(fila["correo"]);
            }
        }

        return correo;
    }

    public bool IngresarCorreoLimiteHoras(string correo)
    {
        string consulta = "INSERT INTO correo_notificacion (correo) VALUES (@correo);";

        ConexionMetics.Open();

        bool exito;
        try
        {
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@correo", correo);

            exito = comandoConsulta.ExecuteNonQuery() >= 1;
        }
        catch (Exception ex)
        {
            exito = false; // No viene ningún correo en la consulta.
        }

        ConexionMetics.Close();

        return exito;
    }

    public bool ActualizarCorreoLimiteHoras(string correo)
    {
        string consulta = "UPDATE correo_notificacion SET correo = @correo;";

        ConexionMetics.Open();

        bool exito;
        try
        {
            SqlCommand comandoConsulta = new SqlCommand(consulta, ConexionMetics);
            comandoConsulta.Parameters.AddWithValue("@correo", correo);

            exito = comandoConsulta.ExecuteNonQuery() >= 1;
        } 
        catch (Exception ex) 
        {
            exito = false; // No viene ningún correo en la consulta.
        }

        ConexionMetics.Close();

        return exito;
    }

    public bool NoEstaInscritoEnGrupo(int idGrupo, string idParticipante)
    {
        bool noEstaInscrito = false;

        List<InscripcionModel> listaInscritos = ObtenerInscripcionesDelGrupo(idGrupo);

        if (listaInscritos == null ||
            listaInscritos.Find(inscripcionModel => inscripcionModel.idParticipante == idParticipante) == null)
        {
            noEstaInscrito = true;
        }

        return noEstaInscrito;
    }

    public string CambiarEstadoDeInscripcion(InscripcionModel inscripcion)
    {
        string estado;

        if (inscripcion.horasMatriculadas <= 0 && inscripcion.horasAprobadas > 0)
        {
            estado = "Aprobado";
        }
        else
        {
            if (inscripcion.horasAprobadas > 0)
            {
                estado = "Incompleto";
            }
            else
            {
                estado = "Inscrito";
            }
        }

        return estado;
    }

}

