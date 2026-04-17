using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;
using webMetics.Models;

namespace webMetics.Controllers
{
    /// <summary>
    /// Gestiona las operaciones CRUD sobre la entidad Categoría (niveles de clasificación de grupos).
    /// </summary>
    public class CategoriaController : Controller
    {
        public CategoriaHandler accesoACategoria;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public CategoriaController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            accesoACategoria = new CategoriaHandler(environment, configuration);
        }

        /// <summary>Obtiene el rol del usuario autenticado desde la cookie "rolUsuario".</summary>
        private int GetRole()
        {
            int role = 0;

            if (HttpContext.Request.Cookies.ContainsKey("rolUsuario"))
            {
                role = Convert.ToInt32(Request.Cookies["rolUsuario"]);
            }

            return role;
        }

        /// <summary>Obtiene el identificador del usuario autenticado desde la cookie "idUsuario".</summary>
        private string GetId()
        {
            string id = "";

            if (HttpContext.Request.Cookies.ContainsKey("idUsuario"))
            {
                id = Convert.ToString(Request.Cookies["idUsuario"]);
            }

            return id;
        }

        /// <summary>Muestra el listado de todas las categorías (niveles) registradas.</summary>
        /// <returns>
        /// View: ListaCategorias —
        /// ViewBag.Categorias, ViewBag.Role, ViewBag.Id, ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>Handlers: CategoriaHandler. Role required: Admin (1).</remarks>
        public ActionResult ListaCategorias()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewBag.Categorias = accesoACategoria.ObtenerCategorias();

            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View();
        }

        /// <summary>Muestra el formulario para registrar una nueva categoría.</summary>
        /// <returns>
        /// View: CrearCategoria — ViewData["Categorias"] (SelectListItem), ViewBag.Role, ViewBag.Id.
        /// </returns>
        /// <remarks>Handlers: CategoriaHandler. Role required: Admin (1).</remarks>
        public ActionResult CrearCategoria()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

            return View();
        }

        /// <summary>Procesa el formulario de creación de categoría.</summary>
        /// <param name="categoria">Datos de la categoría a crear.</param>
        /// <returns>
        /// Redirects to ListaCategorias on success. Sets TempData["successMessage"] or
        /// TempData["errorMessage"]. Returns View CrearCategoria with model on failure.
        /// </returns>
        /// <remarks>Handlers: CategoriaHandler. Role required: Admin (1).</remarks>
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

        /// <summary>Muestra el formulario de edición de categoría.</summary>
        /// <returns>
        /// View: EditarCategoria — ViewData["Categorias"] (SelectListItem), ViewBag.Role, ViewBag.Id.
        /// </returns>
        /// <remarks>Handlers: CategoriaHandler. Role required: Admin (1).</remarks>
        public ActionResult EditarCategoria()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

            return View();
        }

        /// <summary>Elimina una categoría del sistema.</summary>
        /// <param name="idCategoria">ID de la categoría a eliminar.</param>
        /// <returns>
        /// Redirects to ListaCategorias. Sets TempData["successMessage"] on success or
        /// TempData["errorMessage"] on failure.
        /// </returns>
        /// <remarks>Handlers: CategoriaHandler. Role required: Admin (1).</remarks>
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