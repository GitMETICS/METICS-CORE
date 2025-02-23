using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using webMetics.Handlers;
using webMetics.Models;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

/* 
 * Controlador de la entidad Grupo
 * En esta clase se puede retornar todos los grupos, editar, agregar y eliminar algun grupo 
 */

namespace webMetics.Controllers
{
    public class GrupoController : Controller
    {
        private protected ParticipanteHandler accesoAParticipante;
        private protected GrupoHandler accesoAGrupo;
        private protected TemaHandler accesoATema;
        private protected CategoriaHandler accesoACategoria;
        private protected AsesorHandler accesoAAsesor;
        private protected InscripcionHandler accesoAInscripcion;
        private protected GrupoTemaHandler accesoAGrupoTema;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public GrupoController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
            
            accesoAParticipante = new ParticipanteHandler(environment, configuration);
            accesoACategoria = new CategoriaHandler(environment, configuration);
            accesoAGrupo = new GrupoHandler(environment, configuration);
            accesoATema = new TemaHandler(environment, configuration);
            accesoAAsesor = new AsesorHandler(environment, configuration);
            accesoAInscripcion = new InscripcionHandler(environment, configuration);
            accesoAGrupoTema = new GrupoTemaHandler(environment, configuration,accesoATema); // Añadir el GrupoTemaHandler
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

        /*
         * Método de la vista ListaGruposDisponibles que recupera y muestra todos los grupos disponibles para inscripción.
         * Un grupo se considera disponible si:
         * - La fecha de inscripción no ha expirado.
         * - La fecha de inicio aún no ha pasado.
         * - El estado del grupo es "visible".
         *
         * Este método también gestiona la lógica de roles de usuario, permitiendo que los participantes, inscriptores y asesores vean
         * grupos según su rol. Además, se proporciona información sobre grupos a los que el usuario ya está inscrito.
         */
        public ActionResult ListaGruposDisponibles()
        {
            const int RolUsuarioParticipante = 0;
            const int RolUsuarioAdmin = 1;
            const int RolUsuarioAsesor = 2;

            int rolUsuario = GetRole();
            string idUsuario = GetId();

            // Obtener la lista de grupos y participantes
            List<GrupoModel> listaGrupos = accesoAGrupo.ObtenerListaGrupos();
            List<InscripcionModel> participantesEnGrupos = accesoAInscripcion.ObtenerInscripciones();

            // Inicializar las propiedades de ViewBag
            ViewBag.ListaGrupos = listaGrupos
                .OrderBy(grupo => grupo.cupoActual < grupo.cupo ? 0 : 1) // Prioriza cupo disponible
                .ThenBy(grupo => grupo.fechaInicioInscripcion) // Luego, prioriza fecha de inscripción
                .ThenBy(grupo => grupo.nombre) // Finalmente, ordena por nombre
                .ToList();

            ViewBag.ParticipantesEnGrupos = participantesEnGrupos;
            ViewBag.IdParticipante = string.Empty;

            // Manejar los roles de usuario y las suscripciones a grupos
            if (!string.IsNullOrEmpty(idUsuario))
            {
                ViewBag.IdParticipante = idUsuario;

                switch (rolUsuario)
                {
                    case RolUsuarioParticipante:
                        var gruposInscritos = accesoAGrupo.ObtenerListaGruposParticipante(idUsuario);
                        ViewBag.GruposInscritos = gruposInscritos;

                        if (gruposInscritos != null)
                        {
                            ViewBag.ListaGrupos = listaGrupos
                                .Where(grupo => !gruposInscritos.Any(inscrito => inscrito.idGrupo == grupo.idGrupo))
                                .OrderBy(grupo => grupo.cupoActual < grupo.cupo ? 0 : 1) // Prioriza cupo disponible
                                .ThenBy(grupo => grupo.fechaInicioInscripcion) // Luego, prioriza fecha de inscripción
                                .ThenBy(grupo => grupo.nombre) // Finalmente, ordena por nombre
                                .ToList();
                        }
                        break;

                    case RolUsuarioAdmin:
                        ViewBag.ListaGrupos = listaGrupos
                            .OrderBy(grupo => grupo.cupoActual < grupo.cupo ? 0 : 1) // Prioriza cupo disponible
                            .ThenBy(grupo => grupo.fechaInicioInscripcion) // Luego, prioriza fecha de inscripción
                            .ThenBy(grupo => grupo.nombre) // Finalmente, ordena por nombre
                            .ToList();
                        break;

                    case RolUsuarioAsesor:
                        var gruposAsesor = accesoAGrupo.ObtenerListaGruposAsesor(idUsuario);
                        var gruposInscritosAsesor = accesoAGrupo.ObtenerListaGruposParticipante(idUsuario);
                        ViewBag.GruposInscritos = gruposInscritosAsesor;

                        var listaGruposAsesor = listaGrupos;

                        if (gruposInscritosAsesor != null)
                        {
                            listaGruposAsesor = listaGrupos
                                .Where(grupo => !gruposInscritosAsesor.Any(inscrito => inscrito.idGrupo == grupo.idGrupo))
                                .OrderBy(grupo => grupo.cupoActual < grupo.cupo ? 0 : 1) // Prioriza cupo disponible
                                .ThenBy(grupo => grupo.fechaInicioInscripcion) // Luego, prioriza fecha de inscripción
                                .ThenBy(grupo => grupo.nombre) // Finalmente, ordena por nombre
                                .ToList();
                        }

                        if (gruposAsesor != null)
                        {
                            listaGruposAsesor = listaGruposAsesor
                                .Where(grupo => !gruposAsesor.Any(grupoAux => grupoAux.idGrupo == grupo.idGrupo))
                                .OrderBy(grupo => grupo.cupoActual < grupo.cupo ? 0 : 1) // Prioriza cupo disponible
                                .ThenBy(grupo => grupo.fechaInicioInscripcion) // Luego, prioriza fecha de inscripción
                                .ThenBy(grupo => grupo.nombre) // Finalmente, ordena por nombre
                                .ToList();
                        }

                        ViewBag.ListaGrupos = listaGruposAsesor
                            .OrderBy(grupo => grupo.cupoActual < grupo.cupo ? 0 : 1) // Prioriza cupo disponible
                            .ThenBy(grupo => grupo.fechaInicioInscripcion) // Luego, prioriza fecha de inscripción
                            .ThenBy(grupo => grupo.nombre) // Finalmente, ordena por nombre
                            .ToList();
                        break;
                }
            }

            // Definir propiedades adicionales de ViewBag
            ViewBag.DateNow = DateTime.Now;
            ViewBag.Role = rolUsuario;
            ViewBag.Id = idUsuario;

            // Gestionar mensajes de TempData
            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View();
        }

