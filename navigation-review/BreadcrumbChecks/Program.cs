using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using webMetics.Helpers;
using webMetics.TagHelpers;

var checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new Exception(message);
}

IReadOnlyList<BreadcrumbItem> Build(string controller, string action, int role = 1,
    string query = "", object? model = null, Dictionary<string, object?>? data = null,
    string? referer = null, string pathBase = "", bool signedIn = true)
{
    var http = new DefaultHttpContext();
    http.Request.Scheme = "https";
    http.Request.Host = new HostString("localhost", 7201);
    http.Request.PathBase = pathBase;
    http.Request.QueryString = new QueryString(query);
    if (referer != null) http.Request.Headers.Referer = referer;
    var route = new RouteData();
    route.Values["controller"] = controller;
    route.Values["action"] = action;
    var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
    {
        Model = model,
        ["Id"] = signedIn ? "signed-in-user" : "",
        ["Role"] = role
    };
    if (data != null) foreach (var pair in data) viewData[pair.Key] = pair.Value;
    var context = new ViewContext { HttpContext = http, RouteData = route, ViewData = viewData };
    var result = BreadcrumbBuilder.Build(context, new TestUrlHelper(context));
    if (result.Count > 0)
    {
        Check(result[^1].Url == null, "La página actual debe ser texto sin enlace");
        Check(result[0].Label == "Inicio", "La raíz debe ser Inicio");
        Check(result.All(i => i.Url == null || i.Url.StartsWith(pathBase + "/", StringComparison.Ordinal)), "Los enlaces deben ser locales");
        var forbidden = new[] { "Eliminar", "Inscribir?", "CopiarGrupo", "CambiarEstadoVisible", "Descargar", "Subir", "CerrarSesion" };
        Check(result.All(i => i.Url == null || !forbidden.Any(p => i.Url.Contains(p, StringComparison.OrdinalIgnoreCase))), "Un ancestro no debe ejecutar operaciones");
    }
    return result;
}

string Labels(IReadOnlyList<BreadcrumbItem> items) => string.Join(" > ", items.Select(i => i.Label));

Check(Labels(Build("Grupo", "ListaGruposDisponibles", 0)) == "Inicio", "Inicio del participante");
Check(Labels(Build("Grupo", "BuscarGrupos")) == "Inicio", "La búsqueda comparte el breadcrumb del catálogo");
Check(Labels(Build("Asesor", "BuscarGrupos", 2)) == "Inicio > Mis módulos", "Búsqueda de facilitador");

var expected = new Dictionary<(string Controller, string Action), string>
{
    [("Categoria", "ListaCategorias")] = "Niveles",
    [("Categoria", "CrearCategoria")] = "Crear nivel",
    [("Categoria", "EditarCategoria")] = "Editar nivel",
    [("Tema", "ListaTemas")] = "Áreas de competencia",
    [("Tema", "CrearTema")] = "Crear área de competencia",
    [("Tema", "EditarTema")] = "Editar área de competencia",
    [("Asesor", "ListaAsesores")] = "Facilitadores(as)",
    [("Asesor", "CrearAsesor")] = "Crear facilitador(a)",
    [("Asesor", "AgregarAsesor")] = "Crear facilitador(a)",
    [("Asesor", "ActualizarAsesor")] = "Editar facilitador(a)",
    [("Participante", "FormularioParticipante")] = "Agregar participante",
    [("Participante", "BuscarParticipantes")] = "Participantes",
    [("Inscripcion", "VerInscripciones")] = "Lista de inscripciones",
    [("Inscripcion", "BuscarInscripciones")] = "Lista de inscripciones",
    [("Grupo", "CrearGrupo")] = "Crear módulo",
    [("Medallas", "ListaMedallas")] = "Medallas",
    [("Medallas", "EditarMedalla")] = "Editar medalla",
    [("Usuario", "CambiarCredencialesUsuario")] = "Cambiar credenciales",
    [("Usuario", "CambiarContrasenaAdmin")] = "Cambiar contraseña del participante",
    [("EmailSettings", "Index")] = "Configuración del correo",
    [("Usuario", "VerBitacoraAccesos")] = "Registro de accesos",
    [("Usuario", "VerBitacoraAccesoUsuario")] = "Registro de accesos",
    [("Usuario", "VerBitacoraAccesoPorFecha")] = "Registro de accesos",
    [("Usuario", "VerBitacoraUltimoAccesoUsuario")] = "Registro de accesos"
};
foreach (var (page, leaf) in expected)
    Check(Build(page.Controller, page.Action)[^1].Label == leaf, $"Página y alias: {page}");

foreach (var role in new[] { 0, 2 })
    foreach (var page in new[] { ("EmailSettings", "Index"), ("Usuario", "VerBitacoraAccesos"), ("Participante", "VerParticipantes"), ("Categoria", "ListaCategorias") })
        Check(Build(page.Item1, page.Item2, role).Count == 0, $"Sin rama administrativa para rol {role}: {page}");

