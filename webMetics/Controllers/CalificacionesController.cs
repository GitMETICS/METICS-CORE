using Microsoft.AspNetCore.Mvc;
using webMetics.Models;
using webMetics.Handlers;
using MimeKit;
using NPOI.XSSF.UserModel;
using NPOI.XWPF.UserModel;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using OfficeOpenXml;

namespace webMetics.Controllers
{
    public class CalificacionesController : Controller
    {
        private InscripcionHandler accesoAInscripcion;
        private GrupoHandler accesoAGrupo;
        private ParticipanteHandler accesoAParticipante;
        private CalificacionesHandler accesoACalificaciones;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public CalificacionesController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            accesoAInscripcion = new InscripcionHandler(environment, configuration);
            accesoAGrupo = new GrupoHandler(environment, configuration);
            accesoAParticipante = new ParticipanteHandler(environment, configuration);
            accesoACalificaciones = new CalificacionesHandler(environment, configuration);
        }

        public ActionResult VerCalificaciones(int idGrupo)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            ViewBag.ListaCalificaciones = calificaciones;
            ViewBag.IdGrupo = idGrupo;
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            ViewBag.Title = "Calificaciones";
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

        public ActionResult EditarCalificacion(int idGrupo, string idParticipante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            CalificacionModel calificacion = calificaciones.Find(calificacionModel => calificacionModel.participante.idParticipante == idParticipante);
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);

            ViewBag.Calificacion = calificacion;
            ViewBag.NombreGrupo = grupo.nombre;

