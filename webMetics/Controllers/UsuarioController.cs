using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;
using webMetics.Models;

namespace webMetics.Controllers
{
    /// <summary>
    /// Controlador para la gestión de sesión y credenciales de usuario.
    /// Cubre inicio/cierre de sesión, registro de nuevos usuarios, cambio de contraseña,
    /// gestión de correo alternativo y consulta de la bitácora de accesos.
    /// </summary>
    public class UsuarioController : Controller
    {
        // Controladores y Handlers utilizados en el controlador
        private protected CookiesController cookiesController;
        private protected UsuarioHandler accesoAUsuario;
        private protected ParticipanteHandler accesoAParticipante;
        private protected AsesorHandler accesoAAsesor;
        private protected GrupoHandler accesoAGrupo;
        private protected InscripcionHandler accesoAInscripcion;


        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly IDataProtectionProvider _protector;
        private readonly EmailService _emailService;

        public UsuarioController(IWebHostEnvironment environment, IConfiguration configuration, IDataProtectionProvider protector, EmailService emailService)
        {
            _environment = environment;
            _configuration = configuration;
            _protector = protector;
            _emailService = emailService;

            cookiesController = new CookiesController(environment, configuration);
            accesoAUsuario = new UsuarioHandler(environment, configuration);
            accesoAParticipante = new ParticipanteHandler(environment, configuration);
            accesoAAsesor = new AsesorHandler(environment, configuration);
            accesoAGrupo = new GrupoHandler(environment, configuration);
            accesoAInscripcion = new InscripcionHandler(environment, configuration);
        }

