using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;
using webMetics.Models;
/* 
 * Controlador de la entidad Asesor
 * Los asesores son los profesores a cargo de impartir el grupo
 * En esta clase se puede retornar todos los asesores, editar, agregar y eliminar algun asesor 
 */
namespace webMetics.Controllers
{
    // Controlador para gestionar las operaciones relacionadas con los asesores
    public class AsesorController : Controller
    {
        public UsuarioHandler accesoAUsuario;
        public TemaHandler accesoATema;
        public AsesorHandler accesoAAsesor;
        public GrupoHandler GrupoHandler;

        private readonly IWebHostEnvironment _environment;

        public AsesorController(IWebHostEnvironment environment)
        {
            _environment = environment;

            accesoATema = new TemaHandler(environment);
            accesoAUsuario = new UsuarioHandler(environment);
            accesoAAsesor = new AsesorHandler(environment);
            GrupoHandler = new GrupoHandler(environment);
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

        /* Método de la vista ListaAsesores que muestra todos los asesores */
        public ActionResult ListaAsesores()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            if (TempData["errorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["errorMessage"].ToString();
            }
            if (TempData["successMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["successMessage"].ToString();
            }

            // Obtener la lista de asesores y enviarla a la vista a través de ViewBag
            ViewBag.Asesores = accesoAAsesor.ObtenerListaAsesores();
            return View();
        }

        /* Método de la vista del formulario para crear un asesor */
        public ActionResult AgregarAsesor()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            if (TempData["errorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["errorMessage"].ToString();
            }
            if (TempData["successMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["successMessage"].ToString();
            }

            ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();

            return View();
        }

        /* Método para procesar el formulario con los datos necesarios para crear un asesor */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearAsesor(AsesorModel asesor)
        {
            bool exito;
            try
            {
                if (ModelState.IsValid)
                {
                    ViewBag.Role = GetRole();
                    ViewBag.Id = GetId();

                    if (accesoAUsuario.ExisteUsuario(asesor.identificacion))
                    {
                        // Verificar si ya existe un asesor con los mismos datos en la base de datos
                        List<AsesorModel> asesores = accesoAAsesor.ObtenerListaAsesores();
                        if (asesores.Any(a => a.identificacion == asesor.identificacion))
                        {
                            exito = accesoAAsesor.EditarAsesor(asesor);
                        }
                        else
                        {
                            exito = accesoAAsesor.CrearAsesor(asesor);
                        }
                    }
                    else
                    {
                        accesoAUsuario.CrearUsuario(asesor.identificacion, "pass"); // Esta contraseña es provisional en la base de datos
                        exito = accesoAAsesor.CrearAsesor(asesor);
                    }

                    // Si el asesor se creó correctamente, mostrar el mensaje de exito y redirigir a la lista de asesores
                    if (exito)
                    {
                        ViewBag.ExitoAlCrear = exito;
                        TempData["successMessage"] = "El asesor fue creado con éxito.";
                        return RedirectToAction("ListaAsesores");
                    } 
                    else
                    {
                        TempData["errorMessage"] = "Hubo un error y no se pudo crear el asesor.";
                        return RedirectToAction("ListaAsesores");
                    }
                }
                else
                {
                    // Si el formulario no es válido o hubo algún problema, regresar a la vista del formulario
                    ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                    return View(asesor);
                }
            }
            catch (Exception)
            {
                ViewBag.Message = "Hubo un error y no se pudo enviar la petición de crear el asesor.";

                ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                return View("AgregarAsesor");
            }
        }

        /* Método para eliminar un asesor */
        public ActionResult EliminarAsesor(string idAsesor)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            // Verificar si se puede eliminar el asesor (no está asignado a ningún grupo)
            bool eliminar = accesoAAsesor.PuedeEliminarAsesor(idAsesor);

            if (eliminar)
            {
                bool exito = accesoAAsesor.EliminarAsesor(idAsesor);
                if (exito)
                {
                    ViewBag.ExitoAlCrear = exito;
                    TempData["Message"] = "Se eliminó al asesor.";
                    return RedirectToAction("ListaAsesores");
                }
                else
                {
                    ViewBag.ExitoAlCrear = exito;
                    ViewBag.Message = "Hubo un error y no se pudo eliminar al asesor.";
                    ModelState.Clear();
                    return View();
                }
            }
            else
            {
                TempData["Message"] = "No se puede eliminar el asesor porque está asignado a un módulo.";
                return RedirectToAction("ListaAsesores");
            }
        }

        /* Método de la vista del formulario para editar a un asesor */
        public ActionResult EditarAsesor(string idAsesor)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                // Buscar el asesor a editar en la lista de asesores y obtenerlo
                AsesorModel asesor = accesoAAsesor.ObtenerListaAsesores().Find(asesorModel => asesorModel.identificacion == idAsesor);

                // Si no se encuentra el asesor a editar, redirigir a la lista de asesores
                if (asesor == null)
                {
                    return RedirectToAction("ListaAsesores");
                }
                else
                {
                    // Mostrar la vista del formulario con los datos del asesor a editar
                    ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                    return View(asesor);
                }
            }
            catch
            {
                return RedirectToAction("ListaAsesores");
            }
        }

        /* Método de la vista del formulario con los datos necesarios del modelo para editar a un asesor */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarAsesor(AsesorModel asesor)
        {
            try
            {
                if (ModelState.IsValid) 
                {
                    ViewBag.Role = GetRole();
                    ViewBag.Id = GetId();

                    bool exito = accesoAAsesor.EditarAsesor(asesor);

                    if (exito)
                    {
                        TempData["SuccessMessage"] = "Los datos fueron guardados.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "No se pudieron actualizar los datos del asesor.";
                    }

                    return RedirectToAction("ListaAsesores");
                } 
                else
                {
                    ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                    return View("EditarAsesor", asesor);
                }
            }
            catch
            {
                // En caso de error, regresar a la vista del formulario
                ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                return View("EditarAsesor");
            }
        }
    }
}