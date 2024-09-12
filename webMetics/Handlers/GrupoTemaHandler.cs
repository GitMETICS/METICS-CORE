using System.Data;
using Microsoft.Data.SqlClient;
using webMetics.Handlers;
using webMetics.Models;

public class GrupoTemaHandler : BaseDeDatosHandler
{
    private TemaHandler accesoATema;

    public GrupoTemaHandler(IWebHostEnvironment environment, IConfiguration configuration, TemaHandler temaHandler)
        : base(environment, configuration)
    {
        accesoATema = temaHandler;
    }

    // Método auxiliar para ejecutar consultas y devolver DataTable
    private DataTable EjecutarConsulta(string consulta, SqlParameter[] parametros = null)
    {
        using (var comando = new SqlCommand(consulta, ConexionMetics))
        {
            if (parametros != null)
                comando.Parameters.AddRange(parametros);

            using (var adaptador = new SqlDataAdapter(comando))
            {
                var tablaResultado = new DataTable();
                adaptador.Fill(tablaResultado);
                return tablaResultado;
            }
        }
    }

    // Obtener todos los registros de la tabla grupo_tema
    public List<GrupoTemaModel> ObtenerGruposTemas()
    {
        var consulta = "SELECT * FROM grupo_tema;";
        var tablaResultado = EjecutarConsulta(consulta);

        return tablaResultado.AsEnumerable().Select(fila => new GrupoTemaModel
        {
            idGrupo = fila.Field<int>("id_grupo_FK"),
            idTema = fila.Field<int>("id_tema_FK")
        }).ToList();
    }

    // Obtener todas las relaciones para un grupo específico
    public List<TemaModel> ObtenerTemasDelGrupo(int idGrupo)
    {
        var gruposTemas = ObtenerGruposTemasPorGrupo(idGrupo);
        return gruposTemas.Select(gt => accesoATema.ObtenerTema(gt.idTema)).ToList();
    }

    // Método auxiliar para obtener las relaciones grupo-tema por grupo
    private List<GrupoTemaModel> ObtenerGruposTemasPorGrupo(int idGrupo)
    {
        var consulta = "SELECT * FROM grupo_tema WHERE id_grupo_FK = @idGrupo;";
        var parametros = new[] { new SqlParameter("@idGrupo", idGrupo) };
        var tablaResultado = EjecutarConsulta(consulta, parametros);

        return tablaResultado.AsEnumerable().Select(fila => new GrupoTemaModel
        {
            idGrupo = fila.Field<int>("id_grupo_FK"),
            idTema = fila.Field<int>("id_tema_FK")
        }).ToList();
    }

    // Obtener todos los grupos relacionados con un tema específico
    public List<GrupoTemaModel> ObtenerGruposDelTema(int idTema)
    {
        var consulta = "SELECT * FROM grupo_tema WHERE id_tema_FK = @idTema;";
        var parametros = new[] { new SqlParameter("@idTema", idTema) };
        var tablaResultado = EjecutarConsulta(consulta, parametros);

        return tablaResultado.AsEnumerable().Select(fila => new GrupoTemaModel
        {
            idGrupo = fila.Field<int>("id_grupo_FK"),
            idTema = fila.Field<int>("id_tema_FK")
        }).ToList();
    }

    // Insertar una nueva relación entre grupo y tema
    public bool InsertarGrupoTema(int idGrupo, int idTema)
    {
        var consulta = "INSERT INTO grupo_tema (id_grupo_FK, id_tema_FK) VALUES (@idGrupo, @idTema);";
        var parametros = new[]
        {
            new SqlParameter("@idGrupo", idGrupo),
            new SqlParameter("@idTema", idTema)
        };

        return EjecutarComando(consulta, parametros);
    }

    // Eliminar una relación entre grupo y tema
    public bool EliminarGrupoTema(int idGrupo, int idTema)
    {
        var consulta = "DELETE FROM grupo_tema WHERE id_grupo_FK = @idGrupo AND id_tema_FK = @idTema;";
        var parametros = new[]
        {
            new SqlParameter("@idGrupo", idGrupo),
            new SqlParameter("@idTema", idTema)
        };

        return EjecutarComando(consulta, parametros);
    }

    // Actualizar las relaciones de temas por grupo
    public bool ActualizarTemasPorGrupo(int idGrupo, int[] temasSeleccionados)
    {
        using (var transaccion = ConexionMetics.BeginTransaction())
        {
            try
            {
                // 1. Eliminar todos los temas asociados al grupo
                EliminarTemasPorGrupo(idGrupo);

                // 2. Insertar los temas seleccionados
                foreach (var idTema in temasSeleccionados)
                {
                    InsertarGrupoTema(idGrupo, idTema);
                }

                transaccion.Commit();
                return true;
            }
            catch (Exception)
            {
                transaccion.Rollback();
                return false;
            }
        }
    }

    // Método para eliminar todos los temas de un grupo específico
    private bool EliminarTemasPorGrupo(int idGrupo)
    {
        var consulta = "DELETE FROM grupo_tema WHERE id_grupo_FK = @idGrupo;";
        var parametros = new[] { new SqlParameter("@idGrupo", idGrupo) };

        return EjecutarComando(consulta, parametros);
    }

    // Método auxiliar para ejecutar comandos SQL
    private bool EjecutarComando(string consulta, SqlParameter[] parametros = null)
    {
        using (var comando = new SqlCommand(consulta, ConexionMetics))
        {
            if (parametros != null)
                comando.Parameters.AddRange(parametros);

            ConexionMetics.Open();
            var exito = comando.ExecuteNonQuery() >= 1;
            ConexionMetics.Close();

            return exito;
        }
    }
}
