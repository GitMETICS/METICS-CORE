﻿using webMetics.Handlers;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc;

/* 
 * Controlador para el proceso de inscripción de los grupos
 */

namespace webMetics.Controllers
{
    public class InscripcionController : Controller
    {
        public InscripcionHandler accesoAInscripcion;
        public GrupoHandler accesoAGrupo;
        public ParticipanteHandler accesoAParticipante;

        public InscripcionController()
        {
            accesoAInscripcion = new InscripcionHandler();
            accesoAGrupo = new GrupoHandler();
            accesoAParticipante = new ParticipanteHandler();
        }

        /* Método para mostrar la lista de participantes inscritos en un grupo */
        public ActionResult ListaDeParticipantesInscritos(int idGrupo)
        {

            // Obtener la lista de participantes inscritos en el grupo especificado por idGrupo
            ViewBag.InscritosEnGrupo = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);

            return View();
        }

        /* Método para inscribir a un usuario a un grupo */
        public ActionResult Inscribir(int idGrupo, string idParticipante)
        {
            GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo);
            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

            
            if (NoEstaInscritoEnGrupo(idGrupo, idParticipante))
            {
                if (MenorALimiteMaximoHoras(grupo.cantidadHoras, participante.horasMatriculadas))
                {
                    // Insertar la inscripción y enviar el comprobante por correo electrónico
                    InscripcionModel inscripcion = new InscripcionModel
                    {
                        idGrupo = idGrupo,
                        idParticipante = idParticipante
                    };

                    bool exito = accesoAInscripcion.InsertarInscripcion(inscripcion);

                    if (exito)
                    {
                        int horasParticipante = CalcularNumeroHorasAlInscribirse(grupo.cantidadHoras, participante.horasMatriculadas);
                        accesoAParticipante.ActualizarHorasMatriculadasParticipante(participante.idParticipante, horasParticipante);

                        try
                        {
                            string mensaje = ConstructorDelMensaje(grupo, participante);
                            SendEmail(grupo, mensaje, participante.correo);

                            // Configurar los datos para mostrar en la vista
                            ViewBag.Titulo = "Inscripción realizada";
                            ViewBag.Message = "El comprobante de inscripción se le ha enviado al correo";
                            ViewBag.Participante = accesoAParticipante.ObtenerParticipante(idParticipante);
                        }
                        catch
                        {
                            // Configurar los datos para mostrar en la vista
                            ViewBag.Titulo = "Inscripción realizada";
                            ViewBag.Message = "Se ha inscrito en el grupo, pero hubo un error al enviar el correo de inscripción.";
                        }
                        
                        
                    }
                }
            }
            else
            {
                ViewBag.Titulo = "No se pudo realizar la inscripción";
                ViewBag.Message = "Hubo un error y no se pudo enviar la petición de inscripción.";
            }

