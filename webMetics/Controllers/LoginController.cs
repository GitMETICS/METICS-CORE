using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;
using webMetics.Models;
using System.Security.Principal;

/* 
 * Controlador del proceso de login y logout del sistema
 */
namespace webMetics.Controllers
{
    public class LoginController : Controller
    {
        // Controlador encargado de la funcionalidad de inicio de sesión

        // Controladores y Handlers utilizados en el controlador
        private protected CookiesController cookiesController = new CookiesController();
        private protected UsuarioHandler accesoAUsuario = new UsuarioHandler();
        private protected ParticipanteHandler accesoAParticipante = new ParticipanteHandler();

        // Constructor del controlador
        public LoginController()
        {

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
                LoginModel usuarioValido = ValidacionUsuario(usuario);
                if (usuarioValido != null)
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

        // --------------------------------------------------------------------------------------------------------------------------------------------


        // --------------------------------------------------------------------------------------------------------------------------------------------

        /* Método para procesar el formulario de creación de usuario con los datos ingresados */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FormularioDeUsuario(UsuarioModel usuario)
        {
            try
            {
                if (ModelState.IsValid) 
                {
                    bool registrado = RegistrarUsuario(usuario);

                    if (registrado)
                    {
                        ViewBag.Participante = usuario.participante;
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
                    return View("FormularioDeUsuario");
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
        public ActionResult CrearUsuario()
        {
            // Obtener datos necesarios para llenar las opciones del formulario (áreas)
            ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();

            return View("FormularioDeUsuario");
        }

        private bool RegistrarUsuario(UsuarioModel usuario)
        {
            bool exitoUsuario;
            bool exitoParticipante;
            string contrasena = "pass";/*Membership.GeneratePassword(15, 3);*/

            if (accesoAUsuario.ExisteUsuario(usuario.identificacion))
            {
                exitoUsuario = accesoAUsuario.EditarUsuario(usuario.identificacion, contrasena);
            }
            else
            {
                exitoUsuario = accesoAUsuario.CrearUsuario(usuario.identificacion, contrasena);
            }

            if (accesoAParticipante.ExisteParticipante(usuario.participante))
            {
                exitoParticipante = accesoAParticipante.EditarParticipante(usuario.participante);
            }
            else
            {
                exitoParticipante = accesoAParticipante.CrearParticipante(usuario.participante);
            }

            if (exitoUsuario && exitoParticipante)
            {
                string mensaje = ConstructorDelMensajeRegistro(contrasena, usuario.participante);
                EnviarCorreoRegistro(mensaje, usuario.participante.correo);
            }

            return exitoUsuario && exitoParticipante;
        }

        // Método para validar el usuario y realizar el inicio de sesión
        public LoginModel ValidacionUsuario(LoginModel usuario)
        {
            /*// Verificar si se proporcionaron la identificación del usuario y la contraseña
            if (usuario.identificacion != null && usuario.contrasena != null)
            {
                bool exito = accesoAUsuario.Login(usuario.identificacion, usuario.contrasena);

                if (exito)
                {
                    // Si se encontró el participante, crear la autenticación y agregar la cookie del usuario
                    FormsAuthenticationTicket idUsuario = new FormsAuthenticationTicket(1, usuario.identificacion.ToString(), DateTime.Now, DateTime.Now.AddHours(2), true, usuario.identificacion.ToString());
                    string idEcrypt = FormsAuthentication.Encrypt(idUsuario);
                    HttpCookie cookie = new HttpCookie("USUARIOAUTORIZADO", idEcrypt);
                    Response.Cookies.Add(cookie);

                    // Crear y agregar una cookie con el rol del usuario y el id
                    HttpCookie rolCookie = cookiesController.CreateCookie("rolUsuario", accesoAUsuario.ObtenerUsuario(usuario.identificacion).rol.ToString(), DateTime.Now.AddHours(2));
                    HttpCookie idCookie = cookiesController.CreateCookie("idUsuario", accesoAUsuario.ObtenerUsuario(usuario.identificacion).identificacion.ToString(), DateTime.Now.AddHours(2));

                    if (rolCookie != null)
                    {
                        Response.Cookies.Add(rolCookie);
                    }

                    if (idCookie != null)
                    {
                        Response.Cookies.Add(idCookie);
                    }

                    // Retorna el modelo del usuario (inicio de sesión exitoso)
                    return usuario;
                }
                else
                {
                    // Si no se encontró el usuario, retorna null (usuario no válido)
                    return null;
                }
            }
            else
            {
                // Si no se proporcionó la identificación del usuario o la contraseña, retorna null (usuario no válido)
                return null;
            }*/

            return null;
        }

        // Método para cerrar la sesión del usuario
        public ActionResult Logout()
        {
            /*// Limpiar la información del usuario y cerrar la sesión
            HttpContext.User = new GenericPrincipal(new GenericIdentity(""), null);
            FormsAuthentication.SignOut();

            // Eliminar las cookies del usuario
            HttpCookie cookie = Request.Cookies["USUARIOAUTORIZADO"];
            HttpCookie cookieRol = Request.Cookies["rolUsuario"];
            if (cookie != null)
            {
                cookie.Expires = DateTime.Now.AddYears(-1);
                Response.Cookies.Add(cookie);
            }
            if (cookieRol != null)
            {
                cookieRol.Expires = DateTime.Now.AddYears(-1);
                Response.Cookies.Add(cookieRol);
            }*/

            // Redirigir a la página de inicio de sesión
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

            if (accesoAUsuario.Login(usuario.identificacion, usuario.contrasena))
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

        private string ConstructorDelMensajeRegistro(string contrasena, ParticipanteModel participante)
        {
            string mensaje = "" +
                "<h2>Nuevo usuario registrado</h2> " +
                "<p>Nombre: " + participante.nombre + " " + participante.apellido_1 + " " + participante.apellido_2 + "</p>" +
                "<p>Cédula: " + participante.idParticipante + "</p>" +
                "<p>Datos de inicio de sesión:</p>" +
                "<ul><li>Nombre de usuario: " + participante.idParticipante + "</li>" +
                "<li>Contraseña: " + contrasena + "</li></ul>";

            return mensaje;
        }
    }
}