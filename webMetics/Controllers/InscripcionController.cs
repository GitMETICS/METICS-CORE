using webMetics.Handlers;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc;
using NPOI.XSSF.UserModel;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using NPOI.XWPF.UserModel;
using NPOI.SS.UserModel;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using System.Text.RegularExpressions;
using NPOI.SS.Formula.Functions;
using MailKit.Search;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml;
using System.Globalization;
using System.Text;
using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
namespace webMetics.Controllers
{
    /// <summary>
    /// Controlador para la gestión de inscripciones de participantes en grupos.
    /// Cubre alta/baja individual y masiva, importación desde Excel, exportaciones en PDF/Word/Excel,
    /// notificaciones por correo y cambios de estado de inscripción.
    /// Usa IMemoryCache para almacenar la lista de inscripciones durante 30 minutos.
    /// </summary>
    public class InscripcionController : Controller
    {
        private InscripcionHandler accesoAInscripcion;
        private GrupoHandler accesoAGrupo;
        private ParticipanteHandler accesoAParticipante;
        private AsesorHandler accesoAAsesor;
        private UsuarioHandler accesoAUsuario;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        private readonly IMemoryCache _memoryCache;

        public InscripcionController(IWebHostEnvironment environment, IConfiguration configuration, EmailService emailService, IMemoryCache memoryCache)
        {
            _environment = environment;
            _configuration = configuration;
            _emailService = emailService;

            accesoAInscripcion = new InscripcionHandler(environment, configuration);
            accesoAGrupo = new GrupoHandler(environment, configuration);
            accesoAParticipante = new ParticipanteHandler(environment, configuration);
            accesoAAsesor = new AsesorHandler(environment, configuration);
            accesoAUsuario = new UsuarioHandler(environment, configuration);


            _memoryCache = memoryCache;
        }

        /// <summary>Muestra la lista de inscripciones de un grupo específico.</summary>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <returns>
        /// View: ListaDeParticipantesInscritos —
        /// ViewBag.InscritosEnGrupo (List&lt;InscripcionModel&gt;), ViewBag.Role, ViewBag.Id.
        /// </returns>
        /// <remarks>
        /// Handlers: InscripcionHandler.
        /// Role required: Admin (1) o Asesor (2).
        /// </remarks>
        public ActionResult ListaDeParticipantesInscritos(int idGrupo)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            // Obtener la lista de participantes inscritos en el grupo especificado por idGrupo
            ViewBag.InscritosEnGrupo = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);