        public IActionResult BuscarGrupos(string searchTerm, int userRole, string userId)
        {
            const int RolUsuarioParticipante = 0;
            const int RolUsuarioAdmin = 1;
            const int RolUsuarioAsesor = 2;

            // Obtener la lista de participantes
            var listaGrupos = accesoAGrupo.ObtenerListaGrupos();

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                listaGrupos = listaGrupos.Where(p =>
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.descripcion.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombreAsesor.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombreCategoria.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.modalidad.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.lugar.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.TemasSeleccionadosNombres.Any(t => t.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            ViewBag.ListaGrupos = listaGrupos;
            ViewBag.ParticipantesEnGrupos = accesoAInscripcion.ObtenerInscripciones();
            ViewBag.IdParticipante = string.Empty;

            // Manejar los roles de usuario y las suscripciones a grupos
            if (!string.IsNullOrEmpty(userId))
            {
                ViewBag.IdParticipante = userId;

                switch (userRole)
                {
                    case RolUsuarioParticipante:
                        var gruposInscritos = accesoAGrupo.ObtenerListaGruposParticipante(userId);
                        ViewBag.GruposInscritos = gruposInscritos;

                        if (gruposInscritos != null)
                        {
                            ViewBag.ListaGrupos = listaGrupos
                                .Where(grupo => !gruposInscritos.Any(inscrito => inscrito.idGrupo == grupo.idGrupo))
                                .OrderBy(grupo => grupo.cupoActual < grupo.cupo ? 0 : 1) // Prioriza cupo disponible
                                .ThenBy(grupo => grupo.fechaInicioInscripcion) // Luego, prioriza fecha de inscripción
                                .ThenBy(grupo => grupo.nombre) // Finalmente, ordena por nombre
                                .ToList();
                        }
                        break;

                    case RolUsuarioAdmin:
                        ViewBag.ListaGrupos = listaGrupos
                            .OrderBy(grupo => grupo.cupoActual < grupo.cupo ? 0 : 1) // Prioriza cupo disponible
                                .ThenBy(grupo => grupo.fechaInicioInscripcion) // Luego, prioriza fecha de inscripción
                                .ThenBy(grupo => grupo.nombre) // Finalmente, ordena por nombre
                                .ToList();
                        break;

                    case RolUsuarioAsesor:
                        var gruposAsesor = accesoAGrupo.ObtenerListaGruposAsesor(userId);
                        var gruposInscritosAsesor = accesoAGrupo.ObtenerListaGruposParticipante(userId);
                        ViewBag.GruposInscritos = gruposInscritosAsesor;

                        var listaGruposAsesor = listaGrupos;

                        if (gruposInscritosAsesor != null)
                        {
                            listaGruposAsesor = listaGrupos
                                .Where(grupo => !gruposInscritosAsesor.Any(inscrito => inscrito.idGrupo == grupo.idGrupo))
                                .OrderBy(grupo => grupo.cupoActual < grupo.cupo ? 0 : 1) // Prioriza cupo disponible
                                .ThenBy(grupo => grupo.fechaInicioInscripcion) // Luego, prioriza fecha de inscripción
                                .ThenBy(grupo => grupo.nombre) // Finalmente, ordena por nombre
                                .ToList();
                        }

                        if (gruposAsesor != null)
                        {
                            listaGruposAsesor = listaGruposAsesor
                                .Where(grupo => !gruposAsesor.Any(grupoAux => grupoAux.idGrupo == grupo.idGrupo))
                                .OrderBy(grupo => grupo.cupoActual < grupo.cupo ? 0 : 1) // Prioriza cupo disponible
                                .ThenBy(grupo => grupo.fechaInicioInscripcion) // Luego, prioriza fecha de inscripción
                                .ThenBy(grupo => grupo.nombre) // Finalmente, ordena por nombre
                                .ToList();
                        }

                        ViewBag.ListaGrupos = listaGruposAsesor
                            .OrderBy(grupo => grupo.cupoActual < grupo.cupo ? 0 : 1) // Prioriza cupo disponible
                            .ThenBy(grupo => grupo.fechaInicioInscripcion) // Luego, prioriza fecha de inscripción
                            .ThenBy(grupo => grupo.nombre) // Finalmente, ordena por nombre
                            .ToList();
                        break;
                }
            }

            ViewBag.DateNow = DateTime.Now;
            ViewBag.Role = userRole;
            ViewBag.Id = userId;

            // Gestionar mensajes de TempData
            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View("ListaGruposDisponibles");
        }

        /* Método para descargar el programa del grupo */
        public ActionResult DescargarArchivo(int idGrupo)
        {
            // Obtener el archivo y devolverlo para su descarga
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            byte[] archivo = accesoAGrupo.ObtenerArchivo(idGrupo);

            return File(archivo, "application/octet-stream", grupo.nombreArchivo);
        }

        /* Vista del formulario para crear un grupo */
        public ActionResult CrearGrupo()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            List<SelectListItem> asesores = accesoAAsesor.ObtenerListaSeleccionAsesores();
            if (asesores.Count == 0)
            {
                TempData["errorMessage"] = "No hay facilitadores(as) disponibles para crear un módulo.";
                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }

            List<SelectListItem> temas = accesoATema.ObtenerListaSeleccionTemas();
            if (temas.Count == 0)
            {
                TempData["errorMessage"] = "No hay temas disponibles para crear un módulo.";
                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }

            ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
            ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();
            ViewData["Asesores"] = accesoAAsesor.ObtenerListaSeleccionAsesores();
            return View();
        }

        /* Vista del formulario para editar un grupo con los datos ingresados del modelo */
        [HttpPost]
        // [ValidateAntiForgeryToken]
        public ActionResult CrearGrupo(GrupoModel grupo, int[] temasSeleccionados)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                List<TemaModel> temasDisponiblesList = accesoATema.ObtenerTemas();

                if (ModelState.IsValid)
                {
                    // Validar tamaño del archivo adjunto
                    if (grupo.archivoAdjunto != null && grupo.archivoAdjunto.Length > 5242880) // 5MB en bytes
                    {
                        ModelState.AddModelError("archivoAdjunto", "El archivo no puede ser mayor a 5MB.");
                        ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                        ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();
                        ViewData["Asesores"] = accesoAAsesor.ObtenerListaSeleccionAsesores();
                        return View(grupo);
                    }

                    // Validar fechas de inicio y finalización
                    if (grupo.fechaInicioInscripcion > grupo.fechaFinalizacionInscripcion || grupo.fechaInicioGrupo > grupo.fechaFinalizacionGrupo)
                    {
                        if (grupo.fechaInicioInscripcion > grupo.fechaFinalizacionInscripcion)
                        {
                            ModelState.AddModelError("fechaFinalizacionInscripcion", "La fecha de inicio de la inscripción debe ser antes de la fecha de finalización.");
                        }

                        if (grupo.fechaInicioGrupo > grupo.fechaFinalizacionGrupo)
                        {
                            ModelState.AddModelError("fechaFinalizacionGrupo", "La fecha de inicio del módulo debe ser antes de la fecha de finalización.");
                        }

                        ViewBag.ErrorMessage = "La fecha de finalización no puede ser antes de las fechas de inicio.";
                        ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                        ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();
                        ViewData["Asesores"] = accesoAAsesor.ObtenerListaSeleccionAsesores();
                        return View(grupo);
                    }

                    // Validar fecha de finalización de inscripción antes de la fecha de inicio del grupo
                    if (grupo.fechaFinalizacionInscripcion > grupo.fechaFinalizacionGrupo)
                    {
                        ModelState.AddModelError("fechaFinalizacionInscripcion", "La fecha de finalización de inscripción debe ser antes de la fecha de finalización del módulo.");

                        ViewBag.ErrorMessage = "La fecha final de inscripción debe ser antes de la fecha de fin de clases.";
                        ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                        ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();
                        ViewData["Asesores"] = accesoAAsesor.ObtenerListaSeleccionAsesores();
                        return View(grupo);
                    }
                    if (grupo.fechaInicioInscripcion > grupo.fechaInicioGrupo)
                    {
                        ModelState.AddModelError("fechaInicioInscripcion", "La fecha de inicio de inscripción no puede ser después de la fecha de inicio del módulo.");

                        ViewBag.ErrorMessage = "La fecha de inicio de inscripción no puede ser después de la fecha de inicio de clases.";
                        ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                        ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();
                        ViewData["Asesores"] = accesoAAsesor.ObtenerListaSeleccionAsesores();
                        return View(grupo);
                    }


                    // Validar que se hayan seleccionado temas
                    if (temasSeleccionados == null || !temasSeleccionados.Any())
                    {
                        ViewBag.ErrorMessage = "Debe seleccionar al menos una competencia.";
                        ModelState.AddModelError("temasSeleccionados", "Debe seleccionar al menos una competencia.");
                        return View(grupo);
                    }

                    accesoAGrupo.CrearGrupo(grupo, temasSeleccionados);
                    TempData["successMessage"] = "Se guardó el nuevo módulo.";
                    return RedirectToAction("ListaGruposDisponibles", "Grupo");
                }
                else
                {
                    ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                    ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();
                    ViewData["Asesores"] = accesoAAsesor.ObtenerListaSeleccionAsesores();
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
                    return RedirectToAction("ListaGruposDisponibles");
                }
                else
                {
                    TempData["Message"] = "No se pudo cambiar el estado debido a un problema";
                    return RedirectToAction("ListaGruposDisponibles");
                }
            }
            catch (Exception)
            {
                TempData["Message"] = "Hubo un error y no se pudo enviar la petición de cambiar el estado del módulo.";
                return RedirectToAction("ListaGruposDisponibles");
            }
        }

