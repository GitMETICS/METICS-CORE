using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using webMetics.Handlers;
using webMetics.Models;

namespace webMetics.Controllers
{
    public class CategoriaController : Controller
    {
        public CategoriaHandler accesoACategoria;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CategoriaController(IWebHostEnvironment environment, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _environment = environment;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;

            accesoACategoria = new CategoriaHandler(environment, configuration);
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

        public ActionResult ListaCategorias()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewBag.Categorias = accesoACategoria.ObtenerCategorias();

            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View();
        }

        public ActionResult CrearCategoria()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

            return View();
        }

        [HttpPost]
        public ActionResult CrearCategoria(CategoriaModel categoria)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                bool exito = accesoACategoria.CrearCategoria(categoria);
                if (exito)
                {
                    TempData["successMessage"] = "El nivel fue creado con éxito.";
                    return RedirectToAction("ListaCategorias");
                }

                ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

                return View(categoria);
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "No se pudo crear el nivel.";
                return RedirectToAction("ListaCategorias");
            }
        }

        public ActionResult EditarCategoria()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

            return View();
        }

        public ActionResult EliminarCategoria(int idCategoria)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                bool exito = accesoACategoria.EliminarCategoria(idCategoria);
                if (exito)
                {
                    TempData["successMessage"] = "Se eliminó la categoría.";
                }
                else
                {
                    TempData["errorMessage"] = "No se pudo eliminar la categoría.";
                }
            }
            catch
            {
                TempData["errorMessage"] = "No se pudo eliminar la categoría.";
            }

            return RedirectToAction("ListaCategorias");
        }
    }
}