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
        public CategoriaHandler categoriaHandler;
        public AsesorHandler asesorHandler;
        public TemaHandler temaHandler;
        public TipoActividadHandler tipoActividadHandler;
        public GrupoHandler grupoHandler;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public CategoriaController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            categoriaHandler = new CategoriaHandler(environment, configuration);
            asesorHandler = new AsesorHandler(environment, configuration);
            temaHandler = new TemaHandler(environment, configuration);
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

        /* Método de la vista CrearCategoría que muestra el formulario para crear una categoría */
        public ActionResult CrearCategoria()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            return View();
        }

        /* Método de la vista CrearCategoría que muestra el formulario para crear una categoría con los datos ingresados */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearCategoria(CategoriaModel categoria)
        {
            try
            {
                // Recuperar la lista de categorías en minúsculas
                List<CategoriaModel> categorias = categoriaHandler.RecuperarCategoriasLowerCase();
                int cantidadCategorias = categorias.Count + 1;
                categoria.idGenerado = cantidadCategorias.ToString();

                // Verificar si ya existe una categoría con el mismo nombre (ignorando mayúsculas y espacios)
                if (categorias.Any(t => t.nombre == categoria.nombre.Replace(" ", "").ToLower()))
                {
                    ModelState.AddModelError("nombre", "Ya existe una categoría con ese nombre.");
                    return View(categoria);
                }

                // Crear la categoría a través del CategoriaHandler y guardar el resultado en ViewBag.ExitoAlCrear
                ViewBag.ExitoAlCrear = categoriaHandler.CrearCategoria(categoria);

                // Si la creación fue exitosa, mostrar mensaje de éxito y redirigir a la lista de categorías
                if (ViewBag.ExitoAlCrear)
                {
                    ViewBag.Message = "La categoría fue creada con éxito.";
                    ModelState.Clear();
                    return Redirect("~/Categoria/ListaCategorias");
                }
                
                return View();
            }
            catch (Exception)
            {
                ViewBag.Message = "Hubo un error y no se pudo enviar la petición de crear categoría.";
                return View();
            }
        }

        /* Método de la vista ListaCategorias que muestra todas las categorías */
        public ActionResult ListaCategorias()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            // Recuperar todas las categorías y almacenarlas en ViewBag.Categorias para mostrarlas en la vista
            ViewBag.Categorias = categoriaHandler.RecuperarCategorias();
            return View();
        }

        /* Método para eliminar una categoría */
        public ActionResult EliminarCategoria(string nombreCategoria)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            // Recuperar la lista de categorías
            List<CategoriaModel> categorias = categoriaHandler.RecuperarCategorias();

            // Encontrar la categoría correspondiente al nombreCategoria proporcionado
            CategoriaModel categoria = categorias.Find(categoriaModel => categoriaModel.nombre == nombreCategoria);
            string categoriaID = categoria.idGenerado;

            // Verificar si la categoría puede ser eliminada
            bool canBeDeleted = grupoHandler.CanEliminarCategoria(Int32.Parse(categoriaID));
            try
            {
                if (canBeDeleted)
                {
                    // Eliminar la categoría a través del CategoriaHandler y guardar el resultado en ViewBag.ExitoAlCrear
                    ViewBag.ExitoAlCrear = categoriaHandler.EliminarCategoria(nombreCategoria);

                    // Si la eliminación fue exitosa, limpiar el ModelState y redirigir a la lista de categorías
                    if (ViewBag.ExitoAlCrear)
                    {
                        ModelState.Clear();
                        return Redirect("~/Categoria/ListaCategorias");
                    }
                    else
                    {
                        TempData["Message"] = "La categoría no se pudo eliminar debido a un error, intente de nuevo.";
                        ModelState.Clear();
                        return Redirect("~/Categoria/ListaCategorias");
                    }
                }
                else
                {
                    TempData["Message"] = "La categoría no se pudo eliminar porque está siendo usada por un módulo.";
                    ModelState.Clear();
                    return Redirect("~/Categoria/ListaCategorias");
                }
            }
            catch (Exception)
            {
                TempData["Message"] = "La categoría no se pudo eliminar debido a un error, intente de nuevo.";
                ModelState.Clear();
                return Redirect("~/Categoria/ListaCategorias");
            }
        }

        /* Método para editar una categoría */
        [HttpGet]
        public ActionResult EditarCategoria(string nombre)
        {
            ActionResult vista;

            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                // Encontrar la categoría correspondiente al nombre proporcionado
                CategoriaModel modificarCategoria = categoriaHandler.RecuperarCategorias().Find(categoriaModel => categoriaModel.nombre == nombre);

                // Si no se encontró la categoría, redirigir a la lista de categorías
                if (modificarCategoria == null)
                {
                    vista = RedirectToAction("ListaCategorias");
                }
                else
                {
                    vista = View(modificarCategoria);
                }
            }
            catch
            {
                vista = RedirectToAction("ListaCategorias");
            }
            return vista;
        }

        /* Método para editar una categoría con los datos ingresados */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarCategoria(CategoriaModel categoria)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                // Si están vacíos devuelve la vista con los mensajes de validación del modelo
                if (categoria.nombre == null || categoria.descripcion == null)
                {
                    return View(categoria);
                }

                // Recuperar todas las categorías en minúsculas y remover la categoría actual por ID
                List<CategoriaModel> categorias = categoriaHandler.RecuperarCategoriasLowerCase();
                categorias.RemoveAll(categoriaModel => categoriaModel.idGenerado == categoria.idGenerado);

                // Verificar si ya existe una categoría con el mismo nombre (ignorando mayúsculas y espacios)
                if (categorias.Any(t => t.nombre == categoria.nombre.Replace(" ", "").ToLower()))
                {
                    ModelState.AddModelError("nombre", "Ya existe una categoría con ese nombre.");
                    return View(categoria);
                }

                // Editar la categoría a través del CategoriaHandler
                categoriaHandler.EditarCategoria(categoria);
                return RedirectToAction("ListaCategorias", "Categoria");
            }
            catch
            {
                return View();
            }
        }
    }
}