        /* Vista del formulario para editar un grupo */
        public ActionResult EditarGrupo(int idGrupo)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
                List<TemaModel> temasSeleccionadosList = accesoAGrupoTema.ObtenerTemasDelGrupo(idGrupo);
                List<TemaModel> temasDisponiblesList = accesoATema.ObtenerTemas();
                if (grupo == null)
                {
                    TempData["errorMessage"] = "Ocurrió un error al obtener los datos del módulo.";
                }
                else
                {
                    byte[] pdfBytes = accesoAGrupo.ObtenerArchivo(idGrupo);
                    string pdfBase64 = Convert.ToBase64String(pdfBytes);

                    ViewBag.Adjunto = pdfBase64;

                    ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                    ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();
                    ViewData["Asesores"] = accesoAAsesor.ObtenerListaSeleccionAsesores();
                    grupo.TemasSeleccionados = temasSeleccionadosList.Select(t => t.idTema).ToList();
                    ViewData["TemasSeleccionadosCheckList"] = accesoAGrupoTema.ObtenerTemasDelGrupoSelectList(idGrupo);
                    ViewData["TemasDisponiblesCheckList"] = temasDisponiblesList.Select(t => t.idTema).ToList();

                    ViewData["TemasId"] = temasSeleccionadosList.Select(t => t.idTema).ToList();


                    return View(grupo);
                }
            }
            catch
            {
                TempData["errorMessage"] = "Ocurrió un error al obtener los datos del módulo2.";
            }

            return RedirectToAction("ListaGruposDisponibles");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarGrupo(GrupoModel grupo, int[] temasSeleccionados)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();
            List<TemaModel> temasSeleccionadosList = accesoAGrupoTema.ObtenerTemasDelGrupo(grupo.idGrupo);
            List<TemaModel> temasDisponiblesList = accesoATema.ObtenerTemas();

            try
            {
                // Obtener el archivo adjunto y convertirlo a base64 para mostrarlo en la vista
                byte[] pdfBytes = accesoAGrupo.ObtenerArchivo(grupo.idGrupo);
                string pdfBase64 = Convert.ToBase64String(pdfBytes);
                ViewBag.Adjunto = pdfBase64;

                // Cargar los datos necesarios para la vista
                ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                ViewData["Categorias"] = accesoACategoria.ObtenerListaSeleccionCategorias();
                ViewData["Asesores"] = accesoAAsesor.ObtenerListaSeleccionAsesores();
                grupo.TemasSeleccionados = temasSeleccionadosList.Select(t => t.idTema).ToList();
                ViewData["TemasSeleccionadosCheckList"] = accesoAGrupoTema.ObtenerTemasDelGrupoSelectList(grupo.idGrupo);
                ViewData["TemasDisponiblesCheckList"] = temasDisponiblesList.Select(t => t.idTema).ToList();

                ViewData["TemasId"] = temasSeleccionadosList.Select(t => t.idTema).ToList();


                if (ModelState.IsValid)
                {
                    // Validar tamaño del archivo adjunto
                    if (grupo.archivoAdjunto != null && grupo.archivoAdjunto.Length > 5242880) // 5MB en bytes
                    {
                        ModelState.AddModelError("archivoAdjunto", "El archivo no puede ser mayor a 5MB.");
                        return View(grupo);
                    }

                    // Validar fechas de inicio y finalización
                    if (grupo.fechaInicioInscripcion > grupo.fechaFinalizacionInscripcion || grupo.fechaInicioGrupo > grupo.fechaFinalizacionGrupo)
                    {
                        if (grupo.fechaInicioInscripcion > grupo.fechaFinalizacionInscripcion)
                        {
                            ModelState.AddModelError("fechaFinalizacionInscripcion", "La fecha de inicio de la inscripción debe ser antes de la fecha de finalización.");
                        }

                        if (grupo.fechaInicioGrupo > grupo.fechaFinalizacionGrupo)
                        {
                            ModelState.AddModelError("fechaFinalizacionGrupo", "La fecha de inicio del módulo debe ser antes de la fecha de finalización.");
                        }

                        ViewBag.ErrorMessage = "La fecha de finalización no puede ser antes de las fechas de inicio.";
                        return View(grupo);
                    }

                    // Validar fecha de finalización de inscripción antes de la fecha de finalización del grupo
                    if (grupo.fechaFinalizacionInscripcion > grupo.fechaFinalizacionGrupo)
                    {
                        ModelState.AddModelError("fechaFinalizacionInscripcion", "La fecha de finalización de inscripción debe ser antes de la fecha de finalización del módulo.");
                        return View(grupo);
                    }
                    if (grupo.fechaInicioInscripcion > grupo.fechaInicioGrupo)
                    {
                        ModelState.AddModelError("fechaInicioInscripcion", "La fecha de inicio de inscripción no puede ser después de la fecha de inicio del módulo.");
                        return View(grupo);
                    }

                    // Validar que se hayan seleccionado temas
                    if (temasSeleccionados == null || !temasSeleccionados.Any())
                    {
                        ViewBag.ErrorMessage = "Debe seleccionar al menos una competencia.";
                        ModelState.AddModelError("temasSeleccionados", "Debe seleccionar al menos una competencia.");
                        return View(grupo);
                    }

                    // Guardar los datos del grupo
                    accesoAGrupo.EditarGrupo(grupo);

                    // Actualizar los temas asociados al grupo
                    bool exito = accesoAGrupoTema.ActualizarTemasPorGrupo(grupo.idGrupo, temasSeleccionados);

                    if (!exito)
                    {
                        TempData["errorMessage"] = "Error al actualizar los temas.";
                        return RedirectToAction("EditarGrupo", new { idGrupo = grupo.idGrupo });
                    }

                    TempData["successMessage"] = "Se guardaron los datos del módulo.";
                    return RedirectToAction("ListaGruposDisponibles", "Grupo");
                }
                else
                {
                    return View(grupo);
                }
            }
            catch
            {
                TempData["errorMessage"] = "Ocurrió un error al obtener los datos del módulo.";
                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }
        }


        public ActionResult VerAdjunto(int idGrupo)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);

                if (grupo != null)
                {
                    byte[] pdfBytes = accesoAGrupo.ObtenerArchivo(idGrupo);
                    string pdfBase64 = Convert.ToBase64String(pdfBytes);

                    ViewBag.Adjunto = pdfBase64;

                    ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
                    ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();
                    return View(grupo);
                }
            }
            catch
            {
                TempData["errorMessage"] = "Ocurrió un error al obtener los datos del módulo.";
            }

            return RedirectToAction("ListaGruposDisponibles");
        }

        public ActionResult EditarAdjunto(int idGrupo)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);

                if (grupo != null)
                {
                    byte[] pdfBytes = accesoAGrupo.ObtenerArchivo(idGrupo);
                    string pdfBase64 = Convert.ToBase64String(pdfBytes);

                    ViewBag.Adjunto = pdfBase64;

                    // Gestionar mensajes de TempData
                    ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
                    ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();
                    return View(grupo);
                }
            }
            catch
            {
                TempData["errorMessage"] = "Ocurrió un error al obtener los datos del módulo.";
            }

            return RedirectToAction("ListaGruposDisponibles");
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
                    ModelState.AddModelError("archivoAdjunto", "Debe seleccionar un archivo adjunto válido. El tamaño máximo es de 5 MB.");
                }

                // Editar el adjunto del grupo y verificar el éxito de la operación
                if (accesoAGrupo.EditarAdjunto(grupo))
                {
                    TempData["successMessage"] = "Se editó el archivo adjunto.";
                }
                else
                {
                    TempData["errorMessage"] = "No se pudo editar el archivo.";
                }

                return RedirectToAction("EditarAdjunto", new { idGrupo = grupo.idGrupo });
            }
            catch
            {
                TempData["errorMessage"] = "No se pudo editar el archivo.";
                return RedirectToAction("ListaGruposDisponibles");
            }
        }

        public ActionResult EditarTemas(int idGrupo)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            if (grupo == null)
            {
                TempData["errorMessage"] = "El grupo no existe.";
                return RedirectToAction("ListaGruposDisponibles");
            }

            // Obtener los temas asociados al grupo
            List<TemaModel> temasAsociados = accesoAGrupoTema.ObtenerTemasDelGrupo(idGrupo);

            // Obtener la lista de todos los temas disponibles
            List<SelectListItem> listaTemas = accesoATema.ObtenerListaSeleccionTemas();
            if (listaTemas.Count == 0)
            {
                TempData["errorMessage"] = "No hay temas disponibles para crear un módulo.";
                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }

            // Marcar los temas seleccionados
            foreach (var tema in listaTemas)
            {
                tema.Selected = temasAsociados.Any(t => t.idTema == Convert.ToInt32(tema.Value));
            }

            ViewBag.TemasAsociados = temasAsociados;
            ViewBag.ListaTemas = listaTemas;

            return View(grupo);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarTemasAsociados(int idGrupo, int[] TemasSeleccionados)
        {
            try
            {
                // Actualizar los temas asociados al grupo
                bool exito = accesoAGrupoTema.ActualizarTemasPorGrupo(idGrupo, TemasSeleccionados);

                if (exito)
                {
                    TempData["successMessage"] = "Temas actualizados correctamente.";
                }
                else
                {
                    TempData["errorMessage"] = "Error al actualizar los temas.";
                }

                return RedirectToAction("EditarGrupo", new { idGrupo });
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = "Hubo un problema al actualizar los temas: " + ex.Message;
                return RedirectToAction("EditarTemas", new { idGrupo });
            }
        }

    }
}