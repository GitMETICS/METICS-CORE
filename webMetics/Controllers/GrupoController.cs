using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;
using webMetics.Handlers;
using webMetics.Models;

/* 
 * Controlador de la entidad Grupo
 * En esta clase se puede retornar todos los grupos, editar, agregar y eliminar algun grupo 
 */

namespace webMetics.Controllers
{
    public class GrupoController : Controller
    {
        private GrupoHandler accesoAGrupo;
        private TemaHandler accesoATema;
        private AsesorHandler accesoAAsesor;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public GrupoController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            accesoAGrupo = new GrupoHandler(environment, configuration);
            accesoATema = new TemaHandler(environment, configuration);
            accesoAAsesor = new AsesorHandler(environment, configuration);

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

        /* Método de la vista ListaGruposDisponibles que muestra todos los grupos disponibles para inscribirse.
         * Un grupo es disponible si la fecha de inscripción y el día de inicio aún no han pasado y si el estado es visible.
         */
        public ActionResult ListaGruposDisponibles()
        {
            int rolUsuario = GetRole();
            string idUsuario = GetId();

            ViewBag.Role = rolUsuario;
            ViewBag.Id = idUsuario;

            // Obtener y mostrar mensajes de alerta si es necesario
            ViewBag.Message = "";
            if (TempData["errorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["errorMessage"].ToString();
            }
            if (TempData["successMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["successMessage"].ToString();
            }

            List<GrupoModel> listaGrupos = accesoAGrupo.ObtenerListaGrupos();
            ViewBag.ListaGrupos = listaGrupos;
            ViewBag.ParticipantesEnGrupos = accesoAGrupo.ParticipantesEnGrupos();
            ViewBag.IdParticipante = "";

            if (rolUsuario != 0 && idUsuario != "")
            {
                ViewBag.IdParticipante = idUsuario;

                if (rolUsuario == 1)
                {
                    List<GrupoModel> listaGruposInscritos = accesoAGrupo.ObtenerListaGruposParticipante(idUsuario);

                    if (listaGruposInscritos != null)
                    {
                        ViewBag.GruposInscritos = listaGruposInscritos;
                        ViewBag.ListaGrupos = listaGrupos.Where(p => !listaGruposInscritos.Any(x => x.idGrupo == p.idGrupo)).ToList();
                    }
                }
                else
                {
                    if (rolUsuario == 2)
                    {
                        ViewBag.ListaGrupos = accesoAAsesor.ObtenerListaGruposAsesor(idUsuario);
                    }
                }
            }
            
            DateTime now = DateTime.Now;
            ViewBag.DateNow = now;

            // Devolver la vista con la lista de grupos disponibles y participantes en grupos
            return View();
        }

        /* Método para descargar el programa del grupo */
        public ActionResult DescargarArchivo(int idGrupo)
        {
            // Crear el modelo de grupo con el ID proporcionado
            GrupoModel grupo = new GrupoModel
            {
                idGrupo = idGrupo
            };

            // Obtener el archivo y devolverlo para su descarga
            byte[] archivo = accesoAGrupo.ObtenerArchivo(idGrupo);
            return File(archivo, "application/octet-stream", accesoAGrupo.ObtenerNombreArchivo(grupo));
        }

        /* Vista del formulario para crear un grupo */
        public ActionResult CrearGrupo()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            List<AsesorModel> asesores = accesoAAsesor.ObtenerListaAsesores();
            if (asesores.Count == 0)
            {
                TempData["errorMessage"] = "No hay asesores disponibles para crear un módulo.";
                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }

            List<SelectListItem> temas = accesoATema.ObtenerListaSeleccionTemas();
            if (temas.Count == 0)
            {
                TempData["errorMessage"] = "No hay temas disponibles para crear un módulo.";
                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }

            ViewData["Temas"] = temas;
            return View();
        }

        /* Vista del formulario para editar un grupo con los datos ingresados del modelo */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearGrupo(GrupoModel grupo)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                if (ModelState.IsValid)
                {
                    // Validar tamaño del archivo adjunto
                    if (grupo.archivoAdjunto != null && grupo.archivoAdjunto.Length > 5242880) // 5MB en bytes
                    {
                        ModelState.AddModelError("archivoAdjunto", "El archivo no puede ser mayor a 5MB.");
                        ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                        return View(grupo);
                    }

                    // Validar fechas de inicio y finalización
                    if (grupo.fechaInicioInscripcion >= grupo.fechaFinalizacionInscripcion || grupo.fechaInicioGrupo >= grupo.fechaFinalizacionGrupo)
                    {
                        if (grupo.fechaInicioInscripcion >= grupo.fechaFinalizacionInscripcion)
                        {
                            ModelState.AddModelError("fechaFinalizacionInscripcion", "La fecha de inicio de la inscripción debe ser antes de la fecha de finalización.");
                        }

                        if (grupo.fechaInicioGrupo >= grupo.fechaFinalizacionGrupo)
                        {
                            ModelState.AddModelError("fechaFinalizacionGrupo", "La fecha de inicio del módulo debe ser antes de la fecha de finalización.");
                        }

                        ViewBag.ErrorMessage = "La fecha de finalización no puede ser antes de las fechas de inicio.";
                        ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                        return View(grupo);
                    }

                    // Validar fecha de finalización de inscripción antes de la fecha de inicio del grupo
                    if (grupo.fechaFinalizacionInscripcion >= grupo.fechaInicioGrupo)
                    {
                        ModelState.AddModelError("fechaFinalizacionInscripcion", "La fecha de finalización de inscripción debe ser antes de la fecha de inicio del módulo.");
                        ModelState.AddModelError("fechaInicioGrupo", "La fecha de inicio del módulo debe ser después de la fecha de finalización de inscripción.");

                        ViewBag.ErrorMessage = "La fecha final de inscripción no puede ser después de la fecha de inicio de clases.";
                        ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                        return View(grupo);
                    }

                    accesoAGrupo.CrearGrupo(grupo);
                    TempData["successMessage"] = "Los datos del nuevo módulo fueron guardados.";
                    return RedirectToAction("ListaGruposDisponibles", "Grupo");
                }
                else
                {
                    ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                    return View(grupo);
                }
            }
            catch
            {
                TempData["errorMessage"] = "Ocurrió un error al subir los datos del módulo.";
                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }
        }

