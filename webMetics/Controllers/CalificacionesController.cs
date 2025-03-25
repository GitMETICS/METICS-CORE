using Microsoft.AspNetCore.Mvc;
using webMetics.Models;
using webMetics.Handlers;
using OfficeOpenXml;
using Microsoft.AspNetCore.Mvc.Rendering;
using NPOI.XSSF.UserModel;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using NPOI.XWPF.UserModel;
using NPOI.SS.UserModel;
using iText.Kernel.Font;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using NPOI.SS.Formula.Functions;
using System.Globalization;
using System.Text;
using MailKit.Search;
using System.Security.Claims;

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
        private readonly EmailService _emailService;

        public CalificacionesController(IWebHostEnvironment environment, IConfiguration configuration, EmailService emailService)
        {
            _environment = environment;
            _configuration = configuration;
            _emailService = emailService;

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
                TempData["successMessage"] = "Calificación actualizada.";
            }
            catch
            {
                TempData["errorMessage"] = "No se pudo actualizar la calificación.";
            }

            var refererUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(refererUrl))
            {
                return Redirect(refererUrl);
            }

            return RedirectToAction("VerCalificaciones", "Calificaciones", new { idGrupo = idGrupo });
        }

        [HttpPost]
        public async Task<IActionResult> SubirExcelCalificaciones(IFormFile file, int idGrupo)
        {
            if (file == null)
            {
                TempData["errorMessage"] = "Seleccione un archivo de Excel válido.";
            }

            try
            {
                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using var package = new ExcelPackage(stream);
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                    // Iterar sobre las filas con datos (ignorando la primera fila de encabezados)
                    for (int row = 5; row <= worksheet.Dimension.End.Row; row++)
                    {
                        string idParticipante = worksheet.Cells[row, GetColumnIndex(worksheet, "Correo Institucional")].Text;
                        int horasAprobadas = int.TryParse(worksheet.Cells[row, GetColumnIndex(worksheet, "Horas Aprobadas")].Text, out var horasAprobadasAux) ? horasAprobadasAux : 0;
                        int calificacion = int.TryParse(worksheet.Cells[row, GetColumnIndex(worksheet, "Calificación")].Text, out var calificacionAux) ? calificacionAux : 0;

                        InscripcionModel inscripcion = null;

                        try
                        {
                            inscripcion = accesoAInscripcion.ObtenerInscripcionDeGrupoInexistenteParticipante(grupo.nombre, grupo.numeroGrupo, idParticipante);

                            if (inscripcion != null)
                            {
                                inscripcion.horasAprobadas = horasAprobadas;
                                inscripcion.horasMatriculadas -= inscripcion.horasAprobadas;
                                inscripcion.horasMatriculadas = Math.Max(0, inscripcion.horasMatriculadas);
                                inscripcion.estado = accesoAInscripcion.CambiarEstadoDeInscripcion(inscripcion);
                                accesoAInscripcion.EditarInscripcion(inscripcion);

                                accesoAParticipante.ActualizarHorasAprobadasParticipante(idParticipante);
                                accesoAParticipante.ActualizarHorasMatriculadasParticipante(idParticipante);
                            }
                        }
                        catch
                        {
                            int horasMatriculadas = grupo.cantidadHoras;
                            horasMatriculadas -= horasAprobadas;
                            horasMatriculadas = Math.Max(0, grupo.cantidadHoras);

                            InscripcionModel nuevaInscripcion = new InscripcionModel
                            {
                                idParticipante = idParticipante,
                                idGrupo = idGrupo,
                                nombreGrupo = grupo.nombre,
                                numeroGrupo = grupo.numeroGrupo,
                                horasAprobadas = horasAprobadas,
                                horasMatriculadas = horasMatriculadas,
                                estado = "Inscrito"
                            };

                            accesoAInscripcion.InsertarInscripcion(nuevaInscripcion);
                            nuevaInscripcion.estado = accesoAInscripcion.CambiarEstadoDeInscripcion(nuevaInscripcion);
                            accesoAInscripcion.EditarInscripcion(nuevaInscripcion);

                            accesoAParticipante.ActualizarHorasAprobadasParticipante(idParticipante);
                            accesoAParticipante.ActualizarHorasMatriculadasParticipante(idParticipante);
                        }

                        bool calificaciones = accesoACalificaciones.IngresarNota(idGrupo, idParticipante, calificacion);
                    }

                    TempData["successMessage"] = "El archivo fue subido éxitosamente.";
                }
            }
            catch
            {
                TempData["errorMessage"] = "Error al cargar los datos.";
            }

            return RedirectToAction("ListaParticipantes", "Participante", new { idGrupo = idGrupo });
        }

        private int GetColumnIndex(ExcelWorksheet worksheet, string columnName)
        {
            // Normalize and remove accents from the input column name
            string normalizedColumnName = RemoveAccents(columnName.Trim().ToLower());

            // Iterate over the first row to find the column index
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                // Get the header, normalize it, and remove accents
                string header = RemoveAccents(worksheet.Cells[4, col].Text.Trim().ToLower());

                // Compare normalized header with the normalized column name
                if (string.Equals(header, normalizedColumnName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return col;
                }
            }
            throw new Exception($"Column '{columnName}' not found");
        }

        // Helper method to remove accents (diacritics)
        private string RemoveAccents(string text)
        {
            return string.Concat(text.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
                .Normalize(NormalizationForm.FormC);
        }

        public ActionResult DescargarPlantillaSubirCalificaciones()
        {
            // Creamos el archivo de Excel
            XSSFWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Plantilla_Lista_Calificaciones");

            NPOI.SS.UserModel.IRow row0 = sheet.CreateRow(0);
            NPOI.SS.UserModel.IRow row1 = sheet.CreateRow(1);
            NPOI.SS.UserModel.IRow row2 = sheet.CreateRow(2);
            NPOI.SS.UserModel.IRow row3 = sheet.CreateRow(3);

            NPOI.SS.UserModel.ICell cell01 = row0.CreateCell(0);
            cell01.SetCellValue("   ");

            NPOI.SS.UserModel.ICell cell11 = row1.CreateCell(0);
            cell11.SetCellValue("   ");

            NPOI.SS.UserModel.ICell cell21 = row2.CreateCell(0);
            cell21.SetCellValue("   ");

            NPOI.SS.UserModel.ICell cell31 = row3.CreateCell(0);
            cell31.SetCellValue("Nombre");

            NPOI.SS.UserModel.ICell cell32 = row3.CreateCell(1);
            cell32.SetCellValue("Primer Apellido");

            NPOI.SS.UserModel.ICell cell33 = row3.CreateCell(2);
            cell33.SetCellValue("Segundo Apellido");

            NPOI.SS.UserModel.ICell cell34 = row3.CreateCell(3);
            cell34.SetCellValue("Correo Institucional");

            NPOI.SS.UserModel.ICell cell35 = row3.CreateCell(4);
            cell35.SetCellValue("Horas Aprobadas");

            NPOI.SS.UserModel.ICell cell36 = row3.CreateCell(5);
            cell36.SetCellValue("Calificación");

            string fileName = "Plantilla_Lista_Calificaciones.xlsx";
            var stream = new MemoryStream();
            workbook.Write(stream);
            var file = stream.ToArray();

            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // Método optimizado para exportar la lista de los participantes de un grupo a un archivo PDF
        public ActionResult ExportarCalificacionesPDF(int idGrupo)
        {
            List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);

            var filePath = System.IO.Path.Combine(_environment.WebRootPath, "data", "Lista_de_Calificaciones.docx");
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



            iText.Layout.Element.Table table = new iText.Layout.Element.Table(5, true);
            table.AddHeaderCell("Nombre").SetFontSize(10);
            table.AddHeaderCell("Correo Institucional").SetFontSize(10);
            table.AddHeaderCell("Estado").SetFontSize(10);
            table.AddHeaderCell("Horas Aprobadas").SetFontSize(10);
            table.AddHeaderCell("Calificación").SetFontSize(10);

            foreach (var calificacion in calificaciones)
            {
                table.AddCell(calificacion.participante.nombre + " " + calificacion.participante.primerApellido + " " + calificacion.participante.segundoApellido);
                table.AddCell(calificacion.participante.idParticipante);
                table.AddCell(calificacion.estado);
                table.AddCell(calificacion.horasAprobadas.ToString());
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
            XWPFTable table = wordDoc.CreateTable(calificaciones.Count + 4, 5);
            table.SetColumnWidth(0, 3400);
            table.SetColumnWidth(1, 3500);
            table.SetColumnWidth(2, 1200);
            table.SetColumnWidth(3, 800);
            table.SetColumnWidth(4, 1300);

            var headerRow0 = table.Rows[0];
            headerRow0.GetCell(0).SetText("Nombre del/la Facilitador(a)");
            headerRow0.GetCell(1).SetText("Nombre del Módulo");

            var row0 = table.Rows[1];
            row0.GetCell(0).SetText(grupo.nombreAsesor);
            row0.GetCell(1).SetText(grupo.nombre);

            var headerRow = table.Rows[3];
            headerRow.GetCell(0).SetText("Correo Institucional");
            headerRow.GetCell(1).SetText("Nombre");
            headerRow.GetCell(2).SetText("Estado");
            headerRow.GetCell(3).SetText("Horas Aprobadas");
            headerRow.GetCell(4).SetText("Calificación");

            for (int i = 0; i < calificaciones.Count; i++)
            {
                var row = table.Rows[i + 4];
                row.GetCell(0).SetText(calificaciones[i].participante.idParticipante.ToString());
                row.GetCell(1).SetText(calificaciones[i].participante.nombre + " " + calificaciones[i].participante.primerApellido + " " + calificaciones[i].participante.segundoApellido);
                row.GetCell(2).SetText(calificaciones[i].estado.ToString());
                row.GetCell(3).SetText(calificaciones[i].horasAprobadas.ToString());
                row.GetCell(4).SetText(calificaciones[i].calificacion.ToString());
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
            cell32.SetCellValue("Nombre");

            NPOI.SS.UserModel.ICell cell33 = row3.CreateCell(2);
            cell33.SetCellValue("Estado");

            NPOI.SS.UserModel.ICell cell34 = row3.CreateCell(3);
            cell34.SetCellValue("Horas Aprobadas");

            NPOI.SS.UserModel.ICell cell35 = row3.CreateCell(4);
            cell35.SetCellValue("Calificación");

            int rowN = 4;
            foreach (var calificacion in calificaciones)
            {
                NPOI.SS.UserModel.IRow row = sheet.CreateRow(rowN);
                NPOI.SS.UserModel.ICell cell1 = row.CreateCell(0);
                cell1.SetCellValue(calificacion.participante.idParticipante);

                NPOI.SS.UserModel.ICell cell2 = row.CreateCell(1);
                cell2.SetCellValue(calificacion.participante.nombre + " " + calificacion.participante.primerApellido + " " + calificacion.participante.segundoApellido);

                NPOI.SS.UserModel.ICell cell3 = row.CreateCell(2);
                cell3.SetCellValue(calificacion.estado);

                NPOI.SS.UserModel.ICell cell4 = row.CreateCell(3);
                cell4.SetCellValue(calificacion.horasAprobadas);

                NPOI.SS.UserModel.ICell cell5 = row.CreateCell(4);
                cell5.SetCellValue(calificacion.calificacion);

                rowN++;
            }

            string fileName = "Lista_de_Calificaciones_" + grupo.nombre + ".xlsx";
            var stream = new MemoryStream();
            workbook.Write(stream);
            var file = stream.ToArray();

            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // Método para enviar calificación a un participante
        private async Task<IActionResult> EnviarCalificacion(string grupo, string mensaje, string correoParticipante)
        {
            string subject = "Informe de Calificación Final - Módulo " + grupo;

            await _emailService.SendEmailAsync(correoParticipante, subject, mensaje);
            return Ok();
        }

        // Método del constructor del mensaje del correo que será enviado al usuario con la calificación
        private string ConstructorDelMensajeCorreoEnviarCalificacion(GrupoModel grupo, CalificacionModel calificacion)
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
                    string mensaje = ConstructorDelMensajeCorreoEnviarCalificacion(grupo, calificacion);
                    EnviarCalificacion(grupo.nombre, mensaje, calificacion.participante.correo);
                }

                TempData["successMessage"] = "Calificaciones enviadas.";
            }
            catch
            {
                TempData["errorMessage"] = "Error al enviar las calificaciones.";
            }

            return RedirectToAction("ListaParticipantes", "Participante", new { idGrupo });
        }

        private int GetRole()
        {
            int role = 0;

            if (User.Identity.IsAuthenticated)
            {
                string roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
                if (roleClaim != null)
                {
                    role = Convert.ToInt32(roleClaim);
                }
            }
            return role;
        }

        private string GetId()
        {
            string id = "";
            if (User.Identity.IsAuthenticated)
            {
                id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            }
            return id;
        }
    }
}