foreach (var role in new[] { 0, 1, 2 })
{
    Check(Labels(Build("Usuario", "InformacionPersonal", role)) == "Inicio > Mi cuenta > Información personal", "Cuenta por rol");
    Check(Labels(Build("Usuario", "CambiarContrasena", role)) == "Inicio > Mi cuenta > Cambiar contraseña", "Contraseña propia");
}

var personData = new Dictionary<string, object?> { ["Participante"] = new { nombre = "Ana", primerApellido = "Pérez", segundoApellido = "Soto" } };
var person = Build("Participante", "VerDatosParticipante", query: "?idParticipante=ana%40ucr.ac.cr&breadcrumbOrigin=participants", data: personData);
Check(Labels(person) == "Inicio > Personas > Participantes > Ana Pérez Soto", "Detalle desde Personas");
person = Build("Participante", "VerDatosParticipante", query: "?idParticipante=ana&breadcrumbOrigin=inscriptions");
Check(person[1].Url == "/Inscripcion/VerInscripciones", "Detalle desde Inscripciones");
person = Build("Participante", "VerDatosParticipante", 2, "?idParticipante=ana&idGrupo=42&breadcrumbOrigin=participants");
Check(Labels(person).StartsWith("Inicio > Mis módulos > Módulo > Participantes"), "El módulo conserva prioridad sobre un origen general");
Check(person.Any(i => i.Url == "/Participante/ListaParticipantes?idGrupo=42"), "El padre conserva idGrupo");
Check(!person.Any(i => i.Url == "/Participante/VerParticipantes"), "El facilitador no enlaza al listado administrativo");

var moduleData = new Dictionary<string, object?> { ["IdGrupo"] = 18, ["NombreGrupo"] = "Mediación virtual" };
var module = Build("Calificaciones", "EditarCalificacion", 2, data: moduleData);
Check(module.Any(i => i.Label == "Mediación virtual"), "Usar el nombre del módulo disponible");
Check(module.Any(i => i.Url == "/Calificaciones/VerCalificaciones?idGrupo=18"), "Calificaciones conserva contexto del grupo");
module = Build("Calificaciones", "EditarCalificacion", 2, model: new { idGrupo = 9 });
Check(module.Any(i => i.Url == "/Participante/ListaParticipantes?idGrupo=9"), "Recuperar idGrupo del modelo");
module = Build("Calificaciones", "EditarCalificacion", 2);
Check(module.All(i => i.Url == null || !i.Url.Contains("ListaParticipantes") && !i.Url.Contains("VerCalificaciones")), "Sin parámetros no generar padres rotos");
Check(Labels(Build("Participante", "BuscarParticipantesDelGrupo", 2, "?idGrupo=42")) == "Inicio > Mis módulos > Módulo > Participantes", "Alias de búsqueda del grupo");

foreach (var action in new[] { "EditarParticipante", "ActualizarParticipante" })
    Check(Labels(Build("Participante", action, query: "?breadcrumbOrigin=account")) == "Inicio > Mi cuenta > Información personal > Editar participante", "Origen de cuenta persistente en GET y POST");
Check(Labels(Build("Asesor", "EditarAsesor", 2)) == "Inicio > Mi cuenta > Información personal > Editar facilitador(a)", "Edición propia de facilitador");

person = Build("Participante", "VerDatosParticipante", referer: "https://localhost:7201/Inscripcion/VerInscripciones");
Check(person[1].Url == "/Inscripcion/VerInscripciones", "Fallback del origen local");
person = Build("Participante", "VerDatosParticipante", referer: "https://other.invalid/Inscripcion/VerInscripciones");
Check(person[1].Label == "Inscripciones", "Ignorar Referer externo y conservar el fallback actual de la pantalla");
person = Build("Participante", "VerDatosParticipante", query: "?breadcrumbOrigin=https%3A%2F%2Fother.invalid");
Check(person[1].Label == "Inscripciones", "No interpretar un origen arbitrario como URL");
person = Build("Participante", "VerDatosParticipante", query: "?breadcrumbOrigin=participants", referer: "https://localhost:7201/Inscripcion/VerInscripciones");
Check(person[1].Label == "Personas", "Origen explícito gana al Referer");
person = Build("Participante", "VerDatosParticipante", pathBase: "/metics", referer: "https://localhost:7201/metics/Inscripcion/VerInscripciones");
Check(person[1].Url == "/metics/Inscripcion/VerInscripciones", "Instalación bajo PathBase");

