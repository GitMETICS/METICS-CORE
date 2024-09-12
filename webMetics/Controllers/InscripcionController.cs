﻿using webMetics.Handlers;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using NPOI.XSSF.UserModel;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using NPOI.XWPF.UserModel;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.Kernel.Geom;

/* 
 * Controlador para el proceso de inscripción de los grupos
 */

namespace webMetics.Controllers
{
    public class InscripcionController : Controller
    {
        private InscripcionHandler accesoAInscripcion;
        private GrupoHandler accesoAGrupo;
        private ParticipanteHandler accesoAParticipante;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public InscripcionController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            accesoAInscripcion = new InscripcionHandler(environment, configuration);
            accesoAGrupo = new GrupoHandler(environment, configuration);
            accesoAParticipante = new ParticipanteHandler(environment, configuration);
        }

        /* Método para mostrar la lista de participantes inscritos en un grupo */
        public ActionResult ListaDeParticipantesInscritos(int idGrupo)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            // Obtener la lista de participantes inscritos en el grupo especificado por idGrupo
            ViewBag.InscritosEnGrupo = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);

            return View();
        }

        /* Método para inscribir a un usuario a un grupo */
        public ActionResult Inscribir(int idGrupo, string idParticipante)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
                ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);
                
                if (grupo != null && participante != null && NoEstaInscritoEnGrupo(idGrupo, idParticipante))
                {
                    // Insertar la inscripción y enviar el comprobante por correo electrónico
                    InscripcionModel inscripcion = new InscripcionModel
                    {
                        idGrupo = idGrupo,
                        idParticipante = idParticipante,
                        numeroGrupo = grupo.numeroGrupo,
                        nombreGrupo = grupo.nombre,
                        horasMatriculadas = grupo.cantidadHoras,
                        horasAprobadas = 0,
                        estado = "Inscrito"
                    };

                    bool exito = accesoAInscripcion.InsertarInscripcion(inscripcion);

                    if (exito)
                    {
                        int horasParticipante = CalcularNumeroHorasAlInscribirse(grupo.cantidadHoras, participante.horasMatriculadas);
                        accesoAParticipante.ActualizarHorasMatriculadasParticipante(participante.idParticipante, horasParticipante);

                        try
                        {
                            string mensaje = ConstructorDelMensaje(grupo, participante);
                            EnviarCorreoInscripcion(grupo, mensaje, participante.correo);

                            ViewBag.Titulo = "Inscripción realizada";
                            ViewBag.Message = "El comprobante de inscripción se le ha enviado al correo";
                            ViewBag.Participante = accesoAParticipante.ObtenerParticipante(idParticipante);
                        }
                        catch
                        {
                            // Configurar los datos para mostrar en la vista
                            ViewBag.Titulo = "Inscripción realizada";
                            ViewBag.Message = "Se ha inscrito en el grupo, pero hubo un error al enviar el comprobante de inscripción a su correo institucional.";
                        }
                    }
                    else
                    {
                        ViewBag.Titulo = "No se pudo realizar la inscripción";
                        ViewBag.Message = "Ocurrió un error al procesar la solicitud de inscripción.";
                    }
                }
                else
                {
                    ViewBag.Titulo = "No se pudo realizar la inscripción";
                    ViewBag.Message = "No se puede inscribir en este grupo porque ya está inscrito o ha superado el límite máximo de horas.";
                }
            }
            catch
            {
                ViewBag.Titulo = "No se pudo realizar la inscripción";
                ViewBag.Message = "El módulo no se encuentra disponible.";
            }

            return View();
        }

        private bool NoEstaInscritoEnGrupo(int idGrupo, string idParticipante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            bool noEstaInscrito = false;

            List<InscripcionModel> listaInscritos = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);

            if (listaInscritos == null ||
                listaInscritos.Find(inscripcionModel => inscripcionModel.idParticipante == idParticipante) == null)
            {
                noEstaInscrito = true;
            }

            return noEstaInscrito;
        }

        /* Método para que un administrador elimine una inscripción de un usuario */
        public ActionResult EliminarInscripcion(string idParticipante, int idGrupo)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                bool exito = accesoAInscripcion.EliminarInscripcion(idGrupo, idParticipante);

                if (exito)
                {
                    GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
                    ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

                    int horasParticipante = CalcularNumeroHorasAlDesinscribirse(grupo.cantidadHoras, participante.horasMatriculadas);
                    accesoAParticipante.ActualizarHorasMatriculadasParticipante(participante.idParticipante, horasParticipante);

                    TempData["successMessage"] = "Se eliminó la inscripción del participante.";
                    return RedirectToAction("ListaParticipantes", "Participante", new { idGrupo = idGrupo });
                }
                else
                {
                    TempData["errorMessage"] = "No se pudo eliminar la inscripción del participante.";
                    return RedirectToAction("ListaParticipantes", "Participante", new { idGrupo = idGrupo });
                }
            }
            catch (Exception)
            {
                // Si ocurrió una excepción al intentar eliminar la inscripción, mostrar un mensaje y redirigir a la lista de participantes del grupo
                TempData["errorMessage"] = "No se pudo eliminar la inscripción del participante.";
                return RedirectToAction("ListaParticipantes", "Participante", new { idGrupo = idGrupo });
            }
        }

        [HttpPost]
        public ActionResult ActualizarCorreoLimiteHoras(string correoLimiteHoras)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            bool exito = correoLimiteHoras != null ? accesoAInscripcion.ActualizarCorreoLimiteHoras(correoLimiteHoras) : false;

            if (exito)
            {
                TempData["successMessage"] = "Se actualizó el correo de notificación.";
            }
            else
            {
                TempData["errorMessage"] = "Debe introducir un correo de notificación válido.";
            }

            return RedirectToAction("VerParticipantes", "Participante");
        }

        /* Método para que un usuario se desinscriba de un grupo*/
        public ActionResult DesinscribirParticipante(string idParticipante, int idGrupo)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                bool exito = accesoAInscripcion.EliminarInscripcion(idGrupo, idParticipante);

                if (exito)
                {
                    GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
                    ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

                    int horasParticipante = CalcularNumeroHorasAlDesinscribirse(grupo.cantidadHoras, participante.horasMatriculadas);
                    accesoAParticipante.ActualizarHorasMatriculadasParticipante(participante.idParticipante, horasParticipante);
                }
                else
                {
                    TempData["errorMessage"] = "Hubo un error y no se pudo eliminar la inscripción.";
                }

                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "Hubo un error y no se pudo enviar la solicitud de eliminar la inscripción.";

                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }
        }

        private bool MayorALimiteMaximoHoras(int horasGrupo, int horasParticipante)
        {
            return horasParticipante + horasGrupo > 30;
        }

        private int CalcularNumeroHorasAlInscribirse(int horasGrupo, int horasParticipante)
        {
            return horasParticipante + horasGrupo;
        }

        private int CalcularNumeroHorasAlDesinscribirse(int horasGrupo, int horasParticipante)
        {
            int numeroHoras = horasParticipante - horasGrupo;
            if (numeroHoras <= 0)
            {
                numeroHoras = 0;
            }

            return numeroHoras;
        }

        /* Método para enviar confirmación de inscripción al usuario*/
        private void EnviarCorreoInscripcion(GrupoModel grupo, string mensaje, string correoParticipante)
        {
            // Configurar el mensaje de correo electrónico con el comprobante de inscripción y el archivo adjunto (si corresponde)
            // Se utiliza la librería MimeKit para construir el mensaje
            // El mensaje incluye una versión en HTML y texto plano

            // Contenido base del mensaje en HTML y texto plano
            const string BASE_MESSAGE_HTML = ""; // Contenido HTML adicional puede ser agregado aquí
            const string BASE_MESSAGE_TEXT = "";
            const string BASE_SUBJECT = "Comprobante de inscripción"; // Asunto del correo

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

            // Obtener los datos del archivo adjunto (si existe) y agregarlo al mensaje
            byte[] attachmentData = accesoAGrupo.ObtenerArchivo(grupo.idGrupo);
            if (attachmentData != null)
            {
                // Crear la parte adjunta del mensaje
                var attachment = new MimeKit.MimePart("application", "octet-stream")
                {
                    Content = new MimeContent(new MemoryStream(attachmentData)),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = accesoAGrupo.ObtenerNombreArchivo(grupo) // Nombre del archivo adjunto
                };

                // Crear una parte multipart para incluir tanto el cuerpo del mensaje como el archivo adjunto
                var multipart = new Multipart("mixed");
                multipart.Add(bodyBuilder.ToMessageBody());
                multipart.Add(attachment);
                message.Body = multipart;
            }
            else
            {
                // Si no hay archivo adjunto, solo agregar el cuerpo del mensaje al mensaje principal
                message.Body = bodyBuilder.ToMessageBody();
            }

            // Enviar el correo electrónico utilizando un cliente SMTP
            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                // Configurar el cliente SMTP para el servidor de correo de la UCR
                client.Connect("smtp.ucr.ac.cr", 587); // Se utiliza el puerto 587 para enviar correos
                client.Authenticate(from.Address, _configuration["EmailSettings:SMTPPassword"]);

                // Enviar el mensaje
                client.Send(message);

                // Desconectar el cliente SMTP
                client.Disconnect(true);
            }
        }

        //Método del constructor del mensaje del correo que será enviado al usuario con los datos de la inscripción
        public string ConstructorDelMensaje(GrupoModel grupo, ParticipanteModel participante)
        {
            // Construir el contenido del mensaje que se enviará por correo electrónico al usuario
            // Este método toma información del módulo y el participante y crea un mensaje personalizado
            // con los detalles de la inscripción

            // Crear el mensaje con información relevante de la inscripción en formato HTML
            string mensaje = "" +
                "<h2>Comprobante de inscripción a módulo - SISTEMA DE INSCRIPCIONES METICS</h2>" +
                "<p>Nombre: " + participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido + "</p>" +
                "<p>Se ha inscrito al módulo: <strong>" + grupo.nombre + " (Grupo " + grupo.numeroGrupo + ")</strong></p>" +
                
                "<ul><li>Descripción: " + grupo.descripcion + "</li>" +
                "<li>Horario: " + grupo.horario + "</li>" +
                "<li>Modalidad: " + grupo.modalidad + "</li>" +
                "<li>Cantidad de horas: " + grupo.cantidadHoras + "</li>" +
                "<li>Facilitador(a): " + grupo.nombreAsesor + "</li>" +
                "<li>Fecha de inicio: " + grupo.fechaInicioGrupo.ToString("dd/MM/yyyy") + "</li>" +
                "<li>Fecha de finalización: " + grupo.fechaFinalizacionGrupo.ToString("dd/MM/yyyy") + "</li>";
            
            if (!(string.Equals(grupo.modalidad, "Autogestionado", StringComparison.OrdinalIgnoreCase) || string.Equals(grupo.modalidad, "Virtual", StringComparison.OrdinalIgnoreCase)))
            {
                mensaje += "<li>Lugar: " + grupo.lugar + "</li>";
            }
            
            mensaje += "</ul>" +
                "<p>Si necesita desinscribirse de este módulo, puede ingresar al sistema y realizarlo desde la plataforma.</p>";

            return mensaje;
        }

        // Método optimizado para exportar la lista de los participantes de un grupo a un archivo PDF
        public ActionResult ExportarParticipantesPDF(int idGrupo)
        {
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);

            var filePath = System.IO.Path.Combine(_environment.WebRootPath, "data", "Lista_de_Participantes.docx");
            PdfWriter writer = new PdfWriter(filePath);
            PdfDocument pdf = new PdfDocument(writer);

            PageSize pageSize = PageSize.A2;  // Puedes elegir PageSize.A3 para un tamaño más pequeño
            iText.Layout.Document document = new iText.Layout.Document(pdf, pageSize);

            // Establecer fuente en negrita para encabezado
            PdfFont boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
            PdfFont regularFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

            Paragraph header1 = new Paragraph("Nombre del módulo: " + grupo.nombre)
                .SetFontSize(10);
            document.Add(header1);

            Paragraph header2 = new Paragraph("Nombre del/la facilitador(a) asociado(a): " + grupo.nombreAsesor)
                .SetFontSize(10);
            document.Add(header2);

            Paragraph header3 = new Paragraph("")
                .SetFontSize(10);
            document.Add(header3);

            // Crear la tabla (9 columnas)
            iText.Layout.Element.Table table = new iText.Layout.Element.Table(new float[] { 3, 2, 2, 3, 2, 3, 2, 2 });
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Agregar encabezados de la tabla con estilo
            string[] headers = { "Nombre del participante", "Correo institucional", "Condición", "Unidad académica", "Teléfono", "Módulo", "Horas aprobadas", "Calificación del módulo" };
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
            string fileName = "Lista_de_Participantes_" + grupo.nombre + ".pdf";
            return File(System.IO.File.ReadAllBytes(filePath), "application/pdf", fileName);
        }

        public ActionResult ExportarParticipantesWord(int idGrupo)
        {
            // Obtener la lista de participantes del grupo y la información del grupo
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);

            var fileName = "Lista_de_Participantes_" + grupo.nombre + ".docx";

            XWPFDocument wordDoc = new XWPFDocument();

            // Crear un título
            XWPFParagraph titleParagraph = wordDoc.CreateParagraph();
            titleParagraph.Alignment = ParagraphAlignment.CENTER;
            XWPFRun titleRun = titleParagraph.CreateRun();
            titleRun.SetText("Lista de Participantes");
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
            headerRow.GetCell(0).SetText("Nombre del participante");
            headerRow.GetCell(1).SetText("Correo institucional");
            headerRow.GetCell(2).SetText("Condición");
            headerRow.GetCell(3).SetText("Unidad académica");
            headerRow.GetCell(4).SetText("Teléfono");
            headerRow.GetCell(5).SetText("Módulo");
            headerRow.GetCell(6).SetText("Horas aprobadas");
            headerRow.GetCell(7).SetText("Calificación del módulo");

            // Rellenar la tabla con los datos de los participantes e inscripciones
            int rowIndex = 1; // Comenzar después del encabezado

            foreach (var participante in participantes)
            {
                // Obtener las inscripciones del participante
                var inscripcionesParticipante = inscripciones.Where(i => i.idParticipante == participante.idParticipante).ToList();

                // Si el participante tiene inscripciones, crear una fila por cada módulo inscrito
                if (inscripcionesParticipante.Any())
                {
                    foreach (var inscripcion in inscripcionesParticipante)
                    {
                        var row = table.CreateRow(); // Crear una nueva fila para cada inscripción

                        row.GetCell(0).SetText(participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido);
                        row.GetCell(1).SetText(participante.idParticipante);
                        row.GetCell(2).SetText(participante.condicion);
                        row.GetCell(3).SetText(participante.unidadAcademica);
                        row.GetCell(4).SetText(participante.telefono);

                        // Información del módulo
                        row.GetCell(5).SetText(grupo.nombre);
                        row.GetCell(6).SetText(inscripcion.horasAprobadas.ToString());
                        row.GetCell(7).SetText(inscripcion.calificacion.ToString());

                        rowIndex++;
                    }
                }
            }

            // Guardar el archivo y devolver el documento Word como respuesta
            var stream = new MemoryStream();
            wordDoc.Write(stream);
            var file = stream.ToArray();
            return File(file, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        public ActionResult ExportarParticipantesExcel(int idGrupo)
        {
            // Obtener la lista de participantes del grupo y la información del grupo
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);

            XSSFWorkbook workbook = new XSSFWorkbook();

            string sheetName = grupo.nombre.Length > 31 ? grupo.nombre.Substring(0, 31) : grupo.nombre;
            var sheet = workbook.CreateSheet(sheetName);

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

            IRow row1 = sheet.CreateRow(0);
            NPOI.SS.UserModel.ICell cell11 = row1.CreateCell(0);
            cell11.SetCellValue("Nombre del módulo:");
            NPOI.SS.UserModel.ICell cell12 = row1.CreateCell(1);
            cell12.SetCellValue(grupo.nombre);

            IRow row2 = sheet.CreateRow(1);
            NPOI.SS.UserModel.ICell cell21 = row2.CreateCell(0);
            cell21.SetCellValue("Nombre del/la facilitador(a) asociado(a):");
            NPOI.SS.UserModel.ICell cell22 = row2.CreateCell(1);
            cell22.SetCellValue(grupo.nombreAsesor);

            // Crear el encabezado de la tabla
            IRow rowHeaders = sheet.CreateRow(3);
            string[] headers = { "Nombre del participante", "Correo institucional", "Condición", "Unidad académica", "Teléfono", "Módulo", "Horas aprobadas", "Calificación del módulo" };

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
                        row.CreateCell(0).SetCellValue($"{participante.nombre} {participante.primerApellido} {participante.segundoApellido}");
                        row.CreateCell(1).SetCellValue(participante.idParticipante);
                        row.CreateCell(2).SetCellValue(participante.condicion);
                        row.CreateCell(3).SetCellValue(participante.unidadAcademica);
                        row.CreateCell(4).SetCellValue(participante.telefono);

                        // Completar los datos del módulo
                        row.CreateCell(5).SetCellValue(grupo.nombre); // Nombre del módulo
                        row.CreateCell(6).SetCellValue(inscripcion.horasAprobadas); // Horas aprobadas
                        row.CreateCell(7).SetCellValue(inscripcion.calificacion); // Nota del módulo

                        // Aplicar estilo al cuerpo
                        for (int i = 0; i < 8; i++)
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

                    row.CreateCell(0).SetCellValue($"{participante.nombre} {participante.primerApellido} {participante.segundoApellido}");
                    row.CreateCell(1).SetCellValue(participante.idParticipante);
                    row.CreateCell(2).SetCellValue(participante.condicion);
                    row.CreateCell(3).SetCellValue(participante.unidadAcademica);
                    row.CreateCell(4).SetCellValue(participante.telefono);
                    row.CreateCell(5).SetCellValue("N/A");
                    row.CreateCell(6).SetCellValue("N/A");
                    row.CreateCell(7).SetCellValue("N/A");

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
            string fileName = "Lista_de_Participantes_" + grupo.nombre + ".xlsx";
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                var file = stream.ToArray();
                return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
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