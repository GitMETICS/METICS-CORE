using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;

namespace webMetics.Helpers;

public sealed record BreadcrumbItem(string Label, string? Url = null);

/// <summary>Resolves page ancestry using existing read-only destinations; never executes data access.</summary>
public static class BreadcrumbBuilder
{
    private static readonly HashSet<string> Origins = new(StringComparer.OrdinalIgnoreCase)
    {
        "participants", "inscriptions", "account"
    };

    public static IReadOnlyList<BreadcrumbItem> Build(ViewContext context, IUrlHelper url)
    {
        if (string.IsNullOrWhiteSpace(Convert.ToString(context.ViewData["Id"])))
            return Array.Empty<BreadcrumbItem>();

        var controller = Convert.ToString(context.RouteData.Values["controller"]) ?? "";
        var action = Convert.ToString(context.RouteData.Values["action"]) ?? "";
        int.TryParse(Convert.ToString(context.ViewData["Role"]), out var role);
        var key = $"{controller}/{action}".ToLowerInvariant();
        key = key switch
        {
            "grupo/buscargrupos" => "grupo/listagruposdisponibles",
            "asesor/buscargrupos" => "asesor/mismodulos",
            "asesor/agregarasesor" => "asesor/crearasesor",
            "asesor/actualizarasesor" => "asesor/editarasesor",
            "participante/buscarparticipantes" => "participante/verparticipantes",
            "participante/buscarparticipantesdelgrupo" => "participante/listaparticipantes",
            "participante/actualizarparticipante" => "participante/editarparticipante",
            "inscripcion/buscarinscripciones" => "inscripcion/verinscripciones",
            "usuario/verbitacoraaccesousuario" or "usuario/verbitacoraaccesoporfecha"
                or "usuario/verbitacoraultimoaccesousuario" => "usuario/verbitacoraaccesos",
            _ => key
        };

        string? Value(string name)
        {
            foreach (var candidate in new[] { context.HttpContext.Request.Query[name].FirstOrDefault(),
                Convert.ToString(context.RouteData.Values[name]), Convert.ToString(context.ViewData[name]) })
                if (!string.IsNullOrWhiteSpace(candidate)) return candidate;
            foreach (var model in new[] { context.ViewData.Model, context.ViewData["Grupo"],
                context.ViewData["Calificacion"], context.ViewData["Participante"], context.ViewData["Asesor"] })
            {
                var value = Convert.ToString(Property(model, name));
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            return null;
        }

        string? Link(string targetController, string targetAction, params (string Key, string? Value)[] parameters)
        {
            if (parameters.Any(p => string.IsNullOrWhiteSpace(p.Value))) return null;
            var values = new RouteValueDictionary { ["id"] = null };
            foreach (var parameter in parameters) values[parameter.Key] = parameter.Value;
            return url.Action(targetAction, targetController, values);
        }

        var items = new List<BreadcrumbItem>
        {
            new("Inicio", Link("Grupo", "ListaGruposDisponibles"))
        };
        void Group(string label) => items.Add(new(label));
        void Page(string label, string targetController, string targetAction,
            params (string Key, string? Value)[] parameters) =>
            items.Add(new(label, Link(targetController, targetAction, parameters)));
        void Current(string label) => items.Add(new(label));
        void Account()
        {
            Group("Mi cuenta");
            Page("Información personal", "Usuario", "InformacionPersonal");
        }
        void Participants()
        {
            Group("Personas");
            Page("Participantes", "Participante", "VerParticipantes");
        }
        void Module()
        {
            if (role == 2) Page("Mis módulos", "Asesor", "MisModulos");
            var moduleName = Convert.ToString(context.ViewData["NombreGrupo"]);
            if (string.IsNullOrWhiteSpace(moduleName))
                moduleName = Convert.ToString(Property(context.ViewData["Grupo"], "nombre"));
            if (string.IsNullOrWhiteSpace(moduleName) && context.ViewData.Model?.GetType().Name == "GrupoModel")
                moduleName = Convert.ToString(Property(context.ViewData.Model, "nombre"));
            Group(string.IsNullOrWhiteSpace(moduleName) ? "Módulo" : moduleName);
        }

        var origin = GetOrigin(context);
        var idGrupo = Value("idGrupo");
        switch (key)
        {
            case "grupo/listagruposdisponibles":
                return new[] { new BreadcrumbItem("Inicio") };
            case "categoria/listacategorias" when role == 1:
            case "categoria/crearcategoria" when role == 1:
            case "categoria/editarcategoria" when role == 1:
                Group("Gestión académica");
                if (key.EndsWith("listacategorias")) Current("Niveles");
                else
                {
                    Page("Niveles", "Categoria", "ListaCategorias");
                    Current(key.EndsWith("crearcategoria") ? "Crear nivel" : "Editar nivel");
                }
                break;
            case "tema/listatemas" when role == 1:
            case "tema/creartema" when role == 1:
            case "tema/editartema" when role == 1:
                Group("Gestión académica");
                if (key.EndsWith("listatemas")) Current("Áreas de competencia");
                else
                {
                    Page("Áreas de competencia", "Tema", "ListaTemas");
                    Current(key.EndsWith("creartema") ? "Crear área de competencia" : "Editar área de competencia");
                }
                break;
            case "asesor/listaasesores" when role == 1:
            case "asesor/crearasesor" when role == 1:
                Group("Personas");
                if (key.EndsWith("listaasesores")) Current("Facilitadores(as)");
                else
                {
                    Page("Facilitadores(as)", "Asesor", "ListaAsesores");
                    Current("Crear facilitador(a)");
                }
                break;
            case "asesor/editarasesor":
                if (role == 1 && origin != "account")
                {
                    Group("Personas");
                    Page("Facilitadores(as)", "Asesor", "ListaAsesores");
                }
                else Account();
                Current("Editar facilitador(a)");
                break;
            case "asesor/mismodulos" when role is 1 or 2:
                Current("Mis módulos");
                break;
            case "participante/verparticipantes" when role == 1:
                Group("Personas");
                Current("Participantes");
                break;
            case "participante/formularioparticipante" when role == 1:
                Participants();
                Current("Agregar participante");
                break;
            case "participante/editarparticipante":
                if (role == 1 && origin != "account") Participants();
                else Account();
                Current("Editar participante");
                break;
            case "participante/verdatosparticipante" when role is 1 or 2:
                if (!string.IsNullOrWhiteSpace(idGrupo))
                {
                    Module();
                    Page("Participantes", "Participante", "ListaParticipantes", ("idGrupo", idGrupo));
                }
                else if (role == 1 && origin != "participants")
                    Page("Inscripciones", "Inscripcion", "VerInscripciones");
                else if (role == 1) Participants();
                else Page("Mis módulos", "Asesor", "MisModulos");
                var person = context.ViewData["Participante"] ?? context.ViewData.Model;
                var personName = string.Join(" ", new[] { "nombre", "primerApellido", "segundoApellido" }
                    .Select(p => Convert.ToString(Property(person, p)))
                    .Where(p => !string.IsNullOrWhiteSpace(p)));
                Current(string.IsNullOrWhiteSpace(personName) ? "Detalle del participante" : personName);
                break;
            case "grupo/creargrupo" when role is 1 or 2:
                if (role == 1) Group("Contenido");
                else Page("Mis módulos", "Asesor", "MisModulos");
                Current("Crear módulo");
                break;
            case "grupo/editargrupo" when role is 1 or 2:
                Module();
                Current("Editar módulo");
                break;
            case "grupo/veradjunto":
            case "grupo/editaradjunto" when role is 1 or 2:
                Module();
                Current(key.EndsWith("editaradjunto") ? "Editar programa del módulo" : "Programa del módulo");
                break;
            case "participante/listaparticipantes" when role is 1 or 2:
                Module();
                Current("Participantes");
                break;
            case "inscripcion/formularioinscripcionmanual" when role is 1 or 2:
                Module();
                Current("Inscribir participante");
                break;
            case "inscripcion/verinscripciones" when role == 1:
                Current("Lista de inscripciones");
                break;
            case "inscripcion/formularioinscripcion" when role is 1 or 2:
                if (role == 1) Page("Inscripciones", "Inscripcion", "VerInscripciones");
                else Page("Mis módulos", "Asesor", "MisModulos");
                Current("Formulario de inscripción");
                break;
            case "inscripcion/inscribir":
                Current("Resultado de inscripción");
                break;
            case "calificaciones/vercalificaciones" when role is 1 or 2:
            case "calificaciones/editarcalificacion" when role is 1 or 2:
                Module();
                Page("Participantes", "Participante", "ListaParticipantes", ("idGrupo", idGrupo));
                if (key.EndsWith("vercalificaciones")) Current("Calificaciones");
                else
                {
                    Page("Calificaciones", "Calificaciones", "VerCalificaciones", ("idGrupo", idGrupo));
                    Current("Editar calificación");
                }
                break;
            case "medallas/listamedallas" when role == 1:
            case "medallas/editarmedalla" when role == 1:
                Group("Contenido");
                if (key.EndsWith("listamedallas")) Current("Medallas");
                else
                {
                    Page("Medallas", "Medallas", "ListaMedallas");
                    Current("Editar medalla");
                }
                break;
            case "usuario/informacionpersonal":
                Group("Mi cuenta");
                Current("Información personal");
                break;
            case "usuario/cambiarcontrasena":
                Group("Mi cuenta");
                Current("Cambiar contraseña");
                break;
            case "usuario/cambiarcredencialesusuario" when role == 1:
            case "usuario/cambiarcontrasenaadmin" when role == 1:
                Participants();
                Current(key.EndsWith("cambiarcredencialesusuario") ? "Cambiar credenciales" : "Cambiar contraseña del participante");
                break;
            case "emailsettings/index" when role == 1:
            case "usuario/verbitacoraaccesos" when role == 1:
                Group("Mi cuenta");
                Group("Configuración");
                Current(key == "emailsettings/index" ? "Configuración del correo" : "Registro de accesos");
                break;
            default:
                // Authentication, onboarding, legacy missing views, JSON/downloads and mutations are excluded.
                return Array.Empty<BreadcrumbItem>();
        }
        return items;
    }

    private static object? Property(object? model, string name) => model?.GetType()
        .GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(model);

    private static string? GetOrigin(ViewContext context)
    {
        var request = context.HttpContext.Request;
        var origin = request.Query["breadcrumbOrigin"].FirstOrDefault();
        if (origin != null && Origins.Contains(origin)) return origin.ToLowerInvariant();
        if (!Uri.TryCreate(request.Headers.Referer.FirstOrDefault(), UriKind.Absolute, out var referer)
            || !string.Equals(referer.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase)) return null;
        var path = referer.AbsolutePath;
        var pathBase = request.PathBase.Value;
        if (!string.IsNullOrEmpty(pathBase) && path.StartsWith(pathBase + "/", StringComparison.OrdinalIgnoreCase))
            path = path[pathBase.Length..];
        return path.TrimEnd('/').ToLowerInvariant() switch
        {
            "/usuario/informacionpersonal" => "account",
            "/participante/verparticipantes" => "participants",
            "/inscripcion/verinscripciones" => "inscriptions",
            _ => null
        };
    }
}
