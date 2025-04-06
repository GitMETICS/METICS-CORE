﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using System.Security.Claims;
using System.Text.Json;
using webMetics.Handlers;
using webMetics.Models;

/* 
 * Controlador del proceso de login y logout del sistema
 */
namespace webMetics.Controllers
{
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
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly EmailService _emailService;

        public UsuarioController(IWebHostEnvironment environment, IConfiguration configuration, IDataProtectionProvider protector, IHttpContextAccessor httpContextAccessor, EmailService emailService)
        {
            _environment = environment;
            _configuration = configuration;
            _protector = protector;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;

            accesoAUsuario = new UsuarioHandler(environment, configuration);
            accesoAParticipante = new ParticipanteHandler(environment, configuration);
            accesoAAsesor = new AsesorHandler(environment, configuration);
            accesoAGrupo = new GrupoHandler(environment, configuration);
            accesoAInscripcion = new InscripcionHandler(environment, configuration);
        }

        // Método para mostrar la vista de inicio de sesión
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

        // Método para procesar el inicio de sesión cuando se envía el formulario de inicio de sesión
        [HttpPost]
        public async Task<ActionResult> IniciarSesion(LoginModel usuario)
        {
            if (ModelState.IsValid)
            {
                bool usuarioAutorizado = await AutenticarUsuario(usuario);

                if (usuarioAutorizado)
                {
                    return RedirectToAction("ListaGruposDisponibles", "Grupo");
                }
                else
                {
                    TempData["errorMessage"] = "Correo institucional o contraseña inválidos.";
                    return RedirectToAction("IniciarSesion", "Usuario");
                }
            }
            else
            {
                return View(usuario);
            }
        }

