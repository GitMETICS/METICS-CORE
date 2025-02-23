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
        private readonly EmailService _emailService;

        public ParticipanteController(IWebHostEnvironment environment, IConfiguration configuration, EmailService emailService)
        {
            _environment = environment;
            _configuration = configuration;
            _emailService = emailService;

            accesoAUsuario = new UsuarioHandler(environment, configuration);
            accesoAParticipante = new ParticipanteHandler(environment, configuration);
            accesoAGrupo = new GrupoHandler(environment, configuration);
            accesoAAsesor = new AsesorHandler(environment, configuration);
            accesoAInscripcion = new InscripcionHandler(environment, configuration);
        }

        public ActionResult ListaParticipantes(int idGrupo)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);

                ViewBag.IdGrupo = idGrupo;
                ViewBag.NombreGrupo = grupo.nombre;
                ViewBag.NumeroGrupo = grupo.numeroGrupo;

                ViewBag.ListaParticipantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
                ViewBag.Inscripciones = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);
                

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

        public IActionResult BuscarParticipantes(string searchTerm)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            // Obtener la lista de participantes
            var participantes = accesoAParticipante.ObtenerListaParticipantes();

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.horasMatriculadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.horasAprobadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            ViewBag.ListaParticipantes = participantes;

            return View("VerParticipantes");
        }

        [HttpPost]
        public async Task<IActionResult> SubirArchivoExcelParticipantes(IFormFile file)
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
                            horasAprobadas = int.TryParse(worksheet.Cells[row, GetColumnIndex(worksheet, "Horas Aprobadas")].Text, out var horasAprobadas) ? horasAprobadas : 0,
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

        

        public IActionResult NotificarLimiteHoras(string idParticipante)
        {
            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

            if (participante != null && participante.horasAprobadas >= 30)
            {
                try
                {
                    EnviarCorreoNotificacion(idParticipante);
                    bool exito = accesoAParticipante.ActualizarCorreoNotificacionEnviadoParticipante(idParticipante);

                    if (exito)
                    {
                        TempData["successMessage"] = "Se envió el correo de notificación.";
                    }
                    else
                    {
                        TempData["errorMessage"] = "Ocurrió un error al enviar el correo de notificación.";
                    }
                    
                }
                catch 
                {
                    TempData["errorMessage"] = "Ocurrió un error al enviar el correo de notificación.";
                }
            }

            bool enviado = accesoAParticipante.ObtenerCorreoNotificacionEnviadoParticipante(idParticipante);

            return RedirectToAction("VerParticipantes");
        }

        private async Task<IActionResult> EnviarCorreoNotificacion(string idParticipante)
        {
            string subject = "Notificación Límite de Horas Aprobadas - SISTEMA DE INSCRIPCIONES METICS";
            string message = $"El usuario con correo institucional {idParticipante} ha superado las 30 horas aprobadas en el SISTEMA DE COMPETENCIAS DIGITALES PARA LA DOCENCIA - METICS.";
            string receiver = accesoAInscripcion.ObtenerCorreoLimiteHoras() ?? "soporte.metics@ucr.ac.cr";

            await _emailService.SendEmailAsync(receiver, subject, message);
            return Ok();
        }

        public ActionResult ExportarParticipantesPDF(string? searchTerm)
        {
            // Obtener la lista de participantes e inscripciones
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones(); // Relación de horas aprobadas y notas

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.horasMatriculadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.horasAprobadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Crear el archivo PDF
            var filePath = System.IO.Path.Combine(_environment.WebRootPath, "data", "Lista_de_Participantes_Módulos.pdf");
            PdfWriter writer = new PdfWriter(filePath);
            PdfDocument pdf = new PdfDocument(writer);

            // Definir tamaño de página más grande (A2 o A3)
            PageSize pageSize = PageSize.A2;  // Puedes elegir PageSize.A3 para un tamaño más pequeño
            iText.Layout.Document document = new iText.Layout.Document(pdf, pageSize);

            // Establecer fuente en negrita para encabezado
            PdfFont boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
            PdfFont regularFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

            // Crear encabezado del documento
            Paragraph header = new Paragraph("Lista de Participantes y Módulos")
                .SetFont(boldFont)
                .SetFontSize(14)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                .SetMarginBottom(20);
            document.Add(header);

            // Crear la tabla (9 columnas)
            iText.Layout.Element.Table table = new iText.Layout.Element.Table(new float[] { 2, 3, 2, 2, 3, 2, 3, 2, 2 });
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Agregar encabezados de la tabla con estilo
            string[] headers = { "Identificación", "Nombre del participante", "Correo institucional", "Condición", "Unidad académica", "Teléfono", "Módulo", "Horas aprobadas", "Calificación del módulo" };
            foreach (var headerText in headers)
            {
                table.AddHeaderCell(new Cell().Add(new Paragraph(headerText).SetFont(boldFont).SetFontSize(10))
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY));
            }

            // Rellenar la tabla con los datos
            foreach (var participante in participantes)
            {
                // Obtener las inscripciones del participante
                var inscripcionesParticipante = inscripciones.Where(i => i.idParticipante == participante.idParticipante).ToList();

                if (inscripcionesParticipante.Any())
                {
                    foreach (var inscripcion in inscripcionesParticipante)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(participante.numeroIdentificacion).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(participante.idParticipante).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(participante.condicion).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(participante.unidadAcademica).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(participante.telefono).SetFont(regularFont).SetFontSize(9)));

                        // Datos del módulo
                        table.AddCell(new Cell().Add(new Paragraph(inscripcion.nombreGrupo).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(inscripcion.horasAprobadas.ToString()).SetFont(regularFont).SetFontSize(9)));
                        table.AddCell(new Cell().Add(new Paragraph(inscripcion.calificacion.ToString()).SetFont(regularFont).SetFontSize(9)));
                    }
                }
                else
                {
                    // Si no tiene inscripciones, rellenar con "N/A"
                    table.AddCell(new Cell().Add(new Paragraph(participante.numeroIdentificacion).SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph(participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido).SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph(participante.idParticipante).SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph(participante.condicion).SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph(participante.unidadAcademica).SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph(participante.telefono).SetFont(regularFont).SetFontSize(9)));

                    table.AddCell(new Cell().Add(new Paragraph("N/A").SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph("N/A").SetFont(regularFont).SetFontSize(9)));
                    table.AddCell(new Cell().Add(new Paragraph("N/A").SetFont(regularFont).SetFontSize(9)));
                }
            }

            // Añadir la tabla al documento
            document.Add(table);

            // Cerrar el documento
            document.Close();

            // Devolver el archivo PDF 
            string fileName = "Lista_de_Participantes_Módulos.pdf";
            return File(System.IO.File.ReadAllBytes(filePath), "application/pdf", fileName);
        }

        public ActionResult ExportarParticipantesWord(string? searchTerm)
        {
            // Obtener la lista de participantes e inscripciones
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones(); // Relación de horas aprobadas y notas

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.horasMatriculadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.horasAprobadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            var fileName = "Lista_de_Participantes_Módulos.docx";

            // Crear el documento Word
            XWPFDocument wordDoc = new XWPFDocument();

            // Crear un título
            XWPFParagraph titleParagraph = wordDoc.CreateParagraph();
            titleParagraph.Alignment = ParagraphAlignment.CENTER;
            XWPFRun titleRun = titleParagraph.CreateRun();
            titleRun.SetText("Lista de Participantes y Módulos");
            titleRun.IsBold = true;
            titleRun.FontSize = 16;
            titleRun.AddBreak(); // Salto de línea después del título

            // Crear tabla con el número de participantes y columnas para los módulos
            XWPFTable table = wordDoc.CreateTable(participantes.Count + 1, 9); // +1 para la fila de encabezado

            // Ajustar los anchos de columna (simulando margen)
            table.SetColumnWidth(0, 750);  // Ajustar más ancho para la columna de identificación
            table.SetColumnWidth(1, 1000);  // Ancho de columna de nombre
            table.SetColumnWidth(2, 1500);  // Columna de correo
            table.SetColumnWidth(3, 750);  // Columna de condición
            table.SetColumnWidth(4, 1500);  // Columna de unidad académica
            table.SetColumnWidth(5, 750);  // Columna de teléfono
            table.SetColumnWidth(6, 1500);  // Nombre del módulo
            table.SetColumnWidth(7, 750);  // Horas aprobadas
            table.SetColumnWidth(8, 750);  // Nota del módulo

            // Estilo para el encabezado
            var headerRow = table.GetRow(0);
            headerRow.GetCell(0).SetText("Identificación");
            headerRow.GetCell(1).SetText("Nombre del participante");
            headerRow.GetCell(2).SetText("Correo institucional");
            headerRow.GetCell(3).SetText("Condición");
            headerRow.GetCell(4).SetText("Unidad académica");
            headerRow.GetCell(5).SetText("Teléfono");
            headerRow.GetCell(6).SetText("Módulo");
            headerRow.GetCell(7).SetText("Horas aprobadas");
            headerRow.GetCell(8).SetText("Calificación del módulo");

            // Rellenar la tabla con los datos de los participantes e inscripciones
            int rowIndex = 1; // Comenzar después del encabezado

            foreach (var participante in participantes)
            {
                // Obtener las inscripciones del participante
                var inscripcionesParticipante = inscripciones.Where(i => i.idParticipante == participante.idParticipante).ToList();

                if (inscripcionesParticipante.Any())
                {
                    foreach (var inscripcion in inscripcionesParticipante)
                    {
                        var row = table.CreateRow(); // Crear una nueva fila para cada inscripción

                        row.GetCell(0).SetText(participante.numeroIdentificacion.ToString());
                        row.GetCell(1).SetText(participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido);
                        row.GetCell(2).SetText(participante.idParticipante.ToString());
                        row.GetCell(3).SetText(participante.condicion.ToString());
                        row.GetCell(4).SetText(participante.unidadAcademica);
                        row.GetCell(5).SetText(participante.telefono.ToString());

                        // Información del módulo
                        row.GetCell(6).SetText(inscripcion.nombreGrupo);
                        row.GetCell(7).SetText(inscripcion.horasAprobadas.ToString());
                        row.GetCell(8).SetText(inscripcion.calificacion.ToString());

                        rowIndex++;
                    }
                }
                else
                {
                    // Si el participante no tiene inscripciones, crear una fila con "N/A"
                    var row = table.CreateRow();

                    row.GetCell(0).SetText(participante.numeroIdentificacion.ToString());
                    row.GetCell(1).SetText(participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido);
                    row.GetCell(2).SetText(participante.idParticipante.ToString());
                    row.GetCell(3).SetText(participante.condicion.ToString());
                    row.GetCell(4).SetText(participante.unidadAcademica);
                    row.GetCell(5).SetText(participante.telefono.ToString());

                    row.GetCell(6).SetText("N/A");
                    row.GetCell(7).SetText("N/A");
                    row.GetCell(8).SetText("N/A");

                    rowIndex++;
                }
            }

            // Guardar el archivo y devolver el documento Word como respuesta
            var stream = new MemoryStream();
            wordDoc.Write(stream);
            var file = stream.ToArray();
            return File(file, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        public ActionResult ExportarParticipantesExcel(string? searchTerm)
        {
            // Obtener la lista de participantes e inscripciones
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones(); // Relación de horas aprobadas y notas

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.horasMatriculadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.horasAprobadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Creamos el archivo de Excel
            XSSFWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Lista_de_Participantes_Módulos");

            // Crear estilos para el encabezado
            ICellStyle headerStyle = workbook.CreateCellStyle();
            headerStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            headerStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

            // Añadir borde y fondo al encabezado
            headerStyle.BorderBottom = BorderStyle.Thin;
            headerStyle.BorderTop = BorderStyle.Thin;
            headerStyle.BorderLeft = BorderStyle.Thin;
            headerStyle.BorderRight = BorderStyle.Thin;

            // Crear fuente en negrita para el encabezado
            IFont font = workbook.CreateFont();
            headerStyle.SetFont(font);

            // Crear estilos para las celdas del cuerpo
            ICellStyle bodyStyle = workbook.CreateCellStyle();
            bodyStyle.BorderBottom = BorderStyle.Thin;
            bodyStyle.BorderTop = BorderStyle.Thin;
            bodyStyle.BorderLeft = BorderStyle.Thin;
            bodyStyle.BorderRight = BorderStyle.Thin;

            // Crear el encabezado de la tabla
            IRow rowHeaders = sheet.CreateRow(3);
            string[] headers = { "Identificación", "Nombre del participante", "Correo institucional", "Condición", "Unidad académica", "Teléfono", "Módulo", "Horas aprobadas", "Calificación del módulo" };

            for (int i = 0; i < headers.Length; i++)
            {
                NPOI.SS.UserModel.ICell cell = rowHeaders.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;  // Aplicar estilo de encabezado
            }

            int rowN = 4;
            foreach (var participante in participantes)
            {
                // Obtener las inscripciones del participante
                var inscripcionesParticipante = inscripciones.Where(i => i.idParticipante == participante.idParticipante).ToList();

                // Si el participante tiene inscripciones, crear una fila por cada módulo inscrito
                if (inscripcionesParticipante.Any())
                {
                    foreach (var inscripcion in inscripcionesParticipante)
                    {
                        IRow row = sheet.CreateRow(rowN);

                        // Completar los datos del participante en cada fila
                        row.CreateCell(0).SetCellValue(participante.numeroIdentificacion);
                        row.CreateCell(1).SetCellValue($"{participante.nombre} {participante.primerApellido} {participante.segundoApellido}");
                        row.CreateCell(2).SetCellValue(participante.idParticipante);
                        row.CreateCell(3).SetCellValue(participante.condicion);
                        row.CreateCell(4).SetCellValue(participante.unidadAcademica);
                        row.CreateCell(5).SetCellValue(participante.telefono);

                        // Completar los datos del módulo
                        row.CreateCell(6).SetCellValue(inscripcion.nombreGrupo); // Nombre del módulo
                        row.CreateCell(7).SetCellValue(inscripcion.horasAprobadas); // Horas aprobadas
                        row.CreateCell(8).SetCellValue(inscripcion.calificacion); // Nota del módulo

                        // Aplicar estilo al cuerpo
                        for (int i = 0; i < 9; i++)
                        {
                            row.GetCell(i).CellStyle = bodyStyle;
                        }

                        rowN++; // Incrementar para la siguiente fila
                    }
                }
                else
                {
                    // Si no tiene inscripciones, dejar una sola fila para el participante con "N/A"
                    IRow row = sheet.CreateRow(rowN);

                    row.CreateCell(0).SetCellValue(participante.numeroIdentificacion);
                    row.CreateCell(1).SetCellValue($"{participante.nombre} {participante.primerApellido} {participante.segundoApellido}");
                    row.CreateCell(2).SetCellValue(participante.idParticipante);
                    row.CreateCell(3).SetCellValue(participante.condicion);
                    row.CreateCell(4).SetCellValue(participante.unidadAcademica);
                    row.CreateCell(5).SetCellValue(participante.telefono);
                    row.CreateCell(6).SetCellValue("N/A");
                    row.CreateCell(7).SetCellValue("N/A");
                    row.CreateCell(8).SetCellValue("N/A");

                    // Aplicar estilo al cuerpo
                    for (int i = 0; i < 9; i++)
                    {
                        row.GetCell(i).CellStyle = bodyStyle;
                    }

                    rowN++; // Incrementar para la siguiente fila
                }
            }

            // Ajustar automáticamente el ancho de las columnas
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            // Crear el archivo de Excel y devolverlo como respuesta
            string fileName = "Lista_de_Participantes_Módulos.xlsx";
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                var file = stream.ToArray();
                return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        public ActionResult ExportarParticipantesExcel2(string? searchTerm)
        {
            // Obtener la lista de participantes e inscripciones
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones(); // Relación de horas aprobadas y notas

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.horasMatriculadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.horasAprobadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Creamos el archivo de Excel
            XSSFWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Lista_de_Participantes_Módulos");

            // Crear estilos para el encabezado
            ICellStyle headerStyle = workbook.CreateCellStyle();
            headerStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            headerStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

            // Añadir borde y fondo al encabezado
            headerStyle.BorderBottom = BorderStyle.Thin;
            headerStyle.BorderTop = BorderStyle.Thin;
            headerStyle.BorderLeft = BorderStyle.Thin;
            headerStyle.BorderRight = BorderStyle.Thin;

            // Crear fuente en negrita para el encabezado
            IFont font = workbook.CreateFont();
            headerStyle.SetFont(font);

            // Crear estilos para las celdas del cuerpo
            ICellStyle bodyStyle = workbook.CreateCellStyle();
            bodyStyle.BorderBottom = BorderStyle.Thin;
            bodyStyle.BorderTop = BorderStyle.Thin;
            bodyStyle.BorderLeft = BorderStyle.Thin;
            bodyStyle.BorderRight = BorderStyle.Thin;

            // Crear el encabezado de la tabla
            IRow rowHeaders = sheet.CreateRow(3);
            string[] headers = { "Unidad Académica", "Nombre", "Primer Apellido", "Segundo Apellido", "Correo Institucional", "Total Horas Inscritas", "Total Horas Aprobadas" };

            for (int i = 0; i < headers.Length; i++)
            {
                NPOI.SS.UserModel.ICell cell = rowHeaders.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;  // Aplicar estilo de encabezado
            }

            int rowN = 4;
            foreach (var participante in participantes)
            {
                IRow row = sheet.CreateRow(rowN);

                // Completar los datos del participante en cada fila
                row.CreateCell(0).SetCellValue(participante.unidadAcademica);
                row.CreateCell(1).SetCellValue(participante.nombre);
                row.CreateCell(2).SetCellValue(participante.primerApellido);
                row.CreateCell(3).SetCellValue(participante.segundoApellido);
                row.CreateCell(4).SetCellValue(participante.idParticipante);
                row.CreateCell(5).SetCellValue(participante.horasMatriculadas);
                row.CreateCell(6).SetCellValue(participante.horasAprobadas);

                // Aplicar estilo al cuerpo
                for (int i = 0; i < 7; i++)
                {
                    row.GetCell(i).CellStyle = bodyStyle;
                }

                rowN++; // Incrementar para la siguiente fila


                // Ajustar automáticamente el ancho de las columnas
                for (int i = 0; i < headers.Length; i++)
                {
                    sheet.AutoSizeColumn(i);
                }
            }

            // Crear el archivo de Excel y devolverlo como respuesta
            string fileName = "Lista_de_Participantes_Módulos.xlsx";
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                var file = stream.ToArray();
                return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }


        public ActionResult DescargarPlantillaSubirParticipantes()
        {
            // Creamos el archivo de Excel
            XSSFWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Plantilla_Lista_Participantes");

            NPOI.SS.UserModel.IRow row = sheet.CreateRow(0);
            NPOI.SS.UserModel.ICell cell31 = row.CreateCell(0);
            cell31.SetCellValue("Unidad Académica");

            NPOI.SS.UserModel.ICell cell32 = row.CreateCell(1);
            cell32.SetCellValue("Nombre");

            NPOI.SS.UserModel.ICell cell33 = row.CreateCell(2);
            cell33.SetCellValue("Primer Apellido");

            NPOI.SS.UserModel.ICell cell34 = row.CreateCell(3);
            cell34.SetCellValue("Segundo Apellido");

            NPOI.SS.UserModel.ICell cell35 = row.CreateCell(4);
            cell35.SetCellValue("Correo Institucional");

            NPOI.SS.UserModel.ICell cell36 = row.CreateCell(5);
            cell36.SetCellValue("Horas Aprobadas");

            string fileName = "Plantilla_Lista_Participantes.xlsx";
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

            ViewBag.Participante = participante;
            ViewBag.Inscripciones = inscripciones;
            ViewBag.Medallas = accesoAParticipante.ObtenerMedallas(idParticipante);

            if (GetRole() == 2)
            {
                string idAsesor = GetId();
                List<GrupoModel> gruposAsesor = accesoAGrupo.ObtenerListaGruposAsesor(idAsesor);

                List<InscripcionModel> inscripcionesEnComun = inscripciones.Where(i => gruposAsesor.Any(g => g.idGrupo == i.idGrupo)).ToList();

                ViewBag.Inscripciones = inscripcionesEnComun;
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
        public async Task<IActionResult> SubirMedalla(string idParticipante, IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "medallas", imageFile.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Guardar la ruta de la medalla en la base de datos
                accesoAParticipante.AgregarMedalla(idParticipante, imageFile.FileName);

                TempData["successMessage"] = "Se subió la medalla.";
            }
            else
            {
                TempData["errorMessage"] = "Elija una imagen válida.";
            }

            return RedirectToAction("VerDatosParticipante", new { idParticipante = idParticipante } );
        }

        [HttpPost]
        public ActionResult SubirHorasAprobadas(int? idGrupo, string nombreGrupo, int numeroGrupo, string idParticipante, int horasAprobadas)
        {
            if (!idGrupo.HasValue)
            {
                TempData["errorMessage"] = "Debe seleccionar un módulo antes de aprobar horas.";
                return RedirectToAction("VerDatosParticipante", "Participante", new { idParticipante });
            }
            else
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo.Value);
                InscripcionModel inscripcion;

                if (grupo != null)
                {
                    inscripcion = accesoAInscripcion.ObtenerInscripcionParticipante(grupo.idGrupo, idParticipante);
                }
                else
                {
                    inscripcion = accesoAInscripcion.ObtenerInscripcionDeGrupoInexistenteParticipante(nombreGrupo, numeroGrupo, idParticipante);
                }

                int nuevasHorasAprobadas = horasAprobadas; // inscripcion.horasAprobadas + horasAprobadas;

                if (nuevasHorasAprobadas <= inscripcion.horasMatriculadas)
                {
                    inscripcion.horasAprobadas = nuevasHorasAprobadas;
                    inscripcion.estado = accesoAInscripcion.CambiarEstadoDeInscripcion(inscripcion);
                    accesoAInscripcion.EditarInscripcion(inscripcion);

                    accesoAParticipante.ActualizarHorasMatriculadasParticipante(idParticipante);
                    accesoAParticipante.ActualizarHorasAprobadasParticipante(idParticipante);

                    TempData["successMessage"] = "Las horas fueron aprobadas.";
                }
                else
                {
                    TempData["errorMessage"] = "No se pudo aprobar las horas.";
                }

                return RedirectToAction("VerDatosParticipante", "Participante", new { idParticipante });
            }
        }

        public ActionResult FormularioParticipante()
        {
            ViewBag.Id = GetId();
            ViewBag.Role = GetRole();

            ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
            return View("FormularioParticipante");
        }

        [HttpPost]
        public ActionResult FormularioParticipante(ParticipanteModel participante)
        {
            ViewBag.Id = GetId();
            ViewBag.Role = GetRole();

            if (ModelState.IsValid)
            {
                // Aquí se define que el identificador del usuario es el correo.
                participante.idParticipante = participante.correo;

                try
                {
                    IngresarParticipante(participante);

                    TempData["successMessage"] = "Participante agregado.";
                }
                catch
                {
                    TempData["errorMessage"] = "Error al agregar al participante.";
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

                accesoAUsuario.CrearUsuario(participante.idParticipante, contrasena);
                EnviarContrasenaPorCorreo(participante.idParticipante, contrasena);
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
                ViewData["jsonDataDepartamentos"] = accesoAParticipante.GetDepartamentosByArea(participante.area);
                ViewData["jsonDataUnidadesAcademicas"] = accesoAParticipante.GetSeccionesByDepartamento(participante.area,participante.departamento);

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

                    AsesorModel asesorAsociado = accesoAAsesor.ObtenerAsesor(participante.idParticipante);
                    if (asesorAsociado != null)
                    {
                        asesorAsociado.nombre = participante.nombre;
                        asesorAsociado.primerApellido = participante.primerApellido;
                        asesorAsociado.segundoApellido = participante.segundoApellido;
                        asesorAsociado.correo = participante.correo;
                        asesorAsociado.tipoIdentificacion = participante.tipoIdentificacion;
                        asesorAsociado.numeroIdentificacion = participante.numeroIdentificacion;
                        asesorAsociado.telefono = participante.telefono;

                        accesoAAsesor.EditarAsesor(asesorAsociado);
                    }

                    TempData["successMessage"] = "Los datos fueron guardados.";
                    return RedirectToAction("VerParticipantes", "Participante");
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
                return RedirectToAction("VerParticipantes", "Participante");
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
                accesoAParticipante.EliminarParticipante(idParticipante);
                accesoAUsuario.EliminarUsuario(idParticipante);

                TempData["successMessage"] = "Participante eliminado.";
            }
            catch
            {
                TempData["errorMessage"] = "Error al eliminar al participante.";
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
        public static List<SelectListItem> GetAreas()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = "Área de Artes y Letras" },
                new SelectListItem { Text = "Área de Ciencias Agroalimentarias" },
                new SelectListItem { Text = "Área de Ciencias Básicas" },
                new SelectListItem { Text = "Área de Ciencias Sociales" },
                new SelectListItem { Text = "Área de Ingeniería" },
                new SelectListItem { Text = "Área de Salud" },
                new SelectListItem { Text = "Sistema de Educación General" },
                new SelectListItem { Text = "Sistema de Estudios de Posgrado" },
                new SelectListItem { Text = "Sede Regional" },
                new SelectListItem { Text = "Otros" }
            };
        }

        [HttpGet]
        public JsonResult GetDepartamentoJSON(string areaName)
        {
            return Json(GetDepartamento(areaName));
        }

        [HttpGet]
        public static List<SelectListItem> GetDepartamento(string value)
        {
            var departamentos = new List<SelectListItem>();

            switch (value)
            {
                case "Área de Artes y Letras":
                    departamentos.Add(new SelectListItem() { Text = "Facultad de Artes" });
                    departamentos.Add(new SelectListItem() { Text = "Facultad de Letras" });
                    break;

                case "Área de Ciencias Agroalimentarias":
                    departamentos.Add(new SelectListItem() { Text = "Facultad de Ciencias Agroalimentarias" });
                    break;

                case "Área de Ciencias Básicas":
                    departamentos.Add(new SelectListItem() { Text = "Facultad de Ciencias" });
                    break;
                case "Área de Ciencias Sociales":
                    departamentos.Add(new SelectListItem() { Text = "Facultad de Ciencias Económicas" });
                    departamentos.Add(new SelectListItem() { Text = "Facultad de Ciencias Sociales" });
                    departamentos.Add(new SelectListItem() { Text = "Facultad de Derecho" });
                    departamentos.Add(new SelectListItem() { Text = "Facultad de Educación" });
                    break;
                case "Área de Ingeniería":
                    departamentos.Add(new SelectListItem() { Text = "Facultad de Ingeniería" });
                    break;
                case "Área de Salud":
                    departamentos.Add(new SelectListItem() { Text = "Facultad de Farmacia" });
                    departamentos.Add(new SelectListItem() { Text = "Facultad de Medicina" });
                    departamentos.Add(new SelectListItem() { Text = "Facultad de Microbiología" });
                    departamentos.Add(new SelectListItem() { Text = "Facultad de Odontología" });
                    break;
                case "Sistema de Educación General":
                    departamentos.Add(new SelectListItem() { Text = "Estudios Generales" });
                    break;
                case "Sistema de Estudios de Posgrado":
                    departamentos.Add(new SelectListItem() { Text = "Especialidad de Posgrado" });
                    departamentos.Add(new SelectListItem() { Text = "Programa de Doctorado" });
                    departamentos.Add(new SelectListItem() { Text = "Programa de Estudios de Posgrado" });
                    break;
                case "Sede Regional":
                    departamentos.Add(new SelectListItem() { Text = "Ciudad Universitaria Rodrigo Facio" });
                    departamentos.Add(new SelectListItem() { Text = "Sede de Alajuela-CONARE" });
                    departamentos.Add(new SelectListItem() { Text = "Sede Regional de Guanacaste" });
                    departamentos.Add(new SelectListItem() { Text = "Sede Regional de Occidente" });
                    departamentos.Add(new SelectListItem() { Text = "Sede Regional del Atlántico" });
                    departamentos.Add(new SelectListItem() { Text = "Sede Regional del Caribe" });
                    departamentos.Add(new SelectListItem() { Text = "Sede Regional del Pacífico" });
                    departamentos.Add(new SelectListItem() { Text = "Sede Regional del Sur" });
                    break;
                case "Otros":
                    departamentos.Add(new SelectListItem() { Text = "Vicerrectoría de Docencia" });
                    departamentos.Add(new SelectListItem() { Text = "Centros de Investigación" });
                    departamentos.Add(new SelectListItem() { Text = "Otro" });
                    break;
            }
            return departamentos;
        }
        [HttpGet]
        public static List<SelectListItem> GetUnidades(string value)
        {
            var unidades = new List<SelectListItem>();

            switch (value)
            {
                case "Facultad de Artes":
                    unidades.Add(new SelectListItem() { Text = "Escuela de Artes Dramáticas" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Artes Musicales" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Artes Plásticas" });
                    break;

                case "Facultad de Letras":
                    unidades.Add(new SelectListItem() { Text = "Escuela de Filología, Lingüistica y Literatura" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Filosofía" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Lenguas Modernas" });
                    break;

                case "Facultad de Ciencias Agroalimentarias":
                    unidades.Add(new SelectListItem() { Text = "Escuela de Agronomía" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Economía Agrícola y Agronegocios" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Tecnología de Alimentos" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Zootecnia" });
                    break;
                case "Facultad de Ciencias":
                    unidades.Add(new SelectListItem() { Text = "Escuela de Biología" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Física" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Geología" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Matemática" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Química" });
                    break;
                case "Facultad de Ciencias Económicas":
                    unidades.Add(new SelectListItem() { Text = "Escuela de Administración de Negocios" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Administración Pública" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Economía" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Estadística" });
                    break;
                case "Facultad de Ciencias Sociales":
                    unidades.Add(new SelectListItem() { Text = "Escuela de Ciencias de la Comunicación Colectiva" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Psicología" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Ciencias Políticas" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Historia y Geografía" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Trabajo Social" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Historia" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Geografía" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Antropología" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Sociología" });
                    break;
                case "Facultad de Derecho":
                    unidades.Add(new SelectListItem() { Text = "Escuela de Derecho" });
                    break;

                case "Facultad de Educación":
                    unidades.Add(new SelectListItem() { Text = "Escuela de Administración Educativa" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Formación Docente" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Orientación y Educación Especial" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Bibliotecología" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Educación Física y Deportes" });
                    break;
                case "Facultad de Ingeniería":
                    unidades.Add(new SelectListItem() { Text = "Escuela de Arquitectura" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Ciencias de la Computación e Informática" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Ingeniería de Biosistemas" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Ingeniería Civil" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Ingeniería Eléctrica" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Ingeniería Social" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Ingeniería Industrial" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Ingeniería Mecánica" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Ingeniería Química" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Ingeniería Topográfica" });
                    break;
                case "Facultad de Farmacia":
                    unidades.Add(new SelectListItem() { Text = "Escuela de Farmacia" });
                    break;
                case "Facultad de Medicina":
                    unidades.Add(new SelectListItem() { Text = "Escuela de Enfermería" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Medicina" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Nutrición" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Salud Pública" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Tecnologías en Salud" });
                    break;
                case "Facultad de Microbiología":
                    unidades.Add(new SelectListItem() { Text = "Escuela de Microbiología" });
                    break;
                case "Facultad de Odontología":
                    unidades.Add(new SelectListItem() { Text = "Escuela de Odontología" });
                    break;
                case "Estudios Generales":
                    unidades.Add(new SelectListItem() { Text = "Escuela de Estudios Generales" });
                    unidades.Add(new SelectListItem() { Text = "Escuela de Seminarios de Realidad Nacional" });
                    break;
                case "Especialidad de Posgrado":
                    unidades.Add(new SelectListItem() { Text = "Especialidades de Posgrado en Microbiología" });
                    unidades.Add(new SelectListItem() { Text = "Especialidades de Posgrado en Odontología General Avanzada" });
                    break;
                case "Programa de Doctorado":
                    unidades.Add(new SelectListItem() { Text = "Programa de Doctorado en Ciencias" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Doctorado en Ciencias Sociales sobre América Central" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Doctorado en Educación" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Doctorado en Estudios de la Sociedad y la Cultura" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Doctorado en Filología" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Doctorado en Ingeniería" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Doctorado en Sistemas de Producción Agrícola Tropical Sostenible" });
                    break;
                case "Programa de Estudios de Posgrado":
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Administración Pública" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Administración Universitaria" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Administración y Dirección de Empresas" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Antropología" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Arquitectura" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Artes" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Astrofísica" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Bibliotecología y Estudios de la Información" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Biología" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ciencia de Alimentos" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ciencias Agrícolas y Recursos Naturales" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ciencias Biomédicas" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ciencias Cognoscitivas" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ciencias de la Atmósfera" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ciencias de la Educación" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ciencias de la Enfermería" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ciencias del Movimiento Humano y Recreación" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ciencias Médicas" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ciencias Políticas" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Computación e Informática" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Comunicación" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Derecho" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Desarrollo Integrado en Regiones de Bajo Riego" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Desarrollo Sostenible" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Economía" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Educación" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Enseñanza del Castellano y Literatura" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Enseñanza del Inglés como Lengua Extranjera" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Español como Segunda Lengua" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Especialidades Médicas" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Estadística" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Estudios de la Mujer" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Estudios Interdisciplinarios" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Estudios Interdisciplinarios sobre Discapacidad" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Evaluación de Programas y Proyectos" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Evaluación Educativa" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Farmacia" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Filosofía" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Física" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Geografía" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Geología" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Gerencia Agroempresarial" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Gerontología" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Gestión Ambiental y Ecoturismo" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Gestión Hotelera" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Gestión Integrada Áreas Costeras Tropicales" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Gobierno y Políticas Públicas" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Historia" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ingeniería Civil" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ingeniería Eléctrica" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ingeniería en Biosistemas" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ingeniería Industrial" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ingeniería Mecánica" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Ingeniería Química" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Lingüistica" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Literatura" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Matemática" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Medicina Legal y Patología Forense" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Microbiología, Parasitología, Química Clínica e Inmunología" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Nutrición Humana" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Odontología" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Orientación" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Planificación Curricular" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Psicología" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Química" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Salud Pública" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Sociología" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Tecnologías de Información y Comunicación para la Gestión Organizacional" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Telemática" });
                    unidades.Add(new SelectListItem() { Text = "Programa de Posgrado en Trabajo Social" });
                    break;
                case "Ciudad Universitaria Rodrigo Facio":
                    unidades.Add(new SelectListItem() { Text = "Ciudad Universitaria Rodrigo Facio" });
                    break;
                case "Sede de Alajuela-CONARE":
                    unidades.Add(new SelectListItem() { Text = "Recinto de Alajuela" });
                    break;
                case "Sede Regional de Guanacaste":
                    unidades.Add(new SelectListItem() { Text = "Recinto de Liberia" });
                    unidades.Add(new SelectListItem() { Text = "Recinto de Santa Cruz" });
                    break;
                case "Sede Regional de Occidente":
                    unidades.Add(new SelectListItem() { Text = "Recinto de San Ramón" });
                    unidades.Add(new SelectListItem() { Text = "Recinto de Tacares" });
                    break;
                case "Sede Regional del Atlántico":
                    unidades.Add(new SelectListItem() { Text = "Recinto de Turrialba" });
                    unidades.Add(new SelectListItem() { Text = "Recinto de Paraíso" });
                    unidades.Add(new SelectListItem() { Text = "Recinto de Guápiles" });
                    break;
                case "Sede Regional del Caribe":
                    unidades.Add(new SelectListItem() { Text = "Recinto de Limón" });
                    unidades.Add(new SelectListItem() { Text = "Recinto de Siquirres" });
                    break;
                case "Sede Regional del Pacífico":
                    unidades.Add(new SelectListItem() { Text = "Recinto de Puntarenas" });
                    break;
                case "Sede Regional del Sur":
                    unidades.Add(new SelectListItem() { Text = "Recinto de Golfito" });
                    break;
                case "Vicerrectoría de Docencia":
                    unidades.Add(new SelectListItem() { Text = "Vicerrectoría de Docencia" });
                    unidades.Add(new SelectListItem() { Text = "Centros de Investigación" });
                    unidades.Add(new SelectListItem() { Text = "Otro" });
                    break;
                case "Centros de Investigación":
                    unidades.Add(new SelectListItem() { Text = "Vicerrectoría de Docencia" });
                    unidades.Add(new SelectListItem() { Text = "Centros de Investigación" });
                    unidades.Add(new SelectListItem() { Text = "Otro" });
                    break;
                case "Otro":
                    unidades.Add(new SelectListItem() { Text = "Vicerrectoría de Docencia" });
                    unidades.Add(new SelectListItem() { Text = "Centros de Investigación" });
                    unidades.Add(new SelectListItem() { Text = "Otro" });
                    break;
            }
            return unidades;
        }

        [HttpGet]
        public static List<SelectListItem> GetSedes()
        {
            var sedes = new List<SelectListItem>();

            var group1 = new SelectListGroup() { Name = "Sede Central" };
            var group2 = new SelectListGroup() { Name = "Sede del Sur" };
            var group3 = new SelectListGroup() { Name = "Sede del Caribe" };
            var group4 = new SelectListGroup() { Name = "Sede de Guanacaste" };
            var group5 = new SelectListGroup() { Name = "Sede del Atlántico" };
            var group6 = new SelectListGroup() { Name = "Sede de Occidente" };
            var group7 = new SelectListGroup() { Name = "Sede Interuniversitaria de Alajuela" };
            var group8 = new SelectListGroup() { Name = "Sede Regional del Pacífico" };


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
            sedes.Add(new SelectListItem() { Text = "Recinto de Puntarenas", Group = group8 });
            

            return sedes;
        }

        private int GetColumnIndex(ExcelWorksheet worksheet, string columnName)
        {
            // Normalize and remove accents from the input column name
            string normalizedColumnName = RemoveAccents(columnName.Trim().ToLower());

            // Iterate over the first row to find the column index
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                // Get the header, normalize it, and remove accents
                string header = RemoveAccents(worksheet.Cells[1, col].Text.Trim().ToLower());

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

        // Método para enviar confirmación de registro al usuario
        private async Task<IActionResult> EnviarContrasenaPorCorreo(string correo, string contrasena)
            {
                string subject = "Nuevo Usuario en el SISTEMA DE INSCRIPCIONES METICS";
                string message = $"<p>Se ha creado al usuario con correo institucional {correo} en el Sistema de Competencias Digitales para la Docencia - METICS.</p>" +
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