            return View();
        }

        [HttpPost]
        public ActionResult SubirCalificacion(int idGrupo, string idParticipante, int calificacion)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                accesoACalificaciones.IngresarNota(idGrupo, idParticipante, calificacion);
            }
            catch
            {
                TempData["errorMessage"] = "Ocurrió un error y no se pudo actualizar la calificación.";
            }

            return RedirectToAction("VerCalificaciones", "Calificaciones", new { idGrupo = idGrupo });
        }

        [HttpPost]
        public async Task<IActionResult> CargarDesdeExcel(IFormFile file, int idGrupo)
        {
            if (file != null)
            {
                try
                {
                    using (var stream = new MemoryStream())
                    {
                        await file.CopyToAsync(stream);
                        stream.Position = 0;

                        using (var package = new ExcelPackage(stream))
                        {
                            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                            int rowBegin = 0;
                            int colId = 0;
                            int colCalificacion = 0;
                            bool seguir = true;
                            for (int row = 1; row <= worksheet.Dimension.End.Row && seguir; row++)
                            {
                                for (int col = 1; col <= worksheet.Dimension.End.Column && seguir; col++)
                                {
                                    if (worksheet.Cells[row, col].Text == "Correo Institucional") { rowBegin = row; colId = col; }
                                    if (worksheet.Cells[row, col].Text == "Calificación") { rowBegin = row; colCalificacion = col; seguir = false; }
                                }
                            }
                            for (int row = rowBegin + 1; row <= worksheet.Dimension.End.Row; row++)
                            {
                                string idParticipante = worksheet.Cells[row, colId].Text;
                                int calificacion = int.Parse(worksheet.Cells[row, colCalificacion].Text);

                                bool calificaciones = accesoACalificaciones.IngresarNota(idGrupo, idParticipante, calificacion);
                            }

                            TempData["successMessage"] = "El archivo fue subido éxitosamente.";
                        }
                    }
                }
                catch
                {
                    TempData["errorMessage"] = "Error al cargar los datos.";
                }
            }
            else
            {
                TempData["errorMessage"] = "Seleccione un archivo de Excel válido.";
            }

            return RedirectToAction("VerCalificaciones", "Calificaciones", new { idGrupo = idGrupo });
        }

        // Método optimizado para exportar la lista de los participantes de un grupo a un archivo PDF
        public ActionResult ExportarCalificacionesPDF(int idGrupo)
        {
            List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);

            var filePath = Path.Combine(_environment.WebRootPath, "data", "Lista_de_Calificaciones.docx");
            PdfWriter writer = new PdfWriter(filePath);
            PdfDocument pdf = new PdfDocument(writer);
            iText.Layout.Document document = new iText.Layout.Document(pdf);

            // Add content to the PDF
            Paragraph header1 = new Paragraph("Nombre del módulo: " + grupo.nombre)
                .SetFontSize(12);
            document.Add(header1);

            Paragraph header2 = new Paragraph("Nombre del/la facilitador(a) asociado(a): " + grupo.nombreAsesor)
                .SetFontSize(12);
            document.Add(header2);

            Paragraph header3 = new Paragraph("")
                .SetFontSize(12);
            document.Add(header3);



            Table table = new Table(3, true);
            table.AddHeaderCell("Nombre del participante").SetFontSize(10);
            table.AddHeaderCell("Correo Institucional").SetFontSize(10);          
            table.AddHeaderCell("Calificación").SetFontSize(10);

            foreach (var calificacion in calificaciones)
            {
                table.AddCell(calificacion.participante.nombre + " " + calificacion.participante.primerApellido + " " + calificacion.participante.segundoApellido);
                table.AddCell(calificacion.participante.idParticipante);
                table.AddCell(calificacion.calificacion.ToString());
            }

            document.Add(table);
            table.Complete();

            document.Close();

            string fileName = "Lista_de_Calificaciones_" + grupo.nombre + ".pdf";

            return File(System.IO.File.ReadAllBytes(filePath), "application/pdf", fileName);
        }

        public ActionResult ExportarCalificacionesWord(int idGrupo)
        {
            // Obtener la lista de participantes del grupo y la información del grupo
            List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);

            var fileName = "Lista_de_Calificaciones_" + grupo.nombre + ".docx";

            XWPFDocument wordDoc = new XWPFDocument();

            // Create a table
            XWPFTable table = wordDoc.CreateTable(calificaciones.Count + 4, 3);
            table.SetColumnWidth(0, 3750);
            table.SetColumnWidth(1, 3750);
            table.SetColumnWidth(2, 1500);

            var headerRow0 = table.Rows[0];
            headerRow0.GetCell(0).SetText("Nombre del/la Facilitador(a)");
            headerRow0.GetCell(1).SetText("Nombre del Módulo");

            var row0 = table.Rows[1];
            row0.GetCell(0).SetText(grupo.nombreAsesor);
            row0.GetCell(1).SetText(grupo.nombre);

            var headerRow = table.Rows[3];
            headerRow.GetCell(0).SetText("Correo Institucional");
            headerRow.GetCell(1).SetText("Nombre");
            headerRow.GetCell(2).SetText("Calificación");

            for (int i = 0; i < calificaciones.Count; i++)
            {
                var row = table.Rows[i + 4];
                row.GetCell(0).SetText(calificaciones[i].participante.idParticipante.ToString());
                row.GetCell(1).SetText(calificaciones[i].participante.nombre + " " + calificaciones[i].participante.primerApellido + " " + calificaciones[i].participante.segundoApellido);
                row.GetCell(2).SetText(calificaciones[i].calificacion.ToString());
            }

            var stream = new MemoryStream();
            wordDoc.Write(stream);
            var file = stream.ToArray();
            return File(file, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        public ActionResult ExportarCalificacionesExcel(int idGrupo)
        {
            // Obtener la lista de participantes del grupo y la información del grupo
            List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);

            // Creamos el archivo de Excel
            XSSFWorkbook workbook = new XSSFWorkbook();

            string sheetName = grupo.nombre.Length > 31 ? grupo.nombre.Substring(0, 31) : grupo.nombre;

            var sheet = workbook.CreateSheet(sheetName);

            NPOI.SS.UserModel.IRow row1 = sheet.CreateRow(0);
            NPOI.SS.UserModel.ICell cell11 = row1.CreateCell(0);
            cell11.SetCellValue("Nombre del módulo:");

            NPOI.SS.UserModel.ICell cell12 = row1.CreateCell(1);
            cell12.SetCellValue(grupo.nombre);

            NPOI.SS.UserModel.IRow row2 = sheet.CreateRow(1);
            NPOI.SS.UserModel.ICell cell21 = row2.CreateCell(0);
            cell21.SetCellValue("Nombre del/la facilitador(a) asociado(a):");

            NPOI.SS.UserModel.ICell cell22 = row2.CreateCell(1);
            cell22.SetCellValue(grupo.nombreAsesor);

            NPOI.SS.UserModel.IRow row3 = sheet.CreateRow(3);
            NPOI.SS.UserModel.ICell cell31 = row3.CreateCell(0);
            cell31.SetCellValue("Correo Institucional");

            NPOI.SS.UserModel.ICell cell32 = row3.CreateCell(1);
            cell32.SetCellValue("Nombre del participante");

            NPOI.SS.UserModel.ICell cell33 = row3.CreateCell(2);
            cell33.SetCellValue("Calificación");

            int rowN = 4;
            foreach (var calificacion in calificaciones)
            {
                NPOI.SS.UserModel.IRow row = sheet.CreateRow(rowN);
                NPOI.SS.UserModel.ICell cell1 = row.CreateCell(0);
                cell1.SetCellValue(calificacion.participante.idParticipante);

                NPOI.SS.UserModel.ICell cell2 = row.CreateCell(1);
                cell2.SetCellValue(calificacion.participante.nombre + " " + calificacion.participante.primerApellido + " " + calificacion.participante.segundoApellido);

                NPOI.SS.UserModel.ICell cell3 = row.CreateCell(2);
                cell3.SetCellValue(calificacion.calificacion);

                rowN++;
            }

            string fileName = "Lista_de_Calificaciones_" + grupo.nombre + ".xlsx";
            var stream = new MemoryStream();
            workbook.Write(stream);
            var file = stream.ToArray();

            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /* Método para enviar calificación y estado de un grupo */
        private void EnviarCalificaciones(string grupo, string mensaje, string correoParticipante)
        {
            // Configurar el mensaje de correo electrónico con el comprobante de inscripción y el archivo adjunto (si corresponde)
            // Se utiliza la librería MimeKit para construir el mensaje
            // El mensaje incluye una versión en HTML y texto plano

            // Contenido base del mensaje en HTML y texto plano
            const string BASE_MESSAGE_HTML = ""; // Contenido HTML adicional puede ser agregado aquí
            const string BASE_MESSAGE_TEXT = "";
            const string BASE_SUBJECT = ""; // Asunto del correo

            MimeMessage message = new MimeMessage();

            // Configurar el remitente y el destinatario
            MailboxAddress from = new MailboxAddress("COMPETENCIAS DIGITALES", "COMPETENCIAS.DIGITALES@ucr.ac.cr"); // TODO: Cambiar el correo del remitente al de UCR
            message.From.Add(from);
            MailboxAddress to = new MailboxAddress("Receiver", correoParticipante);
            message.To.Add(to);

            message.Subject = BASE_SUBJECT + "Informe de Calificación Final - Módulo " + grupo; // Asignar el asunto del correo

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
                client.Authenticate(from.Address, _configuration["EmailSettings:SMTPPassword"]); 

                client.Send(message);

                // Desconectar el cliente SMTP
                client.Disconnect(true);
            }
        }

        // Método del constructor del mensaje del correo que será enviado al usuario con la calificación
        private string ConstructorDelMensaje(GrupoModel grupo, CalificacionModel calificacion)
        {
            // Construir el contenido del mensaje que se enviará por correo electrónico al usuario
            // Este método toma información del grupo y el participante y crea un mensaje personalizado
            // con los detalles de la inscripción

            // Crear el mensaje con información relevante de la inscripción en formato HTML
            string mensaje = "<h2>Informe de Calificación Final - " + grupo.nombre + "</h2>" +
                "<table style='border:1px solid;width:75%;'><thead><tr>" +
                "<th style='border:1px solid;'>Nombre del participante</th>" +
                "<th style='border:1px solid;'>Cédula</th>" +
                "<th style='border:1px solid;'>Grupo</th>" +
                "<th style='border:1px solid;'>Calificación final</th>" +
                "</tr></thead><tbody><tr>" +
                "<td style='border:1px solid;'>" + calificacion.participante.nombre + " " + calificacion.participante.primerApellido + " " + calificacion.participante.segundoApellido + "</td>" +
                "<td style='border:1px solid;'>" + calificacion.participante.idParticipante + "</td>" +
                "<td style='border:1px solid;'>" + grupo.idGrupo+ " " + grupo.nombre + "</td>" +
                "<td style='border:1px solid;'>" + calificacion.calificacion + "</td>" +
                "</tr></tbody></table>" +
                "<h4>Información adicional del grupo:</h4><ul>" +
                "<li>Modalidad: " + grupo.modalidad + "</li> " +
                "<li>Cantidad de horas: " + grupo.cantidadHoras + "</li> " +
                "<li>Facilitador(a): " + grupo.nombreAsesor + "</li> " +
                "<li>Fecha de inicio: " + grupo.fechaInicioGrupo + "</li>" +
                "<li>Fecha de finalización: " + grupo.fechaFinalizacionGrupo + "</li></ul>";

            return mensaje;
        }

        public ActionResult EnviarCalificaciones(int idGrupo, List<string> participantesSeleccionados)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            List<CalificacionModel> calificaciones = new List<CalificacionModel>();

            foreach (string idParticipante in participantesSeleccionados)
            {
                CalificacionModel calificacion = accesoACalificaciones.ObtenerCalificacion(idGrupo, idParticipante);
                calificaciones.Add(calificacion);
            }

            try
            {
                foreach (CalificacionModel calificacion in calificaciones)
                {
                    string mensaje = ConstructorDelMensaje(grupo, calificacion);
                    EnviarCalificaciones(grupo.nombre, mensaje, calificacion.participante.correo);
                }

                TempData["successMessage"] = "Calificaciones enviadas.";
            }
            catch
            {
                TempData["errorMessage"] = "Error al enviar las calificaciones.";
            }

            return RedirectToAction("VerCalificaciones", new { idGrupo });
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