using Microsoft.AspNetCore.Mvc;
using webMetics.Models;
using webMetics.Handlers;

namespace webMetics.Controllers
{
    public class CalificacionesController : Controller
    {
        private InscripcionHandler accesoAInscripcion;
        private GrupoHandler accesoAGrupo;
        private ParticipanteHandler accesoAParticipante;
        private CalificacionesHandler accesoACalificaciones;

        public CalificacionesController()
        {
            accesoAInscripcion = new InscripcionHandler();
            accesoAGrupo = new GrupoHandler();
            accesoAParticipante = new ParticipanteHandler();
            accesoACalificaciones = new CalificacionesHandler();
        }

        public ActionResult VerCalificaciones(int idGrupo)
        {
            List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            ViewBag.ListaCalificaciones = calificaciones;
            ViewBag.IdGrupo = idGrupo;
            GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo);
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
            List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            CalificacionModel calificacion = calificaciones.Find(calificacionModel => calificacionModel.participante.idParticipante == idParticipante);
            GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo);

            ViewBag.Calificacion = calificacion;
            ViewBag.NombreGrupo = grupo.nombre;

            return View();
        }

        [HttpPost]
        public ActionResult SubirCalificacion(int idGrupo, string idParticipante, int calificacion)
        {
            try
            {
                accesoACalificaciones.IngresarNota(idGrupo, idParticipante, calificacion);
            }
            catch
            {
                TempData["errorMessage"] = "Ocurrió un error y no se pudo actualizar la calificación.";
            }

            return RedirectToAction("VerCalificaciones", "Calificaciones", new { idGrupo = idGrupo });
        }

        [HttpPost]
        public ActionResult CargarDesdeExcel(string/*HttpPostedFileBase*/ file, int idGrupo)
        {
            /*if (file != null && file.ContentLength > 0)
            {
                try
                {
                    using (var package = new ExcelPackage(file.InputStream))
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
                                if (worksheet.Cells[row, col].Text == "Identificación") { rowBegin = row; colId = col; }
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
                catch (Exception ex)
                {
                    TempData["errorMessage"] = "Error al cargar los datos desde Excel: " + ex.Message;
                }
            }
            else
            {
                TempData["errorMessage"] = "Por favor, seleccione un archivo de Excel válido.";
            }

            // Redirige a la vista adecuada.*/
            return RedirectToAction("VerCalificaciones", "Calificaciones", new { idGrupo = idGrupo });
        }

        // Método optimizado para exportar la lista de los participantes de un grupo a un archivo PDF
        [HttpPost]
        public ActionResult ExportarCalificacionesPDF(int idGrupo)
        {
            /*// Exportar la lista de calificaciones de un grupo a un archivo PDF

            // Obtener la lista de participantes del grupo y la información del grupo
            List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo);

            ExportarCalificacionesModel datosExportar = new ExportarCalificacionesModel()
            {
                nombreGrupo = grupo.nombre,
                nombreAsesorAsociado = grupo.nombreAsesorAsociado,
                listaCalificaciones = calificaciones
            };

            return new Rotativa.PartialViewAsPdf("ExportarCalificaciones", datosExportar)
            {
                FileName = "Lista_de_Calificaciones_" + grupo.nombre + ".pdf"
            };*/
            return RedirectToAction("VerCalificaciones", "Calificaciones", new { idGrupo = idGrupo });
        }

        [HttpPost]
        public ActionResult ExportarCalificacionesWord(int idGrupo)
        {
            /*// Obtener la lista de participantes del grupo y la información del grupo
            List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo);

            // Crear un nuevo documento Word
            using (var doc = DocX.Create(Server.MapPath("~/App_Data/ListaCalificaciones.docx")))
            {
                // Agregar el contenido al documento Word
                doc.InsertParagraph($"Nombre del módulo: {grupo.nombre}");
                doc.InsertParagraph($"Nombre del asesor asociado: {grupo.nombreAsesorAsociado}\n");
                doc.InsertParagraph("Lista de participantes:");

                var table = doc.AddTable(calificaciones.Count + 1, 3);

                table.Design = TableDesign.TableGrid;
                table.Design = TableDesign.ColorfulList;
                table.AutoFit = AutoFit.ColumnWidth;

                var headerRow = table.Rows[0];
                headerRow.Cells[0].Paragraphs[0].Append("Identificación");
                headerRow.Cells[1].Paragraphs[0].Append("Nombre");
                headerRow.Cells[2].Paragraphs[0].Append("Calificación");

                for (int i = 0; i < calificaciones.Count; i++)
                {
                    var row = table.Rows[i + 1];
                    row.Cells[0].Paragraphs[0].Append(calificaciones[i].participante.idParticipante.ToString());
                    row.Cells[1].Paragraphs[0].Append(calificaciones[i].participante.nombre + " " + calificaciones[i].participante.apellido_1 + " " + calificaciones[i].participante.apellido_2);
                    row.Cells[2].Paragraphs[0].Append(calificaciones[i].calificacion.ToString());
                }

                doc.InsertTable(table);
                doc.Save();
            }

            // Descargar el archivo Word
            var filePath = Server.MapPath("~/App_Data/ListaCalificaciones.docx");
            var fileName = "Lista_de_Calificaciones_" + grupo.nombre + ".docx";

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);*/
            return RedirectToAction("VerCalificaciones", "Calificaciones", new { idGrupo = idGrupo });
        }

        [HttpPost]
        public ActionResult ExportarCalificacionesExcel(int idGrupo)
        {
            /*// Obtener la lista de participantes del grupo y la información del grupo
            List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo);
            StringWriter sw = new StringWriter();
            HtmlTextWriter htw = new HtmlTextWriter(sw);

            // Crear una tabla de datos para los participantes
            DataTable dt = new DataTable("Calificaciones");
            dt.Columns.AddRange(new DataColumn[3] {
                new DataColumn("Identificación", typeof(string)),
                new DataColumn("Nombre", typeof(string)),
                new DataColumn("Calificación", typeof(double))
            });

            foreach (var calificacion in calificaciones)
            {
                dt.Rows.Add(calificacion.participante.idParticipante,
                    calificacion.participante.nombre + " " + calificacion.participante.apellido_1 + " " + calificacion.participante.apellido_2,
                    calificacion.calificacion);
            }

            // Creamos el archivo de Excel
            var grid = new GridView();
            grid.DataSource = dt;
            grid.DataBind();

            // Generamos un nombre de archivo único
            string fileName = "Lista_de_Calificaciones_" + grupo.nombre + ".xlsx";

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

        /* Método para enviar calificación y estado de un grupo */
        private void SendEmail(string grupo, string mensaje, string correoParticipante)
        {
            /*// Configurar el mensaje de correo electrónico con el comprobante de inscripción y el archivo adjunto (si corresponde)
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
                client.Authenticate(from.Address, "pass"); // Cambiar la contraseña real para enviar el correo

                // Enviar el mensaje
                client.Send(message);

                // Desconectar el cliente SMTP
                client.Disconnect(true);
            }*/
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
                "<td style='border:1px solid;'>" + calificacion.participante.nombre + " " + calificacion.participante.apellido_1 + " " + calificacion.participante.apellido_2 + "</td>" +
                "<td style='border:1px solid;'>" + calificacion.participante.idParticipante + "</td>" +
                "<td style='border:1px solid;'>" + grupo.idGrupo+ " " + grupo.nombre + "</td>" +
                "<td style='border:1px solid;'>" + calificacion.calificacion + "</td>" +
                "</tr></tbody></table>" +
                "<h4>Información adicional del grupo:</h4><ul>" +
                "<li>Modalidad: " + grupo.modalidad + "</li> " +
                "<li>Cantidad de horas: " + grupo.cantidadHoras + "</li> " +
                "<li>Asesor: " + grupo.nombreAsesorAsociado + "</li> " +
                "<li>Fecha de inicio: " + grupo.fechaInicioGrupo + "</li>" +
                "<li>Fecha de finalización: " + grupo.fechaFinalizacionGrupo + "</li></ul>";

            return mensaje;
        }

        [HttpPost]
        public ActionResult EnviarCalificaciones(int idGrupo)
        {
            List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo);

            string mensaje;
            try
            {
                foreach (CalificacionModel calificacion in calificaciones)
                {
                    mensaje = ConstructorDelMensaje(grupo, calificacion);
                    SendEmail(grupo.nombre, mensaje, calificacion.participante.correo);
                }

                TempData["successMessage"] = "Las calificaciones fueron enviadas éxitosamente al correo de cada participante.";
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = "Error al enviar las calificaciones: " + ex.Message;
            }

            // Redirige a la vista adecuada.
            return RedirectToAction("VerCalificaciones", "Calificaciones", new { idGrupo = idGrupo });
        }
    }
}