        /* Método para eliminar un grupo */
        public ActionResult EliminarGrupo(int? idGrupo)
        {
            ViewBag.ExitoAlCrear = false;
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();
                // Eliminar el grupo y verificar el éxito de la operación
                ViewBag.ExitoAlCrear = accesoAGrupo.EliminarGrupo(idGrupo.Value);
                if (ViewBag.ExitoAlCrear)
                {
                    return Redirect("~/Grupo/ListaGruposDisponibles");
                }
                else
                {
                    TempData["Message"] = "El módulo no se pudo eliminar debido a un error, intente de nuevo.";
                    return Redirect("~/Grupo/ListaGruposDisponibles");
                }
            }
            catch (Exception)
            {
                TempData["Message"] = "Hubo un error y no se pudo enviar la petición de eliminar el módulo.";
                return Redirect("~/Grupo/ListaGruposDisponibles");
            }
        }

        /* Cambiar estado de visibilidad de un grupo */
        public ActionResult CambiarEstadoVisible(int idGrupo)
        {
            ViewBag.ExitoAlCrear = false;
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();
                // Cambiar el estado de visibilidad del grupo y verificar el éxito de la operación
                ViewBag.ExitoAlCrear = accesoAGrupo.CambiarEstadoVisible(idGrupo);
                if (ViewBag.ExitoAlCrear)
                {
                    return Redirect("~/Grupo/ListaGruposDisponibles");
                }
                else
                {
                    TempData["Message"] = "No se pudo cambiar el estado debido a un problema";
                    return Redirect("~/Grupo/ListaGruposDisponibles");
                }
            }
            catch (Exception)
            {
                TempData["Message"] = "Hubo un error y no se pudo enviar la petición de cambiar el estado del módulo.";
                return Redirect("~/Grupo/ListaGruposDisponibles");
            }
        }

        /* Vista del formulario para editar un grupo */
        public ActionResult EditarGrupo(int? idGrupo)
        {
            ActionResult vista;
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();
                // Obtener información del grupo a editar
                GrupoModel modificarGrupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo.Value);
                if (modificarGrupo == null)
                {
                    vista = Redirect("~/Grupo/ListaGruposDisponibles");
                }
                else
                {
                    // Obtener el tema seleccionado y la lista de temas disponibles
                    ViewData["TemaSeleccionado"] = accesoAGrupo.ObtenerTemaSeleccionado(modificarGrupo);
                    List<SelectListItem> temas = accesoATema.ObtenerListaSeleccionTemas();
                    SelectListItem temaSeleccionado = ViewData["TemaSeleccionado"] as SelectListItem;

                    // Quitar el tema seleccionado de la lista de temas disponibles y ponerlo al principio
                    SelectListItem temaEncontrado = temas.FirstOrDefault(t => t.Value == temaSeleccionado.Value);
                    if (temaEncontrado != null)
                    {
                        temas.Remove(temaEncontrado);
                    }
                    temas.Insert(0, temaSeleccionado);

                    // Pasar la lista de temas disponibles y el grupo a editar a la vista
                    ViewData["Temas"] = temas;
                    vista = View(modificarGrupo);
                }
            }
            catch
            {
                vista = RedirectToAction("ListaGruposDisponibles");
            }
            return vista;
        }

        /* Vista del formulario para editar un grupo con los datos ingresados del modelo */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarGrupo(GrupoModel grupo)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                if (ModelState.IsValid)
                {
                    // Validar tamaño del archivo adjunto
                    if (grupo.archivoAdjunto != null && grupo.archivoAdjunto.Length > 5242880) // 5MB en bytes
                    {
                        ModelState.AddModelError("archivoAdjunto", "El archivo no puede ser mayor a 5MB.");
                        ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                        return View(grupo);
                    }

                    // Validar fechas de inicio y finalización
                    if (grupo.fechaInicioInscripcion >= grupo.fechaFinalizacionInscripcion || grupo.fechaInicioGrupo >= grupo.fechaFinalizacionGrupo)
                    {
                        if (grupo.fechaInicioInscripcion >= grupo.fechaFinalizacionInscripcion)
                        {
                            ModelState.AddModelError("fechaFinalizacionInscripcion", "La fecha de inicio de la inscripción debe ser antes de la fecha de finalización.");
                        }

                        if (grupo.fechaInicioGrupo >= grupo.fechaFinalizacionGrupo)
                        {
                            ModelState.AddModelError("fechaFinalizacionGrupo", "La fecha de inicio del módulo debe ser antes de la fecha de finalización.");
                        }

                        ViewBag.ErrorMessage = "La fecha de finalización no puede ser antes de las fechas de inicio.";
                        ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                        return View(grupo);
                    }

                    // Validar fecha de finalización de inscripción antes de la fecha de inicio del grupo
                    if (grupo.fechaFinalizacionInscripcion >= grupo.fechaInicioGrupo)
                    {
                        ModelState.AddModelError("fechaFinalizacionInscripcion", "La fecha de finalización de inscripción debe ser antes de la fecha de inicio del módulo.");
                        ModelState.AddModelError("fechaInicioGrupo", "La fecha de inicio del módulo debe ser después de la fecha de finalización de inscripción.");

                        ViewBag.ErrorMessage = "La fecha final de inscripción no puede ser después de la fecha de inicio de clases.";
                        ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                        return View(grupo);
                    }

                    accesoAGrupo.EditarGrupo(grupo);
                    TempData["successMessage"] = "Los datos del módulo fueron guardados.";
                    return RedirectToAction("ListaGruposDisponibles", "Grupo");
                }
                else
                {
                    ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                    return View(grupo);
                }
            }
            catch
            {
                TempData["errorMessage"] = "Ocurrió un error al obtener los datos del módulo.";
                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }
        }

        /* Método para añadir un archivo de programa del grupo */
        [HttpGet]
        public ActionResult EditarAdjunto(int? idGrupo)
        {
            ActionResult vista;
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();
                // Obtener información del grupo para editar el adjunto
                GrupoModel modificarGrupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo.Value);
                if (modificarGrupo == null)
                {
                    vista = Redirect("~/Grupo/ListaGruposDisponibles");
                }
                else
                {
                    vista = View(modificarGrupo);
                }
            }
            catch
            {
                vista = RedirectToAction("ListaGruposDisponibles");
            }
            return vista;
        }

        /* Método para añadir un archivo de programa del grupo */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 5 * 1024 * 1024)]
        public ActionResult EditarAdjunto(GrupoModel grupo)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                if (grupo.archivoAdjunto == null) // 5MB in bytes
                {
                    ModelState.AddModelError("archivoAdjunto", "Debe seleccionar un archivo adjunto válido.");
                    return View(grupo);
                }

                // Editar el adjunto del grupo y verificar el éxito de la operación
                ViewBag.ExitoAlCrear = accesoAGrupo.EditarAdjunto(grupo);

                if (ViewBag.ExitoAlCrear)
                {
                    ViewBag.Message = "El archivo adjunto fue editado con éxito.";
                    ModelState.Clear();
                    return Redirect("~/Grupo/ListaGruposDisponibles");
                }

                ViewBag.ExitoAlCrear = false;
                TempData["msg"] = "No se pudo editar el archivo, intente de nuevo.";
                return Redirect("/Grupo/ListaGruposDisponibles");
            }
            catch (Exception e)
            {
                return View(e);
            }
        }
    }
}