            return View();
        }

        /// <summary>
        /// Muestra todas las inscripciones del sistema. Usa caché de 30 minutos;
        /// si <paramref name="reload"/> es <c>true</c>, fuerza la recarga desde la base de datos.
        /// </summary>
        /// <param name="reload">Si <c>true</c>, invalida la caché y recarga desde BD.</param>
        /// <returns>
        /// View: VerInscripciones —
        /// ViewBag.ListaInscripciones (List&lt;InscripcionModel&gt; con participante cargado),
        /// ViewBag.TodasLasMedallas, ViewBag.Role, ViewBag.Id,
        /// ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>
        /// Handlers: InscripcionHandler, ParticipanteHandler.
        /// Role required: Admin (1).
        /// </remarks>
        public ActionResult VerInscripciones(bool reload = false)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            List<InscripcionModel> inscripciones;

            // Verificar si los datos están en caché o si reload es true
            if (reload || !_memoryCache.TryGetValue("Inscripciones", out inscripciones))
            {
                // Si no está en caché, obtener los datos de la base de datos
                inscripciones = accesoAInscripcion.ObtenerInscripciones();

                // Si los datos existen, almacenarlos en caché por 30 minutos
                if (inscripciones != null)
                {
                    foreach (InscripcionModel inscripcion in inscripciones)
                    {
                        inscripcion.participante = accesoAParticipante.ObtenerParticipante(inscripcion.idParticipante);
                    }

                    // Almacenar los datos en caché
                    _memoryCache.Set("Inscripciones", inscripciones, TimeSpan.FromMinutes(30));
                }
            }

            // Asignar los datos al ViewBag
            if (inscripciones != null)
            {
                ViewBag.ListaInscripciones = inscripciones;
            }
            else
            {
                ViewBag.ListaInscripciones = null;
            }

            ViewBag.TodasLasMedallas = accesoAParticipante.ObtenerTodasMedallas();


            // Manejar los mensajes de TempData
            if (TempData["errorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["errorMessage"].ToString();
            }
            if (TempData["successMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["successMessage"].ToString();
            }

            return View();
        }

        /// <summary>
        /// Filtra inscripciones por nombre, apellidos, correo, unidad académica, nombre de grupo, horas o estado.
        /// Reutiliza la vista VerInscripciones.
        /// </summary>
        /// <param name="searchTerm">Texto libre para filtrar.</param>
        /// <returns>
        /// View: VerInscripciones —
        /// ViewBag.ListaInscripciones (filtrada), ViewBag.TodasLasMedallas, ViewBag.Role, ViewBag.Id.
        /// </returns>
        /// <remarks>
        /// Handlers: InscripcionHandler, ParticipanteHandler.
        /// Role required: Admin (1).
        /// </remarks>
        public IActionResult BuscarInscripciones(string searchTerm)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            // Obtener la lista de inscripciones
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones();

            if (inscripciones != null)
            {
                foreach (InscripcionModel inscripcion in inscripciones)
                {
                    inscripcion.participante = accesoAParticipante.ObtenerParticipante(inscripcion.idParticipante);
                }

                ViewBag.ListaInscripciones = inscripciones;
            }

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                inscripciones = inscripciones.Where(inscripcion =>
                    inscripcion.participante.unidadAcademica != null && inscripcion.participante.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.segundoApellido != null && inscripcion.participante.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.nombreGrupo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasMatriculadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasAprobadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.estado != null && inscripcion.estado.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }


            ViewBag.ListaInscripciones = inscripciones;
            ViewBag.TodasLasMedallas = accesoAParticipante.ObtenerTodasMedallas();

            return View("VerInscripciones");
        }

        /// <summary>
        /// Muestra el formulario para inscribir manualmente a un participante en cualquier grupo (por nombre y número).
        /// </summary>
        /// <returns>
        /// View: FormularioInscripcion —
        /// ViewBag.ListaNombresGrupos, ViewBag.ListaNumerosGrupos, ViewBag.Role, ViewBag.Id.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler.
        /// Role required: Admin (1).
        /// </remarks>
        public ActionResult FormularioInscripcion()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            var grupos = accesoAGrupo.ObtenerListaGrupos();

            ViewBag.ListaNombresGrupos = new SelectList(grupos, "nombre", "nombre");
            ViewBag.ListaNumerosGrupos = new SelectList(grupos, "numeroGrupo", "numeroGrupo");

            return View("FormularioInscripcion");
        }

        /// <summary>
        /// Procesa el formulario de inscripción manual por nombre y número de grupo.
        /// Invalida la caché de inscripciones al redirigir.
        /// </summary>
        /// <param name="inscripcion">Modelo con nombreGrupo, numeroGrupo e idParticipante.</param>
        /// <returns>
        /// Redirects to VerInscripciones (reload=true) on success; sets TempData["successMessage"] o TempData["errorMessage"].
        /// View: FormularioInscripcion con errores si ModelState es inválido.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, InscripcionHandler (vía Inscribir).
        /// Role required: Admin (1).
        /// </remarks>
        [HttpPost]
        public ActionResult FormularioInscripcion(InscripcionModel inscripcion)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            if (ModelState.IsValid)
            {
                GrupoModel grupo = accesoAGrupo.ObtenerGrupoPorNombre(inscripcion.nombreGrupo, inscripcion.numeroGrupo);
                inscripcion.idGrupo = grupo.idGrupo;

                try
                {
                    Inscribir(inscripcion.idGrupo, inscripcion.idParticipante);

                    TempData["successMessage"] = "Participante inscrito.";
                }
                catch
                {
                    TempData["errorMessage"] = "Error al inscribir al participante.";
                }

                return RedirectToAction("VerInscripciones", "Inscripcion", new { reload = true });
            }
            else
            {
                return View("FormularioInscripcion", inscripcion);
            }
        }
        /// <summary>
        /// Muestra el formulario para inscribir manualmente a un participante en un grupo específico,
        /// con los datos del grupo precargados.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo en el que se inscribirá el participante.</param>
        /// <returns>
        /// View: FormularioInscripcionManual —
        /// ViewBag.IdGrupo, ViewBag.NombreGrupo, ViewBag.NumeroGrupo, ViewBag.Role, ViewBag.Id.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler.
        /// Role required: Admin (1) o Asesor (2).
        /// </remarks>
        public ActionResult FormularioInscripcionManual(int idGrupo)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);

            ViewBag.IdGrupo = idGrupo;
            ViewBag.NombreGrupo = grupo.nombre;
            ViewBag.NumeroGrupo = grupo.numeroGrupo;

            return View("FormularioInscripcionManual");
        }

        /// <summary>
        /// Procesa la inscripción manual de un participante existente en un grupo.
        /// Redirige al asesor a MisModulos; al admin a ListaGruposDisponibles.
        /// </summary>
        /// <param name="inscripcion">Modelo con idGrupo e idParticipante.</param>
        /// <returns>
        /// Redirects to Asesor/MisModulos (rol 2) o Grupo/ListaGruposDisponibles (rol 1) on success.
        /// View: FormularioInscripcionManual con errores si ModelState es inválido.
        /// Sets TempData["errorMessage"] si el participante no existe.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, ParticipanteHandler, InscripcionHandler (vía InscribirManualmente).
        /// Role required: Admin (1) o Asesor (2).
        /// </remarks>
        [HttpPost]
        public async Task<IActionResult> FormularioInscripcionManual(InscripcionModel inscripcion)
        {
            int rolUsuario = GetRole();
            ViewBag.Role = GetRole();
            ViewBag.Id = rolUsuario;

            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(inscripcion.idGrupo);
            ViewBag.IdGrupo = inscripcion.idGrupo;
            ViewBag.NombreGrupo = grupo.nombre;
            ViewBag.NumeroGrupo = grupo.numeroGrupo;

            if (ModelState.IsValid)
            {

                try
                {
                    await InscribirManualmente(inscripcion.idGrupo, inscripcion.idParticipante);
                }
                catch
                {
                    TempData["errorMessage"] = "Error al inscribir al participante.";
                }
                if (rolUsuario == 2)
                {
                    return RedirectToAction("MisModulos", "Asesor");

                }
                else
                {
                    return RedirectToAction("ListaGruposDisponibles", "Grupo");
                }

            }
            else
            {
                return View("FormularioInscripcionManual", inscripcion);
            }
        }
        /// <summary>
        /// Inscribe al usuario sesionado en un grupo. Si el usuario es asesor sin registro de participante,
        /// crea automáticamente el participante a partir de los datos del asesor.
        /// Envía un correo de comprobante de inscripción.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <param name="idParticipante">Correo institucional del participante a inscribir.</param>
        /// <returns>
        /// View: Inscribir —
        /// ViewBag.Titulo, ViewBag.Message, ViewBag.Participante, ViewBag.Grupo, ViewBag.Role, ViewBag.Id.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, ParticipanteHandler, AsesorHandler, InscripcionHandler.
        /// Role required: Participante (0) o Asesor (2).
        /// </remarks>
        public ActionResult Inscribir(int idGrupo, string idParticipante)
        {
            try
            {
                int rolUsuario = GetRole();
                string idUsuario = GetId();

                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
                ParticipanteModel participante = null;
                AsesorModel asesor = null;

                if (rolUsuario == 0) 
                {
                    participante = accesoAParticipante.ObtenerParticipante(idUsuario);
                }

                if (rolUsuario == 2)
                {
                    asesor = accesoAAsesor.ObtenerAsesor(idUsuario);

                    ParticipanteModel participanteAux = accesoAParticipante.ObtenerParticipante(idUsuario);
                    
                    if (participanteAux == null && asesor != null) // Si no existe un participante asociado al Id del usuario asesor, entonces se crea uno
                    {
                        ParticipanteModel nuevoParticipante = new ParticipanteModel
                        {
                            idParticipante = asesor.idAsesor,
                            nombre = asesor.nombre,
                            primerApellido = asesor.primerApellido,
                            segundoApellido = asesor.segundoApellido,
                            correo = asesor.correo,
                            tipoIdentificacion = asesor.tipoIdentificacion,
                            numeroIdentificacion = asesor.numeroIdentificacion,
                            area = "",
                            departamento = "",
                            unidadAcademica = "",
                            sede = "",
                            tipoParticipante = "",
                            condicion = "",
                            telefono = "",
                            horasMatriculadas = 0,
                            horasAprobadas = 0
                        };

                        accesoAParticipante.CrearParticipante(nuevoParticipante);

                        participante = nuevoParticipante;
                    }
                }
                
                if (grupo != null && (participante != null || asesor != null) && accesoAInscripcion.NoEstaInscritoEnGrupo(idGrupo, idParticipante))
                {
                    // Insertar la inscripción y enviar el comprobante por correo electrónico
                    InscripcionModel inscripcion = new InscripcionModel
                    {
                        idGrupo = idGrupo,
                        idParticipante = idParticipante,
                        numeroGrupo = grupo.numeroGrupo,
                        nombreGrupo = grupo.nombre,
                        horasMatriculadas = grupo.cantidadHoras,
                        horasAprobadas = 0,
                        estado = "Inscrito"
                    };

                    bool exito = accesoAInscripcion.InsertarInscripcion(inscripcion);

                    if (exito)
                    {
                        accesoAParticipante.ActualizarHorasMatriculadasParticipante(idParticipante);

                        try
                        {
                            string mensaje = ConstructorDelMensajeNotificacionInscripcion(grupo, participante);
                            EnviarCorreoInscripcion(grupo, mensaje, participante.correo);

                            ViewBag.Titulo = "Inscripción realizada";
                            ViewBag.Message = "El comprobante de inscripción se le ha enviado al correo";
                            ViewBag.Participante = accesoAParticipante.ObtenerParticipante(idParticipante);
                            ViewBag.Grupo = grupo;
                        }
                        catch
                        {
                            // Configurar los datos para mostrar en la vista
                            ViewBag.Titulo = "Inscripción realizada";
                            ViewBag.Message = "Se ha inscrito en el grupo, pero hubo un error al enviar el comprobante de inscripción a su correo institucional.";
                            ViewBag.Grupo = grupo;
                        }
                    }
                    else
                    {
                        ViewBag.Titulo = "No se pudo realizar la inscripción";
                        ViewBag.Message = "Ocurrió un error al procesar la solicitud de inscripción.";
                    }
                }
                else
                {
                    ViewBag.Titulo = "No se pudo realizar la inscripción";
                    ViewBag.Message = "No se puede inscribir en este grupo porque ya está inscrito o ha superado el límite máximo de horas.";
                }

                ViewBag.Role = rolUsuario;
                ViewBag.Id = idUsuario;
            }
            catch
            {
                ViewBag.Titulo = "No se pudo realizar la inscripción";
                ViewBag.Message = "El módulo no se encuentra disponible.";

                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();
            }

            return View();
        }


        /// <summary>
        /// Inscribe manualmente a un participante existente en un grupo (por admin o asesor).
        /// A diferencia de Inscribir, requiere que el participante ya exista en el sistema.
        /// Envía un correo de comprobante de inscripción.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <param name="idParticipante">Correo institucional del participante a inscribir.</param>
        /// <returns>
        /// View con ViewBag.Titulo y ViewBag.Message indicando resultado.
        /// Sets TempData["errorMessage"] si el participante no existe o ya está inscrito.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, ParticipanteHandler, InscripcionHandler.
        /// Role required: Admin (1) o Asesor (2).
        /// </remarks>
        public async Task<IActionResult> InscribirManualmente(int idGrupo, string idParticipante)
        {
            try
            {
                int rolUsuario = GetRole();
                string idUsuario = GetId();

                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
                ParticipanteModel participante = null;

              

                if (rolUsuario == 1 || rolUsuario == 2)
                {
                    ParticipanteModel participanteAux = accesoAParticipante.ObtenerParticipante(idParticipante);


                    if (participanteAux == null)
                    {
                        TempData["errorMessage"] = "Error al inscribir al participante. El correo debe corresponder a un participante existente.";

                        return View();
                    }
                    else
                    {
                        participante = participanteAux;

                    }
                }

                      
                if (grupo != null && (participante != null) && accesoAInscripcion.NoEstaInscritoEnGrupo(idGrupo, idParticipante))
                {
                    // Insertar la inscripción y enviar el comprobante por correo electrónico
                    InscripcionModel inscripcion = new InscripcionModel
                    {
                        idGrupo = idGrupo,
                        idParticipante = idParticipante,
                        numeroGrupo = grupo.numeroGrupo,
                        nombreGrupo = grupo.nombre,
                        horasMatriculadas = grupo.cantidadHoras,
                        horasAprobadas = 0,
                        estado = "Inscrito"
                    };

                    bool exito = await accesoAInscripcion.InsertarInscripcionAsync(inscripcion);

                    if (exito)
                    {
                        TempData["successMessage"] = "Participante inscrito.";

                        accesoAParticipante.ActualizarHorasMatriculadasParticipante(idParticipante);

                        try
                        {
                            string mensaje = ConstructorDelMensajeNotificacionInscripcion(grupo, participante);
                            EnviarCorreoInscripcion(grupo, mensaje, participante.correo);

                            ViewBag.Titulo = "Inscripción realizada";
                            ViewBag.Message = "El comprobante de inscripción se ha enviado al correo";
                            ViewBag.Participante = accesoAParticipante.ObtenerParticipante(idParticipante);
                            ViewBag.Grupo = grupo;
                        }
                        catch
                        {
                            // Configurar los datos para mostrar en la vista
                            ViewBag.Titulo = "Inscripción realizada";
                            ViewBag.Message = "Se ha inscrito en el grupo, pero hubo un error al enviar el comprobante de inscripción a su correo institucional.";
                            ViewBag.Grupo = grupo;
                        }
                    }
                    else
                    {
                        ViewBag.Titulo = "No se pudo realizar la inscripción";
                        ViewBag.Message = "Ocurrió un error al procesar la solicitud de inscripción.";
                    }
                }
                else
                {
                    ViewBag.Titulo = "No se pudo realizar la inscripción";
                    ViewBag.Message = "No se puede inscribir en este grupo porque ya está inscrito o ha superado el límite máximo de horas.";
                    TempData["errorMessage"] = "No se puede inscribir en este grupo porque ya está inscrito o ha superado el límite máximo de horas.";

                }

                ViewBag.Role = rolUsuario;
                ViewBag.Id = idUsuario;
            }
            catch
            {
                ViewBag.Titulo = "No se pudo realizar la inscripción";
                ViewBag.Message = "El módulo no se encuentra disponible.";

                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();
            }

            return View();
        }

        /// <summary>
        /// Elimina la inscripción de un participante en un grupo y recalcula sus horas matriculadas y aprobadas.
        /// Redirige a la URL referente si está disponible; de lo contrario a ListaGruposDisponibles.
        /// </summary>
        /// <param name="nombreGrupo">Nombre del grupo.</param>
        /// <param name="numeroGrupo">Número del grupo.</param>
        /// <param name="idParticipante">Correo institucional del participante.</param>
        /// <returns>
        /// Redirects a Referer o Grupo/ListaGruposDisponibles.
        /// Sets TempData["successMessage"] o TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: InscripcionHandler, ParticipanteHandler.
        /// Role required: Admin (1).
        /// </remarks>
        public ActionResult EliminarInscripcion(string nombreGrupo, int numeroGrupo, string idParticipante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                bool exito = accesoAInscripcion.EliminarInscripcion(nombreGrupo, numeroGrupo, idParticipante);

                if (exito)
                {
                    ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

                    accesoAParticipante.ActualizarHorasMatriculadasParticipante(participante.idParticipante);
                    accesoAParticipante.ActualizarHorasAprobadasParticipante(participante.idParticipante);

                    TempData["successMessage"] = "Se eliminó la inscripción del participante.";
                }
                else
                {
                    TempData["errorMessage"] = "No se pudo eliminar la inscripción del participante.";
                }
            }
            catch
            {
                // Si ocurrió una excepción al intentar eliminar la inscripción, mostrar un mensaje y redirigir a la lista de participantes del grupo
                TempData["errorMessage"] = "No se pudo eliminar la inscripción del participante.";
            }

            var refererUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(refererUrl))
            {
                return Redirect(refererUrl);
            }

            return RedirectToAction("ListaGruposDisponibles", "Grupo");
        }


        /// <summary>
        /// Elimina las inscripciones de una lista de participantes en un grupo y recalcula sus horas.
        /// </summary>
        /// <param name="participantesSeleccionados">Lista de IDs de participantes a desinscribir.</param>
        /// <param name="nombreGrupo">Nombre del grupo.</param>
        /// <param name="numeroGrupo">Número del grupo.</param>
        /// <returns>
        /// Redirects a Referer o Grupo/ListaGruposDisponibles.
        /// Sets TempData["successMessage"] con el conteo de eliminados y/o TempData["errorMessage"] con los fallidos.
        /// </returns>
        /// <remarks>
        /// Handlers: InscripcionHandler, ParticipanteHandler.
        /// Role required: Admin (1).
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarInscripcionesMasivo(List<string> participantesSeleccionados, string nombreGrupo, int numeroGrupo)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                if (participantesSeleccionados == null || !participantesSeleccionados.Any())
                {
                    TempData["errorMessage"] = "Debe seleccionar al menos un participante.";
                }

                int eliminados = 0;
                int errores = 0;

                foreach (var idParticipante in participantesSeleccionados)
                {
                    try
                    {
                        bool exito = accesoAInscripcion.EliminarInscripcion(nombreGrupo, numeroGrupo, idParticipante);

                        ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

                        accesoAParticipante.ActualizarHorasMatriculadasParticipante(participante.idParticipante);
                        accesoAParticipante.ActualizarHorasAprobadasParticipante(participante.idParticipante);

                        if (exito)
                        {
                            eliminados++;
                        }
                        else
                        {
                            errores++;
                        }
                    }
                    catch
                    {
                        errores++;
                    }
                }

                if (eliminados > 0)
                {
                    TempData["successMessage"] = $"Se eliminaron {eliminados} inscripción(es) correctamente.";
                }

                if (errores > 0)
                {
                    TempData["errorMessage"] = $"No se pudieron eliminar {errores} inscripción(es).";
                }
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = $"Ocurrió un error al eliminar las inscripciones: {ex.Message}";
            }

            var refererUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(refererUrl))
            {
                return Redirect(refererUrl);
            }

            return RedirectToAction("ListaGruposDisponibles", "Grupo");
        }


        /// <summary>
        /// Elimina la inscripción de un participante en un grupo por su idGrupo.
        /// </summary>
        /// <param name="idParticipante">Correo institucional del participante.</param>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <returns>
        /// Redirects to Grupo/ListaGruposDisponibles. Sets TempData["errorMessage"] si falla.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, InscripcionHandler, ParticipanteHandler.
        /// Role required: Any (autenticado).
        /// TODO: Eliminar esto porque está repetido (ver EliminarInscripcion).
        /// </remarks>
        public ActionResult DesinscribirParticipante(string idParticipante, int idGrupo)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
                bool exito = accesoAInscripcion.EliminarInscripcion(grupo.nombre, grupo.numeroGrupo, idParticipante);

                if (exito)
                {
                    ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

                    accesoAParticipante.ActualizarHorasMatriculadasParticipante(participante.idParticipante);
                    accesoAParticipante.ActualizarHorasAprobadasParticipante(participante.idParticipante);
                }
                else
                {
                    TempData["errorMessage"] = "Hubo un error y no se pudo eliminar la inscripción.";
                }

                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "Hubo un error y no se pudo enviar la solicitud de eliminar la inscripción.";

                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }
        }

        /// <summary>
        /// Envía al participante el comprobante de inscripción por correo, adjuntando el programa del grupo si existe.
        /// </summary>
        private async Task<IActionResult> EnviarCorreoInscripcion(GrupoModel grupo, string mensaje, string correoParticipante)
        {
            string subject = "Comprobante de Inscripción a Módulo - SISTEMA DE INSCRIPCIONES METICS";

            byte[] archivoAdjunto = accesoAGrupo.ObtenerArchivo(grupo.idGrupo);
            if (archivoAdjunto != null)
            {
                await _emailService.SendEmailAsync(correoParticipante, subject, mensaje, archivoAdjunto, grupo.nombreArchivo);
            }
            else
            {
                // Si no hay archivo adjunto, solo agregar el cuerpo del mensaje al mensaje principal
                await _emailService.SendEmailAsync(correoParticipante, subject, mensaje);
            }
         
            return Ok();
        }

        /// <summary>
        /// Construye el cuerpo HTML del correo de comprobante de inscripción con los datos del grupo y el participante.
        /// </summary>
        /// <param name="grupo">Grupo en el que se inscribió el participante.</param>
        /// <param name="participante">Participante inscrito.</param>
        /// <returns>Cadena HTML con el cuerpo del correo de comprobante.</returns>
        public string ConstructorDelMensajeNotificacionInscripcion(GrupoModel grupo, ParticipanteModel participante)
        {
            // Construir el contenido del mensaje que se enviará por correo electrónico al usuario
            // Este método toma información del módulo y el participante y crea un mensaje personalizado
            // con los detalles de la inscripción

            // Crear el mensaje con información relevante de la inscripción en formato HTML
            string mensaje = "" +
                "<h2>Comprobante de inscripción a módulo - SISTEMA DE INSCRIPCIONES METICS</h2>" +
                "<p>Nombre: " + participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido + "</p>" +
                "<p>Se ha inscrito al módulo: <strong>" + grupo.nombre + " (Grupo " + grupo.numeroGrupo + ")</strong></p>" +

                "<ul>" +
                "<li>Descripción: " + grupo.descripcion + "</li>" +
                "<br>" +
                "<li>Enlace: " + grupo.enlace + "</li>" +
                "<li>Clave de inscripción: " + grupo.claveInscripcion + "</li>" +
                "<br>" +
                "<li>Horario: " + grupo.horario + "</li>" +
                "<li>Modalidad: " + grupo.modalidad + "</li>" +
                "<li>Cantidad de horas: " + grupo.cantidadHoras + "</li>" +
                "<li>Facilitador(a): " + grupo.nombreAsesor + "</li>" +
                "<li>Fecha de inicio: " + grupo.fechaInicioGrupo.ToString("dd/MM/yyyy") + "</li>" +
                "<li>Fecha de finalización: " + grupo.fechaFinalizacionGrupo.ToString("dd/MM/yyyy") + "</li>";
                
            
            if (!(string.Equals(grupo.modalidad, "Autogestionado", StringComparison.OrdinalIgnoreCase) || string.Equals(grupo.modalidad, "Virtual", StringComparison.OrdinalIgnoreCase)))
            {
                mensaje += "<li>Lugar: " + grupo.lugar + "</li>";
            }
            
            mensaje += "</ul>" +
                "<p>Si necesita desinscribirse de este módulo, puede ingresar al sistema y realizarlo desde la plataforma.</p>" +
                "<p> </p>" +
                "<p><b>> Por favor no compartir enlace y contraseña de este curso con ningún otro docente.</b></p>";
                
            return mensaje;
        }

        /// <summary>
        /// Genera y descarga la plantilla Excel para la importación masiva de inscripciones.
        /// Columnas: Correo Institucional, Módulo, Grupo, Horas, Estado, Horas Completadas, Calificación.
        /// </summary>
        /// <returns>FileResult (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet) — "Plantilla_Inscripciones.xlsx".</returns>
        /// <remarks>Role required: Admin (1).</remarks>
        public ActionResult DescargarPlantillaSubirInscripciones()
        {
            XSSFWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Plantilla_Inscripciones");

            string[] columnNames = {
                "Correo Institucional", "Módulo", "Grupo", "Horas", "Estado", "Horas Completadas", "Calificación"
            };

            NPOI.SS.UserModel.IRow row = sheet.CreateRow(0);

            for (int i = 0; i < columnNames.Length; i++)
            {
                NPOI.SS.UserModel.ICell cell = row.CreateCell(i);

                cell.SetCellValue(columnNames[i]);
            }

            string fileName = "Plantilla_Inscripciones.xlsx";
            var stream = new MemoryStream();
            workbook.Write(stream);
            var file = stream.ToArray();

            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Importa inscripciones desde un archivo Excel. Actualiza inscripciones existentes
        /// o crea nuevas, y recalcula las horas del participante.
        /// </summary>
        /// <param name="file">Archivo Excel con columnas: Correo institucional, Módulo, Grupo, Horas,
        /// Estado, Horas completadas, Calificación.</param>
        /// <returns>
        /// Redirects to VerInscripciones (reload=true). Sets TempData["successMessage"] o TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: InscripcionHandler, ParticipanteHandler.
        /// Role required: Admin (1).
        /// </remarks>
        [HttpPost]
        public async Task<IActionResult> SubirArchivoExcelInscripciones(IFormFile file)
        {
            if (file == null)
            {
                TempData["errorMessage"] = "Seleccione un archivo de Excel válido.";
                return RedirectToAction("VerInscripciones");
            }

            try
            {
                List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones();

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using var package = new ExcelPackage(stream);
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        GrupoModel grupo = new GrupoModel
                        {
                            numeroGrupo = int.TryParse(worksheet.Cells[row, GetColumnIndex(worksheet, "Grupo")].Text, out var numeroGrupo) ? numeroGrupo : 0,
                            nombre = worksheet.Cells[row, GetColumnIndex(worksheet, "Módulo")].Text,
                            modalidad = "",
                            cantidadHoras = int.TryParse(worksheet.Cells[row, GetColumnIndex(worksheet, "Horas")].Text, out var cantidadHoras) ? cantidadHoras : 0,
                        };

                        InscripcionModel inscripcion = new InscripcionModel
                        {
                            idParticipante = worksheet.Cells[row, GetColumnIndex(worksheet, "Correo institucional")].Text,
                            numeroGrupo = grupo.numeroGrupo,
                            nombreGrupo = grupo.nombre,
                            horasMatriculadas = grupo.cantidadHoras,
                            horasAprobadas = int.TryParse(worksheet.Cells[row, GetColumnIndex(worksheet, "Horas completadas")].Text, out var horasCompletadas) ? horasCompletadas : 0,
                            estado = worksheet.Cells[row, GetColumnIndex(worksheet, "Estado")].Text,
                            calificacion = worksheet.Cells[row, GetColumnIndex(worksheet, "Calificación")].Text != "" ? double.Parse(worksheet.Cells[row, GetColumnIndex(worksheet, "Calificación")].Text) : 0
                        };

                        ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(inscripcion.idParticipante);

                        if (!string.IsNullOrEmpty(inscripcion.idParticipante))
                        {
                            inscripciones = await IngresarInscripcionAsync(inscripciones, inscripcion, grupo, participante);
                        }
                    }
                }

                TempData["successMessage"] = "El archivo fue subido éxitosamente.";
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = "Error al cargar los datos: " + ex;
            }

            return RedirectToAction("VerInscripciones", "Inscripcion", new { reload = true });
        }

        /// <summary>
        /// Inserta o actualiza una inscripción en la BD y recalcula las horas del participante.
        /// Si ya existe una inscripción con el mismo grupo y participante, la actualiza; de lo contrario la inserta.
        /// </summary>
        private async Task<List<InscripcionModel>> IngresarInscripcionAsync(List<InscripcionModel> inscripciones, InscripcionModel inscripcion, GrupoModel grupo, ParticipanteModel participante)
        {
            if (grupo != null && participante != null)
            {
                bool exito = false;

                InscripcionModel inscripcionEncontrada = inscripciones.Find(
                    inscripcionModel => inscripcionModel.nombreGrupo == inscripcion.nombreGrupo
                                         && inscripcionModel.numeroGrupo == inscripcion.numeroGrupo
                                         && inscripcionModel.idParticipante == inscripcion.idParticipante);

                if (inscripcionEncontrada != null)
                {
                    inscripcion.idInscripcion = inscripcionEncontrada.idInscripcion;
                    exito = await accesoAInscripcion.EditarInscripcionAsync(inscripcion);

                    inscripciones.Remove(inscripcionEncontrada);
                }
                else 
                {
                    exito = await accesoAInscripcion.InsertarInscripcionAsync(inscripcion);
                }

                if (exito)
                {
                    await accesoAParticipante.ActualizarHorasMatriculadasParticipanteAsync(participante.idParticipante);
                    await accesoAParticipante.ActualizarHorasAprobadasParticipanteAsync(participante.idParticipante);
                }
            }

            return inscripciones;
        }

        /// <summary>Busca el índice (base-1) de una columna en la primera fila del worksheet, ignorando acentos y mayúsculas.</summary>
        private int GetColumnIndex(ExcelWorksheet worksheet, string columnName)
        {
            // Normalize and remove accents from the input column name
            string normalizedColumnName = RemoveAccents(columnName.Trim().ToLower());

            // Iterate over the first row to find the column index
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                // Get the header, normalize it, and remove accents
                string header = RemoveAccents(worksheet.Cells[1, col].Text.Trim().ToLower());

                // Compare normalized header with the normalized column name
                if (string.Equals(header, normalizedColumnName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return col;
                }
            }
            throw new Exception($"Column '{columnName}' not found");
        }

        /// <summary>Elimina diacríticos (acentos) de un texto normalizando a Unicode FormD.</summary>
        private string RemoveAccents(string text)
        {
            return string.Concat(text.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
                .Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Genera y descarga un PDF (A2) con la lista de participantes e inscripciones de un grupo específico.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <param name="searchTerm">Texto opcional para filtrar participantes.</param>
        /// <returns>FileResult (application/pdf) — "Lista_de_Participantes_{nombre}.pdf".</returns>
        /// <remarks>
        /// Handlers: GrupoHandler, ParticipanteHandler, InscripcionHandler.
        /// Role required: Admin (1) o Asesor (2).
        /// </remarks>
        public ActionResult ExportarParticipantesPDF(int idGrupo, string? searchTerm)
        {
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                inscripciones = inscripciones.Where(inscripcion =>
                    inscripcion.participante.unidadAcademica != null && inscripcion.participante.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.segundoApellido != null && inscripcion.participante.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.nombreGrupo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasMatriculadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasAprobadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.estado != null && inscripcion.estado.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            var filePath = System.IO.Path.Combine(_environment.WebRootPath, "data", "Lista_de_Participantes.docx");
            PdfWriter writer = new PdfWriter(filePath);
            PdfDocument pdf = new PdfDocument(writer);

            PageSize pageSize = PageSize.A2;  // Puedes elegir PageSize.A3 para un tamaño más pequeño
            iText.Layout.Document document = new iText.Layout.Document(pdf, pageSize);

            // Establecer fuente en negrita para encabezado
            PdfFont boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
            PdfFont regularFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

            Paragraph header1 = new Paragraph("Nombre del módulo: " + grupo.nombre)
                .SetFontSize(10);
            document.Add(header1);

            Paragraph header2 = new Paragraph("Nombre del/la facilitador(a) asociado(a): " + grupo.nombreAsesor)
                .SetFontSize(10);
            document.Add(header2);

            Paragraph header3 = new Paragraph("")
                .SetFontSize(10);
            document.Add(header3);

            // Crear la tabla (9 columnas)
            iText.Layout.Element.Table table = new iText.Layout.Element.Table(new float[] { 3, 2, 2, 3, 2, 3, 2, 2 });
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Agregar encabezados de la tabla con estilo
            string[] headers = { "Nombre del participante", "Correo institucional", "Condición", "Unidad académica", "Teléfono", "Módulo", "Horas aprobadas", "Calificación del módulo" };
            foreach (var headerText in headers)
            {
                table.AddHeaderCell(new Cell().Add(new Paragraph(headerText).SetFont(boldFont).SetFontSize(10))
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY));
            }

            // Rellenar la tabla con los datos
            foreach (var participante in participantes)
            {
                // Obtener las inscripciones del participante
                var inscripcionesParticipante = inscripciones.Where(i => i.idParticipante == participante.idParticipante).ToList();

                if (inscripcionesParticipante.Any())
                {
                    foreach (var inscripcion in inscripcionesParticipante)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(participante.idParticipante).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(participante.condicion).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(participante.unidadAcademica).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(participante.telefono).SetFont(regularFont).SetFontSize(9)));

                        // Datos del módulo
                        table.AddCell(new Cell().Add(new Paragraph(inscripcion.nombreGrupo).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(inscripcion.horasAprobadas.ToString()).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(inscripcion.calificacion.ToString()).SetFont(regularFont).SetFontSize(9)));
                    }
                }
                else
                {
                    // Si no tiene inscripciones, rellenar con "N/A"
                    table.AddCell(new Cell().Add(new Paragraph(participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido).SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph(participante.idParticipante).SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph(participante.condicion).SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph(participante.unidadAcademica).SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph(participante.telefono).SetFont(regularFont).SetFontSize(9)));

                    table.AddCell(new Cell().Add(new Paragraph("N/A").SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph("N/A").SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph("N/A").SetFont(regularFont).SetFontSize(9)));
                }
            }

            // Añadir la tabla al documento
            document.Add(table);

            // Cerrar el documento
            document.Close();

            // Devolver el archivo PDF
            string fileName = "Lista_de_Participantes_" + grupo.nombre + ".pdf";
            return File(System.IO.File.ReadAllBytes(filePath), "application/pdf", fileName);
        }

        /// <summary>
        /// Genera y descarga un Word (.docx) con la lista de participantes e inscripciones de un grupo específico.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <param name="searchTerm">Texto opcional para filtrar participantes.</param>
        /// <returns>FileResult (application/vnd.openxmlformats-officedocument.wordprocessingml.document) — "Lista_de_Participantes_{nombre}.docx".</returns>
        /// <remarks>
        /// Handlers: GrupoHandler, ParticipanteHandler, InscripcionHandler.
        /// Role required: Admin (1) o Asesor (2).
        /// </remarks>
        public ActionResult ExportarParticipantesWord(int idGrupo, string? searchTerm)
        {
            // Obtener la lista de participantes del grupo y la información del grupo
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                inscripciones = inscripciones.Where(inscripcion =>
                    inscripcion.participante.unidadAcademica != null && inscripcion.participante.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.segundoApellido != null && inscripcion.participante.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.nombreGrupo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasMatriculadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasAprobadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.estado != null && inscripcion.estado.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            var fileName = "Lista_de_Participantes_" + grupo.nombre + ".docx";

            XWPFDocument wordDoc = new XWPFDocument();

            // Crear un título
            XWPFParagraph titleParagraph = wordDoc.CreateParagraph();
            titleParagraph.Alignment = ParagraphAlignment.CENTER;
            XWPFRun titleRun = titleParagraph.CreateRun();
            titleRun.SetText("Lista de Participantes");
            titleRun.IsBold = true;
            titleRun.FontSize = 16;
            titleRun.AddBreak(); // Salto de línea después del título

            // Crear tabla con el número de participantes y columnas para los módulos
            XWPFTable table = wordDoc.CreateTable(participantes.Count + 1, 9); // +1 para la fila de encabezado

            // Ajustar los anchos de columna (simulando margen)
            table.SetColumnWidth(0, 750);  // Ajustar más ancho para la columna de identificación
            table.SetColumnWidth(1, 1000);  // Ancho de columna de nombre
            table.SetColumnWidth(2, 1500);  // Columna de correo
            table.SetColumnWidth(3, 750);  // Columna de condición
            table.SetColumnWidth(4, 1500);  // Columna de unidad académica
            table.SetColumnWidth(5, 750);  // Columna de teléfono
            table.SetColumnWidth(6, 1500);  // Nombre del módulo
            table.SetColumnWidth(7, 750);  // Horas aprobadas
            table.SetColumnWidth(8, 750);  // Nota del módulo

            // Estilo para el encabezado
            var headerRow = table.GetRow(0);
            headerRow.GetCell(0).SetText("Nombre del participante");
            headerRow.GetCell(1).SetText("Correo institucional");
            headerRow.GetCell(2).SetText("Condición");
            headerRow.GetCell(3).SetText("Unidad académica");
            headerRow.GetCell(4).SetText("Teléfono");
            headerRow.GetCell(5).SetText("Módulo");
            headerRow.GetCell(6).SetText("Horas aprobadas");
            headerRow.GetCell(7).SetText("Calificación del módulo");

            // Rellenar la tabla con los datos de los participantes e inscripciones
            int rowIndex = 1; // Comenzar después del encabezado

            foreach (var participante in participantes)
            {
                // Obtener las inscripciones del participante
                var inscripcionesParticipante = inscripciones.Where(i => i.idParticipante == participante.idParticipante).ToList();

                // Si el participante tiene inscripciones, crear una fila por cada módulo inscrito
                if (inscripcionesParticipante.Any())
                {
                    foreach (var inscripcion in inscripcionesParticipante)
                    {
                        var row = table.CreateRow(); // Crear una nueva fila para cada inscripción

                        row.GetCell(0).SetText(participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido);
                        row.GetCell(1).SetText(participante.idParticipante);
                        row.GetCell(2).SetText(participante.condicion);
                        row.GetCell(3).SetText(participante.unidadAcademica);
                        row.GetCell(4).SetText(participante.telefono);

                        // Información del módulo
                        row.GetCell(5).SetText(grupo.nombre);
                        row.GetCell(6).SetText(inscripcion.horasAprobadas.ToString());
                        row.GetCell(7).SetText(inscripcion.calificacion.ToString());

                        rowIndex++;
                    }
                }
            }

            // Guardar el archivo y devolver el documento Word como respuesta
            var stream = new MemoryStream();
            wordDoc.Write(stream);
            var file = stream.ToArray();
            return File(file, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        /// <summary>
        /// Genera y descarga un Excel (.xlsx) con la lista de participantes e inscripciones de un grupo específico.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <param name="searchTerm">Texto opcional para filtrar participantes.</param>
        /// <returns>FileResult (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet) — "Lista_de_Participantes_{nombre}.xlsx".</returns>
        /// <remarks>
        /// Handlers: GrupoHandler, ParticipanteHandler, InscripcionHandler.
        /// Role required: Admin (1) o Asesor (2).
        /// </remarks>
        public ActionResult ExportarParticipantesExcel(int idGrupo, string? searchTerm)
        {
            // Obtener la lista de participantes del grupo y la información del grupo
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                inscripciones = inscripciones.Where(inscripcion =>
                    inscripcion.participante.unidadAcademica != null && inscripcion.participante.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.segundoApellido != null && inscripcion.participante.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.nombreGrupo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasMatriculadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasAprobadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.estado != null && inscripcion.estado.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            XSSFWorkbook workbook = new XSSFWorkbook();

            string sheetName = grupo.nombre.Length > 31 ? grupo.nombre.Substring(0, 31) : grupo.nombre;
            var sheet = workbook.CreateSheet(sheetName);

            // Crear estilos para el encabezado
            ICellStyle headerStyle = workbook.CreateCellStyle();
            headerStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            headerStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

            // Añadir borde y fondo al encabezado
            headerStyle.BorderBottom = BorderStyle.Thin;
            headerStyle.BorderTop = BorderStyle.Thin;
            headerStyle.BorderLeft = BorderStyle.Thin;
            headerStyle.BorderRight = BorderStyle.Thin;

            // Crear fuente en negrita para el encabezado
            IFont font = workbook.CreateFont();
            headerStyle.SetFont(font);

            // Crear estilos para las celdas del cuerpo
            ICellStyle bodyStyle = workbook.CreateCellStyle();
            bodyStyle.BorderBottom = BorderStyle.Thin;
            bodyStyle.BorderTop = BorderStyle.Thin;
            bodyStyle.BorderLeft = BorderStyle.Thin;
            bodyStyle.BorderRight = BorderStyle.Thin;

            IRow row1 = sheet.CreateRow(0);
            NPOI.SS.UserModel.ICell cell11 = row1.CreateCell(0);
            cell11.SetCellValue("Nombre del módulo:");
            NPOI.SS.UserModel.ICell cell12 = row1.CreateCell(1);
            cell12.SetCellValue(grupo.nombre);

            IRow row2 = sheet.CreateRow(1);
            NPOI.SS.UserModel.ICell cell21 = row2.CreateCell(0);
            cell21.SetCellValue("Nombre del/la facilitador(a) asociado(a):");
            NPOI.SS.UserModel.ICell cell22 = row2.CreateCell(1);
            cell22.SetCellValue(grupo.nombreAsesor);

            // Crear el encabezado de la tabla
            IRow rowHeaders = sheet.CreateRow(3);
            string[] headers = { "Nombre del participante", "Correo institucional", "Condición", "Unidad académica", "Teléfono", "Módulo", "Horas aprobadas", "Calificación del módulo" };

            for (int i = 0; i < headers.Length; i++)
            {
                NPOI.SS.UserModel.ICell cell = rowHeaders.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;  // Aplicar estilo de encabezado
            }

            int rowN = 4;
            foreach (var participante in participantes)
            {
                // Obtener las inscripciones del participante
                var inscripcionesParticipante = inscripciones.Where(i => i.idParticipante == participante.idParticipante).ToList();

                // Si el participante tiene inscripciones, crear una fila por cada módulo inscrito
                if (inscripcionesParticipante.Any())
                {
                    foreach (var inscripcion in inscripcionesParticipante)
                    {
                        IRow row = sheet.CreateRow(rowN);

                        // Completar los datos del participante en cada fila
                        row.CreateCell(0).SetCellValue($"{participante.nombre} {participante.primerApellido} {participante.segundoApellido}");
                        row.CreateCell(1).SetCellValue(participante.idParticipante);
                        row.CreateCell(2).SetCellValue(participante.condicion);
                        row.CreateCell(3).SetCellValue(participante.unidadAcademica);
                        row.CreateCell(4).SetCellValue(participante.telefono);

                        // Completar los datos del módulo
                        row.CreateCell(5).SetCellValue(grupo.nombre); // Nombre del módulo
                        row.CreateCell(6).SetCellValue(inscripcion.horasAprobadas); // Horas aprobadas
                        row.CreateCell(7).SetCellValue(inscripcion.calificacion); // Nota del módulo

                        // Aplicar estilo al cuerpo
                        for (int i = 0; i < 8; i++)
                        {
                            row.GetCell(i).CellStyle = bodyStyle;
                        }

                        rowN++; // Incrementar para la siguiente fila
                    }
                }
                else
                {
                    // Si no tiene inscripciones, dejar una sola fila para el participante con "N/A"
                    IRow row = sheet.CreateRow(rowN);

                    row.CreateCell(0).SetCellValue($"{participante.nombre} {participante.primerApellido} {participante.segundoApellido}");
                    row.CreateCell(1).SetCellValue(participante.idParticipante);
                    row.CreateCell(2).SetCellValue(participante.condicion);
                    row.CreateCell(3).SetCellValue(participante.unidadAcademica);
                    row.CreateCell(4).SetCellValue(participante.telefono);
                    row.CreateCell(5).SetCellValue("N/A");
                    row.CreateCell(6).SetCellValue("N/A");
                    row.CreateCell(7).SetCellValue("N/A");

                    // Aplicar estilo al cuerpo
                    for (int i = 0; i < 9; i++)
                    {
                        row.GetCell(i).CellStyle = bodyStyle;
                    }

                    rowN++; // Incrementar para la siguiente fila
                }
            }

            // Ajustar automáticamente el ancho de las columnas
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            // Crear el archivo de Excel y devolverlo como respuesta
            string fileName = "Lista_de_Participantes_" + grupo.nombre + ".xlsx";
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                var file = stream.ToArray();
                return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        /// <summary>
        /// Genera y descarga un PDF (A2) con todos los participantes del sistema y sus inscripciones.
        /// </summary>
        /// <param name="searchTerm">Texto opcional para filtrar participantes.</param>
        /// <returns>FileResult (application/pdf) — "Lista_de_Inscripciones.pdf".</returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, InscripcionHandler.
        /// Role required: Admin (1).
        /// </remarks>
        public ActionResult ExportarTodosParticipantesPDF(string? searchTerm)
        {
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones();

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            var filePath = System.IO.Path.Combine(_environment.WebRootPath, "data", "Lista_de_Inscripciones.docx");
            PdfWriter writer = new PdfWriter(filePath);
            PdfDocument pdf = new PdfDocument(writer);

            PageSize pageSize = PageSize.A2;  // Puedes elegir PageSize.A3 para un tamaño más pequeño
            iText.Layout.Document document = new iText.Layout.Document(pdf, pageSize);

            // Establecer fuente en negrita para encabezado
            PdfFont boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
            PdfFont regularFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

            Paragraph header3 = new Paragraph("")
                .SetFontSize(10);
            document.Add(header3);

            // Crear la tabla (9 columnas)
            iText.Layout.Element.Table table = new iText.Layout.Element.Table(new float[] { 3, 2, 2, 3, 2, 3, 2, 2 });
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Agregar encabezados de la tabla con estilo
            string[] headers = { "Nombre del participante", "Correo institucional", "Condición", "Unidad académica", "Teléfono", "Módulo", "Horas aprobadas", "Calificación del módulo" };
            foreach (var headerText in headers)
            {
                table.AddHeaderCell(new Cell().Add(new Paragraph(headerText).SetFont(boldFont).SetFontSize(10))
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY));
            }

            // Rellenar la tabla con los datos
            foreach (var participante in participantes)
            {
                // Obtener las inscripciones del participante
                var inscripcionesParticipante = inscripciones.Where(i => i.idParticipante == participante.idParticipante).ToList();

                if (inscripcionesParticipante.Any())
                {
                    foreach (var inscripcion in inscripcionesParticipante)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(participante.idParticipante).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(participante.condicion).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(participante.unidadAcademica).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(participante.telefono).SetFont(regularFont).SetFontSize(9)));

                        // Datos del módulo
                        table.AddCell(new Cell().Add(new Paragraph(inscripcion.nombreGrupo).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(inscripcion.horasAprobadas.ToString()).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(inscripcion.calificacion.ToString()).SetFont(regularFont).SetFontSize(9)));
                    }
                }
                else
                {
                    // Si no tiene inscripciones, rellenar con "N/A"
                    table.AddCell(new Cell().Add(new Paragraph(participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido).SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph(participante.idParticipante).SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph(participante.condicion).SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph(participante.unidadAcademica).SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph(participante.telefono).SetFont(regularFont).SetFontSize(9)));

                    table.AddCell(new Cell().Add(new Paragraph("N/A").SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph("N/A").SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph("N/A").SetFont(regularFont).SetFontSize(9)));
                }
            }

            // Añadir la tabla al documento
            document.Add(table);

            // Cerrar el documento
            document.Close();

            // Devolver el archivo PDF
            string fileName = "Lista_de_Inscripciones.pdf";
            return File(System.IO.File.ReadAllBytes(filePath), "application/pdf", fileName);
        }

        /// <summary>
        /// Genera y descarga un Word (.docx) con todos los participantes del sistema y sus inscripciones.
        /// </summary>
        /// <param name="searchTerm">Texto opcional para filtrar participantes.</param>
        /// <returns>FileResult (application/vnd.openxmlformats-officedocument.wordprocessingml.document) — "Lista_de_Inscripciones.docx".</returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, InscripcionHandler.
        /// Role required: Admin (1).
        /// </remarks>
        public ActionResult ExportarTodosParticipantesWord(string? searchTerm)
        {
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones();

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            var fileName = "Lista_de_Inscripciones.docx";

            XWPFDocument wordDoc = new XWPFDocument();

            // Crear un título
            XWPFParagraph titleParagraph = wordDoc.CreateParagraph();
            titleParagraph.Alignment = ParagraphAlignment.CENTER;
            XWPFRun titleRun = titleParagraph.CreateRun();
            titleRun.SetText("Lista de Inscripciones");
            titleRun.IsBold = true;
            titleRun.FontSize = 16;
            titleRun.AddBreak(); // Salto de línea después del título

            // Crear tabla con el número de participantes y columnas para los módulos
            XWPFTable table = wordDoc.CreateTable(participantes.Count + 1, 9); // +1 para la fila de encabezado

            // Ajustar los anchos de columna (simulando margen)
            table.SetColumnWidth(0, 750);  // Ajustar más ancho para la columna de identificación
            table.SetColumnWidth(1, 1000);  // Ancho de columna de nombre
            table.SetColumnWidth(2, 1500);  // Columna de correo
            table.SetColumnWidth(3, 750);  // Columna de condición
            table.SetColumnWidth(4, 1500);  // Columna de unidad académica
            table.SetColumnWidth(5, 750);  // Columna de teléfono
            table.SetColumnWidth(6, 1500);  // Nombre del módulo
            table.SetColumnWidth(7, 750);  // Horas aprobadas
            table.SetColumnWidth(8, 750);  // Nota del módulo

            // Estilo para el encabezado
            var headerRow = table.GetRow(0);
            headerRow.GetCell(0).SetText("Nombre del participante");
            headerRow.GetCell(1).SetText("Correo institucional");
            headerRow.GetCell(2).SetText("Condición");
            headerRow.GetCell(3).SetText("Unidad académica");
            headerRow.GetCell(4).SetText("Teléfono");
            headerRow.GetCell(5).SetText("Módulo");
            headerRow.GetCell(6).SetText("Horas aprobadas");
            headerRow.GetCell(7).SetText("Calificación del módulo");

            // Rellenar la tabla con los datos de los participantes e inscripciones
            int rowIndex = 1; // Comenzar después del encabezado

            foreach (var participante in participantes)
            {
                // Obtener las inscripciones del participante
                var inscripcionesParticipante = inscripciones.Where(i => i.idParticipante == participante.idParticipante).ToList();

                // Si el participante tiene inscripciones, crear una fila por cada módulo inscrito
                if (inscripcionesParticipante.Any())
                {
                    foreach (var inscripcion in inscripcionesParticipante)
                    {
                        var row = table.CreateRow(); // Crear una nueva fila para cada inscripción

                        row.GetCell(0).SetText(participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido);
                        row.GetCell(1).SetText(participante.idParticipante);
                        row.GetCell(2).SetText(participante.condicion);
                        row.GetCell(3).SetText(participante.unidadAcademica);
                        row.GetCell(4).SetText(participante.telefono);

                        // Información del módulo
                        row.GetCell(5).SetText(inscripcion.nombreGrupo);
                        row.GetCell(6).SetText(inscripcion.horasAprobadas.ToString());
                        row.GetCell(7).SetText(inscripcion.calificacion.ToString());

                        rowIndex++;
                    }
                }
            }

            // Guardar el archivo y devolver el documento Word como respuesta
            var stream = new MemoryStream();
            wordDoc.Write(stream);
            var file = stream.ToArray();
            return File(file, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        /// <summary>
        /// Genera y descarga un Excel (.xlsx) con todos los participantes del sistema y sus inscripciones.
        /// </summary>
        /// <param name="searchTerm">Texto opcional para filtrar participantes por nombre, correo o unidad académica.</param>
        /// <returns>FileResult (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet) — "Lista_de_Inscripciones.xlsx".</returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, InscripcionHandler.
        /// Role required: Admin (1).
        /// </remarks>
        public ActionResult ExportarTodosParticipantesExcel(string? searchTerm)
        {
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones();

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            XSSFWorkbook workbook = new XSSFWorkbook();

            string sheetName = "Inscripciones";
            var sheet = workbook.CreateSheet(sheetName);

            // Crear estilos para el encabezado
            ICellStyle headerStyle = workbook.CreateCellStyle();
            headerStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            headerStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

            // Añadir borde y fondo al encabezado
            headerStyle.BorderBottom = BorderStyle.Thin;
            headerStyle.BorderTop = BorderStyle.Thin;
            headerStyle.BorderLeft = BorderStyle.Thin;
            headerStyle.BorderRight = BorderStyle.Thin;

            // Crear fuente en negrita para el encabezado
            IFont font = workbook.CreateFont();
            headerStyle.SetFont(font);

            // Crear estilos para las celdas del cuerpo
            ICellStyle bodyStyle = workbook.CreateCellStyle();
            bodyStyle.BorderBottom = BorderStyle.Thin;
            bodyStyle.BorderTop = BorderStyle.Thin;
            bodyStyle.BorderLeft = BorderStyle.Thin;
            bodyStyle.BorderRight = BorderStyle.Thin;

            // Crear el encabezado de la tabla
            IRow rowHeaders = sheet.CreateRow(3);
            string[] headers = { "Nombre del participante", "Correo institucional", "Condición", "Unidad académica", "Teléfono", "Módulo", "Número de Grupo", "Horas aprobadas", "Calificación del módulo" };

            for (int i = 0; i < headers.Length; i++)
            {
                NPOI.SS.UserModel.ICell cell = rowHeaders.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
            }

            int rowN = 4;
            foreach (var participante in participantes)
            {
                // Obtener las inscripciones del participante
                var inscripcionesParticipante = inscripciones.Where(i => i.idParticipante == participante.idParticipante).ToList();

                // Si el participante tiene inscripciones, crear una fila por cada módulo inscrito
                if (inscripcionesParticipante.Any())
                {
                    foreach (var inscripcion in inscripcionesParticipante)
                    {
                        IRow row = sheet.CreateRow(rowN);

                        // Completar los datos del participante en cada fila
                        row.CreateCell(0).SetCellValue($"{participante.nombre} {participante.primerApellido} {participante.segundoApellido ?? ""}");
                        row.CreateCell(1).SetCellValue(participante.idParticipante);
                        row.CreateCell(2).SetCellValue(participante.condicion ?? "N/A");
                        row.CreateCell(3).SetCellValue(participante.unidadAcademica ?? "N/A");
                        row.CreateCell(4).SetCellValue(participante.telefono ?? "N/A");

                        // Completar los datos del módulo
                        row.CreateCell(5).SetCellValue(inscripcion.nombreGrupo ?? "N/A");
                        row.CreateCell(6).SetCellValue(inscripcion.numeroGrupo);
                        row.CreateCell(7).SetCellValue(inscripcion.horasAprobadas);
                        row.CreateCell(8).SetCellValue(inscripcion.calificacion.ToString() ?? "N/A");

                        // Aplicar estilo al cuerpo
                        for (int i = 0; i < headers.Length; i++)
                        {
                            row.GetCell(i).CellStyle = bodyStyle;
                        }

                        rowN++;
                    }
                }
                else
                {
                    // Si no tiene inscripciones, dejar una sola fila para el participante con "N/A"
                    IRow row = sheet.CreateRow(rowN);

                    row.CreateCell(0).SetCellValue($"{participante.nombre} {participante.primerApellido} {participante.segundoApellido ?? ""}");
                    row.CreateCell(1).SetCellValue(participante.idParticipante);
                    row.CreateCell(2).SetCellValue(participante.condicion ?? "N/A");
                    row.CreateCell(3).SetCellValue(participante.unidadAcademica ?? "N/A");
                    row.CreateCell(4).SetCellValue(participante.telefono ?? "N/A");
                    row.CreateCell(5).SetCellValue("N/A");
                    row.CreateCell(6).SetCellValue("N/A");
                    row.CreateCell(7).SetCellValue("N/A");
                    row.CreateCell(8).SetCellValue("N/A");

                    // Aplicar estilo al cuerpo
                    for (int i = 0; i < headers.Length; i++)
                    {
                        row.GetCell(i).CellStyle = bodyStyle;
                    }

                    rowN++;
                }
            }

            // Ajustar automáticamente el ancho de las columnas
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            // Crear el archivo de Excel y devolverlo como respuesta
            string fileName = "Lista_de_Inscripciones.xlsx";
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                var file = stream.ToArray();
                return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        /// <summary>Obtiene el rol del usuario autenticado desde la cookie "rolUsuario".</summary>
        private int GetRole()
        {
            int role = 0;

            if (HttpContext.Request.Cookies.ContainsKey("rolUsuario"))
            {
                role = Convert.ToInt32(Request.Cookies["rolUsuario"]);
            }

            return role;
        }

        /// <summary>Obtiene el identificador del usuario autenticado desde la cookie "idUsuario".</summary>
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
        /// Cambia de forma masiva el estado de inscripciones de "Inscrito" a "Incompleto" para
        /// los participantes seleccionados dentro de un grupo.
        /// </summary>
        /// <param name="participantesSeleccionados">Lista de IDs de participantes cuyas inscripciones se actualizarán.</param>
        /// <param name="idGrupo">ID del grupo al que pertenecen las inscripciones.</param>
        /// <returns>
        /// Redirects to la URL referente (Referer) si está disponible; de lo contrario redirige a
        /// Participante/ListaParticipantes con el idGrupo. Sets TempData["successMessage"] con el
        /// conteo de actualizaciones exitosas y/o TempData["errorMessage"] con el conteo de errores.
        /// </returns>
        /// <remarks>
        /// Handlers: InscripcionHandler, GrupoHandler.
        /// Role required: Admin (1).
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarEstadoInscritoAIncompleto(List<string> participantesSeleccionados, int idGrupo)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                if (participantesSeleccionados == null || !participantesSeleccionados.Any())
                {
                    TempData["errorMessage"] = "Debe seleccionar al menos un participante.";
                    return RedirectToAction("ListaParticipantes", "Participante", new { idGrupo = idGrupo });
                }

                int actualizados = 0;
                int errores = 0;

                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);

                foreach (var idParticipante in participantesSeleccionados)
                {
                    try
                    {
                        InscripcionModel inscripcion = accesoAInscripcion.ObtenerInscripcionParticipante(idGrupo, idParticipante);

                        // Solo cambiar si el estado actual es "Inscrito"
                        if (inscripcion != null && inscripcion.estado == "Inscrito")
                        {
                            inscripcion.estado = "Incompleto";
                            bool exito = accesoAInscripcion.EditarInscripcion(inscripcion);

                            if (exito)
                            {
                                actualizados++;
                            }
                            else
                            {
                                errores++;
                            }
                        }
                    }
                    catch
                    {
                        errores++;
                    }
                }

                if (actualizados > 0)
                {
                    TempData["successMessage"] = $"Se actualizaron {actualizados} inscripción(es) a estado 'Incompleto'.";
                }

                if (errores > 0)
                {
                    TempData["errorMessage"] = $"No se pudieron actualizar {errores} inscripción(es).";
                }
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = $"Ocurrió un error al cambiar los estados: {ex.Message}";
            }

            var refererUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(refererUrl))
            {
                return Redirect(refererUrl);
            }

            return RedirectToAction("ListaParticipantes", "Participante", new { idGrupo = idGrupo });
        }
    }
}