using Microsoft.AspNetCore.Mvc;
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
        private AsesorHandler accesoAAsesor;
        private TemaHandler accesoATema;
        private GrupoHandler accesoAGrupo;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public TemaController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            accesoACategoria = new CategoriaHandler(environment, configuration);
            accesoAAsesor = new AsesorHandler(environment, configuration);
            accesoATema = new TemaHandler(environment, configuration);
            accesoAGrupo = new GrupoHandler(environment, configuration);
        }

        public int GetRole()
        {
            int role = 0;

            if (HttpContext.Request.Cookies.ContainsKey("rolUsuario"))
            {
                role = Convert.ToInt32(Request.Cookies["rolUsuario"]);
            }

            return role;
        }

        public string GetId()
        {
            string id = "";

            if (HttpContext.Request.Cookies.ContainsKey("idUsuario"))
            {
                id = Convert.ToString(Request.Cookies["idUsuario"]);
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
                    TempData["successMessage"] = "El área de competencia fue creado con éxito.";
                    return RedirectToAction("ListaTemas");
                }

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
        public ActionResult EditarTema()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
            ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

            return View();
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