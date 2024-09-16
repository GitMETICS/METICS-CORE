using System.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;
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

    public List<SelectListItem> ObtenerGruposTemasSelectList()
    {
        var consulta = "SELECT * FROM grupo_tema;";
        var tablaResultado = EjecutarConsulta(consulta);

        return tablaResultado.AsEnumerable().Select(fila => new SelectListItem
        {
            Value = fila.Field<int>("id_grupo_FK").ToString(), // The value sent when the item is selected
            Text = $"Grupo: {fila.Field<int>("id_grupo_FK")}, Tema: {fila.Field<int>("id_tema_FK")}", // The display text
            Selected = false // Can be dynamically set if needed
        }).ToList();
    }
    public List<int> ObtenerIdsTemasDelGrupo(int idGrupo)
    {
        var gruposTemas = ObtenerGruposTemasPorGrupo(idGrupo);

        // Devolver solo los ID de los temas asociados al grupo
        return gruposTemas.Select(gt => gt.idTema).ToList();
    }

    public List<string> ObtenerNombresTemasDelGrupo(int idGrupo)
    {
        // Obtener relaciones grupo-tema por grupo
        var gruposTemas = ObtenerGruposTemasPorGrupo(idGrupo);

        // Obtener los temas relacionados con el grupo
        var temas = gruposTemas.Select(gt => accesoATema.ObtenerTema(gt.idTema)).ToList();

        // Devolver una lista de nombres de los temas
        return temas.Select(t => t.nombre).ToList();
    }

    // Obtener todas las relaciones para un grupo específico
    public List<TemaModel> ObtenerTemasDelGrupo(int idGrupo)
    {
        var gruposTemas = ObtenerGruposTemasPorGrupo(idGrupo);
        return gruposTemas.Select(gt => accesoATema.ObtenerTema(gt.idTema)).ToList();
    }
    public List<SelectListItem> ObtenerTemasDelGrupoSelectList(int idGrupo)
    {
        var gruposTemas = ObtenerGruposTemasPorGrupo(idGrupo); // Obtiene relaciones grupo-tema
        var temas = gruposTemas.Select(gt => accesoATema.ObtenerTema(gt.idTema)).ToList(); // Obtiene los temas del grupo

        // Transformar los temas en una lista de SelectListItem
        return temas.Select(t => new SelectListItem
        {
            Value = t.idTema.ToString(),  // El valor del SelectListItem será el ID del tema
            Text = t.nombre               // El texto del SelectListItem será el nombre del tema
        }).ToList();
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
        Debug.WriteLine("eliminacion afuera");


        try
        {
            // 1. Eliminar todos los temas asociados al grupo
            EliminarTemasPorGrupo(idGrupo);
            Debug.WriteLine("eliminacion.");


            // 2. Insertar los temas seleccionados
            foreach (var idTema in temasSeleccionados)
            {
                InsertarGrupoTema(idGrupo, idTema);
            }
            Debug.WriteLine("insercion.");


            return true;
        }
        catch (Exception)
        {

            return false;
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