foreach (var page in new[] { ("Grupo", "EliminarGrupo"), ("Grupo", "CopiarGrupo"), ("Grupo", "EditarTemas"), ("Inscripcion", "InscribirManualmente"), ("Inscripcion", "DesinscribirParticipante"), ("Participante", "ObtenerParticipantesPaginados"), ("Calificaciones", "ExportarCalificacionesPDF"), ("Usuario", "IniciarSesion"), ("Usuario", "FormularioRegistro"), ("Usuario", "CompletarCorreoAlternativo"), ("Usuario", "CompletarGradoAcademico"), ("Usuario", "CompletarCarreraYAreas"), ("Participante", "FormularioRecuperarContrasena"), ("Home", "Error"), ("Home", "ThrowTest"), ("Unknown", "Unknown") })
    Check(Build(page.Item1, page.Item2).Count == 0, $"Excluir operaciones, autenticación y páginas sin mapa: {page}");
Check(Build("Categoria", "ListaCategorias", signedIn: false).Count == 0, "Sin breadcrumbs administrativos sin sesión");
Check(Build("Inscripcion", "Inscribir", 0).All(i => i.Url == null || i.Url == "/Grupo/ListaGruposDisponibles"), "El resultado de inscripción no vuelve a ejecutar Inscribir");
Check(Build("Grupo", "VerAdjunto", 0).All(i => i.Url == null || i.Url == "/Grupo/ListaGruposDisponibles"), "Un programa abierto directamente no reactiva enlaces ocultos");
Check(Build("Grupo", "EditarAdjunto", 0).Count == 0, "Sin breadcrumb de edición de programa para participante");
var headingContext = new ViewContext
{
    HttpContext = new DefaultHttpContext(),
    RouteData = new RouteData(),
    ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
    {
        ["Id"] = "signed-in-user", ["Role"] = 1, ["Title"] = "Inscripciones"
    }
};
headingContext.RouteData.Values["controller"] = "Inscripcion";
headingContext.RouteData.Values["action"] = "VerInscripciones";
PageHeadingBuilder.Configure(headingContext, new TestUrlHelper(headingContext), "Lista de inscripciones", "Módulo de ejemplo");
Check(headingContext.ViewData["UseSharedPageHeader"] is true, "Activar el encabezado compartido en páginas con breadcrumb");
Check(Equals(headingContext.ViewData["PageHeading"], "Lista de inscripciones"), "Conservar título personalizado");
Check(Equals(headingContext.ViewData["PageSubheading"], "Módulo de ejemplo"), "Conservar el nombre contextual como subtítulo");
foreach (var tag in new[] { "h1", "h2", "hr", "div" })
{
    var tagContext = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "heading");
    var output = new TagHelperOutput(tag, new TagHelperAttributeList { new("metics-page-heading") },
        (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    output.Content.SetContent("Título anterior");
    new PageHeadingTagHelper { ViewContext = headingContext }.Process(tagContext, output);
    Check(output.TagName == null && output.Content.IsEmptyOrWhiteSpace, "Evitar el encabezado duplicado: " + tag);
}
headingContext.ViewData["Id"] = "";
PageHeadingBuilder.Configure(headingContext, new TestUrlHelper(headingContext));
Check(headingContext.ViewData["UseSharedPageHeader"] is false, "Conservar encabezado original sin sesión");
var fallbackOutput = new TagHelperOutput("h1", new TagHelperAttributeList { new("metics-page-heading") },
    (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
fallbackOutput.Content.SetContent("Encabezado original");
new PageHeadingTagHelper { ViewContext = headingContext }.Process(
    new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "fallback"), fallbackOutput);
Check(fallbackOutput.TagName == "h1" && fallbackOutput.Content.GetContent() == "Encabezado original", "El fallback conserva el contenido original");
Check(!fallbackOutput.Attributes.ContainsName("metics-page-heading"), "No emitir atributos de control en el HTML");
Console.WriteLine($"OK: {checks} comprobaciones de breadcrumbs y encabezados; sin acceder a BD ni ejecutar acciones MVC.");

sealed class TestUrlHelper(ActionContext context) : IUrlHelper
{
    public ActionContext ActionContext => context;
    public string? Action(UrlActionContext actionContext)
    {
        var values = new RouteValueDictionary(actionContext.Values);
        var query = values.Where(v => v.Value != null).Select(v => Uri.EscapeDataString(v.Key) + "=" + Uri.EscapeDataString(Convert.ToString(v.Value)!));
        var suffix = string.Join("&", query);
        return context.HttpContext.Request.PathBase + "/" + actionContext.Controller + "/" + actionContext.Action + (suffix.Length == 0 ? "" : "?" + suffix);
    }
    public string? Content(string? contentPath) => contentPath;
    public bool IsLocalUrl(string? url) => url?.StartsWith('/') == true && !url.StartsWith("//");
    public string? Link(string? routeName, object? values) => null;
    public string? RouteUrl(UrlRouteContext routeContext) => null;
}
