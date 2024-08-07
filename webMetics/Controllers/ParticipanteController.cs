﻿using Microsoft.AspNetCore.Mvc;
using webMetics.Models;
using webMetics.Handlers;
using MimeKit;
using OfficeOpenXml;
using Microsoft.AspNetCore.Mvc.Rendering;
using NPOI.XSSF.UserModel;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using NPOI.XWPF.UserModel;

/* 
 * Controlador de la entidad Participante
 */
namespace webMetics.Controllers
{
    public class ParticipanteController : Controller
    {
        private protected UsuarioHandler accesoAUsuario;
        private protected ParticipanteHandler accesoAParticipante;
        private protected GrupoHandler accesoAGrupo;
        private protected AsesorHandler accesoAAsesor;
        private protected InscripcionHandler accesoAInscripcion;

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
            accesoAInscripcion = new InscripcionHandler(environment, configuration);
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

            if (participantes != null)
            {
                foreach (ParticipanteModel participante in participantes)
                {
                    string idParticipante = participante.idParticipante;
                    participante.gruposInscritos = accesoAGrupo.ObtenerListaGruposParticipante(idParticipante);
                }

                ViewBag.ListaParticipantes = participantes;
            }
            else
            {
                ViewBag.ListaParticipantes = null;
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

        public ActionResult InformacionPersonal(string id)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(id);
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesParticipante(id);
            List<GrupoModel> grupos = accesoAGrupo.ObtenerListaGruposParticipante(id);

            ViewBag.Participante = participante;
            ViewBag.Inscripciones = inscripciones;
            ViewBag.ListaGrupos = grupos;

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
                            numeroIdentificacion = "",
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
                            horasMatriculadas = 0, // int.TryParse(worksheet.Cells[row, GetColumnIndex(worksheet, "Horas matriculadas")].Text, out var horasMatriculadas) ? horasMatriculadas : 0,
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

        public ActionResult ExportarParticipantesPDF()
        {
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();

            var filePath = Path.Combine(_environment.WebRootPath, "data", "Lista_de_Participantes_Módulos.docx");
            PdfWriter writer = new PdfWriter(filePath);
            PdfDocument pdf = new PdfDocument(writer);
            iText.Layout.Document document = new iText.Layout.Document(pdf);

            Paragraph header3 = new Paragraph("Lista de Participantes")
                .SetFontSize(10);
            document.Add(header3);

            Table table = new Table(6, true);
            table.AddHeaderCell("Identificación").SetFontSize(8);
            table.AddHeaderCell("Nombre del participante").SetFontSize(8);
            table.AddHeaderCell("Condición").SetFontSize(8);
            table.AddHeaderCell("Unidad académica").SetFontSize(8);
            table.AddHeaderCell("Correo institucional").SetFontSize(8);
            table.AddHeaderCell("Teléfono").SetFontSize(8);

            foreach (var participante in participantes)
            {
                table.AddCell(participante.numeroIdentificacion);
                table.AddCell(participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido);
                table.AddCell(participante.condicion);
                table.AddCell(participante.unidadAcademica);
                table.AddCell(participante.idParticipante);
                table.AddCell(participante.telefono);
            }

            document.Add(table);

            document.Close();

            string fileName = "Lista_de_Participantes_Módulos.pdf";

            return File(System.IO.File.ReadAllBytes(filePath), "application/pdf", fileName);
        }

        public ActionResult ExportarParticipantesWord()
        {
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();

            var fileName = "Lista_de_Participantes_Módulos.docx";

            XWPFDocument wordDoc = new XWPFDocument();

            XWPFTable table = wordDoc.CreateTable(participantes.Count, 6);
            table.SetColumnWidth(0, 1500);
            table.SetColumnWidth(1, 2500);
            table.SetColumnWidth(2, 1000);
            table.SetColumnWidth(3, 2000);
            table.SetColumnWidth(4, 2500);
            table.SetColumnWidth(5, 1000);

            var headerRow = table.Rows[0];
            headerRow.GetCell(0).SetText("Identificación");
            headerRow.GetCell(1).SetText("Nombre del participante");
            headerRow.GetCell(2).SetText("Condición");
            headerRow.GetCell(3).SetText("Unidad académica");
            headerRow.GetCell(4).SetText("Correo institucional");
            headerRow.GetCell(5).SetText("Teléfono");
            

            for (int i = 1; i < participantes.Count; i++)
            {
                var row = table.Rows[i];
                row.GetCell(0).SetText(participantes[i].numeroIdentificacion.ToString());
                row.GetCell(1).SetText(participantes[i].nombre + " " + participantes[i].primerApellido + " " + participantes[i].segundoApellido);
                row.GetCell(2).SetText(participantes[i].condicion.ToString());
                row.GetCell(3).SetText(participantes[i].unidadAcademica);
                row.GetCell(4).SetText(participantes[i].idParticipante.ToString());
                row.GetCell(5).SetText(participantes[i].telefono.ToString());
            }

            var stream = new MemoryStream();
            wordDoc.Write(stream);
            var file = stream.ToArray();
            return File(file, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        public ActionResult ExportarParticipantesExcel()
        {
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();

            // Creamos el archivo de Excel
            XSSFWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Lista_de_Participantes_Módulos");

            NPOI.SS.UserModel.IRow row3 = sheet.CreateRow(3);
            NPOI.SS.UserModel.ICell cell31 = row3.CreateCell(0);
            cell31.SetCellValue("Identificación");

            NPOI.SS.UserModel.ICell cell32 = row3.CreateCell(1);
            cell32.SetCellValue("Nombre del participante");

            NPOI.SS.UserModel.ICell cell33 = row3.CreateCell(2);
            cell33.SetCellValue("Condición");

            NPOI.SS.UserModel.ICell cell34 = row3.CreateCell(3);
            cell34.SetCellValue("Unidad académica");

            NPOI.SS.UserModel.ICell cell35 = row3.CreateCell(4);
            cell35.SetCellValue("Correo institucional");

            NPOI.SS.UserModel.ICell cell36 = row3.CreateCell(5);
            cell36.SetCellValue("Teléfono");

            int rowN = 4;
            foreach (var participante in participantes)
            {
                NPOI.SS.UserModel.IRow row = sheet.CreateRow(rowN);
                NPOI.SS.UserModel.ICell cell1 = row.CreateCell(0);
                cell1.SetCellValue(participante.numeroIdentificacion);

                NPOI.SS.UserModel.ICell cell2 = row.CreateCell(1);
                cell2.SetCellValue(participante.nombre + ' ' + participante.primerApellido + ' ' + participante.segundoApellido);

                NPOI.SS.UserModel.ICell cell3 = row.CreateCell(2);
                cell3.SetCellValue(participante.condicion);

                NPOI.SS.UserModel.ICell cell4 = row.CreateCell(3);
                cell4.SetCellValue(participante.unidadAcademica);

                NPOI.SS.UserModel.ICell cell5 = row.CreateCell(4);
                cell5.SetCellValue(participante.idParticipante);

                NPOI.SS.UserModel.ICell cell6 = row.CreateCell(5);
                cell6.SetCellValue(participante.telefono);

                rowN++;
            }

            string fileName = "Lista_de_Participantes_Módulos.xlsx";
            var stream = new MemoryStream();
            workbook.Write(stream);
            var file = stream.ToArray();

            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public ActionResult VerDatosParticipante(string idParticipante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesParticipante(idParticipante);
            List<GrupoModel> grupos = accesoAGrupo.ObtenerListaGruposParticipante(idParticipante);

            ViewBag.Participante = participante;
            ViewBag.Inscripciones = inscripciones;
            ViewBag.ListaGrupos = grupos;

            if (GetRole() == 2)
            {
                string idAsesor = GetId();
                List<GrupoModel> gruposAsesor = accesoAGrupo.ObtenerListaGruposAsesor(idAsesor);

                List<GrupoModel> gruposEnComun = gruposAsesor.Join(
                    grupos,
                    grupoAsesor => grupoAsesor.idGrupo,
                    grupoParticipante => grupoParticipante.idGrupo,
                    (grupoAsesor, grupoParticipante) => new GrupoModel
                    {
                        idGrupo = grupoParticipante.idGrupo,
                        nombre = grupoParticipante.nombre,
                        cantidadHoras = grupoParticipante.cantidadHoras,
                        modalidad = grupoParticipante.modalidad
                    }
                ).ToList();

                ViewBag.ListaGrupos = gruposEnComun;
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
        public ActionResult SubirHorasAprobadas(int idGrupo, string idParticipante, int horasAprobadas)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);
            int nuevoTotalHorasAprobadas = participante.horasAprobadas + horasAprobadas;

            InscripcionModel inscripcion = accesoAInscripcion.ObtenerInscripcionParticipante(idGrupo, idParticipante);
            int nuevasHorasAprobadas = inscripcion.horasAprobadas + horasAprobadas;


            if (nuevasHorasAprobadas <= inscripcion.horasMatriculadas && nuevasHorasAprobadas >= 0)
            {
                inscripcion.horasAprobadas = nuevasHorasAprobadas;
                accesoAInscripcion.EditarInscripcion(inscripcion);

                if (nuevoTotalHorasAprobadas <= participante.horasMatriculadas && nuevoTotalHorasAprobadas >= 0)
                {
                    accesoAParticipante.ActualizarHorasAprobadasParticipante(idParticipante, nuevoTotalHorasAprobadas);
                }

                TempData["successMessage"] = "Las horas fueron aprobadas.";
            }
            else
            {
                TempData["errorMessage"] = "No se pudo aprobar las horas.";
            }

            return RedirectToAction("VerDatosParticipante", "Participante", new { idParticipante });
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
                }

                if (!accesoAParticipante.ExisteParticipante(participante.idParticipante))
                {
                    accesoAParticipante.CrearParticipante(participante);
                    TempData["successMessage"] = "Se agregó el nuevo participante.";
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
            if (!accesoAUsuario.ExisteUsuario(participante.idParticipante))
            {
                string contrasena = GenerateRandomPassword();

                accesoAUsuario.CrearUsuario(participante.idParticipante, "temporal"); // TODO: Esta es una contraseña temporal. Cambiar.
                // EnviarContrasenaPorCorreo(participante.idParticipante, contrasena); // TODO: Se envía la contraseña por correo para que luego el usuario pueda ingresar.
            }

            if (!accesoAParticipante.ExisteParticipante(participante.idParticipante))
            {
                accesoAParticipante.CrearParticipante(participante);
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
                    participante.idParticipante = participante.correo;
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
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                bool eliminadoExitosamente = accesoAParticipante.EliminarParticipante(idParticipante);

                if (eliminadoExitosamente)
                {
                    TempData["successMessage"] = "Se eliminó al participante.";
                }
                else
                {
                    TempData["errorMessage"] = "No se pudo eliminar al participante.";
                }
            }
            catch (Exception)
            {
                // Si ocurrió una excepción al intentar eliminar la inscripción, mostrar un mensaje y redirigir a la lista de participantes del grupo
                TempData["errorMessage"] = "Ocurrió un error y no se pudo eliminar al participante.";
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

        /* Método para enviar confirmación de registro al usuario*/
        private void EnviarContrasenaPorCorreo(string correo, string contrasena)
        {
            try
            {
                // Se utiliza la librería MimeKit para construir el mensaje
                // El mensaje incluye una versión en HTML y texto plano

                // Contenido base del mensaje en HTML y texto plano
                const string BASE_MESSAGE_HTML = ""; // Contenido HTML adicional puede ser agregado aquí
                const string BASE_MESSAGE_TEXT = "";
                const string BASE_SUBJECT = "Nuevo Usuario en el Sistema de Competencias Digitales para la Docencia-METICS"; // Asunto del correo

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
                    "<p>Se ha creado al usuario con identificación " + correo + " en el Sistema de Competencias Digitales para la Docencia-METICS.</p>" +
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