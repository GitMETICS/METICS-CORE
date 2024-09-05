using webMetics.Handlers;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using NPOI.XSSF.UserModel;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using NPOI.XWPF.UserModel;

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
                
                if (grupo != null && participante != null 
                    && NoEstaInscritoEnGrupo(idGrupo, idParticipante) 
                    && MenorALimiteMaximoHoras(grupo.cantidadHoras, participante.horasMatriculadas))
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

        private bool MenorALimiteMaximoHoras(int horasGrupo, int horasParticipante)
        {
            return horasParticipante + horasGrupo <= 50; // TODO: Refactor.
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
                "<p>Se ha inscrito al módulo: <strong>" + grupo.nombre + " Grupo (" + grupo.numeroGrupo + ")</strong></p>" +
                
                "<ul><li>Descripcion: " + grupo.descripcion + "</li>" +
                "<li>Horario: " + grupo.horario + "</li>" +
                "<li>Modalidad: " + grupo.modalidad + "</li>" +
                "<li>Cantidad de horas: " + grupo.cantidadHoras + "</li>" +
                "<li>Facilitador(a): " + grupo.nombreAsesor + "</li>" +
                "<li>Fecha de inicio: " + grupo.fechaInicioGrupo + "</li>" +
                "<li>Fecha de finalización: " + grupo.fechaFinalizacionGrupo + "</li>";
            
            if (!(string.Equals(grupo.modalidad, "Autogestionado", StringComparison.OrdinalIgnoreCase) || string.Equals(grupo.modalidad, "Virtual", StringComparison.OrdinalIgnoreCase)))
            {
                mensaje += "<li>Lugar: " + grupo.lugar + "</li>";
            }
            
            mensaje += "</ul><br />" +
                "<p>En caso de ser necesario, para proceder con la desinscripción de este módulo, por favor ingrese al sistema y complete el proceso correspondiente.</p>";

            return mensaje;
        }

        // Método optimizado para exportar la lista de los participantes de un grupo a un archivo PDF
        public ActionResult ExportarParticipantesPDF(int idGrupo)
        {
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);

            var filePath = Path.Combine(_environment.WebRootPath, "data", "Lista_de_Participantes.docx");
            PdfWriter writer = new PdfWriter(filePath);
            PdfDocument pdf = new PdfDocument(writer);
            iText.Layout.Document document = new iText.Layout.Document(pdf);

            Paragraph header1 = new Paragraph("Nombre del módulo: " + grupo.nombre)
                .SetFontSize(10);
            document.Add(header1);

            Paragraph header2 = new Paragraph("Nombre del/la facilitador(a) asociado(a): " + grupo.nombreAsesor)
                .SetFontSize(10);
            document.Add(header2);

            Paragraph header3 = new Paragraph("")
                .SetFontSize(10);
            document.Add(header3);

            Table table = new Table(5, true);
            table.AddHeaderCell("Nombre del participante").SetFontSize(8);
            table.AddHeaderCell("Correo institucional").SetFontSize(8);
            table.AddHeaderCell("Condición").SetFontSize(8);
            table.AddHeaderCell("Unidad académica").SetFontSize(8);
            table.AddHeaderCell("Teléfono").SetFontSize(8);

            foreach (var participante in participantes)
            {
                table.AddCell(participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido);
                table.AddCell(participante.idParticipante);
                table.AddCell(participante.condicion);
                table.AddCell(participante.unidadAcademica);
                table.AddCell(participante.telefono);
            }

            document.Add(table);

            table.Complete();

            document.Close();

            string fileName = "Lista_de_Participantes_" + grupo.nombre + ".pdf";

            return File(System.IO.File.ReadAllBytes(filePath), "application/pdf", fileName);
        }

        public ActionResult ExportarParticipantesWord(int idGrupo)
        {
            // Obtener la lista de participantes del grupo y la información del grupo
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);

            var fileName = "Lista_de_Participantes_" + grupo.nombre + ".docx";

            XWPFDocument wordDoc = new XWPFDocument();

            // Create a table
            XWPFTable table = wordDoc.CreateTable(grupo.cupo + 3, 6);
            var tblLayout1 = table.GetCTTbl().tblPr.AddNewTblLayout();
            table.SetColumnWidth(0, 2000);
            table.SetColumnWidth(1, 2000);
            table.SetColumnWidth(2, 1500);
            table.SetColumnWidth(3, 1500);
            table.SetColumnWidth(4, 1500);
            table.SetColumnWidth(5, 1500);


            var headerRow0 = table.Rows[0];
            headerRow0.GetCell(0).SetText("Nombre del/la Facilitador(a)");
            headerRow0.GetCell(1).SetText("Nombre del Módulo");
            var row0 = table.Rows[1];
            row0.GetCell(0).SetText(grupo.nombreAsesor);
            row0.GetCell(1).SetText(grupo.nombre);


            var headerRow = table.Rows[2];
            headerRow.GetCell(0).SetText("Nombre del participante");
            headerRow.GetCell(1).SetText("Correo institucional");
            headerRow.GetCell(2).SetText("Condición");
            headerRow.GetCell(3).SetText("Unidad académica");
            headerRow.GetCell(4).SetText("Teléfono");


            for (int i = 0; i < participantes.Count; i++)
            {
                var row = table.Rows[i + 3];
                row.GetCell(0).SetText(participantes[i].nombre + " " + participantes[i].primerApellido + " " + participantes[i].segundoApellido);
                row.GetCell(1).SetText(participantes[i].idParticipante.ToString());
                row.GetCell(2).SetText(participantes[i].condicion.ToString());
                row.GetCell(3).SetText(participantes[i].unidadAcademica);
                row.GetCell(4).SetText(participantes[i].telefono.ToString());
            }

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


            NPOI.SS.UserModel.ICell cell32 = row3.CreateCell(0);
            cell32.SetCellValue("Nombre del participante");

            NPOI.SS.UserModel.ICell cell35 = row3.CreateCell(1);
            cell35.SetCellValue("Correo institucional");

            NPOI.SS.UserModel.ICell cell33 = row3.CreateCell(2);
            cell33.SetCellValue("Condición");

            NPOI.SS.UserModel.ICell cell34 = row3.CreateCell(3);
            cell34.SetCellValue("Unidad académica");

            NPOI.SS.UserModel.ICell cell36 = row3.CreateCell(4);
            cell36.SetCellValue("Teléfono");

            int rowN = 4;
            foreach (var participante in participantes)
            {
                NPOI.SS.UserModel.IRow row = sheet.CreateRow(rowN);


                NPOI.SS.UserModel.ICell cell2 = row.CreateCell(0);
                cell2.SetCellValue(participante.nombre + ' ' + participante.primerApellido + ' ' + participante.segundoApellido);

                NPOI.SS.UserModel.ICell cell5 = row.CreateCell(1);
                cell5.SetCellValue(participante.idParticipante);

                NPOI.SS.UserModel.ICell cell3 = row.CreateCell(2);
                cell3.SetCellValue(participante.condicion);

                NPOI.SS.UserModel.ICell cell4 = row.CreateCell(3);
                cell4.SetCellValue(participante.unidadAcademica);

                NPOI.SS.UserModel.ICell cell6 = row.CreateCell(4);
                cell6.SetCellValue(participante.telefono);

                rowN++;
            }

            string fileName = "Lista_de_Participantes_" + grupo.nombre + ".xlsx";
            var stream = new MemoryStream();
            workbook.Write(stream);
            var file = stream.ToArray();

            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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