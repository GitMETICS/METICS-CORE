using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;
using webMetics.Models;

/* 
 * Controlador de la entidad Categoria
 * Los grupos pertenecen a una categoría
 * En esta clase se puede retornar todos las categorías, editar, agregar y eliminar una categoría 
 */

namespace webMetics.Controllers
{
    public class CategoriaController : Controller
    {
        // Declaración de los handlers necesarios
        public CategoriaHandler accesoACategoria;
        public AsesorHandler asesorHandler;
        public CategoriaHandler categoriaHandler;
        public TipoActividadHandler tipoActividadHandler;
        public GrupoHandler grupoHandler;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public CategoriaController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            accesoACategoria = new CategoriaHandler(environment, configuration);
            asesorHandler = new AsesorHandler(environment, configuration);
            categoriaHandler = new CategoriaHandler(environment, configuration);
            tipoActividadHandler = new TipoActividadHandler(environment, configuration);
            grupoHandler = new GrupoHandler(environment, configuration);
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

        /* Vista de la lista de categorias */
        public ActionResult ListaCategorias()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewBag.Categorias = accesoACategoria.ObtenerCategorias();

            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View();
        }

        /* Vista del formulario para crear un categoria */
        public ActionResult CrearCategoria()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

            return View();
        }

        /* Formulario para crear un categoria con los datos del modelo ingresados */
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
                    TempData["successMessage"] = "La categoría fue creada con éxito.";
                    return RedirectToAction("ListaCategorias");
                }

                ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

                return View(categoria);
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "No se pudo crear la categoría.";
                return RedirectToAction("ListaCategorias");
            }
        }

        /* Vista del formulario para crear un categoria */
        public ActionResult EditarCategoria()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();

            return View();
        }

        /* Método para eliminar un categoria */
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