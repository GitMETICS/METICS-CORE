using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;
using webMetics.Models;


/* 
 * Controlador de la entidad TipoDeActividad
 */
namespace webMetics.Controllers
{
    public class TipoActividadController : Controller
    {
        public TipoActividadHandler tipoActividadHandler;
        public GrupoHandler grupoHandler;

        public TipoActividadController()
        {
            tipoActividadHandler = new TipoActividadHandler();
            grupoHandler = new GrupoHandler();
        }

        /* Vista de los tipos de actividades */
        public ActionResult ListaTiposActividad()
        {
            // Recupera y muestra la lista de tipos de actividad
            ViewBag.TiposActividad = tipoActividadHandler.RecuperarTiposDeActividades();
            return View();
        }

        /* Vista del formulario para crear una actividad */
        public ActionResult CrearTipoActividad()
        {
            // Muestra el formulario para crear un nuevo tipo de actividad
            return View();
        }

        /* Formulario para crear una actividad con los datos del modelo ingresados */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearTipoActividad(TipoActividadModel tipoActividad)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Verifica si ya existe una actividad con el mismo nombre (ignorando mayúsculas y espacios)
                    List<TipoActividadModel> tiposActividad = tipoActividadHandler.RecuperarTiposDeActividadesLowerCase();
                    if (tiposActividad.Any(t => t.nombre == tipoActividad.nombre.Replace(" ", "").ToLower()))
                    {
                        // Si ya existe, muestra un mensaje de error y vuelve al formulario con el mismo nombre ingresado
                        ModelState.AddModelError("nombre", "Ya existe una actividad con ese nombre.");
                        return View(tipoActividad);
                    }

                    // Intenta crear el tipo de actividad en la base de datos
                    ViewBag.ExitoAlCrear = tipoActividadHandler.CrearTipoActividad(tipoActividad);
                    if (ViewBag.ExitoAlCrear)
                    {
                        // Si se crea con éxito, muestra un mensaje de éxito y redirige a la lista de tipos de actividad
                        TempData["Success"] = "El tipo de actividad fue creado con éxito";
                        ModelState.Clear();
                        return Redirect("~/TipoActividad/ListaTiposActividad");
                    }
                }
                return View();
            }
            catch (Exception ex)
            {
                // Manejo de errores: muestra mensajes personalizados para diferentes tipos de errores
                string errorMessage;
                if (ex.Message.Contains("Violation of UNIQUE KEY constraint"))
                {
                    errorMessage = "No se pudo crear la actividad porque ya existe una con el mismo nombre.";
                }
                else
                {
                    errorMessage = "Hubo un error y no se pudo crear tipo de actividad.";
                }
                ViewBag.Message = errorMessage;
                ViewBag.ExitoAlCrear = false;
                return View();
            }
        }

        /* Método para eliminar un tipo de actividad */
        public ActionResult EliminarTipoActividad(string nombre)
        {
            try
            {
                // Recupera la lista de tipos de actividad y encuentra el tipo de actividad correspondiente al nombre proporcionado
                List<TipoActividadModel> actividades = tipoActividadHandler.RecuperarTiposDeActividades();
                TipoActividadModel tipoActividad = actividades.Find(tipoActividadModel => tipoActividadModel.nombre == nombre);
                string tipoActividadID = tipoActividad.idGenerado;

                // Verifica si el tipo de actividad puede ser eliminado (si no está asociado a temas)
                bool canBeDeleted = grupoHandler.CanEliminarActividad(Int32.Parse(tipoActividadID));
                if (!canBeDeleted)
                {
                    TempData["Message"] = "No se puede eliminar el tipo de actividad porque hay temas asociados a él.";
                    ModelState.Clear();
                    return Redirect("~/TipoActividad/ListaTiposActividad");
                }
                else
                {
                    // Intenta eliminar el tipo de actividad de la base de datos
                    ViewBag.ExitoAlCrear = tipoActividadHandler.EliminarTipoActividad(nombre);
                    if (ViewBag.ExitoAlCrear)
                    {
                        TempData["Success"] = "La actividad fue eliminada con éxito.";
                        ModelState.Clear();
                        return Redirect("~/TipoActividad/ListaTiposActividad");
                    }
                    else
                    {
                        TempData["Message"] = "El tipo de actividad no se pudo eliminar por un error.";
                        ModelState.Clear();
                        return Redirect("~/TipoActividad/ListaTiposActividad");
                    }
                }
            }
            catch (Exception)
            {
                TempData["Message"] = "Hubo un error y no se pudo enviar la petición de eliminar tipo de actividad.";
                return Redirect("~/TipoActividad/ListaTiposActividad");
            }
        }

        /* Vista del formulario para editar una actividad */
        [HttpGet]
        public ActionResult EditarTipoActividad(string nombre)
        {
            ActionResult vista;
            try
            {
                // Recupera el tipo de actividad correspondiente al nombre proporcionado
                TipoActividadModel modificarTipoActividad = tipoActividadHandler.RecuperarTiposDeActividades().Find(tipoActividadModel => tipoActividadModel.nombre == nombre);
                if (modificarTipoActividad == null)
                {
                    vista = RedirectToAction("ListaTiposActividad");
                }
                else
                {
                    vista = View(modificarTipoActividad);
                }
            }
            catch
            {
                vista = RedirectToAction("ListaTiposActividad");
            }
            return vista;
        }

        /* Vista del formulario para editar una actividad con los datos del modelo ingresados */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarTipoActividad(TipoActividadModel tipoActividad)
        {
            try
            {
                // Verifica si los campos requeridos están vacíos y muestra mensajes de validación del modelo
                if (tipoActividad.nombre == null || tipoActividad.descripcion == null)
                {
                    return View(tipoActividad);
                }

                // Verifica si ya existe una actividad con el mismo nombre (ignorando mayúsculas y espacios)
                List<TipoActividadModel> tiposActividad = tipoActividadHandler.RecuperarTiposDeActividadesLowerCase();
                tiposActividad.RemoveAll(t => t.idGenerado == tipoActividad.idGenerado);
                if (tiposActividad.Any(t => t.nombre == tipoActividad.nombre.Replace(" ", "").ToLower()))
                {
                    ModelState.AddModelError("nombre", "Ya existe una actividad con ese nombre.");
                    return View(tipoActividad);
                }

                // Edita el tipo de actividad en la base de datos
                tipoActividadHandler.EditarTipoActividad(tipoActividad);
                return RedirectToAction("ListaTiposActividad", "TipoActividad");
            }
            catch
            {
                return View();
            }
        }
    }
}