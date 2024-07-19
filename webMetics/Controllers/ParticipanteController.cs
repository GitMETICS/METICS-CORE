using Microsoft.AspNetCore.Mvc;
using webMetics.Models;
using webMetics.Handlers;
using MimeKit;
using NPOI.XSSF.UserModel;
using NPOI.XWPF.UserModel;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using NPOI.HPSF;
using OfficeOpenXml;
using Microsoft.AspNetCore.Mvc.Rendering;

/* 
 * Controlador de la entidad Participante
 */
namespace webMetics.Controllers
{
    public class ParticipanteController : Controller
    {
        private UsuarioHandler accesoAUsuario;
        private ParticipanteHandler accesoAParticipante;
        private GrupoHandler accesoAGrupo;
        private AsesorHandler accesoAAsesor;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public ParticipanteController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            accesoAUsuario = new UsuarioHandler(environment, configuration);
            accesoAParticipante = new ParticipanteHandler(environment, configuration);
            accesoAGrupo = new GrupoHandler(environment, configuration);
            accesoAAsesor = new AsesorHandler(environment, configuration);
        }

        /* Método para ver la lista de participantes de un grupo */
        public ActionResult ListaParticipantes(int idGrupo)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                ViewBag.IdGrupo = idGrupo;
                ViewBag.ListaParticipantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
                
