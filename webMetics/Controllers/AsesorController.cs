using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;
using webMetics.Models;

namespace webMetics.Controllers
{
    /// <summary>
    /// Gestiona las operaciones sobre la entidad Asesor (facilitadores). Permite listar, crear,
    /// editar y eliminar asesores, así como que el propio asesor consulte los módulos que tiene
    /// asignados. Al editar el correo de un asesor se propaga el cambio de ID al usuario, al
    /// participante (si existe) y a los grupos asignados.
    /// </summary>
    public class AsesorController : Controller
    {
        private protected UsuarioHandler accesoAUsuario;
        private protected TemaHandler accesoATema;
        private protected AsesorHandler accesoAAsesor;
        private protected ParticipanteHandler accesoAParticipante;
        private protected GrupoHandler accesoAGrupo;
        private protected InscripcionHandler accesoAInscripcion;

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
            accesoAInscripcion = new InscripcionHandler(environment, configuration);
        }

        /// <summary>
        /// Muestra la lista de módulos (grupos) asociados al asesor autenticado, adaptando el
        /// contenido según el rol del usuario (Admin ve todos, Asesor ve solo los suyos,
        /// Participante ve los disponibles no inscritos).
        /// </summary>
        /// <returns>
        /// View: MisModulos —
        /// ViewBag.ListaGrupos, ViewBag.ParticipantesEnGrupos, ViewBag.GruposInscritos (rol 0),
        /// ViewBag.IdParticipante, ViewBag.DateNow, ViewBag.Role, ViewBag.Id,
        /// ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, InscripcionHandler.
        /// Role required: Any (comportamiento varía según rol 0, 1, 2).
        /// </remarks>
        public ActionResult MisModulos()
        {
            const int RolUsuarioParticipante = 0;
            const int RolUsuarioAdmin = 1;
            const int RolUsuarioAsesor = 2;

            int rolUsuario = GetRole();
            string idUsuario = GetId();

            // Obtener la lista de grupos y participantes
            List<GrupoModel> listaGrupos = accesoAGrupo.ObtenerListaGruposAsesor(idUsuario);
            List<InscripcionModel> participantesEnGrupos = accesoAInscripcion.ObtenerInscripciones();

            // Inicializar las propiedades de ViewBag
            ViewBag.ListaGrupos = listaGrupos;
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
                                .ToList();
                        }
                        break;

                    case RolUsuarioAdmin:
                        ViewBag.ListaGrupos = listaGrupos;
                        break;

