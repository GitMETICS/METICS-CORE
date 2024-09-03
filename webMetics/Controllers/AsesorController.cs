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
        private protected UsuarioHandler accesoAUsuario;
        private protected TemaHandler accesoATema;
        private protected AsesorHandler accesoAAsesor;
        private protected ParticipanteHandler accesoAParticipante;
        private protected GrupoHandler accesoAGrupo;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public AsesorController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            accesoATema = new TemaHandler(environment, configuration);
            accesoAUsuario = new UsuarioHandler(environment, configuration);
            accesoAAsesor = new AsesorHandler(environment, configuration);
            accesoAParticipante = new ParticipanteHandler(environment, configuration);
            accesoAGrupo = new GrupoHandler(environment, configuration);
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
            ViewBag.Asesores = accesoAAsesor.ObtenerAsesores();
            return View();
        }

        /* Método de la vista del formulario para crear un asesor */
        public ActionResult CrearAsesor()
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

        // Método para procesar el formulario con los datos necesarios para crear un asesor
        [HttpPost]
        public ActionResult AgregarAsesor(AsesorModel asesor)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                if (ModelState.IsValid)
                {
                    // Aquí se define que el correo es la identificación.
                    asesor.idAsesor = asesor.correo;

                    if (!accesoAUsuario.ExisteUsuario(asesor.idAsesor))
                    {
                        if (!string.IsNullOrEmpty(asesor.contrasena) && asesor.contrasena == asesor.confirmarContrasena)
                        {
                            accesoAUsuario.CrearUsuario(asesor.idAsesor, asesor.contrasena);
                        }
                        else
                        {
                            ViewBag.ErrorMessage = "Las contraseñas ingresadas deben coincidir.";
                            ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                            return View("CrearAsesor", asesor);
                        }
                    }

                    if (!accesoAAsesor.ExisteAsesor(asesor.idAsesor))
                    {
                        accesoAAsesor.CrearAsesor(asesor);

                        TempData["successMessage"] = "El/la facilitador(a) se agregó con éxito.";
                        return RedirectToAction("ListaAsesores");

                    }
                    else
                    {
                        TempData["errorMessage"] = "Ya existe un(a) facilitador(a) con los mismos datos.";
                        return RedirectToAction("ListaAsesores");
                    }

                }
                else
                {
                    // Si el formulario no es válido o hubo algún problema, regresar a la vista del formulario
                    ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                    return View("CrearAsesor", asesor);
                }
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "Ocurrió un error crear el/la facilitador(a).";
                return RedirectToAction("ListaAsesores");
            }
        }


        // Método de la vista del formulario para editar a un asesor
        public ActionResult EditarAsesor(string idAsesor)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                AsesorModel asesor = accesoAAsesor.ObtenerAsesor(idAsesor);
                ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                return View(asesor);
            }
            catch
            {
                TempData["errorMessage"] = "Ocurrió un error al obtener los datos del/la facilitador(a).";
                return RedirectToAction("ListaAsesores");
            }
        }

        [HttpPost]
        public ActionResult ActualizarAsesor(AsesorModel asesor)
        {
            int role = GetRole();

            ViewBag.Id = GetId();
            ViewBag.Role = role;

            if (!ModelState.IsValid)
            {
                ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                return View("EditarAsesor", asesor);
            }

            try
            {
                asesor.idAsesor = asesor.correo;
                accesoAAsesor.EditarAsesor(asesor);

                if (role == 1)
                {
                    if (asesor.contrasena == asesor.confirmarContrasena)
                    {
                        accesoAUsuario.EditarUsuario(asesor.idAsesor, 2, asesor.contrasena);
                        TempData["successMessage"] = "Los datos fueron guardados.";
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Las contraseñas ingresadas deben coincidir.";
                        ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                        return View("EditarAsesor", asesor);
                    }
                }
                else
                {
                    TempData["successMessage"] = "Los datos fueron guardados.";
                }

                if (role == 1)
                {
                    return RedirectToAction("ListaAsesores");
                }
                else
                {
                    return RedirectToAction("ListaGruposDisponibles", "Grupo");
                }
            }
            catch
            {
                TempData["errorMessage"] = "Ocurrió un error al editar los datos del/la facilitador(a).";
                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }
        }


        public ActionResult EliminarAsesor(string idAsesor)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            // Verificar si se puede eliminar el asesor (no está asignado a ningún grupo)
            if (PuedeEliminarAsesor(idAsesor))
            {
                bool exito = accesoAAsesor.EliminarAsesor(idAsesor);
                if (exito)
                {
                    TempData["successMessage"] = "El/la facilitador(a) se eliminó.";
                }
                else
                {
                    TempData["errorMessage"] = "Hubo un error y no se pudo eliminar el/la facilitador(a).";
                }
            }
            else
            {
                TempData["errorMessage"] = "No se puede eliminar el/la facilitador(a) porque está asignado(a) a un módulo.";
            }

            return RedirectToAction("ListaAsesores");
        }

        // Método para verificar si un asesor puede ser eliminado
        public bool PuedeEliminarAsesor(string idAsesor)
        {
            bool eliminar = true;

            try
            {
                List<GrupoModel> gruposAsesor = accesoAGrupo.ObtenerListaGruposAsesor(idAsesor);
                if (gruposAsesor != null && gruposAsesor.Any(g => g.esVisible == true))
                {
                    eliminar = false;
                }
            }
            catch
            {
                eliminar = false;
            }

            return eliminar;
        }

        private int GetRole()
        {
            int role = 0;

            if (HttpContext.Request.Cookies.ContainsKey("rolUsuario"))
            {
                role = Convert.ToInt32(Request.Cookies["rolUsuario"]);
            }

            return role;
        }

        private string GetId()
        {
            string id = "";

            if (HttpContext.Request.Cookies.ContainsKey("idUsuario"))
            {
                id = Convert.ToString(Request.Cookies["idUsuario"]);
            }

            return id;
        }
    }
}