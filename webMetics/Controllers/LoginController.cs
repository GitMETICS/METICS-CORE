using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;
using webMetics.Models;

/* 
 * Controlador del proceso de login y logout del sistema
 */
namespace webMetics.Controllers
{
    public class LoginController : Controller
    {
        // Controlador encargado de la funcionalidad de inicio de sesión

        // Controladores y Handlers utilizados en el controlador
        private protected CookiesController cookiesController;
        private protected UsuarioHandler accesoAUsuario;
        private protected ParticipanteHandler accesoAParticipante;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly IDataProtectionProvider _protector;

        public LoginController(IWebHostEnvironment environment, IConfiguration configuration, IDataProtectionProvider protector)
        {
            _environment = environment;
            _configuration = configuration;
            _protector = protector;

            cookiesController = new CookiesController(environment, configuration);
            accesoAUsuario = new UsuarioHandler(environment, configuration);
            accesoAParticipante = new ParticipanteHandler(environment, configuration);
        }

        // Método para mostrar la vista de inicio de sesión
        public ActionResult Login()
        {
            // Retorna la vista de inicio de sesión
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

        // Método para procesar el inicio de sesión cuando se envía el formulario de inicio de sesión
        [HttpPost]
        public ActionResult Login(LoginModel usuario)
        {
            // Verificar si el modelo enviado desde el formulario es válido
            if (ModelState.IsValid)
            {
                // Validar el usuario y contraseña ingresados
                LoginModel usuarioAutorizado = ValidacionUsuario(usuario);

                if (usuarioAutorizado != null)
                {
                    // Si el usuario y contraseña son válidos, redirigir a la página de inicio
                    return RedirectToAction("ListaGruposDisponibles", "Grupo");
                }
                else
                {
                    // Si el usuario y contraseña son inválidos, mostrar un mensaje de error
                    TempData["errorMessage"] = "Número de identificación o contraseña inválidos.";
                    return RedirectToAction("Login");
                }
            }
            else
            {
                // Si el modelo no es válido, volver a mostrar la vista de inicio de sesión con el modelo original
                return View(usuario);
            }
        }

        /* Método para procesar el formulario de creación de usuario con los datos ingresados */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearUsuario(UsuarioModel usuario)
        {
            try
            {
                if (ModelState.IsValid) 
                {
                    bool registrado = RegistrarUsuario(usuario);

                    if (registrado)
                    {
                        ViewBag.Correo = usuario.correo;
                        ViewBag.Titulo = "Registro realizado";
                        ViewBag.Message = "Los datos fueron guardados éxitosamente. La contraseña será enviada al correo";
                    }
                    else
                    {
                        ViewBag.Titulo = "No se pudo realizar el registro";
                        ViewBag.Message = "Los datos no pudieron ser guardados. Por favor, inténtelo nuevamente.";
                    }
                    return View("ParticipanteRegistrado");
                }
                else
                {
                    ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
                    return View("Registrarse");
                }
            }
            catch (Exception e)
            {
                // Si ocurre una excepción, mostrar el mensaje de error en la vista
                ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
                ViewBag.ErrorMessage = e;
                return View();
            }
        }

        /* Método para mostrar el formulario para crear un nuevo usuario */
        public ActionResult Registrarse()
        {
            // Obtener datos necesarios para llenar las opciones del formulario (áreas)
            ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();

            return View("Registrarse");
        }

        private bool RegistrarUsuario(UsuarioModel usuario)
        {
            bool exitoUsuario = false;
            bool exitoParticipante = false;

            if (!accesoAUsuario.ExisteUsuario(usuario.identificacion))
            {
                exitoUsuario = accesoAUsuario.CrearUsuario(usuario.identificacion, usuario.contrasena);

                ParticipanteModel participante = new ParticipanteModel()
                {
                    idParticipante = usuario.identificacion,
                    nombre = "",
                    apellido_1 = "",
                    apellido_2 = "",
                    correo = usuario.correo,
                    tipoIdentificacion = "",
                    tipoParticipante = "",
                    unidadAcademica = "",
                    area = "",
                    departamento = "",
                    seccion = "",
                    condicion = "",
                    telefonos = "",
                    horasMatriculadas = 0,
                    horasAprobadas = 0
                };

                exitoParticipante = accesoAParticipante.CrearParticipante(participante);

                usuario.participante = participante;
            }

            if (exitoUsuario && exitoParticipante)
            {
                string mensaje = "Se ha registrado en el proyecto METICS.";
                EnviarCorreoRegistro(mensaje, usuario.correo);
            }

            return exitoUsuario;
        }

        // Método para validar el usuario y realizar el inicio de sesión
        public LoginModel ValidacionUsuario(LoginModel usuario)
        {
            try
            {
                bool autorizado = accesoAUsuario.AutenticarUsuario(usuario.identificacion, usuario.contrasena);

                if (autorizado)
                {
                    LoginModel usuarioAutorizado = accesoAUsuario.ObtenerUsuario(usuario.identificacion);
                    int rolUsuario = usuarioAutorizado.rol;
                    string idUsuario = usuarioAutorizado.identificacion;

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

                    return usuarioAutorizado;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        // Método para cerrar la sesión del usuario
        public ActionResult Logout()
        {
            // Eliminar las cookies del usuario
            Response.Cookies.Delete("USUARIOAUTORIZADO");
            Response.Cookies.Delete("rolUsuario");
            Response.Cookies.Delete("idUsuario");

            return Redirect("/Login/Login");
        }

        public ActionResult GestionarContrasena(string idParticipante)
        {
            NewLoginModel usuario = new NewLoginModel() { identificacion = idParticipante.ToString() };

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
            bool exito = false;
            string errorMessage = "";

            if (accesoAUsuario.AutenticarUsuario(usuario.identificacion, usuario.contrasena))
            {
                if (usuario.nuevaContrasena == usuario.confirmarContrasena)
                {
                    exito = accesoAUsuario.EditarUsuario(usuario.identificacion, usuario.nuevaContrasena);
                }
                else
                {
                    errorMessage = "Las nuevas contraseñas no coinciden. Por favor, asegúrese de que ambas contraseñas sean idénticas.";
                }
            }
            else
            {
                errorMessage = "Contraseña actual incorrecta. Por favor, inténtelo de nuevo.";
            }


            if (exito)
            {
                TempData["successMessage"] = "Su contraseña fue cambiada éxitosamente.";
            }
            else
            {
                if (errorMessage != "")
                {
                    TempData["errorMessage"] = errorMessage;
                }
                else
                {
                    TempData["errorMessage"] = "No se pudo cambiar su contraseña.";
                }
            }

            return RedirectToAction("GestionarContrasena", new { idParticipante = usuario.identificacion });
        }

        /* Método para enviar confirmación de registro al usuario*/
        private void EnviarCorreoRegistro(string mensaje, string correoParticipante)
        {
            /*// Configurar el mensaje de correo electrónico con el comprobante de inscripción y el archivo adjunto (si corresponde)
            // Se utiliza la librería MimeKit para construir el mensaje
            // El mensaje incluye una versión en HTML y texto plano

            // Contenido base del mensaje en HTML y texto plano
            const string BASE_MESSAGE_HTML = ""; // Contenido HTML adicional puede ser agregado aquí
            const string BASE_MESSAGE_TEXT = "";
            const string BASE_SUBJECT = "Registro realizado"; // Asunto del correo

            MimeMessage message = new MimeMessage();

            // Configurar el remitente y el destinatario
            MailboxAddress from = new MailboxAddress("COMPETENCIAS DIGITALES", "COMPETENCIAS.DIGITALES@ucr.ac.cr"); // TODO: Cambiar el correo del remitente
            message.From.Add(from);
            MailboxAddress to = new MailboxAddress("Receiver", correoParticipante);
            message.To.Add(to);

            message.Subject = BASE_SUBJECT; // Asignar el asunto del correo

            // Crear el cuerpo del mensaje con el contenido HTML y texto plano
            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = BASE_MESSAGE_HTML + mensaje;
            bodyBuilder.TextBody = BASE_MESSAGE_TEXT;
            bodyBuilder.HtmlBody += "</p>";

            message.Body = bodyBuilder.ToMessageBody();

            // Enviar el correo electrónico utilizando un cliente SMTP
            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                // Configurar el cliente SMTP para el servidor de correo de la UCR
                client.Connect("smtp.ucr.ac.cr", 587); // Se utiliza el puerto 587 para enviar correos
                client.Authenticate(from.Address, "4r7QaF4.NhnJkH!"); // Cambiar la cuenta de correo y contraseña real para enviar el correo

                // Enviar el mensaje
                client.Send(message);

                // Desconectar el cliente SMTP
                client.Disconnect(true);
            }*/
        }
    }
}