                    case RolUsuarioAsesor:
                        var gruposAsesor = accesoAGrupo.ObtenerListaGruposAsesor(idUsuario);
                        ViewBag.ListaGrupos = gruposAsesor;

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
        /// Filtra la lista de grupos del asesor autenticado según un término de búsqueda y devuelve
        /// la vista MisModulos con los resultados.
        /// </summary>
        /// <param name="searchTerm">Texto para filtrar por nombre, descripción, asesor, categoría, modalidad, lugar o temas del grupo.</param>
        /// <returns>
        /// View: MisModulos —
        /// ViewBag.ListaGrupos (filtrada), ViewBag.Role, ViewBag.Id.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler.
        /// Role required: Asesor (2).
        /// </remarks>
        public IActionResult BuscarGrupos(string searchTerm)
        {
            int rolUsuario = GetRole();
            string idUsuario = GetId();

            // Obtener la lista de participantes
            var grupos = accesoAGrupo.ObtenerListaGruposAsesor(idUsuario);

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                grupos = grupos.Where(p =>
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.descripcion.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombreAsesor.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombreCategoria.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.modalidad.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.lugar.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.TemasSeleccionadosNombres.Any(t => t.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            ViewBag.ListaGrupos = grupos;
            ViewBag.Role = rolUsuario;
            ViewBag.Id = idUsuario;

            return View("MisModulos");
        }

        /// <summary>
        /// Muestra el listado completo de asesores registrados en el sistema.
        /// </summary>
        /// <returns>
        /// View: ListaAsesores —
        /// ViewBag.Asesores, ViewBag.Role, ViewBag.Id, ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>
        /// Handlers: AsesorHandler.
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Muestra el formulario para registrar un nuevo asesor.
        /// </summary>
        /// <returns>
        /// View: CrearAsesor —
        /// ViewData["Temas"] (SelectListItem), ViewBag.Role, ViewBag.Id,
        /// ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>
        /// Handlers: TemaHandler.
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Procesa el formulario de creación de asesor. Crea el usuario asociado si no existe
        /// (con la contraseña indicada) y luego registra al asesor.
        /// </summary>
        /// <param name="asesor">Datos del asesor, incluyendo correo (usado como ID), contraseña y confirmación.</param>
        /// <returns>
        /// Redirects to ListaAsesores on success/duplicate. Sets TempData["successMessage"] or
        /// TempData["errorMessage"]. Returns View CrearAsesor with validation errors if ModelState
        /// is invalid or las contraseñas no coinciden.
        /// </returns>
        /// <remarks>
        /// Handlers: UsuarioHandler, AsesorHandler, TemaHandler.
        /// Role required: Admin (1).
        /// </remarks>
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


        /// <summary>
        /// Muestra el formulario precargado con los datos del asesor para su edición.
        /// </summary>
        /// <param name="idAsesor">Correo/ID del asesor a editar.</param>
        /// <returns>
        /// View: EditarAsesor (model: AsesorModel) —
        /// ViewData["Temas"] (SelectListItem), ViewBag.Role, ViewBag.Id.
        /// Redirects to ListaAsesores with TempData["errorMessage"] si ocurre un error.
        /// </returns>
        /// <remarks>
        /// Handlers: AsesorHandler, TemaHandler.
        /// Role required: Admin (1) o el propio Asesor (2).
        /// </remarks>
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

        /// <summary>
        /// Procesa el formulario de edición de asesor. Si el Admin cambia el correo, propaga el
        /// cambio de ID al usuario, participante (si existe) y grupos asociados. Sincroniza además
        /// los datos personales en el registro de participante si existe.
        /// </summary>
        /// <param name="asesor">Datos actualizados del asesor, incluyendo el ID original y el nuevo correo.</param>
        /// <returns>
        /// Admin: Redirects to ListaAsesores with TempData["successMessage"] on success, or
        /// TempData["errorMessage"] on error. Asesor: Redirects to Usuario/InformacionPersonal.
        /// Returns View EditarAsesor si ModelState inválido o contraseñas no coinciden.
        /// </returns>
        /// <remarks>
        /// Handlers: AsesorHandler, UsuarioHandler, ParticipanteHandler, GrupoHandler.
        /// Role required: Admin (1) o el propio Asesor (2).
        /// </remarks>
        [HttpPost]
        public ActionResult ActualizarAsesor(AsesorModel asesor)
        {
            ViewBag.Id = GetId();
            ViewBag.Role = GetRole();

            if (!ModelState.IsValid)
            {
                ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                return View("EditarAsesor", asesor);
            }
            else
            {
                try
                {
                    NewLoginModel usuario = new NewLoginModel
                    {
                        oldId = asesor.idAsesor,
                        id = asesor.correo,
                        role = 2,
                        nuevaContrasena = asesor.contrasena
                    };

                    if (GetRole() == 1)
                    {
                        if (asesor.contrasena == asesor.confirmarContrasena)
                        {
                            accesoAAsesor.EditarAsesor(asesor);

                            if (asesor.idAsesor != asesor.correo)
                            {
                                CrearUsuario(usuario);
                            }

                            accesoAUsuario.EditarUsuario(usuario.id, usuario.role, usuario.nuevaContrasena);
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
                        accesoAAsesor.EditarAsesor(asesor);
                    }

                    ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(usuario.id);

                    if (participante != null)
                    {
                        AsesorModel asesorActualizado = accesoAAsesor.ObtenerAsesor(usuario.id);

                        participante.nombre = asesorActualizado.nombre;
                        participante.primerApellido = asesorActualizado.primerApellido;
                        participante.segundoApellido = asesorActualizado.segundoApellido;
                        participante.correo = asesorActualizado.correo;
                        participante.tipoIdentificacion = asesorActualizado.tipoIdentificacion;
                        participante.numeroIdentificacion = asesorActualizado.numeroIdentificacion;
                        participante.telefono = asesorActualizado.telefono;

                        accesoAParticipante.EditarParticipante(participante);
                    }

                    if (GetRole() == 1)
                    {
                        TempData["successMessage"] = "Los datos de/la facilitador(a) fueron guardados.";
                        return RedirectToAction("ListaAsesores");
                    }
                    else
                    {
                        TempData["successMessage"] = "Sus datos fueron guardados.";
                        return RedirectToAction("InformacionPersonal", "Usuario");
                    }
                }
                catch
                {
                    TempData["errorMessage"] = "Ocurrió un error al editar los datos del/la facilitador(a).";
                }

                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }
        }

        /// <summary>Crea un nuevo usuario con el nuevo ID, migra los registros dependientes y elimina el antiguo usuario.</summary>
        private bool CrearUsuario(NewLoginModel usuario)
        {
            bool exito = accesoAUsuario.CrearUsuario(usuario.id, usuario.nuevaContrasena, usuario.role);

            if (usuario.role == 0 && exito)
            {
                EditarIdParticipante(usuario);
            }

            if (usuario.role == 2 && exito)
            {
                EditarIdAsesor(usuario);
            }

            exito = accesoAUsuario.EliminarUsuario(usuario.oldId);

            return exito;
        }

        /// <summary>Actualiza el ID del registro de participante asociado al antiguo correo del asesor.</summary>
        private void EditarIdParticipante(NewLoginModel usuario)
        {
            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(usuario.oldId);

            if (participante != null)
            {
                participante.idParticipante = usuario.id;
                participante.correo = usuario.oldId;

                accesoAParticipante.EditarIdParticipante(participante);
            }
        }

        /// <summary>Migra el registro de asesor al nuevo ID: crea el asesor con el nuevo correo, reasigna los grupos y elimina el registro antiguo.</summary>
        private void EditarIdAsesor(NewLoginModel usuario)
        {
            EditarIdParticipante(usuario);

            AsesorModel existingAsesor = accesoAAsesor.ObtenerAsesor(usuario.oldId);

            if (existingAsesor != null)
            {
                AsesorModel newAsesor = new AsesorModel
                {
                    idAsesor = usuario.id,
                    correo = usuario.id,
                    nombre = existingAsesor.nombre,
                    primerApellido = existingAsesor.primerApellido,
                    segundoApellido = existingAsesor.segundoApellido,
                    tipoIdentificacion = existingAsesor.tipoIdentificacion,
                    numeroIdentificacion = existingAsesor.numeroIdentificacion,
                    descripcion = existingAsesor.descripcion,
                    telefono = existingAsesor.telefono
                };

                accesoAAsesor.CrearAsesor(newAsesor);

                List<GrupoModel> gruposAsesor = accesoAGrupo.ObtenerListaGruposAsesor(usuario.oldId);

                if (gruposAsesor != null)
                {
                    accesoAGrupo.EditarIdGruposAsesor(usuario.id, usuario.oldId);
                }

                accesoAAsesor.EliminarAsesor(usuario.oldId);
            }
        }

        /// <summary>
        /// Elimina un asesor del sistema si no tiene grupos activos (visibles) asignados.
        /// </summary>
        /// <param name="idAsesor">Correo/ID del asesor a eliminar.</param>
        /// <returns>
        /// Redirects to ListaAsesores. Sets TempData["successMessage"] on success or
        /// TempData["errorMessage"] si el asesor tiene grupos asignados o ocurre un error.
        /// </returns>
        /// <remarks>
        /// Handlers: AsesorHandler, GrupoHandler (vía PuedeEliminarAsesor).
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Verifica si un asesor puede ser eliminado comprobando que no tenga grupos activos (visibles).
        /// </summary>
        /// <param name="idAsesor">Correo/ID del asesor.</param>
        /// <returns><c>true</c> si el asesor no tiene grupos visibles asignados; <c>false</c> en caso contrario.</returns>
        /// <remarks>Handlers: GrupoHandler.</remarks>
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
    }
}