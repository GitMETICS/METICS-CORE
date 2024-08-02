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
        // Declaración de objetos de acceso a datos para manejar las entidades relacionadas
        private CategoriaHandler categoriaHandler;
        private TipoActividadHandler tipoActividadHandler;
        private AsesorHandler asesorHandler;
        private TemaHandler temaHandler;
        private GrupoHandler grupoHandler;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public TemaController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            categoriaHandler = new CategoriaHandler(environment, configuration);
            tipoActividadHandler = new TipoActividadHandler(environment, configuration);
            asesorHandler = new AsesorHandler(environment, configuration);
            temaHandler = new TemaHandler(environment, configuration);
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

        /* Vista del formulario para crear un tema */
        public ActionResult CrearTema()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            // Cargar los datos necesarios para llenar las opciones del formulario (categorias, tipos de actividad y asesores)
            ViewData["Categorias"] = null;
            ViewData["TipoActividad"] = null;
            ViewData["Categorias"] = categoriaHandler.RecuperarCategoriasIndexadas();
            ViewData["TipoActividad"] = tipoActividadHandler.RecuperarTiposDeActividadesSeleccionables();
            return View();
        }

        /* Formulario para crear un tema con los datos del modelo ingresados */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearTema(TemaModel tema)
        {
            try
            {
                
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                // Intenta crear el tema y su relación con el asesor asociado
                ViewBag.ExitoAlCrear = temaHandler.CrearTema(tema);
                if (ViewBag.ExitoAlCrear)
                {
                    // Si se crea el tema exitosamente, muestra un mensaje de éxito y redirige a la lista de categorías
                    TempData["Success"] = "El tema fue creado con éxito.";
                    ModelState.Clear();
                    return Redirect("~/Categoria/ListaCategorias");
                }
                // Si hay errores de validación, recarga los datos necesarios para llenar las opciones del formulario y muestra el formulario con los errores
                ViewData["Categorias"] = categoriaHandler.RecuperarCategoriasIndexadas();
                ViewData["TipoActividad"] = tipoActividadHandler.RecuperarTiposDeActividadesSeleccionables();
                ViewBag.Asesores = asesorHandler.ObtenerNombresDeAsesoresIndexados();
                return View(tema);
            }
            catch (Exception)
            {
                // Si ocurre un error, muestra un mensaje de error y redirige a la lista de categorías
                TempData["Message"] = "Hubo un error y no se pudo crear el tema.";
                ViewBag.ExitoAlCrear = false;
                return Redirect("~/Categoria/ListaCategorias");
            }
        }

        /* Vista para mostrar los temas asignados a una categoría */
        public ActionResult MostrarTemas(string nombre)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                // Muestra los temas asignados a la categoría proporcionada
                ViewBag.Nombre = nombre;
                ViewBag.Temas = temaHandler.RecuperarTemasDeCategoria(nombre);
                return View();
            }
            catch
            {
                // Si ocurre un error, redirige a la lista de categorías
                return Redirect("~/Categoria/ListaCategorias");
            }
        }

        /* Método para eliminar un tema de una categoría */
        public ActionResult EliminarTema(string nombre)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                // Verificar si el tema puede ser eliminado (no está asociado a un grupo)
                bool canBeDeleted = grupoHandler.CanEliminarTema(Int32.Parse(nombre));
                if (!canBeDeleted)
                {
                    // Si el tema está asociado a un grupo, mostrar un mensaje y redirigir a la lista de categorías
                    TempData["Message"] = "El tema no se puede eliminar porque está asociado a un módulo.";
                    ModelState.Clear();
                    return Redirect("~/Categoria/ListaCategorias");
                }
                else
                {
                    // Intentar eliminar el tema de la categoría
                    ViewBag.ExitoAlCrear = temaHandler.EliminarTema(nombre);
                    if (ViewBag.ExitoAlCrear)
                    {
                        // Si se elimina el tema exitosamente, mostrar un mensaje de éxito y redirigir a la lista de categorías
                        TempData["Success"] = "El tema fue eliminado con éxito.";
                        return Redirect("~/Categoria/ListaCategorias");
                    }
                    else
                    {
                        // Si ocurre un error al eliminar el tema, mostrar un mensaje de error y redirigir a la lista de categorías
                        TempData["Message"] = "El tema no se pudo eliminar por un error.";
                        ModelState.Clear();
                        return Redirect("~/Categoria/ListaCategorias");
                    }
                }
            }
            catch (Exception)
            {
                // Si ocurre un error, mostrar un mensaje de error y redirigir a la lista de categorías
                TempData["Message"] = "Hubo un error y no se pudo enviar la petición de eliminar el tipo de actividad.";
                ModelState.Clear();
                return Redirect("~/Categoria/ListaCategorias");
            }
        }
    }
}