        /// <summary>Muestra el formulario de inicio de sesión.</summary>
        /// <returns>
        /// View: IniciarSesion —
        /// ViewBag.ErrorMessage, ViewBag.SuccessMessage (desde TempData).
        /// </returns>
        public ActionResult IniciarSesion()
        {
            if (TempData["errorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["errorMessage"].ToString();
            }
            if (TempData["successMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["successMessage"].ToString();
            }

            return View("IniciarSesion");
        }

        /// <summary>
        /// Procesa el formulario de inicio de sesión. Si las credenciales son válidas, crea las cookies
        /// de sesión y redirige según los datos de perfil pendientes.
        /// Registra el intento (EXITO / FRACASO) en la bitácora de accesos.
        /// </summary>
        /// <param name="usuario">Modelo con correo institucional y contraseña.</param>
        /// <returns>
        /// Redirects to CompletarCorreoAlternativo si el usuario no tiene correo alternativo.
        /// Redirects to CompletarCarreraYAreas si el participante (rol 0) no tiene carrera registrada.
        /// Redirects to Grupo/ListaGruposDisponibles on success.
        /// Redirects to IniciarSesion on failure; sets TempData["errorMessage"].
        /// View: IniciarSesion con errores de validación si ModelState es inválido.
        /// </returns>
        /// <remarks>
        /// Handlers: UsuarioHandler.
        /// Sets cookies: USUARIOAUTORIZADO (protegido), rolUsuario, idUsuario.
        /// Redirect logic is determined by DeterminarRedireccionPostLogin.
        /// </remarks>
        [HttpPost]
        public ActionResult IniciarSesion(LoginModel usuario)
        {
            if (ModelState.IsValid)
            {
                LoginModel usuarioAutorizado = AutenticarUsuario(usuario);

                if (usuarioAutorizado != null)
                {
                    accesoAUsuario.InsertarAccesoUsuarioBitacora(usuarioAutorizado.id, "EXITO");

                    return DeterminarRedireccionPostLogin(usuarioAutorizado.id, usuarioAutorizado.rol);
                }
                else
                {
                    // Se usa el correo proporcionado para identificar al usuario que intentó el acceso.
                    accesoAUsuario.InsertarAccesoUsuarioBitacora(usuario.id, "FRACASO");

                    TempData["errorMessage"] = "Correo institucional o contraseña inválidos.";
                    return RedirectToAction("IniciarSesion", "Usuario");
                }
            }
            else
            {
                return View(usuario);
            }
        }

        /// <summary>Muestra el formulario de auto-registro de nuevos usuarios.</summary>
        /// <returns>
        /// View: FormularioRegistro —
        /// ViewData["jsonDataAreas"] (jerarquía de áreas UCR).
        /// </returns>
        /// <remarks>Handlers: ParticipanteHandler (para cargar áreas).</remarks>
        public ActionResult FormularioRegistro()
        {
            // Obtener datos necesarios para llenar las opciones del formulario (áreas)
            ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
            return View();
        }

        /// <summary>
        /// Procesa el formulario de auto-registro. Crea el usuario, el participante asociado y envía
        /// la contraseña temporal por correo.
        /// </summary>
        /// <param name="usuario">Modelo con los datos del nuevo usuario.</param>
        /// <returns>
        /// View: ParticipanteRegistrado on success —
        /// ViewBag.Correo, ViewBag.Titulo, ViewBag.Message.
        /// View: FormularioRegistro con errores si ModelState es inválido o la creación falla.
        /// </returns>
        /// <remarks>
        /// Handlers: UsuarioHandler, ParticipanteHandler, AsesorHandler (vía CrearUsuario).
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FormularioRegistro(UsuarioModel usuario)
        {
            if (ModelState.IsValid)
            {
                string contrasena = GenerateRandomPassword();
                bool exito = CrearUsuario(usuario, contrasena);

                if (exito)
                {
                    ViewBag.Correo = usuario.correo;
                    ViewBag.Titulo = "Registro realizado";
                    ViewBag.Message = "Se registró éxitosamente. La contraseña temporal será enviada al correo";
                    EnviarCorreoRegistro(usuario.correo, contrasena);

                    return View("ParticipanteRegistrado");
                }
            }

            if (TempData["errorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["errorMessage"].ToString();
            }
            if (TempData["successMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["successMessage"].ToString();
            }

            ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
            return View(usuario);
        }

        /// <summary>
        /// Crea un usuario y su participante asociado. Si el admin había pre-registrado al usuario,
        /// actualiza sus datos y asigna rol de asesor si corresponde; de lo contrario crea un registro nuevo.
        /// </summary>
        /// <param name="usuario">Modelo con los datos del usuario a crear.</param>
        /// <param name="contrasena">Contraseña temporal generada para el usuario.</param>
        /// <returns><c>true</c> si la operación fue exitosa; <c>false</c> si el usuario ya existe o hubo un error.</returns>
        /// <remarks>Handlers: UsuarioHandler, ParticipanteHandler, AsesorHandler.</remarks>
        private bool CrearUsuario(UsuarioModel usuario, string contrasena)
        {
            bool exito = false;
            int rolUsuario = 0;

            try
            {
                usuario.id = usuario.correo; // Esto define que el identificador del usuario es el correo

                ParticipanteModel nuevoParticipante = new ParticipanteModel()
                {
                    idParticipante = usuario.id,
                    nombre = usuario.nombre,
                    primerApellido = usuario.primerApellido,
                    segundoApellido = usuario.segundoApellido,
                    tipoIdentificacion = usuario.tipoIdentificacion,
                    numeroIdentificacion = usuario.numeroIdentificacion,
                    correo = usuario.correo,
                    correoAlternativo = usuario.correoAlternativo,
                    tipoParticipante = usuario.tipoParticipante,
                    condicion = usuario.condicion,
                    telefono = usuario.telefono,
                    area = usuario.area,
                    departamento = usuario.departamento,
                    unidadAcademica = usuario.unidadAcademica,
                    sede = usuario.sede,
                    horasMatriculadas = 0,
                    horasAprobadas = 0
                };

                bool registradoPorAdmin = !accesoAUsuario.ObtenerRegistradoPorUsuario(usuario.id);
                if (registradoPorAdmin)
                {
                    if (!accesoAUsuario.ExisteUsuario(usuario.id))
                    {
                        accesoAUsuario.CrearUsuario(usuario.id, contrasena);
                    }

                    AsesorModel asesor = accesoAAsesor.ObtenerAsesor(usuario.id);
                    if (asesor != null)
                    {
                        asesor.nombre = usuario.nombre;
                        asesor.primerApellido = usuario.primerApellido;
                        asesor.segundoApellido = usuario.segundoApellido;
                        asesor.tipoIdentificacion = usuario.tipoIdentificacion;
                        asesor.numeroIdentificacion = usuario.numeroIdentificacion;
                        asesor.correo = usuario.correo;
                        asesor.correoAlternativo = usuario.correoAlternativo;
                        asesor.telefono = usuario.telefono;

                        rolUsuario = 2;

                        accesoAAsesor.EditarAsesor(asesor);
                    }

                    accesoAUsuario.EditarUsuario(usuario.id, rolUsuario, contrasena); // Si el admin había ingresado un asesor con ese id, se guarda como asesor al registrarse

                    if (!accesoAParticipante.ExisteParticipante(usuario.id))
                    {
                        accesoAParticipante.CrearParticipante(nuevoParticipante);
                    }
                    else
                    {
                        ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(usuario.id);
                        nuevoParticipante.idParticipante = usuario.id;
                        nuevoParticipante.horasMatriculadas = participante.horasMatriculadas;
                        nuevoParticipante.horasAprobadas = participante.horasAprobadas;

                        accesoAParticipante.EditarParticipante(nuevoParticipante);
                    }

                    accesoAUsuario.ActualizarRegistradoPorUsuario(usuario.id);
                    exito = true;
                }
                else
                {
                    if (!accesoAUsuario.ExisteUsuario(usuario.id))
                    {
                        accesoAUsuario.CrearUsuario(usuario.id, contrasena, 0, usuario.correoAlternativo);

                        if (!accesoAParticipante.ExisteParticipante(usuario.id))
                        {
                            accesoAParticipante.CrearParticipante(nuevoParticipante);
                        }
                        else
                        {
                            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(usuario.id);
                            nuevoParticipante.idParticipante = usuario.id;
                            nuevoParticipante.horasMatriculadas = participante.horasMatriculadas;
                            nuevoParticipante.horasAprobadas = participante.horasAprobadas;

                            accesoAParticipante.EditarParticipante(nuevoParticipante);
                        }
                        exito = true;
                    }
                    else
                    {
                        TempData["errorMessage"] = "Ya existe un usuario con el mismo correo institucional.";
                        exito = false;
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = "No se pudo crear el usuario. Inténtelo de nuevo.";
                exito = false;
            }

            return exito;
        }

        /// <summary>
        /// Autentica al usuario y, si es válido, escribe las cookies de sesión
        /// (USUARIOAUTORIZADO protegida, rolUsuario, idUsuario).
        /// Admins tienen sesión de 120 minutos; otros usuarios 20 minutos.
        /// </summary>
        /// <param name="usuario">Modelo con id (correo) y contraseña.</param>
        /// <returns>El <see cref="LoginModel"/> del usuario autenticado, o <c>null</c> si las credenciales son incorrectas.</returns>
        /// <remarks>Handlers: UsuarioHandler.</remarks>
        private LoginModel AutenticarUsuario(LoginModel usuario)
        {
            LoginModel usuarioAutorizado = null;

            try
            {
                if (accesoAUsuario.AutenticarUsuario(usuario.id, usuario.contrasena))
                {
                    usuarioAutorizado = accesoAUsuario.ObtenerUsuario(usuario.id);

                    int rolUsuario = usuarioAutorizado.rol;
                    string idUsuario = usuarioAutorizado.id;

                    int minutos = 20;
                    if (rolUsuario == 1)
                    {
                        minutos = 120;
                    }

                    IDataProtector protector = _protector.CreateProtector("USUARIOAUTORIZADO");
                    string idEncriptado = protector.Protect(idUsuario);

                    Response.Cookies.Append("USUARIOAUTORIZADO", idEncriptado, new CookieOptions
                    {
                        Expires = DateTime.Now.AddMinutes(minutos)
                    });

                    Response.Cookies.Append("rolUsuario", rolUsuario.ToString(), new CookieOptions
                    {
                        Expires = DateTime.Now.AddMinutes(minutos)
                    });

                    Response.Cookies.Append("idUsuario", idUsuario, new CookieOptions
                    {
                        Expires = DateTime.Now.AddMinutes(minutos)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AutenticarUsuario: {ex.Message}");
            }

            return usuarioAutorizado;
        }

        /// <summary>
        /// Muestra la página de perfil del usuario con sesión iniciada. El contenido varía según el rol:
        /// participante ve inscripciones y medallas; asesor ve sus grupos y, si también es participante, sus inscripciones.
        /// </summary>
        /// <returns>
        /// View: InformacionPersonal —
        /// ViewBag.Usuario (UsuarioModel), ViewBag.Participante, ViewBag.Inscripciones,
        /// ViewBag.ListaGrupos, ViewBag.Medallas (rol 0);
        /// ViewBag.Asesor, ViewBag.ListaGruposAsesor (rol 2);
        /// ViewBag.Id, ViewBag.Role, ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, InscripcionHandler, GrupoHandler, AsesorHandler.
        /// Role required: Any (autenticado).
        /// </remarks>
        public ActionResult InformacionPersonal()
        {
            const int RolUsuarioParticipante = 0;
            const int RolUsuarioAdmin = 1;
            const int RolUsuarioAsesor = 2;

            int role = GetRole();
            string idUsuario = GetId();

            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idUsuario);
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesParticipante(idUsuario);
            List<GrupoModel> gruposParticipante = accesoAGrupo.ObtenerListaGruposParticipante(idUsuario);

            AsesorModel asesor = accesoAAsesor.ObtenerAsesor(idUsuario);
            List<GrupoModel> gruposAsesor = accesoAGrupo.ObtenerListaGruposAsesor(idUsuario);

            switch (role)
            {
                case RolUsuarioParticipante:
                    UsuarioModel usuarioParticipante = new UsuarioModel()
                    {
                        id = idUsuario,
                        nombre = participante.nombre,
                        primerApellido = participante.primerApellido,
                        segundoApellido = participante.segundoApellido,
                        tipoIdentificacion = participante.tipoIdentificacion,
                        numeroIdentificacion = participante.numeroIdentificacion,
                        correo = participante.correo,
                        correoAlternativo = participante.correoAlternativo,
                        tipoParticipante = participante.tipoParticipante,
                        condicion = participante.condicion,
                        telefono = participante.telefono,
                        area = participante.area,
                        departamento = participante.departamento,
                        unidadAcademica = participante.unidadAcademica,
                        sede = participante.sede,
                    };

                    ViewBag.Usuario = usuarioParticipante;
                    ViewBag.Participante = participante;
                    ViewBag.Inscripciones = inscripciones;
                    ViewBag.ListaGrupos = gruposParticipante;
                    ViewBag.Medallas = accesoAParticipante.ObtenerMedallas(idUsuario);

                    break;

                case RolUsuarioAdmin:
                    break;

                case RolUsuarioAsesor:
                    UsuarioModel usuarioAsesor = new UsuarioModel()
                    {
                        id = idUsuario,
                        nombre = asesor.nombre,
                        primerApellido = asesor.primerApellido,
                        segundoApellido = asesor.segundoApellido,
                        tipoIdentificacion = asesor.tipoIdentificacion,
                        numeroIdentificacion = asesor.numeroIdentificacion,
                        correo = asesor.correo,
                        correoAlternativo = asesor.correoAlternativo,
                        telefono = asesor.telefono,
                    };

                    ViewBag.Usuario = usuarioAsesor;
                    ViewBag.Asesor = asesor;
                    ViewBag.ListaGruposAsesor = gruposAsesor;

                    if (participante != null)
                    {
                        ViewBag.Participante = participante;
                        ViewBag.Inscripciones = inscripciones;
                        ViewBag.ListaGrupos = gruposParticipante;
                        ViewBag.Medallas = accesoAParticipante.ObtenerMedallas(idUsuario);
                    }

                    break;

                default:
                    break;
            }

            ViewBag.Id = GetId();
            ViewBag.Role = GetRole();

            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View();
        }

        /// <summary>
        /// Cierra la sesión eliminando las cookies de autenticación y redirige a la pantalla de login.
        /// </summary>
        /// <returns>Redirects to IniciarSesion.</returns>
        public ActionResult CerrarSesion()
        {
            // Eliminar datos del usuario
            Response.Cookies.Delete("USUARIOAUTORIZADO");
            Response.Cookies.Delete("rolUsuario");
            Response.Cookies.Delete("idUsuario");

            return RedirectToAction("IniciarSesion");
        }

        /// <summary>
        /// Muestra el formulario para que el usuario ingrese su correo alternativo tras el primer inicio de sesión.
        /// Redirige a IniciarSesion si no hay sesión activa.
        /// </summary>
        /// <returns>
        /// View: CompletarCorreoAlternativo con el modelo UsuarioModel (id, nombre, correo) —
        /// ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// Redirects to IniciarSesion si la sesión no es válida.
        /// </returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, AsesorHandler.
        /// Role required: Any (autenticado, sin correo alternativo).
        /// </remarks>
        public ActionResult CompletarCorreoAlternativo()
        {
            // Validar que el usuario esté logueado
            string idUsuario = GetId();
            if (string.IsNullOrEmpty(idUsuario))
            {
                return RedirectToAction("IniciarSesion");
            }

            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idUsuario);
            AsesorModel asesor = accesoAAsesor.ObtenerAsesor(idUsuario);

            UsuarioModel usuario = new UsuarioModel()
            {
                id = idUsuario,
                nombre = participante?.nombre ?? asesor?.nombre ?? "Usuario",
                primerApellido = participante?.primerApellido ?? asesor?.primerApellido ?? "",
                correo = idUsuario // El correo es el ID del usuario
            };

            if (TempData["errorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["errorMessage"].ToString();
            }
            if (TempData["successMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["successMessage"].ToString();
            }

            return View(usuario);
        }

        /// <summary>
        /// Persiste el correo alternativo del usuario y sincroniza el cambio en participante y asesor si aplica.
        /// </summary>
        /// <param name="usuario">Modelo que contiene el correoAlternativo.</param>
        /// <returns>
        /// Redirects to Grupo/ListaGruposDisponibles on success; sets TempData["successMessage"].
        /// Redirects to CompletarCorreoAlternativo on error; sets TempData["errorMessage"].
        /// Redirects to IniciarSesion si la sesión no es válida.
        /// </returns>
        /// <remarks>
        /// Handlers: UsuarioHandler, ParticipanteHandler, AsesorHandler.
        /// Role required: Any (autenticado).
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CompletarCorreoAlternativo(UsuarioModel usuario)
        {
            string idUsuario = GetId();

            if (string.IsNullOrEmpty(idUsuario))
            {
                return RedirectToAction("IniciarSesion");
            }

            if (string.IsNullOrWhiteSpace(usuario.correoAlternativo))
            {
                TempData["errorMessage"] = "Es necesario ingresar un correo alternativo.";
                return RedirectToAction("CompletarCorreoAlternativo");
            }

            try
            {
                // Actualizar solo el correoAlternativo en la BD
                bool exito = accesoAUsuario.ActualizarCorreoAlternativo(idUsuario, usuario.correoAlternativo);

                if (exito)
                {
                    // También actualizar en los modelos relacionados (Participante y Asesor)
                    ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idUsuario);
                    if (participante != null)
                    {
                        participante.correoAlternativo = usuario.correoAlternativo;
                        accesoAParticipante.EditarParticipante(participante);
                    }

                    AsesorModel asesor = accesoAAsesor.ObtenerAsesor(idUsuario);
                    if (asesor != null)
                    {
                        asesor.correoAlternativo = usuario.correoAlternativo;
                        accesoAAsesor.EditarAsesor(asesor);
                    }

                    TempData["successMessage"] = "Correo alternativo guardado correctamente.";
                    return DeterminarRedireccionPostLogin(idUsuario, GetRole());
                }
                else
                {
                    TempData["errorMessage"] = "Error al guardar el correo alternativo. Intente nuevamente.";
                    return RedirectToAction("CompletarCorreoAlternativo");
                }
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = "Error al procesar la solicitud. Intente nuevamente.";
                return RedirectToAction("CompletarCorreoAlternativo");
            }
        }

        /// <summary>
        /// Muestra el formulario (solo para admins) para cambiar el correo/id y contraseña de otro usuario.
        /// </summary>
        /// <param name="idUsuario">Correo institucional del usuario a modificar.</param>
        /// <param name="nombreCompleto">Nombre completo a mostrar en la vista.</param>
        /// <returns>
        /// View: CambiarCredencialesUsuario con el modelo NewLoginModel —
        /// ViewBag.NombreCompleto, ViewBag.Id, ViewBag.Role, ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// Redirects to Grupo/ListaGruposDisponibles si idUsuario es vacío.
        /// Redirects to CerrarSesion si el rol no es Admin (1).
        /// </returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, AsesorHandler.
        /// Role required: Admin (1).
        /// </remarks>
        public ActionResult CambiarCredencialesUsuario(string idUsuario, string nombreCompleto)
        {
            if (GetRole() == 1)
            {
                if (!string.IsNullOrEmpty(idUsuario))
                {
                    int rolUsuario = 0;

                    ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idUsuario);
                    AsesorModel asesor = accesoAAsesor.ObtenerAsesor(idUsuario);

                    if (asesor != null)
                    {
                        rolUsuario = 2;
                    }

                    ViewBag.NombreCompleto = nombreCompleto;

                    NewLoginModel usuario = new NewLoginModel() { oldId = idUsuario, id = idUsuario, role = rolUsuario };

                    if (TempData["errorMessage"] != null)
                    {
                        ViewBag.ErrorMessage = TempData["errorMessage"].ToString();
                    }
                    if (TempData["successMessage"] != null)
                    {
                        ViewBag.SuccessMessage = TempData["successMessage"].ToString();
                    }

                    ViewBag.Id = GetId();
                    ViewBag.Role = GetRole();

                    return View(usuario);
                }
                else
                {
                    TempData["errorMessage"] = "Identificador de usuario no válido.";
                    return RedirectToAction("ListaGruposDisponibles", "Grupo");
                }
            }
            else
            {
                return RedirectToAction("CerrarSesion");
            }
        }

        /// <summary>
        /// Procesa el cambio de credenciales de un usuario realizado por el administrador.
        /// Opcionalmente envía la nueva contraseña por correo.
        /// </summary>
        /// <param name="usuario">Modelo con oldId, id, nuevaContrasena, confirmarContrasena, rol y enviarPorCorreo.</param>
        /// <returns>
        /// Redirects to CambiarCredencialesUsuario on success or failure; sets TempData["successMessage"] o TempData["errorMessage"].
        /// Redirects to CerrarSesion si el rol no es Admin (1).
        /// </returns>
        /// <remarks>
        /// Handlers: UsuarioHandler, ParticipanteHandler, AsesorHandler (vía EditarIdUsuario).
        /// Role required: Admin (1).
        /// </remarks>
        [HttpPost]
        public ActionResult CambiarCredencialesUsuario(NewLoginModel usuario)
        {
            if (GetRole() == 1)
            {
                if (string.IsNullOrWhiteSpace(usuario.id))
                {
                    TempData["errorMessage"] = "El identificador no puede estar en blanco.";

                    return RedirectToAction("CambiarCredencialesUsuario", new { idUsuario = usuario.id });
                }
                
                if (string.IsNullOrWhiteSpace(usuario.nuevaContrasena) || string.IsNullOrWhiteSpace(usuario.confirmarContrasena))
                {
                    TempData["errorMessage"] = "Las contraseñas no pueden estar en blanco.";

                    return RedirectToAction("CambiarCredencialesUsuario", new { idUsuario = usuario.id });
                }

                if (usuario.nuevaContrasena == usuario.confirmarContrasena)
                {
                    EditarIdUsuario(usuario);

                    if (usuario.enviarPorCorreo) { EnviarContrasenaAdmin(usuario.id, usuario.confirmarContrasena); }

                    TempData["successMessage"] = "Las credenciales del usuario se cambiaron correctamente.";

                    return RedirectToAction("CambiarCredencialesUsuario", new { idUsuario = usuario.id });
                }
                else
                {
                    TempData["errorMessage"] = "Las contraseñas no coinciden.";

                    return RedirectToAction("CambiarCredencialesUsuario", new { idUsuario = usuario.id });
                }

                
            }
            else {
                return RedirectToAction("CerrarSesion");
            }
        }

        /// <summary>
        /// Actualiza las credenciales (id y contraseña) de un usuario y, según su rol,
        /// actualiza también el registro de participante o asesor asociado.
        /// </summary>
        private void EditarIdUsuario(NewLoginModel usuario)
        {
            accesoAUsuario.CrearUsuario(usuario.id, usuario.confirmarContrasena, usuario.role);

            if (usuario.role == 0)
            {
                EditarIdParticipante(usuario);
            }

            if (usuario.role == 2)
            {
                EditarIdAsesor(usuario);
            }

            accesoAUsuario.EditarUsuario(usuario.id, usuario.role, usuario.confirmarContrasena);
        }

        /// <summary>Actualiza el id y correo del participante al nuevo id del usuario.</summary>
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

        /// <summary>
        /// Actualiza el id del asesor: crea un nuevo registro con el nuevo id, transfiere sus grupos y elimina el anterior.
        /// También actualiza el participante asociado vía EditarIdParticipante.
        /// </summary>
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
        /// Muestra el formulario para que el usuario cambie su propia contraseña.
        /// Valida que la cookie USUARIOAUTORIZADO corresponda al usuario sesionado.
        /// </summary>
        /// <returns>
        /// View: CambiarContrasena con el modelo NewLoginModel —
        /// ViewBag.Id, ViewBag.Role, ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// Redirects to CerrarSesion si la sesión no es válida.
        /// </returns>
        /// <remarks>Role required: Any (autenticado).</remarks>
        public ActionResult CambiarContrasena()
        {
            string idUsuario = string.Empty;

            if (Request.Cookies.ContainsKey("USUARIOAUTORIZADO"))
            {
                string idEncriptado = Request.Cookies["USUARIOAUTORIZADO"];
                IDataProtector protector = _protector.CreateProtector("USUARIOAUTORIZADO");
                idUsuario = protector.Unprotect(idEncriptado);
            }

            if (!string.IsNullOrEmpty(idUsuario) && GetId() == idUsuario)
            {
                ViewBag.Id = GetId();
                ViewBag.Role = GetRole();

                NewLoginModel usuario = new NewLoginModel() { id = idUsuario };

                if (TempData["errorMessage"] != null)
                {
                    ViewBag.ErrorMessage = TempData["errorMessage"].ToString();
                }
                if (TempData["successMessage"] != null)
                {
                    ViewBag.SuccessMessage = TempData["successMessage"].ToString();
                }

                return View(usuario);
            }
            else
            {
                return RedirectToAction("CerrarSesion");
            }
        }


        /// <summary>
        /// Procesa el cambio de contraseña del propio usuario, verificando primero la contraseña actual.
        /// </summary>
        /// <param name="usuario">Modelo con contrasena (actual), nuevaContrasena y confirmarContrasena.</param>
        /// <returns>
        /// Redirects to CambiarContrasena. Sets TempData["successMessage"] o TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: UsuarioHandler.
        /// Role required: Any (autenticado).
        /// </remarks>
        [HttpPost]
        public ActionResult CambiarContrasena(NewLoginModel usuario)
        {
            if (accesoAUsuario.AutenticarUsuario(usuario.id, usuario.contrasena))
            {
                if (usuario.nuevaContrasena == usuario.confirmarContrasena)
                {
                    accesoAUsuario.EditarUsuario(GetId(), GetRole(), usuario.nuevaContrasena);

                    TempData["successMessage"] = "Se cambió la contraseña.";
                }
                else
                {
                    TempData["errorMessage"] = "Las nuevas contraseñas no coinciden. Por favor, asegúrese de que ambas contraseñas sean idénticas.";
                }
            }
            else
            {
                TempData["errorMessage"] = "Contraseña actual incorrecta.";
            }

            return RedirectToAction("CambiarContrasena", new { id = GetId() });
        }

        /// <summary>
        /// Muestra el formulario (solo para admins) para restablecer la contraseña de cualquier usuario.
        /// </summary>
        /// <param name="idUsuario">Correo institucional del usuario cuya contraseña se restablecerá.</param>
        /// <param name="nombreCompleto">Nombre completo a mostrar en la vista.</param>
        /// <returns>
        /// View: CambiarContrasenaAdmin con el modelo NewLoginModel —
        /// ViewBag.Usuario (ParticipanteModel), ViewBag.NombreCompleto,
        /// ViewBag.Id, ViewBag.Role, ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// Redirects to Participante/VerParticipantes si idUsuario es vacío.
        /// Redirects to CerrarSesion si el rol no es Admin (1).
        /// </returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, AsesorHandler.
        /// Role required: Admin (1).
        /// </remarks>
        public ActionResult CambiarContrasenaAdmin(string idUsuario, string nombreCompleto)
        {
            // Ensure the current user is an admin
            if (GetRole() == 1)
            {
                // Validate the user ID to be changed
                if (!string.IsNullOrEmpty(idUsuario))
                {
                    ViewBag.Usuario = accesoAParticipante.ObtenerParticipante(idUsuario);
                    int rolUsuario = 0;

                    AsesorModel asesor = accesoAAsesor.ObtenerAsesor(idUsuario);
                    if (asesor != null)
                    {
                        rolUsuario = 2;
                    }

                    ViewBag.NombreCompleto = nombreCompleto;

                    NewLoginModel usuario = new NewLoginModel() { id = idUsuario, role = rolUsuario };

                    if (TempData["errorMessage"] != null)
                    {
                        ViewBag.ErrorMessage = TempData["errorMessage"].ToString();
                    }
                    if (TempData["successMessage"] != null)
                    {
                        ViewBag.SuccessMessage = TempData["successMessage"].ToString();
                    }

                    ViewBag.Id = GetId();
                    ViewBag.Role = GetRole();

                    return View(usuario);
                }
                else
                {
                    TempData["errorMessage"] = "Identificador de usuario no válido.";
                    return RedirectToAction("VerParticipantes", "Participante");
                }
            }

            // If not admin, redirect to logout or dashboard
            return RedirectToAction("CerrarSesion");
        }

        /// <summary>
        /// Procesa el restablecimiento de contraseña de otro usuario por parte del administrador,
        /// sin requerir la contraseña actual. Opcionalmente notifica al usuario por correo.
        /// </summary>
        /// <param name="usuario">Modelo con id, rol, nuevaContrasena, confirmarContrasena y enviarPorCorreo.</param>
        /// <returns>
        /// Redirects to CambiarContrasenaAdmin. Sets TempData["successMessage"] o TempData["errorMessage"].
        /// Redirects to CerrarSesion si el rol no es Admin (1).
        /// </returns>
        /// <remarks>
        /// Handlers: UsuarioHandler.
        /// Role required: Admin (1).
        /// </remarks>
        [HttpPost]
        public ActionResult CambiarContrasenaAdmin(NewLoginModel usuario)
        {
            // Ensure only admins can perform this action
            if (GetRole() == 1)
            {
                // Check if the new passwords are blank
                if (string.IsNullOrWhiteSpace(usuario.nuevaContrasena) || string.IsNullOrWhiteSpace(usuario.confirmarContrasena))
                {
                    TempData["errorMessage"] = "Las nuevas contraseñas no pueden estar en blanco.";
                }
                else if (usuario.nuevaContrasena == usuario.confirmarContrasena)
                {
                    // Admin resets the user's password without needing the old one
                    accesoAUsuario.EditarUsuario(usuario.id, usuario.role, usuario.nuevaContrasena);

                    if (usuario.enviarPorCorreo) { EnviarContrasenaAdmin(usuario.id, usuario.nuevaContrasena); }

                    TempData["successMessage"] = "La contraseña del usuario se cambió correctamente.";
                }
                else
                {
                    TempData["errorMessage"] = "Las nuevas contraseñas no coinciden.";
                }

                return RedirectToAction("CambiarContrasenaAdmin", new { idUsuario = usuario.id });
            }

            // If not admin, redirect to logout or dashboard
            return RedirectToAction("CerrarSesion");
        }

        /// <summary>Muestra la bitácora de todos los accesos al sistema (sin filtro de fecha ni usuario).</summary>
        /// <returns>
        /// View: VerBitacoraAccesos —
        /// ViewBag.BitacoraAccesos (List&lt;BitacoraAcceso&gt;), ViewBag.Id, ViewBag.Role,
        /// ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>
        /// Handlers: UsuarioHandler.
        /// Role required: Admin (1).
        /// </remarks>
        public ActionResult VerBitacoraAccesos()
        {
            int role = GetRole();
            string idUsuario = GetId();

            List<BitacoraAcceso> accesos = accesoAUsuario.SelectBitacoraAccesosPorFecha(null, null, null);

            ViewBag.Id = GetId();
            ViewBag.Role = GetRole();
            ViewBag.BitacoraAccesos = accesos;

            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View();
        }

        /// <summary>
        /// Muestra la bitácora de accesos de un usuario específico para los últimos N días.
        /// </summary>
        /// <param name="idUsuario">Correo institucional del usuario a consultar.</param>
        /// <param name="diasAtras">Cantidad de días hacia atrás a incluir (predeterminado: 30).</param>
        /// <returns>
        /// View: VerBitacoraAccesos —
        /// ViewBag.BitacoraAccesos, ViewBag.Id, ViewBag.Role, ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>
        /// Handlers: UsuarioHandler.
        /// Role required: Admin (1).
        /// </remarks>
        [HttpGet]
        public ActionResult VerBitacoraAccesoUsuario(string idUsuario, int diasAtras = 30)
        {
            if (string.IsNullOrEmpty(idUsuario))
            {
                return View("VerBitacoraAccesos");
            }

            List<BitacoraAcceso> accesos = accesoAUsuario.SelectBitacoraAccesoUsuario(idUsuario, diasAtras);

            ViewBag.Id = GetId();
            ViewBag.Role = GetRole();
            ViewBag.BitacoraAccesos = accesos;

            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View("VerBitacoraAccesos");
        }

        /// <summary>
        /// Muestra la bitácora de accesos filtrada por rango de fechas y/o estado (EXITO / FRACASO).
        /// Si fechaDesde es null se usa 1 semana atrás; si fechaHasta es null se usa la fecha actual.
        /// </summary>
        /// <param name="fechaDesde">Fecha inicial del rango (formato string compatible con SQL).</param>
        /// <param name="fechaHasta">Fecha final del rango.</param>
        /// <param name="estadoAcceso">Filtro de estado: "EXITO", "FRACASO" o null para todos.</param>
        /// <returns>
        /// View: VerBitacoraAccesos —
        /// ViewBag.BitacoraAccesos, ViewBag.Id, ViewBag.Role, ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>
        /// Handlers: UsuarioHandler.
        /// Role required: Admin (1).
        /// </remarks>
        [HttpGet]
        public ActionResult VerBitacoraAccesoPorFecha(string fechaDesde, string fechaHasta, string estadoAcceso)
        {
            List<BitacoraAcceso> accesos = new List<BitacoraAcceso>();

            // Si fechaDesde es null se selecciona 1 semana antes
            // Si fechaHasta es null se selecciona fecha actual
            // Si estadoAcceso es null no hay filtro
            accesos = accesoAUsuario.SelectBitacoraAccesosPorFecha(fechaDesde, fechaHasta, estadoAcceso);
            

            ViewBag.Id = GetId();
            ViewBag.Role = GetRole();
            ViewBag.BitacoraAccesos = accesos;

            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View("VerBitacoraAccesos");
        }

        /// <summary>Muestra el último acceso registrado en la bitácora para un usuario específico.</summary>
        /// <param name="idUsuario">Correo institucional del usuario.</param>
        /// <returns>
        /// View: VerBitacoraAccesos —
        /// ViewBag.BitacoraAccesos (lista con un solo elemento), ViewBag.Id, ViewBag.Role.
        /// </returns>
        /// <remarks>
        /// Handlers: UsuarioHandler.
        /// Role required: Admin (1).
        /// </remarks>
        [HttpGet]
        public ActionResult VerBitacoraUltimoAccesoUsuario(string idUsuario)
        {
            BitacoraAcceso ultimoAcceso = null;
            List<BitacoraAcceso> accesos = new List<BitacoraAcceso>();

            if (!string.IsNullOrEmpty(idUsuario))
            {
                ultimoAcceso = accesoAUsuario.SelectUltimoAccesoUsuario(idUsuario);

                accesos.Add(ultimoAcceso);
            }

            ViewBag.Id = GetId();
            ViewBag.Role = GetRole();
            ViewBag.BitacoraAccesos = accesos;

            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View("VerBitacoraAccesos");
        }


        /// <summary>Envía al nuevo usuario un correo con su contraseña temporal de registro.</summary>
        private async Task<IActionResult> EnviarCorreoRegistro(string correo, string contrasena)
        {
            string subject = "Registro en el SISTEMA DE INSCRIPCIONES METICS";
            string message = $"<p>Se ha registrado al usuario con correo institucional {correo} en el Sistema de Competencias Digitales para la Docencia - METICS.</p>" +
                $"</p>Su contraseña temporal es <strong>{contrasena}</strong></p>" +
                $"<p>Recuerde que puede cambiar la contraseña al iniciar sesión en el sistema desde el ícono de usuario.</p>";

            await _emailService.SendEmailAsync(correo, subject, message);
            return Ok();
        }

        /// <summary>Envía al usuario un correo notificándole que el admin cambió su contraseña.</summary>
        private async Task<IActionResult> EnviarContrasenaAdmin(string correo, string contrasena)
        {
            string subject = "Cambio de contraseña en el SISTEMA DE INSCRIPCIONES METICS";
            string message = $"<p>Se ha cambiado la contraseña del usuario con correo institucional {correo} en el Sistema de Competencias Digitales para la Docencia - METICS.</p>" +
                $"</p>Su contraseña temporal es <strong>{contrasena}</strong></p>" +
                $"<p>Recuerde que puede cambiar la contraseña al iniciar sesión en el sistema desde el ícono de usuario.</p>";

            await _emailService.SendEmailAsync(correo, subject, message);
            return Ok();
        }

        /// <summary>Genera una contraseña aleatoria de 10 caracteres alfanuméricos y con símbolos especiales.</summary>
        private string GenerateRandomPassword()
        {
            int length = 10;
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+";
            var random = new Random();
            string password = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return password;
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
        /// Determina a dónde redirigir al usuario tras un login o paso de completación exitoso.
        /// Verifica en orden: correoAlternativo (todos los roles), carrera (solo participantes rol 0).
        /// </summary>
        private ActionResult DeterminarRedireccionPostLogin(string idUsuario, int rol)
        {
            string correoAlternativo = accesoAUsuario.ObtenerCorreoAlternativo(idUsuario);
            if (string.IsNullOrWhiteSpace(correoAlternativo))
                return RedirectToAction("CompletarCorreoAlternativo", "Usuario");

            if (rol == 0)
            {
                ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idUsuario);
                if (participante != null && string.IsNullOrWhiteSpace(participante.carrera))
                    return RedirectToAction("CompletarCarreraYAreas", "Usuario");
            }

            return RedirectToAction("ListaGruposDisponibles", "Grupo");
        }
    }
}