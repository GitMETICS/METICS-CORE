using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;
using webMetics.Models;
using MimeKit;

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

        public UsuarioController(IWebHostEnvironment environment, IConfiguration configuration, IDataProtectionProvider protector)
        {
            _environment = environment;
            _configuration = configuration;
            _protector = protector;

            cookiesController = new CookiesController(environment, configuration);
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
        public ActionResult IniciarSesion(LoginModel usuario)
        {
            if (ModelState.IsValid)
            {
                LoginModel usuarioAutorizado = AutenticarUsuario(usuario);

                if (usuarioAutorizado != null)
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

            try
            {
                usuario.id = usuario.correo; // Esto define que el identificador del usuario es el correo
                ParticipanteModel participante = new ParticipanteModel()
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

                // Verificar si el usuario ya existe
                bool usuarioExiste = accesoAUsuario.ExisteUsuario(usuario.id);

                // Si el usuario no existe, crear un nuevo registro
                if (!usuarioExiste)
                {
                    accesoAUsuario.CrearUsuario(usuario.id, contrasena);
                    accesoAParticipante.CrearParticipante(participante);
                    exito = true;
                }
                // Si el usuario existe, verificar si fue registrado por el propio usuario
                else if (!accesoAUsuario.ObtenerRegistradoPorUsuario(usuario.id)) // Si no fue registrado por el propio usuario, actualizar los datos del participante
                {
                    accesoAParticipante.EditarParticipante(participante);
                    exito = true;
                }
                else
                {
                    TempData["errorMessage"] = "Ya existe un usuario con el mismo correo institucional.";
                }
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = "No se pudo crear el usuario. Inténtelo de nuevo.";
            }

            return exito;
        }

        // Método para autenticar el usuario y realizar el inicio de sesión
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

                    IDataProtector protector = _protector.CreateProtector("USUARIOAUTORIZADO");
                    string idEncriptado = protector.Protect(idUsuario);

                    Response.Cookies.Append("USUARIOAUTORIZADO", idEncriptado, new CookieOptions
                    {
                        Expires = DateTime.Now.AddMinutes(20)
                    });

                    Response.Cookies.Append("rolUsuario", rolUsuario.ToString(), new CookieOptions
                    {
                        Expires = DateTime.Now.AddMinutes(20)
                    });

                    Response.Cookies.Append("idUsuario", idUsuario, new CookieOptions
                    {
                        Expires = DateTime.Now.AddMinutes(20)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AutenticarUsuario: {ex.Message}");
            }

            return usuarioAutorizado;
        }

        public ActionResult InformacionPersonal(string id)
        {
            const int RolUsuarioParticipante = 0;
            const int RolUsuarioAdmin = 1;
            const int RolUsuarioAsesor = 2;

            int role = GetRole();

            ViewBag.Id = GetId();
            ViewBag.Role = role;

            switch (role)
            {
                case RolUsuarioParticipante:
                    ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(id);
                    List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesParticipante(id);
                    List<GrupoModel> gruposParticipante = accesoAGrupo.ObtenerListaGruposParticipante(id);

                    UsuarioModel usuarioParticipante = new UsuarioModel()
                    {
                        id = id,
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

                    break;

                case RolUsuarioAdmin:
                    break;

                case RolUsuarioAsesor:
                    AsesorModel asesor = accesoAAsesor.ObtenerAsesor(id);
                    List<GrupoModel> gruposAsesor = accesoAGrupo.ObtenerListaGruposAsesor(id);

                    UsuarioModel usuarioAsesor = new UsuarioModel()
                    {
                        id = id,
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
                    ViewBag.ListaGrupos = gruposAsesor;

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
        public ActionResult CerrarSesion()
        {
            // Eliminar datos del usuario
            Response.Cookies.Delete("USUARIOAUTORIZADO");
            Response.Cookies.Delete("rolUsuario");
            Response.Cookies.Delete("idUsuario");

            return RedirectToAction("IniciarSesion");
        }

        public ActionResult CambiarContrasena(string id)
        {
            if (GetId() == id)
            {
                ViewBag.Id = GetId();
                ViewBag.Role = GetRole();

                NewLoginModel usuario = new NewLoginModel() { id = id };

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

        /* Método para enviar confirmación de registro al usuario*/
        private void EnviarCorreoRegistro(string correo, string contrasena)
        {
            try
            {
                // Configurar el mensaje de correo electrónico de registro
                // Se utiliza la librería MimeKit para construir el mensaje
                // El mensaje incluye una versión en HTML y texto plano

                // Contenido base del mensaje en HTML y texto plano
                const string BASE_MESSAGE_HTML = ""; // Contenido HTML adicional puede ser agregado aquí
                const string BASE_MESSAGE_TEXT = "";
                const string BASE_SUBJECT = "Registro en el SISTEMA DE INSCRIPCIONES METICS"; // Asunto del correo

                MimeMessage message = new MimeMessage();

                // Configurar el remitente y el destinatario
                MailboxAddress from = new MailboxAddress("COMPETENCIAS DIGITALES", "COMPETENCIAS.DIGITALES@ucr.ac.cr");
                message.From.Add(from);
                MailboxAddress to = new MailboxAddress("Receiver", correo);
                message.To.Add(to);

                message.Subject = BASE_SUBJECT; // Asignar el asunto del correo

                // Crear el cuerpo del mensaje con el contenido HTML y texto plano
                BodyBuilder bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = BASE_MESSAGE_HTML +
                    "<p>Se ha registrado al usuario con identificación " + correo + " en el Sistema de Competencias Digitales para la Docencia - METICS.</p>" +
                    "<p>Su contraseña temporal es <strong>" + contrasena + "</strong></p>" +
                    "<p>Recuerde que puede cambiar la contraseña al iniciar sesión en el sistema desde el ícono de usuario.";
                bodyBuilder.TextBody = BASE_MESSAGE_TEXT;
                bodyBuilder.HtmlBody += "</p>";

                message.Body = bodyBuilder.ToMessageBody();

                // Enviar el correo electrónico utilizando un cliente SMTP
                using var client = new MailKit.Net.Smtp.SmtpClient();
                // Configurar el cliente SMTP para el servidor de correo de la UCR
                client.Connect("smtp.ucr.ac.cr", 587); // Se utiliza el puerto 587 para enviar correos
                client.Authenticate(from.Address, _configuration["EmailSettings:SMTPPassword"]);

                // Enviar el mensaje
                client.Send(message);

                // Desconectar el cliente SMTP
                client.Disconnect(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
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