            return View();
        }

        private bool NoEstaInscritoEnGrupo(int idGrupo, string idParticipante)
        {
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
        public ActionResult EliminarInscripcion(string idParticipante, string idGrupo)
        {
            try
            {
                // Intentar eliminar la inscripción del usuario con el idGenerado en el grupo especificado por idGrupo
                ViewBag.ExitoAlCrear = accesoAInscripcion.EliminarInscripcion(idParticipante, idGrupo);

                // Si la eliminación fue exitosa, redirigir a la lista de participantes del grupo
                if (ViewBag.ExitoAlCrear)
                {
                    GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(Convert.ToInt32(idGrupo));
                    ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

                    int horasParticipante = CalcularNumeroHorasAlDesinscribirse(grupo.cantidadHoras, participante.horasMatriculadas);
                    accesoAParticipante.ActualizarHorasMatriculadasParticipante(participante.idParticipante, horasParticipante);

                    return Redirect("~/Participante/ListaParticipantes?idGrupo=" + idGrupo);
                }
                else
                {
                    // Si hubo un error al eliminar la inscripción, mostrar un mensaje y limpiar el modelo para la vista
                    ViewBag.Message = "Hubo un error y no se pudo eliminar al asesor.";
                    ModelState.Clear();
                    return View();
                }
            }
            catch (Exception)
            {
                // Si ocurrió una excepción al intentar eliminar la inscripción, mostrar un mensaje y redirigir a la lista de participantes del grupo
                ViewBag.Message = "Hubo un error y no se pudo enviar la petición de eliminar al asesor.";
                return Redirect("~/Participante/ListaParticipantes?idGrupo=" + idGrupo);
            }
        }

        /* Método para que un usuario se desinscriba de un grupo*/
        public ActionResult DesinscribirParticipante(string idParticipante, string idGrupo)
        {
            try
            {
                // Intentar eliminar la inscripción del participante con el idParticipante en el grupo especificado por idGrupo
                ViewBag.ExitoAlCrear = accesoAInscripcion.EliminarInscripcion(idParticipante, idGrupo);

                // Si la desinscripción fue exitosa, redirigir a la lista de grupos disponibles
                if (ViewBag.ExitoAlCrear)
                {
                    GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(Convert.ToInt32(idGrupo));
                    ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

                    int horasParticipante = CalcularNumeroHorasAlDesinscribirse(grupo.cantidadHoras, participante.horasMatriculadas);
                    accesoAParticipante.ActualizarHorasMatriculadasParticipante(participante.idParticipante, horasParticipante);

                    return Redirect("~/Grupo/ListaGruposDisponibles");
                }
                else
                {
                    // Si hubo un error al eliminar la inscripción, mostrar un mensaje y limpiar el modelo para la vista
                    ViewBag.Message = "Hubo un error y no se pudo eliminar al participante.";
                    ModelState.Clear();
                    return Redirect("~/Grupo/ListaGruposDisponibles");
                }
            }
            catch (Exception)
            {
                // Si ocurrió una excepción al intentar eliminar la inscripción, mostrar un mensaje y redirigir a la lista de grupos disponibles
                ViewBag.Message = "Hubo un error y no se pudo enviar la petición de eliminar al participante.";
                return Redirect("~/Grupo/ListaGruposDisponibles");
            }
        }

        private bool MenorALimiteMaximoHoras(int horasGrupo, int horasParticipante)
        {
            return horasParticipante + horasGrupo <= 30;
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
        private void SendEmail(GrupoModel grupo, string mensaje, string correoParticipante)
        {
            /*// Configurar el mensaje de correo electrónico con el comprobante de inscripción y el archivo adjunto (si corresponde)
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
            byte[] attachmentData = accesoAGrupo.ObtenerArchivo(grupo);
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
                client.Authenticate(from.Address, "pass"); // Cambiar la cuenta de correo y contraseña real para enviar el correo

                // Enviar el mensaje
                client.Send(message);

                // Desconectar el cliente SMTP
                client.Disconnect(true);
            }*/
        }

        //Método del constructor del mensaje del correo que será enviado al usuario con los datos de la inscripción
        public string ConstructorDelMensaje(GrupoModel grupo, ParticipanteModel participante)
        {
            // Construir el contenido del mensaje que se enviará por correo electrónico al usuario
            // Este método toma información del módulo y el participante y crea un mensaje personalizado
            // con los detalles de la inscripción

            // Crear el mensaje con información relevante de la inscripción en formato HTML
            string mensaje = "" +
                "<h2>Comprobante de inscripción</h2> " +
                "<p>Nombre: " + participante.nombre + " " + participante.apellido_1 + " " + participante.apellido_2 + "</p>" +
                "<p>Cédula: " + participante.idParticipante + "</p>" +
                "<p>Se ha inscrito al módulo: <strong>" + grupo.idGrupo + " " + grupo.nombre + "</strong></p>" +
                "<ul><li>Horario: " + grupo.horario + "</li>" +
                "<li>Modalidad: " + grupo.modalidad + "</li>" +
                "<li>Cantidad de horas: " + grupo.cantidadHoras + "</li>" +
                "<li>Asesor: " + grupo.nombreAsesorAsociado + "</li>" +
                "<li>Fecha de inicio: " + grupo.fechaInicioGrupo + "</li>" +
                "<li>Fecha de finalización: " + grupo.fechaFinalizacionGrupo + "</li>";
            
            if (string.Equals(grupo.modalidad, "presencial", StringComparison.OrdinalIgnoreCase))
            {
                mensaje += "<li>Lugar: " + grupo.lugar + "</li>";
            }
            
            mensaje += "</ul>";

            return mensaje;
        }

        /*//Método original para exportar la lista de los participantes de un grupo a un archivo PDF
        [HttpPost]
        public FileResult Export(int idGrupo)
        {
            // Exportar la lista de participantes de un grupo a un archivo PDF

            // Obtener la lista de participantes del grupo y la información del grupo
            List<ParticipanteModel> lista = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
            GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo);

            // Construir una string HTML con los detalles de los participantes
            StringBuilder sb = new StringBuilder();
            //Inicio de la tabla
            sb.Append("<table border='1' cellpadding='5' cellspacing='0' style='border: 1px solid #ccc;font-family: Arial; font-size: 10pt;'>");
            // Agregar los encabezados y detalles de los participantes...
            sb.Append("<h2>Lista de participantes</h2>");
            sb.Append("<h4> Grupo: " + grupo.nombre + " </h4>");
            sb.Append("<h4> Asesor: " + grupo.nombreAsesorAsociado + " </h4>");
            sb.Append("<tr>");
            sb.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc'>Nombre completo</th>");
            sb.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc'>Condición</th>");
            sb.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc'>Unidad académica</th>");
            sb.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc'>Correo</th>");
            sb.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc'>Teléfono</th>");
            sb.Append("</tr>");
            //Contenido de la tabla
            foreach (var participante in lista)
            {
                sb.Append("<tr>");
                //Append data.
                sb.Append("<td style='border: 1px solid #ccc'>");
                sb.Append(participante.nombre + " " + participante.apellido_1 + " " + participante.apellido_2);
                sb.Append("</td>");

                sb.Append("<td style='border: 1px solid #ccc'>");
                sb.Append(participante.condicion);
                sb.Append("</td>");


                sb.Append("<td style='border: 1px solid #ccc'>");
                sb.Append(participante.unidadAcademica);
                sb.Append("</td>");


                sb.Append("<td style='border: 1px solid #ccc'>");
                sb.Append(participante.correo);
                sb.Append("</td>");

                sb.Append("<td style='border: 1px solid #ccc'>");
                sb.Append(participante.telefonos);
                sb.Append("</td>");

                sb.Append("</tr>");
            }

            //Final de la tabla
            sb.Append("</table>");

            // Crear el archivo PDF utilizando la librería iTextSharp
            using (MemoryStream stream = new MemoryStream())
            {
                StringReader sr = new StringReader(sb.ToString());
                Document pdfDoc = new Document(PageSize.A4, 10f, 10f, 100f, 0f);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();
                XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, sr);
                //pdfDoc.Close();
                return File(stream.ToArray(), "application/pdf", "Lista de Participantes.pdf");
            }
        }*/

        // Método optimizado para exportar la lista de los participantes de un grupo a un archivo PDF
        [HttpPost]
        public ActionResult ExportarParticipantesPDF(int idGrupo)
        {
            /*// Exportar la lista de participantes de un grupo a un archivo PDF

            // Obtener la lista de participantes del grupo y la información del grupo
            List<ParticipanteModel> lista = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
            GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo);

            ExportarParticipantesModel datosExportar = new ExportarParticipantesModel()
            {
                nombreGrupo = grupo.nombre,
                nombreAsesorAsociado = grupo.nombreAsesorAsociado,
                listaParticipantes = lista
            };

            return new Rotativa.PartialViewAsPdf("ExportarParticipantes", datosExportar)
            {
                FileName = "Lista_de_Participantes_" + grupo.nombre + ".pdf"
            };*/
            return RedirectToAction("VerCalificaciones", "Calificaciones", new { idGrupo = idGrupo });
        }

        [HttpPost]
        public ActionResult ExportarParticipantesWord(int idGrupo)
        {
            /*// Obtener la lista de participantes del grupo y la información del grupo
            List<ParticipanteModel> lista = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
            GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo);

            // Crear un nuevo documento Word
            using (var doc = DocX.Create(Server.MapPath("~/App_Data/ListaParticipantes.docx")))
            {
                // Agregar el contenido al documento Word
                doc.InsertParagraph($"Nombre del módulo: {grupo.nombre}");
                doc.InsertParagraph($"Nombre del asesor asociado: {grupo.nombreAsesorAsociado}\n");
                doc.InsertParagraph("Lista de participantes: ");

                var table = doc.AddTable(lista.Count + 1, 6);

                table.Design = TableDesign.TableGrid;
                table.Design = TableDesign.ColorfulList;
                table.AutoFit = AutoFit.ColumnWidth;

                var headerRow = table.Rows[0];
                headerRow.Cells[0].Paragraphs[0].Append("Identificación");
                headerRow.Cells[1].Paragraphs[0].Append("Nombre");
                headerRow.Cells[2].Paragraphs[0].Append("Condición");
                headerRow.Cells[3].Paragraphs[0].Append("Unidad académica");
                headerRow.Cells[4].Paragraphs[0].Append("Correo institucional");
                headerRow.Cells[5].Paragraphs[0].Append("Teléfono");

                for (int i = 0; i < lista.Count; i++)
                {
                    var row = table.Rows[i + 1];
                    row.Cells[0].Paragraphs[0].Append(lista[i].idParticipante);
                    row.Cells[1].Paragraphs[0].Append(lista[i].nombre + " " + lista[i].apellido_1 + " " + lista[i].apellido_2);
                    row.Cells[2].Paragraphs[0].Append(lista[i].condicion);
                    row.Cells[3].Paragraphs[0].Append(lista[i].unidadAcademica);
                    row.Cells[4].Paragraphs[0].Append(lista[i].correo);
                    row.Cells[5].Paragraphs[0].Append(lista[i].telefonos);
                }

                doc.InsertTable(table);
                doc.Save();
            }
        
            // Descargar el archivo Word
            var filePath = Server.MapPath("~/App_Data/ListaParticipantes.docx");
            var fileName = "Lista_de_Participantes_" + grupo.nombre + ".docx";

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);*/
            return RedirectToAction("VerCalificaciones", "Calificaciones", new { idGrupo = idGrupo });
        }

        [HttpPost]
        public ActionResult ExportarParticipantesExcel(int idGrupo)
        {
            /*// Obtener la lista de participantes del grupo y la información del grupo
            List<ParticipanteModel> lista = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
            GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo);
            StringWriter sw = new StringWriter();
            HtmlTextWriter htw = new HtmlTextWriter(sw);

            // Crear una tabla de datos para los participantes
            DataTable dt = new DataTable("Participantes");
            dt.Columns.AddRange(new DataColumn[6] {
                new DataColumn("Identificación", typeof(string)),
                new DataColumn("Nombre", typeof(string)),
                new DataColumn("Condición", typeof(string)),
                new DataColumn("Unidad académica", typeof(string)),
                new DataColumn("Correo institucional", typeof(string)),
                new DataColumn("Teléfono", typeof(int))
            });

            foreach (var participante in lista)
            {
                dt.Rows.Add(participante.idParticipante, 
                    participante.nombre + " " + participante.apellido_1 + " " + participante.apellido_2, 
                    participante.condicion, 
                    participante.unidadAcademica, 
                    participante.correo, 
                    participante.telefonos);
            }
            
            // Creamos el archivo de Excel
            var grid = new GridView();
            grid.DataSource = dt;
            grid.DataBind();

            // Generamos un nombre de archivo único
            string fileName = "Lista_de_Participantes_" + grupo.nombre + ".xlsx";
            
            // Configurar la respuesta HTTP para descargar el archivo
            Response.ClearContent();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment; filename=" + fileName);
            Response.ContentType = "application/ms-excel";

            // Agregar títulos y encabezados a la respuesta Excel
            htw.WriteLine($"<div>Nombre del módulo: {grupo.nombre}</div>");
            htw.WriteLine($"<div>Nombre del asesor asociado: {grupo.nombreAsesorAsociado}</div><br>");
            grid.RenderControl(htw);
            
            Response.Output.Write(sw.ToString());

            Response.Flush();
            Response.End();*/

            return new EmptyResult();
        }
    }
}