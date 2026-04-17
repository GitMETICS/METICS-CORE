using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using webMetics.Handlers;
using webMetics.Models;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace webMetics.Controllers
{
    /// <summary>
    /// Controlador para la entidad Grupo (módulo/taller).
    /// Gestiona la visualización, creación, edición, eliminación y copia de grupos,
    /// así como la administración de archivos adjuntos y temas asociados.
    /// </summary>
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

        /// <summary>Obtiene el rol del usuario actual desde la cookie "rolUsuario".</summary>
        private int GetRole()
        {
            int role = 0;

            if (HttpContext.Request.Cookies.ContainsKey("rolUsuario"))
            {
                role = Convert.ToInt32(Request.Cookies["rolUsuario"]);
            }

            return role;
        }

        /// <summary>Obtiene el identificador del usuario actual desde la cookie "idUsuario".</summary>
        private string GetId()
        {
            string id = "";

            if (HttpContext.Request.Cookies.ContainsKey("idUsuario"))
            {
                id = Convert.ToString(Request.Cookies["idUsuario"]);
            }

            return id;
        }

        /// <summary>
        /// Muestra la lista de grupos disponibles para inscripción, filtrada según el rol del usuario.
        /// Un grupo es visible si su período de inscripción está activo y su estado es visible.
        /// Para participantes y asesores se excluyen los grupos en los que ya están inscritos.
        /// </summary>
        /// <returns>
        /// View: ListaGruposDisponibles —
        /// ViewBag.ListaGrupos (lista ordenada de GrupoModel),
        /// ViewBag.ParticipantesEnGrupos (todas las inscripciones activas),
        /// ViewBag.GruposInscritos (grupos del usuario, solo roles 0 y 2),
        /// ViewBag.IdParticipante, ViewBag.Role, ViewBag.Id, ViewBag.DateNow,
        /// ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, InscripcionHandler.
        /// Role required: Any (comportamiento varía según rol 0 = Participante, 1 = Admin, 2 = Asesor).
        /// </remarks>
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
                .ThenBy(grupo => DateTime.Now < grupo.fechaFinalizacionInscripcion ? 0 : 1) // Prioriza fecha de inscripción
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
                                .ThenBy(grupo => DateTime.Now < grupo.fechaFinalizacionInscripcion ? 0 : 1) // Prioriza fecha de inscripción
                                .ThenBy(grupo => grupo.nombre) // Finalmente, ordena por nombre
                                .ToList();
                        }
                        break;

                    case RolUsuarioAdmin:
                        ViewBag.ListaGrupos = listaGrupos
                            .OrderBy(grupo => grupo.cupoActual < grupo.cupo ? 0 : 1) // Prioriza cupo disponible
                            .ThenBy(grupo => DateTime.Now < grupo.fechaFinalizacionInscripcion ? 0 : 1) // Prioriza fecha de inscripción
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
                                .ThenBy(grupo => DateTime.Now < grupo.fechaFinalizacionInscripcion ? 0 : 1) // Prioriza fecha de inscripción
                                .ThenBy(grupo => grupo.nombre) // Finalmente, ordena por nombre
                                .ToList();
                        }

                        if (gruposAsesor != null)
                        {
                            listaGruposAsesor = listaGruposAsesor
                                .Where(grupo => !gruposAsesor.Any(grupoAux => grupoAux.idGrupo == grupo.idGrupo))
                                .OrderBy(grupo => grupo.cupoActual < grupo.cupo ? 0 : 1) // Prioriza cupo disponible
                                .ThenBy(grupo => DateTime.Now < grupo.fechaFinalizacionInscripcion ? 0 : 1) // Prioriza fecha de inscripción
                                .ThenBy(grupo => grupo.nombre) // Finalmente, ordena por nombre
                                .ToList();
                        }

                        ViewBag.ListaGrupos = listaGruposAsesor
                            .OrderBy(grupo => grupo.cupoActual < grupo.cupo ? 0 : 1) // Prioriza cupo disponible
                            .ThenBy(grupo => DateTime.Now < grupo.fechaFinalizacionInscripcion ? 0 : 1) // Prioriza fecha de inscripción
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

        /// <summary>
        /// Filtra y muestra grupos cuyo nombre, descripción, asesor, categoría, modalidad, lugar o temas
        /// contengan el término de búsqueda. Reutiliza la vista ListaGruposDisponibles.
        /// </summary>
        /// <param name="searchTerm">Texto libre para filtrar grupos.</param>
        /// <param name="userRole">Rol del usuario (0 = Participante, 1 = Admin, 2 = Asesor).</param>
        /// <param name="userId">Identificador del usuario, usado para excluir grupos ya inscritos.</param>
        /// <returns>
        /// View: ListaGruposDisponibles —
        /// ViewBag.ListaGrupos, ViewBag.ParticipantesEnGrupos, ViewBag.GruposInscritos (roles 0/2),
        /// ViewBag.IdParticipante, ViewBag.Role, ViewBag.Id, ViewBag.DateNow,
        /// ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, InscripcionHandler.
        /// Role required: Any.
        /// </remarks>
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

        /// <summary>
        /// Devuelve el archivo de programa del grupo como descarga directa.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <returns>FileResult (application/octet-stream) con el nombre original del archivo.</returns>
        /// <remarks>
        /// Handlers: GrupoHandler.
        /// Role required: Any.
        /// </remarks>
        public ActionResult DescargarArchivo(int idGrupo)
        {
            // Obtener el archivo y devolverlo para su descarga
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            byte[] archivo = accesoAGrupo.ObtenerArchivo(idGrupo);

            return File(archivo, "application/octet-stream", grupo.nombreArchivo);
        }

        /// <summary>
        /// Muestra el formulario vacío para crear un nuevo grupo.
        /// Redirige a ListaGruposDisponibles si no hay asesores o temas disponibles.
        /// </summary>
        /// <returns>
        /// View: CrearGrupo —
        /// ViewData["Temas"], ViewData["Categorias"], ViewData["Asesores"],
        /// ViewBag.Role, ViewBag.Id.
        /// Redirects to ListaGruposDisponibles si no hay asesores o temas; sets TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: AsesorHandler, TemaHandler, CategoriaHandler.
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Procesa el formulario de creación de un nuevo grupo, validando fechas, tamaño del adjunto
        /// y selección de temas antes de persistir.
        /// </summary>
        /// <param name="grupo">Modelo con los datos del nuevo grupo, incluido el archivo adjunto.</param>
        /// <param name="temasSeleccionados">Arreglo de IDs de temas (competencias) seleccionados.</param>
        /// <returns>
        /// Redirects to ListaGruposDisponibles on success; sets TempData["successMessage"].
        /// View: CrearGrupo con errores de validación si los datos son inválidos.
        /// Redirects to ListaGruposDisponibles on exception; sets TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, TemaHandler, CategoriaHandler, AsesorHandler.
        /// Role required: Admin (1).
        /// </remarks>
        [HttpPost]
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

        /// <summary>
        /// Elimina el grupo indicado y redirige a la lista de grupos.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo a eliminar.</param>
        /// <returns>
        /// Redirects to ListaGruposDisponibles on success or failure.
        /// Sets TempData["Message"] si no se pudo eliminar o hubo excepción.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler.
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Alterna el estado de visibilidad (visible/oculto) del grupo indicado.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <returns>
        /// Redirects to ListaGruposDisponibles. Sets TempData["Message"] si ocurre un error.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler.
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Muestra el formulario de edición del grupo, precargado con sus datos actuales,
        /// el archivo adjunto en Base64 y los temas seleccionados.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo a editar.</param>
        /// <returns>
        /// View: EditarGrupo con el modelo GrupoModel —
        /// ViewBag.Adjunto (Base64), ViewData["Temas"], ViewData["Categorias"], ViewData["Asesores"],
        /// ViewData["TemasSeleccionadosCheckList"], ViewData["TemasDisponiblesCheckList"], ViewData["TemasId"],
        /// ViewBag.Role, ViewBag.Id.
        /// Redirects to ListaGruposDisponibles on error; sets TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, GrupoTemaHandler, TemaHandler, CategoriaHandler, AsesorHandler.
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Procesa el formulario de edición de un grupo, validando fechas, tamaño del adjunto y temas,
        /// luego persiste los cambios y actualiza los temas asociados.
        /// </summary>
        /// <param name="grupo">Modelo con los datos actualizados del grupo.</param>
        /// <param name="temasSeleccionados">Arreglo de IDs de temas seleccionados.</param>
        /// <returns>
        /// Redirects to ListaGruposDisponibles on success; sets TempData["successMessage"].
        /// View: EditarGrupo con errores de validación si los datos son inválidos.
        /// Redirects to ListaGruposDisponibles on exception; sets TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, GrupoTemaHandler, TemaHandler, CategoriaHandler, AsesorHandler.
        /// Role required: Admin (1).
        /// </remarks>
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


        /// <summary>
        /// Muestra la vista de visualización del archivo adjunto (programa) de un grupo en Base64.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <returns>
        /// View: VerAdjunto con el modelo GrupoModel —
        /// ViewBag.Adjunto (Base64), ViewBag.Role, ViewBag.Id,
        /// ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// Redirects to ListaGruposDisponibles on error; sets TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler.
        /// Role required: Any.
        /// </remarks>
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

        /// <summary>
        /// Muestra el formulario para reemplazar el archivo adjunto de un grupo, con vista previa en Base64.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <returns>
        /// View: EditarAdjunto con el modelo GrupoModel —
        /// ViewBag.Adjunto (Base64), ViewBag.Role, ViewBag.Id,
        /// ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// Redirects to ListaGruposDisponibles on error; sets TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler.
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Procesa la subida de un nuevo archivo adjunto para el grupo. Límite de 5 MB.
        /// </summary>
        /// <param name="grupo">Modelo que contiene el archivo adjunto (<c>archivoAdjunto</c>) y el <c>idGrupo</c>.</param>
        /// <returns>
        /// Redirects to EditarAdjunto on success or validation failure; sets TempData["successMessage"] or TempData["errorMessage"].
        /// Redirects to ListaGruposDisponibles on exception; sets TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler.
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Muestra el formulario para gestionar los temas (competencias) asociados a un grupo,
        /// marcando los ya seleccionados.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <returns>
        /// View: EditarTemas con el modelo GrupoModel —
        /// ViewBag.TemasAsociados (List&lt;TemaModel&gt;), ViewBag.ListaTemas (con Selected marcado),
        /// ViewBag.Role, ViewBag.Id.
        /// Redirects to ListaGruposDisponibles si el grupo no existe o no hay temas; sets TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, GrupoTemaHandler, TemaHandler.
        /// Role required: Admin (1).
        /// </remarks>
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



        /// <summary>
        /// Persiste la actualización de los temas asociados a un grupo y redirige a la edición del grupo.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <param name="TemasSeleccionados">Arreglo de IDs de temas seleccionados.</param>
        /// <returns>
        /// Redirects to EditarGrupo on success or failure; sets TempData["successMessage"] or TempData["errorMessage"].
        /// Redirects to EditarTemas on exception; sets TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoTemaHandler.
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Crea una copia del grupo indicado con el nombre prefijado "Copia de …" y sin archivo adjunto.
        /// Los temas asociados se replican en la copia.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo original a copiar.</param>
        /// <returns>
        /// Redirects to ListaGruposDisponibles. Sets TempData["successMessage"] on success or TempData["errorMessage"] on failure.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, GrupoTemaHandler.
        /// Role required: Admin (1).
        /// </remarks>
        public ActionResult CopiarGrupo(int idGrupo)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();


                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);

                // Obtener los temas asociados al grupo
                List<TemaModel> temasAsociados = accesoAGrupoTema.ObtenerTemasDelGrupo(idGrupo);

                int[] temasArray = new int[temasAsociados.Count];

                // Convertir temas a valor asociado
                for (int i = 0; i < temasAsociados.Count; i++)
                {
                    temasArray[i] = temasAsociados[i].idTema;
                }

                // Cambiar el nombre del grupo para saber que es una copia
                if (grupo.nombre.Length > 220)
                {
                    // Si el nombre es mayor a 220 caracteres, truncar y añadir "..."
                    grupo.nombre = grupo.nombre.Substring(0, 220) + "...";
                }
                grupo.nombre = "Copia de " + grupo.nombre;

                // Set de archivos en null
                grupo.nombreArchivo = null;
                grupo.archivoAdjunto = null;

                // Copiar atributos del grupo y añadirlo a la lista de grupos como copia
                if (accesoAGrupo.CrearGrupo(grupo, temasArray))
                {
                    TempData["successMessage"] = "Se guardó la copia del módulo.";
                }
                else
                {
                    TempData["errorMessage"] = "No se pudo copiar el módulo.";
                }

            } catch (Exception ex)
            {
                TempData["errorMessage"] = "Ocurrió un error al copiar el módulo: " + ex.Message;
            }
            return RedirectToAction("ListaGruposDisponibles", "Grupo");
        }
    }
}