using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using webMetics.Handlers;
using webMetics.Models;
/* 
 * Controlador de la entidad Tema
 */
namespace webMetics.Controllers
{
    public class TemaController : Controller
    {
        private CategoriaHandler accesoACategoria;
        private TemaHandler accesoATema;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TemaController(IWebHostEnvironment environment, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _environment = environment;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;

            accesoACategoria = new CategoriaHandler(environment, configuration);
            accesoATema = new TemaHandler(environment, configuration);
        }

        private int GetRole()
        {
            int role = 0;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var session = httpContext.Session;
                var cookies = httpContext.Request.Cookies;

                role = session.GetInt32("UsuarioRol") ?? 0;
            }
            return role;
        }

        private string GetId()
        {
            string id = "";

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var session = httpContext.Session;
                var cookies = httpContext.Request.Cookies;

                id = session.GetString("UsuarioId") ?? "";
            }

            return id;
        }

        /* Vista de la lista de temas */
        public ActionResult ListaTemas()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewBag.Temas = accesoATema.ObtenerTemas();

            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View();
        }

        /* Vista del formulario para crear un tema */
        public ActionResult CrearTema()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
            ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

            return View();
        }

        /* Formulario para crear un tema con los datos del modelo ingresados */
        [HttpPost]
        public ActionResult CrearTema(TemaModel tema)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                bool exito = accesoATema.CrearTema(tema);
                if (exito)
                {
                    TempData["successMessage"] = "Se creó el área de competencia.";
                    return RedirectToAction("ListaTemas");
                }

                ViewBag.ErrorMessage = "No se pudo crear el área de competencia.";
                ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

                return View(tema);
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "No se pudo crear el área de competencia.";
                return RedirectToAction("ListaTemas");
            }
        }

        /* Vista del formulario para crear un tema */
        public ActionResult EditarTema(int idTema)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            TemaModel tema = accesoATema.ObtenerTema(idTema);

            ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
            ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

            return View(tema);
        }

        [HttpPost]
        public ActionResult EditarTema(TemaModel tema)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                bool exito = accesoATema.EditarTema(tema);
                if (exito)
                {
                    TempData["successMessage"] = "Se editó el área de competencia.";
                    return RedirectToAction("ListaTemas");
                }

                ViewBag.ErrorMessage = "No se pudo editar el área de competencia.";
                ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

                return View(tema);
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "No se pudo editar el área de competencia.";
                return RedirectToAction("ListaTemas");
            }
        }

        /* Método para eliminar un tema */
        public ActionResult EliminarTema(int idTema)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                bool exito = accesoATema.EliminarTema(idTema);
                if (exito)
                {
                    TempData["successMessage"] = "Se eliminó el área de competencia.";
                }
                else
                {
                    TempData["errorMessage"] = "No se pudo eliminar el área de competencia.";
                }
            }
            catch
            {
                TempData["errorMessage"] = "No se pudo eliminar el área de competencia.";
            }

            return RedirectToAction("ListaTemas");
        }
    }
}