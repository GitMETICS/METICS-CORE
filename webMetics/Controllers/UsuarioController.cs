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
                else
                {
                    return RedirectToAction("IniciarSesion");
                }
            }
            else
            {
                ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
                return View(usuario);
            }
        }

        // Método para crear un usuario y hacer match con la base de datos si ya hay un participante con los mismos datos
        private bool CrearUsuario(UsuarioModel usuario, string contrasena)
        {
            bool exito = false;

            try
            {
                usuario.id = usuario.correo; // Esto define que el identificador del usuario es el correo.

                // Si no existe el participante en la base de datos, debe crearse un usuario.
                if (!accesoAParticipante.ExisteParticipante(usuario.id))
                {
                    if (!accesoAUsuario.ExisteUsuario(usuario.id))
                    {
                        accesoAUsuario.CrearUsuario(usuario.id, contrasena);

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

                        accesoAParticipante.CrearParticipante(participante);
                        exito = true;
                    }
                    else
                    {
                        TempData["errorMessage"] = "Ya existe un usuario con los mismos datos.";
                        exito = false;
                    }
                } 
                // Si sí existe el participante en la base de datos, significa que existe su usuario.
                else
                {
                    // No puede existir un participante que no tenga también un usuario.
                    if (!accesoAUsuario.ExisteUsuario(usuario.id))
                    {
                        TempData["errorMessage"] = "Ocurrió un error al procesar los datos.";
                        exito = false;
                    }
                    else
                    {
                        // Modificamos la contraseña temporal que estaba en la base de datos del usuario con la nueva contraseña.
                        accesoAUsuario.EditarUsuario(usuario.id, 0, contrasena); // TODO: Hacer bien esto con el rol por default.

                        // Actualizamos el participante con los nuevos datos.
                        ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(usuario.id);

                        if (string.IsNullOrEmpty(participante.numeroIdentificacion))
                        {
                            ParticipanteModel participanteActualizado = new ParticipanteModel()
                            {
                                idParticipante = participante.idParticipante,
                                nombre = usuario.nombre,
                                primerApellido = usuario.primerApellido,
                                segundoApellido = usuario.segundoApellido,
                                tipoIdentificacion = usuario.tipoIdentificacion,
                                numeroIdentificacion = usuario.numeroIdentificacion,
                                correo = participante.correo,
                                tipoParticipante = usuario.tipoParticipante,
                                condicion = usuario.condicion,
                                telefono = usuario.telefono,
                                area = usuario.area,
                                departamento = usuario.departamento,
                                unidadAcademica = usuario.unidadAcademica,
                                sede = usuario.sede,
                                horasMatriculadas = participante.horasMatriculadas,
                                horasAprobadas = participante.horasAprobadas
                            };

                            accesoAParticipante.EditarParticipante(participanteActualizado);
                            exito = true;
                        }
                        else
                        {
                            // Si ya están los otros datos en la base de datos, significa que ya el usuario se había registrado.
                            TempData["errorMessage"] = "Ya existe un usuario con el mismo correo institucional y número de identificación.";
                            exito = false;
                        }
                    }
                }
            }
            catch
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
                        Expires = DateTime.Now.AddHours(2)
                    });

                    Response.Cookies.Append("rolUsuario", rolUsuario.ToString(), new CookieOptions
                    {
                        Expires = DateTime.Now.AddHours(2)
                    });

                    Response.Cookies.Append("idUsuario", idUsuario, new CookieOptions
                    {
                        Expires = DateTime.Now.AddHours(2)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AutenticarUsuario: {ex.Message}");
            }

            return usuarioAutorizado;
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

        [HttpPost]
        public ActionResult CambiarContrasena(NewLoginModel usuario)
        {
            if (accesoAUsuario.AutenticarUsuario(usuario.id, usuario.contrasena))
            {
                if (usuario.nuevaContrasena == usuario.confirmarContrasena)
                {
                    accesoAUsuario.EditarUsuario(usuario.id, GetRole(), usuario.nuevaContrasena);

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

            return RedirectToAction("CambiarContrasena", new { idParticipante = usuario.id });
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
                const string BASE_SUBJECT = "Registro en el Sistema de Competencias Digitales para la Docencia-METICS"; // Asunto del correo

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
                    "<p>Se ha registrado al usuario con identificación " + correo + " en el Sistema de Competencias Digitales para la Docencia-METICS.</p>" +
                    "<p>Su contraseña temporal es " + contrasena + "</p>" +
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
                TempData["errorMessage"] = ex.ToString();
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