        public IActionResult IniciarSesionSSO()
        {
            return Challenge(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> InicioSesionSSOCallback()
        {
            if (User.Identity.IsAuthenticated)
            {
                bool usuarioAutorizadoSSO = await AutenticarUsuarioSSO(User);

                if (usuarioAutorizadoSSO)
                {
                    return RedirectToAction("ListaGruposDisponibles", "Grupo");
                }
                else
                {
                    TempData["errorMessage"] = "Error en la autenticación SSO.";
                    return RedirectToAction("IniciarSesion", "Usuario");
                }
            }
            else
            {
                TempData["errorMessage"] = "No se pudo autenticar con SSO.";
                return RedirectToAction("IniciarSesion", "Usuario");
            }
        }

        // Método para mostrar el formulario para crear un nuevo usuario
        public ActionResult FormularioRegistro()
        {
            // Obtener datos necesarios para llenar las opciones del formulario (áreas)
            ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
            return View();
        }

        // Método para procesar el formulario de creación de usuario con los datos ingresados
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

        // Método para crear un usuario y hacer match con la base de datos si ya hay un participante con los mismos datos.
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
                        accesoAUsuario.CrearUsuario(usuario.id, contrasena);

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

        // Método para autenticar al usuario e iniciar sesión
        private async Task<bool> AutenticarUsuario(LoginModel usuario)
        {
            try
            {
                bool usuarioValidacion = accesoAUsuario.AutenticarUsuario(usuario.id, usuario.contrasena);
                if (usuarioValidacion)
                {
                    // Se obtiene el usuario solo si fue autorizado
                    LoginModel usuarioAutorizado = accesoAUsuario.ObtenerUsuario(usuario.id);

                    // Crear Claims para el usuario autenticado
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, usuarioAutorizado.id),
                        new Claim(ClaimTypes.Role, usuarioAutorizado.rol.ToString()), // Agregar el rol como un Claim
                    };

                    // Crear la identidad del usuario
                    var claimsIdentity = new ClaimsIdentity(
                        claims,
                        authenticationType: CookieAuthenticationDefaults.AuthenticationScheme,
                        nameType: ClaimTypes.NameIdentifier,
                        roleType: ClaimTypes.Role); // El nombre del esquema debe coincidir con el configurado en Program.cs
                    
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, // Para recordar al usuario
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(20) // Establecer la duración de la sesión
                    };

                    ClaimsPrincipal cp = new ClaimsPrincipal(claimsIdentity);
                    // Iniciar sesión del usuario utilizando el esquema de autenticación de la aplicación
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, cp, authProperties); // Utilizar el esquema "Cookies"
                    HttpContext.User = cp;

                    // Almacenamiento en Sesión
                    HttpContext.Session.SetString("UsuarioId", usuarioAutorizado.id);
                    HttpContext.Session.SetInt32("UsuarioRol", usuarioAutorizado.rol);

                    //Almacenar objeto en la sesion.
                    HttpContext.Session.SetString("UsuarioAutorizado", JsonSerializer.Serialize(usuarioAutorizado));

                    return true;
                }
                else
                {
                    //Si la autenticación falla, lanzar una excepción
                    throw new Exception("Authentication failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AutenticarUsuario: {ex.Message}");
                // Considerar registrar el error o mostrar un mensaje al usuario
                return false; // o lanzar la excepción, dependiendo de su manejo de errores
            }
        }

        private async Task<bool> AutenticarUsuarioSSO(ClaimsPrincipal userClaims)
        {
            try
            {
                // Obtener el ID del usuario del proveedor SSO
                var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new Exception("User ID not found in claims.");
                }

                // Obtener información adicional del usuario desde los Claims
                string nombre = userClaims.FindFirst(ClaimTypes.Name)?.Value;
                string email = userClaims.FindFirst(ClaimTypes.Email)?.Value;
                string rol = userClaims.FindFirst(ClaimTypes.Role)?.Value;

                // Crear objeto LoginModel con los datos del usuario
                var usuarioAutorizado = new LoginModel
                {
                    id = userId,
                    rol = rol != null ? int.Parse(rol) : 0 // Asumiendo que el rol es un entero
                };

                // Crear Claims para la aplicación .NET Core
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuarioAutorizado.id),
                    new Claim(ClaimTypes.Role, usuarioAutorizado.rol.ToString())
                };

                // Crear la identidad del usuario
                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    ClaimTypes.NameIdentifier,
                    ClaimTypes.Role);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(20)
                };

                ClaimsPrincipal cp = new ClaimsPrincipal(claimsIdentity);

                // Iniciar sesión del usuario utilizando el esquema de autenticación de la aplicación
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, cp, authProperties);
                HttpContext.User = cp;

                // Almacenar datos en Sesión
                HttpContext.Session.SetString("UsuarioId", usuarioAutorizado.id);
                HttpContext.Session.SetInt32("UsuarioRol", usuarioAutorizado.rol);
                HttpContext.Session.SetString("UsuarioAutorizado", JsonSerializer.Serialize(usuarioAutorizado));

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AutenticarUsuarioSSO: {ex.Message}");
                return false;
            }
        }

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

        // Método para cerrar la sesión del usuario
        public async Task<ActionResult>CerrarSesion()
        {
            // Administrar sesión del usuario
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("IniciarSesion");
        }

        public ActionResult CambiarCredencialesUsuario(string idUsuario, string nombreCompleto)
        {
            // Ensure the current user is an admin
            if (GetRole() == 1)
            {
                // Validate the user ID to be changed
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

        [HttpPost]
        public ActionResult CambiarCredencialesUsuario(NewLoginModel usuario)
        {
            // Ensure only admins can perform this action
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

            // accesoAUsuario.EliminarUsuario(usuario.oldId);
        }

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

        public ActionResult CambiarContrasena()
        {
            string idUsuario = GetId();
            int role = GetRole();

            NewLoginModel usuario = new NewLoginModel() { id = idUsuario };

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


        // Método para enviar confirmación de registro al usuario
        private async Task<IActionResult> EnviarCorreoRegistro(string correo, string contrasena)
        {
            string subject = "Registro en el SISTEMA DE INSCRIPCIONES METICS";
            string message = $"<p>Se ha registrado al usuario con correo institucional {correo} en el Sistema de Competencias Digitales para la Docencia - METICS.</p>" +
                $"</p>Su contraseña temporal es <strong>{contrasena}</strong></p>" +
                $"<p>Recuerde que puede cambiar la contraseña al iniciar sesión en el sistema desde el ícono de usuario.</p>";

            await _emailService.SendEmailAsync(correo, subject, message);
            return Ok();
        }

        private async Task<IActionResult> EnviarContrasenaAdmin(string correo, string contrasena)
        {
            string subject = "Cambio de contraseña en el SISTEMA DE INSCRIPCIONES METICS";
            string message = $"<p>Se ha cambiado la contraseña del usuario con correo institucional {correo} en el Sistema de Competencias Digitales para la Docencia - METICS.</p>" +
                $"</p>Su contraseña temporal es <strong>{contrasena}</strong></p>" +
                $"<p>Recuerde que puede cambiar la contraseña al iniciar sesión en el sistema desde el ícono de usuario.</p>";

            await _emailService.SendEmailAsync(correo, subject, message);
            return Ok();
        }

        private string GenerateRandomPassword()
        {
            int length = 10;
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+";
            var random = new Random();
            string password = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return password;
        }
        private int GetRole()
        {
            int role = 0;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var session = httpContext.Session;
                var cookies = httpContext.Request.Cookies;

                role = session.GetInt32("UsuarioRol") ?? 0;
            }
            return role;
        }

        private string GetId()
        {
            string id = "";

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var session = httpContext.Session;
                var cookies = httpContext.Request.Cookies;

                id = session.GetString("UsuarioId") ?? "";
            }

            return id;
        }
    }
}