using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;
using webMetics.Models;
namespace webMetics.Controllers
{
    /// <summary>
    /// Gestiona las operaciones CRUD sobre la entidad Tema (áreas de competencia).
    /// Los temas se asocian a categorías y pueden asignarse a grupos.
    /// </summary>
    public class TemaController : Controller
    {
        private CategoriaHandler accesoACategoria;
        private TemaHandler accesoATema;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public TemaController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            accesoACategoria = new CategoriaHandler(environment, configuration);
            accesoATema = new TemaHandler(environment, configuration);
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

        /// <summary>Muestra el listado de todas las áreas de competencia (temas) registradas.</summary>
        /// <returns>
        /// View: ListaTemas —
        /// ViewBag.Temas, ViewBag.Role, ViewBag.Id, ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>Handlers: TemaHandler. Role required: Admin (1).</remarks>
        public ActionResult ListaTemas()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewBag.Temas = accesoATema.ObtenerTemas();

            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View();
        }

        /// <summary>Muestra el formulario para registrar una nueva área de competencia.</summary>
        /// <returns>
        /// View: CrearTema —
        /// ViewData["Temas"] (SelectListItem), ViewData["Categorias"] (SelectListItem),
        /// ViewBag.Role, ViewBag.Id.
        /// </returns>
        /// <remarks>Handlers: TemaHandler, CategoriaHandler. Role required: Admin (1).</remarks>
        public ActionResult CrearTema()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
            ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

            return View();
        }

        /// <summary>Procesa el formulario de creación de área de competencia.</summary>
        /// <param name="tema">Datos del tema a crear.</param>
        /// <returns>
        /// Redirects to ListaTemas on success. Sets TempData["successMessage"] or
        /// TempData["errorMessage"]. Returns View CrearTema with model on failure.
        /// </returns>
        /// <remarks>Handlers: TemaHandler, CategoriaHandler. Role required: Admin (1).</remarks>
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

        /// <summary>Muestra el formulario precargado con los datos del área de competencia para su edición.</summary>
        /// <param name="idTema">ID del tema a editar.</param>
        /// <returns>
        /// View: EditarTema (model: TemaModel) —
        /// ViewData["Temas"] (SelectListItem), ViewData["Categorias"] (SelectListItem),
        /// ViewBag.Role, ViewBag.Id.
        /// </returns>
        /// <remarks>Handlers: TemaHandler, CategoriaHandler. Role required: Admin (1).</remarks>
        public ActionResult EditarTema(int idTema)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            TemaModel tema = accesoATema.ObtenerTema(idTema);

            ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
            ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

            return View(tema);
        }

        /// <summary>Procesa el formulario de edición de área de competencia.</summary>
        /// <param name="tema">Datos actualizados del tema.</param>
        /// <returns>
        /// Redirects to ListaTemas on success. Sets TempData["successMessage"] or
        /// TempData["errorMessage"]. Returns View EditarTema with model on failure.
        /// </returns>
        /// <remarks>Handlers: TemaHandler, CategoriaHandler. Role required: Admin (1).</remarks>
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

        /// <summary>Elimina un área de competencia del sistema.</summary>
        /// <param name="idTema">ID del tema a eliminar.</param>
        /// <returns>
        /// Redirects to ListaTemas. Sets TempData["successMessage"] on success or
        /// TempData["errorMessage"] on failure.
        /// </returns>
        /// <remarks>Handlers: TemaHandler. Role required: Admin (1).</remarks>
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