                ViewBag.Title = "Lista de participantes";
                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
                ViewBag.NombreGrupo = grupo.nombre;
                return View();
            }
            catch
            {
                // Si ocurre un error, redirigir a la lista de grupos disponibles
                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }
        }

        /* Método para ver todos los participantes y sus horas matriculadas y aprobadas (administrador) */
        public ActionResult VerParticipantes()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();

            foreach (ParticipanteModel participante in participantes)
            {
                string idParticipante = participante.idParticipante;
                participante.gruposInscritos = accesoAGrupo.ObtenerListaGruposParticipante(idParticipante);
            }

            ViewBag.ListaParticipantes = participantes;

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

        public ActionResult InformacionPersonal(string id)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(id);

            ViewBag.Participante = participante;

            ViewBag.ListaGrupos = accesoAGrupo.ObtenerListaGruposParticipante(id);

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

        [HttpPost]
        public async Task<IActionResult> SubirArchivoExcel(IFormFile file)
        {
            if (file == null)
            {
                TempData["errorMessage"] = "Seleccione un archivo de Excel válido.";
                return RedirectToAction("VerParticipantes");
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using var package = new ExcelPackage(stream);
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                    var participantes = new List<ParticipanteModel>();

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var participante = new ParticipanteModel
                        {
                            idParticipante = worksheet.Cells[row, GetColumnIndex(worksheet, "Correo Institucional")].Text,
                            tipoIdentificacion = "",
                            correo = worksheet.Cells[row, GetColumnIndex(worksheet, "Correo Institucional")].Text,
                            nombre = worksheet.Cells[row, GetColumnIndex(worksheet, "Nombre")].Text,
                            primerApellido = worksheet.Cells[row, GetColumnIndex(worksheet, "Primer Apellido")].Text,
                            segundoApellido = worksheet.Cells[row, GetColumnIndex(worksheet, "Segundo Apellido")].Text,
                            condicion = "",
                            tipoParticipante = "",
                            area = "",
                            unidadAcademica = worksheet.Cells[row, GetColumnIndex(worksheet, "Unidad Académica")].Text,
                            departamento = "",
                            telefono = "",
                            horasMatriculadas = int.TryParse(worksheet.Cells[row, GetColumnIndex(worksheet, "Horas matriculadas")].Text, out var horasMatriculadas) ? horasMatriculadas : 0,
                            horasAprobadas = int.TryParse(worksheet.Cells[row, GetColumnIndex(worksheet, "Horas aprobadas")].Text, out var horasAprobadas) ? horasAprobadas : 0,
                            gruposInscritos = new List<GrupoModel>()
                        };

                        participantes.Add(participante);
                    }

                    foreach (var participante in participantes)
                    {
                        if (participante.idParticipante != "")
                        {
                            IngresarParticipante(participante);
                        }
                    }
                }

                TempData["successMessage"] = "El archivo fue subido éxitosamente.";
            }
            catch
            {
                TempData["errorMessage"] = "Error al cargar los datos.";
            }

            return RedirectToAction("VerParticipantes");
        }

        private int GetColumnIndex(ExcelWorksheet worksheet, string columnName)
        {
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                if (worksheet.Cells[1, col].Text == columnName)
                {
                    return col;
                }
            }
            return -1;
        }

        public ActionResult VerDatosParticipante(string idParticipante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);
            
            ViewBag.Participante = participante;
            ViewBag.ListaGrupos = accesoAGrupo.ObtenerListaGruposParticipante(idParticipante);

            if (HttpContext.Request.Cookies.ContainsKey("rolUsuario"))
            {
                int rolUsuario = Convert.ToInt32(Request.Cookies["rolUsuario"]);

                if (rolUsuario == 2)
                {
                    string idAsesor = Convert.ToString(Request.Cookies["idUsuario"]);
                    List<GrupoModel> gruposAsesor = accesoAAsesor.ObtenerListaGruposAsesor(idAsesor);
                    List<GrupoModel> gruposParticipante = ViewBag.ListaGrupos;

                    List<GrupoModel> gruposEnComun = gruposAsesor.Join(
                        gruposParticipante,
                        grupoAsesor => grupoAsesor.idGrupo,
                        grupoParticipante => grupoParticipante.idGrupo,
                        (grupoAsesor, grupoParticipante) => new GrupoModel
                        {
                            idGrupo = grupoParticipante.idGrupo,
                            nombre = grupoParticipante.nombre,
                            cantidadHoras = grupoParticipante.cantidadHoras
                        }
                    ).ToList();

                    ViewBag.ListaGrupos = gruposEnComun;
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

            return View();
        }

        [HttpPost]
        public ActionResult SubirHorasAprobadas(string idParticipante, int horasAprobadas)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);
            int nuevasHorasAprobadas = participante.horasAprobadas + horasAprobadas;

            if (nuevasHorasAprobadas <= participante.horasMatriculadas && nuevasHorasAprobadas >= 0)
            {
                accesoAParticipante.ActualizarHorasAprobadasParticipante(idParticipante, nuevasHorasAprobadas);

                TempData["successMessage"] = "Las horas fueron aprobadas.";
            }
            else
            {
                TempData["errorMessage"] = "No se pudo aprobar las horas.";
            }

            return RedirectToAction("VerDatosParticipante", "Participante", new { idParticipante });
        }

        // Vista para buscar y editar los datos de un participante
        public ActionResult ObtenerParticipante()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            return View(); // Muestra la vista para buscar al participante
        }

        public ActionResult FormularioParticipante()
        {
            ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
            return View("FormularioParticipante");
        }

        [HttpPost]
        public ActionResult FormularioParticipante(ParticipanteModel participante)
        {
            if (ModelState.IsValid)
            {
                // Aquí se define que el identificador del usuario es el correo.
                participante.idParticipante = participante.correo;

                if (!accesoAUsuario.ExisteUsuario(participante.idParticipante))
                {
                    string contrasena = GenerateRandomPassword();

                    accesoAUsuario.CrearUsuario(participante.idParticipante, contrasena);
                    EnviarCorreoContraseñaRegistro(participante.idParticipante, participante.correo, contrasena);
                }

                if (!accesoAParticipante.ExisteParticipante(participante.idParticipante))
                {
                    bool creado = accesoAParticipante.CrearParticipante(participante);
                    if (creado) 
                    {
                        TempData["successMessage"] = "Se agregó el nuevo participante.";
                    }
                    else
                    {
                        TempData["errorMessage"] = "No se pudo agregar el participante.";
                    }
                }
                else
                {
                    TempData["errorMessage"] = "Ya existe un participante con los mismos datos.";
                }

                return RedirectToAction("VerParticipantes");
            }
            else
            {
                ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
                return View("FormularioParticipante", participante);
            }
        }

        private void IngresarParticipante(ParticipanteModel participante)
        {
            if (!accesoAUsuario.ExisteUsuario(participante.idParticipante)) // En caso de que queramos hacer la autenticacion con el correo y no la identificacion, habria que cambiar esto
            {
                string contrasena = GenerateRandomPassword();

                accesoAUsuario.CrearUsuario(participante.idParticipante, contrasena);
                EnviarCorreoContraseñaRegistro(participante.idParticipante, participante.correo, contrasena);
            }

            if (!accesoAParticipante.ExisteParticipante(participante.idParticipante))
            {
                accesoAParticipante.CrearParticipante(participante);
            }
        }

        // Método para obtener los datos de un participante según el ID proporcionado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ObtenerParticipante(string idParticipante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            // Busca al participante en la lista de participantes por su ID
            ParticipanteModel getParticipante = accesoAParticipante.ObtenerListaParticipantes().Find(participanteModel => participanteModel.idParticipante == idParticipante);
            if (getParticipante == null)
            {
                // Si no se encuentra el participante, redirige de nuevo a la vista para buscar
                return RedirectToAction("ObtenerParticipante");
            }
            else
            {
                // Si se encuentra el participante, redirige a la vista para editar sus datos
                return RedirectToAction("EditarParticipante", new { idParticipante = idParticipante });
            }
        }

        // Método de la vista del formulario para editar a un participante
        public ActionResult EditarParticipante(string idParticipante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                ViewBag.IdParticipante = idParticipante;
                ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

                ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
                return View(participante);
            }
            catch
            {
                TempData["Message"] = "Ocurrió un error al obtener los datos solicitados.";
                return RedirectToAction("VerParticipantes");
            }
        }

        // Método de la vista del formulario con los datos necesarios del modelo para editar a un asesor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarParticipante(ParticipanteModel participante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                if (ModelState.IsValid)
                {
                    accesoAParticipante.EditarParticipante(participante);
                    TempData["successMessage"] = "Los datos fueron guardados.";
                    return RedirectToAction("ListaGruposDisponibles", "Grupo");
                }
                else
                {
                    ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
                    return View("EditarParticipante", participante);
                }
            }
            catch
            {
                TempData["Message"] = "Ocurrió un error al editar los datos.";
                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }
        }

        /* Método para que un administrador elimine un participante */
        [HttpPost]
        public ActionResult EliminarParticipante(string idParticipante)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                bool eliminadoExitosamente = accesoAParticipante.EliminarParticipante(idParticipante);

                if (eliminadoExitosamente)
                {
                    TempData["successMessage"] = "El participante fue eliminado.";
                }
                else
                {
                    TempData["errorMessage"] = "No se pudo eliminar al participante.";
                }
            }
            catch (Exception)
            {
                // Si ocurrió una excepción al intentar eliminar la inscripción, mostrar un mensaje y redirigir a la lista de participantes del grupo
                TempData["errorMessage"] = "Hubo un error y no se pudo eliminar al participante.";
            }

            return RedirectToAction("VerParticipantes", "Participante");
        }

        // Muestra un modal con info del participante
        [HttpPost]
        public ActionResult DisplayModal(string idParticipante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewBag.ListaGrupos = accesoAGrupo.ObtenerListaGruposParticipante(idParticipante);
            ViewBag.IdParticipante = idParticipante;

            return PartialView("_Modal");
        }

        // Métodos AJAX para obtener tipos de participantes, departamentos y secciones según el área seleccionada en el formulario
        [HttpGet]
        public JsonResult GetTiposParticipante(string tipoParticipante)
        {
            string[] tipoDeParticipantes = Enum.GetNames(typeof(TipoDeParticipantes));
            List<string> tipoDeParticipantesLista = new List<string>(tipoDeParticipantes);

            return Json(tipoDeParticipantesLista);
        }

        [HttpGet]
        public JsonResult GetDepartamentosByArea(string areaName)
        {
            List<string> departamentos = accesoAParticipante.GetDepartamentosByArea(areaName);
            return Json(departamentos);
        }

        [HttpGet]
        public JsonResult GetSeccionesByDepartamento(string areaName, string departamentoName)
        {
            List<string> secciones = accesoAParticipante.GetSeccionesByDepartamento(areaName, departamentoName);
            return Json(secciones);
        }

        [HttpGet]
        public static List<SelectListItem> GetSedes()
        {
            var sedes = new List<SelectListItem>();

            var group1 = new SelectListGroup() { Name = "Sede Central" };
            var group2 = new SelectListGroup() { Name = "Sede del Sur"};
            var group3 = new SelectListGroup() { Name = "Sede del Caribe" };
            var group4 = new SelectListGroup() { Name = "Sede de Guanacaste" };
            var group5 = new SelectListGroup() { Name = "Sede del Atlántico" };
            var group6 = new SelectListGroup() { Name = "Sede de Occidente" };
            var group7 = new SelectListGroup() { Name = "Sede Interuniversitaria de Alajuela" };

            sedes.Add(new SelectListItem() { Text = "Ciudad Universitaria Rodrigo Facio", Group = group1 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Golfito", Group = group2 });
            sedes.Add(new SelectListItem() { Text = "Recinto en Limón", Group = group3 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Siquirres", Group = group3 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Liberia", Group = group4 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Santa Cruz", Group = group4 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Turrialba", Group = group5 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Paraíso", Group = group5 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Guápiles", Group = group5 });
            sedes.Add(new SelectListItem() { Text = "Recinto de San Ramón", Group = group6 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Tacáres", Group = group6 });
            sedes.Add(new SelectListItem() { Text = "Recinto en Alajuela", Group = group7 });

            return sedes;
        }




        /* Método para enviar confirmación de registro al usuario*/
        private void EnviarCorreoContraseñaRegistro(string identificacion, string correo, string contrasena)
        {
            try
            {
                // Configurar el mensaje de correo electrónico con el comprobante de inscripción y el archivo adjunto (si corresponde)
                // Se utiliza la librería MimeKit para construir el mensaje
                // El mensaje incluye una versión en HTML y texto plano

                // Contenido base del mensaje en HTML y texto plano
                const string BASE_MESSAGE_HTML = ""; // Contenido HTML adicional puede ser agregado aquí
                const string BASE_MESSAGE_TEXT = "";
                const string BASE_SUBJECT = "Nuevo Usuario en Proyecto Módulos"; // Asunto del correo

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
                    "Se le ha creado un nuevo usuario con identificación " + identificacion + " en el proyecto Módulos. " +
                    "Su contraseña temporal es " + contrasena + "." ;
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