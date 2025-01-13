using webMetics.Handlers;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc;
using NPOI.XSSF.UserModel;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using NPOI.XWPF.UserModel;
using NPOI.SS.UserModel;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using System.Text.RegularExpressions;
using NPOI.SS.Formula.Functions;
using MailKit.Search;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml;
using System.Globalization;
using System.Text;
using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly EmailService _emailService;

        private readonly IMemoryCache _memoryCache;

        public InscripcionController(IWebHostEnvironment environment, IConfiguration configuration, EmailService emailService, IMemoryCache memoryCache)
        {
            _environment = environment;
            _configuration = configuration;
            _emailService = emailService;

            accesoAInscripcion = new InscripcionHandler(environment, configuration);
            accesoAGrupo = new GrupoHandler(environment, configuration);
            accesoAParticipante = new ParticipanteHandler(environment, configuration);
            _memoryCache = memoryCache;
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

        public ActionResult VerInscripciones(bool reload = false)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            List<InscripcionModel> inscripciones;

            // Verificar si los datos están en caché o si reload es true
            if (reload || !_memoryCache.TryGetValue("Inscripciones", out inscripciones))
            {
                // Si no está en caché, obtener los datos de la base de datos
                inscripciones = accesoAInscripcion.ObtenerInscripciones();

                // Si los datos existen, almacenarlos en caché por 30 minutos
                if (inscripciones != null)
                {
                    foreach (InscripcionModel inscripcion in inscripciones)
                    {
                        inscripcion.participante = accesoAParticipante.ObtenerParticipante(inscripcion.idParticipante);
                    }

                    // Almacenar los datos en caché
                    _memoryCache.Set("Inscripciones", inscripciones, TimeSpan.FromMinutes(30));
                }
            }

            // Asignar los datos al ViewBag
            if (inscripciones != null)
            {
                ViewBag.ListaInscripciones = inscripciones;
            }
            else
            {
                ViewBag.ListaInscripciones = null;
            }

            // Manejar los mensajes de TempData
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

        public IActionResult BuscarInscripciones(string searchTerm)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            // Obtener la lista de inscripciones
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones();

            if (inscripciones != null)
            {
                foreach (InscripcionModel inscripcion in inscripciones)
                {
                    inscripcion.participante = accesoAParticipante.ObtenerParticipante(inscripcion.idParticipante);
                }

                ViewBag.ListaInscripciones = inscripciones;
            }

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                inscripciones = inscripciones.Where(inscripcion =>
                    inscripcion.participante.unidadAcademica != null && inscripcion.participante.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.segundoApellido != null && inscripcion.participante.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.nombreGrupo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasMatriculadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasAprobadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.estado != null && inscripcion.estado.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }


            ViewBag.ListaInscripciones = inscripciones;

            return View("VerInscripciones");
        }

        public ActionResult FormularioInscripcion()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            var grupos = accesoAGrupo.ObtenerListaGrupos();

            ViewBag.ListaNombresGrupos = new SelectList(grupos, "nombre", "nombre");
            ViewBag.ListaNumerosGrupos = new SelectList(grupos, "numeroGrupo", "numeroGrupo");

            return View("FormularioInscripcion");
        }

        [HttpPost]
        public ActionResult FormularioInscripcion(InscripcionModel inscripcion)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            if (ModelState.IsValid)
            {
                GrupoModel grupo = accesoAGrupo.ObtenerGrupoPorNombre(inscripcion.nombreGrupo, inscripcion.numeroGrupo);
                inscripcion.idGrupo = grupo.idGrupo;

                try
                {
                    Inscribir(inscripcion.idGrupo, inscripcion.idParticipante);

                    TempData["successMessage"] = "Participante inscrito.";
                }
                catch
                {
                    TempData["errorMessage"] = "Error al inscribir al participante.";
                }

                return RedirectToAction("VerInscripciones", "Inscripcion", new { reload = true });
            }
            else
            {
                return View("FormularioInscripcion", inscripcion);
            }
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
                
                if (grupo != null && participante != null && accesoAInscripcion.NoEstaInscritoEnGrupo(idGrupo, idParticipante))
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
                        accesoAParticipante.ActualizarHorasMatriculadasParticipante(idParticipante);

                        try
                        {
                            string mensaje = ConstructorDelMensajeNotificacionInscripcion(grupo, participante);
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

        /* Método para que un administrador elimine una inscripción de un usuario */
        public ActionResult EliminarInscripcion(string nombreGrupo, int numeroGrupo, string idParticipante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                bool exito = accesoAInscripcion.EliminarInscripcion(nombreGrupo, numeroGrupo, idParticipante);

                if (exito)
                {
                    ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

                    accesoAParticipante.ActualizarHorasMatriculadasParticipante(participante.idParticipante);
                    accesoAParticipante.ActualizarHorasAprobadasParticipante(participante.idParticipante);

                    TempData["successMessage"] = "Se eliminó la inscripción del participante.";

                    return RedirectToAction("VerInscripciones", "Inscripcion", new { reload = true });

                }
                else
                {
                    TempData["errorMessage"] = "No se pudo eliminar la inscripción del participante.";
                }
            }
            catch (Exception)
            {
                // Si ocurrió una excepción al intentar eliminar la inscripción, mostrar un mensaje y redirigir a la lista de participantes del grupo
                TempData["errorMessage"] = "No se pudo eliminar la inscripción del participante.";
            }

            var refererUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(refererUrl))
            {
                return Redirect(refererUrl);
            }

            return RedirectToAction("ListaGruposDisponibles", "Grupo");
        }

        // TODO: Eliminar esto porque está repetido
        public ActionResult DesinscribirParticipante(string idParticipante, int idGrupo)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
                bool exito = accesoAInscripcion.EliminarInscripcion(grupo.nombre, grupo.numeroGrupo, idParticipante);

                if (exito)
                {
                    ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

                    accesoAParticipante.ActualizarHorasMatriculadasParticipante(participante.idParticipante);
                    accesoAParticipante.ActualizarHorasAprobadasParticipante(participante.idParticipante);
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

        // Método para enviar confirmación de inscripción al usuario
        private async Task<IActionResult> EnviarCorreoInscripcion(GrupoModel grupo, string mensaje, string correoParticipante)
        {
            string subject = "Comprobante de Inscripción a Módulo - SISTEMA DE INSCRIPCIONES METICS";

            byte[] archivoAdjunto = accesoAGrupo.ObtenerArchivo(grupo.idGrupo);
            if (archivoAdjunto != null)
            {
                await _emailService.SendEmailAsync(correoParticipante, subject, mensaje, archivoAdjunto, grupo.nombreArchivo);
            }
            else
            {
                // Si no hay archivo adjunto, solo agregar el cuerpo del mensaje al mensaje principal
                await _emailService.SendEmailAsync(correoParticipante, subject, mensaje);
            }
         
            return Ok();
        }

        // Método del constructor del mensaje del correo que será enviado al usuario con los datos de la inscripción
        public string ConstructorDelMensajeNotificacionInscripcion(GrupoModel grupo, ParticipanteModel participante)
        {
            // Construir el contenido del mensaje que se enviará por correo electrónico al usuario
            // Este método toma información del módulo y el participante y crea un mensaje personalizado
            // con los detalles de la inscripción

            // Crear el mensaje con información relevante de la inscripción en formato HTML
            string mensaje = "" +
                "<h2>Comprobante de inscripción a módulo - SISTEMA DE INSCRIPCIONES METICS</h2>" +
                "<p>Nombre: " + participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido + "</p>" +
                "<p>Se ha inscrito al módulo: <strong>" + grupo.nombre + " (Grupo " + grupo.numeroGrupo + ")</strong></p>" +

                "<ul>" +
                "<li>Descripción: " + grupo.descripcion + "</li>" +
                "<br>" +
                "<li>Enlace: " + grupo.enlace + "</li>" +
                "<li>Clave de inscripción: " + grupo.claveInscripcion + "</li>" +
                "<br>" +
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

        public ActionResult DescargarPlantillaSubirInscripciones()
        {
            XSSFWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Plantilla_Inscripciones");

            string[] columnNames = {
                "Correo Institucional", "Módulo", "Grupo", "Horas", "Estado", "Horas Completadas"
            };

            NPOI.SS.UserModel.IRow row = sheet.CreateRow(0);

            for (int i = 0; i < columnNames.Length; i++)
            {
                NPOI.SS.UserModel.ICell cell = row.CreateCell(i);

                cell.SetCellValue(columnNames[i]);
            }

            string fileName = "Plantilla_Inscripciones.xlsx";
            var stream = new MemoryStream();
            workbook.Write(stream);
            var file = stream.ToArray();

            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost]
        public async Task<IActionResult> SubirArchivoExcelInscripciones(IFormFile file)
        {
            if (file == null)
            {
                TempData["errorMessage"] = "Seleccione un archivo de Excel válido.";
                return RedirectToAction("VerInscripciones");
            }

            try
            {
                List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones();

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using var package = new ExcelPackage(stream);
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        GrupoModel grupo = new GrupoModel
                        {
                            numeroGrupo = int.TryParse(worksheet.Cells[row, GetColumnIndex(worksheet, "Grupo")].Text, out var numeroGrupo) ? numeroGrupo : 0,
                            nombre = worksheet.Cells[row, GetColumnIndex(worksheet, "Módulo")].Text,
                            modalidad = "",
                            cantidadHoras = int.TryParse(worksheet.Cells[row, GetColumnIndex(worksheet, "Horas")].Text, out var cantidadHoras) ? cantidadHoras : 0,
                        };

                        InscripcionModel inscripcion = new InscripcionModel
                        {
                            idParticipante = worksheet.Cells[row, GetColumnIndex(worksheet, "Correo institucional")].Text,
                            numeroGrupo = grupo.numeroGrupo,
                            nombreGrupo = grupo.nombre,
                            horasMatriculadas = grupo.cantidadHoras,
                            horasAprobadas = int.TryParse(worksheet.Cells[row, GetColumnIndex(worksheet, "Horas completadas")].Text, out var horasCompletadas) ? horasCompletadas : 0,
                            estado = worksheet.Cells[row, GetColumnIndex(worksheet, "Estado")].Text
                        };

                        ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(inscripcion.idParticipante);

                        if (!string.IsNullOrEmpty(inscripcion.idParticipante))
                        {
                            inscripciones = await IngresarInscripcionAsync(inscripciones, inscripcion, grupo, participante);
                        }
                    }
                }

                TempData["successMessage"] = "El archivo fue subido éxitosamente.";
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = "Error al cargar los datos: " + ex;
            }

            return RedirectToAction("VerInscripciones", "Inscripcion", new { reload = true });
        }

        private async Task<List<InscripcionModel>> IngresarInscripcionAsync(List<InscripcionModel> inscripciones, InscripcionModel inscripcion, GrupoModel grupo, ParticipanteModel participante)
        {
            if (grupo != null && participante != null)
            {
                bool exito = false;

                InscripcionModel inscripcionEncontrada = inscripciones.Find(
                    inscripcionModel => inscripcionModel.nombreGrupo == inscripcion.nombreGrupo
                                         && inscripcionModel.numeroGrupo == inscripcion.numeroGrupo
                                         && inscripcionModel.idParticipante == inscripcion.idParticipante);

                if (inscripcionEncontrada != null)
                {
                    inscripcion.idInscripcion = inscripcionEncontrada.idInscripcion;
                    exito = await accesoAInscripcion.EditarInscripcionAsync(inscripcion);

                    inscripciones.Remove(inscripcionEncontrada);
                }
                else 
                {
                    exito = await accesoAInscripcion.InsertarInscripcionAsync(inscripcion);
                }

                if (exito)
                {
                    await accesoAParticipante.ActualizarHorasMatriculadasParticipanteAsync(participante.idParticipante);
                    await accesoAParticipante.ActualizarHorasAprobadasParticipanteAsync(participante.idParticipante);
                }
            }

            return inscripciones;
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

        // Método optimizado para exportar la lista de los participantes de un grupo a un archivo PDF
        public ActionResult ExportarParticipantesPDF(int idGrupo, string? searchTerm)
        {
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                inscripciones = inscripciones.Where(inscripcion =>
                    inscripcion.participante.unidadAcademica != null && inscripcion.participante.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.segundoApellido != null && inscripcion.participante.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.nombreGrupo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasMatriculadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasAprobadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.estado != null && inscripcion.estado.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

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

        public ActionResult ExportarParticipantesWord(int idGrupo, string? searchTerm)
        {
            // Obtener la lista de participantes del grupo y la información del grupo
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                inscripciones = inscripciones.Where(inscripcion =>
                    inscripcion.participante.unidadAcademica != null && inscripcion.participante.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.segundoApellido != null && inscripcion.participante.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.nombreGrupo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasMatriculadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasAprobadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.estado != null && inscripcion.estado.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

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

        public ActionResult ExportarParticipantesExcel(int idGrupo, string? searchTerm)
        {
            // Obtener la lista de participantes del grupo y la información del grupo
            GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                inscripciones = inscripciones.Where(inscripcion =>
                    inscripcion.participante.unidadAcademica != null && inscripcion.participante.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.segundoApellido != null && inscripcion.participante.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.participante.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.nombreGrupo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasMatriculadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.horasAprobadas.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    inscripcion.estado != null && inscripcion.estado.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

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


        /*
         
         
         */

        // Método optimizado para exportar la lista de los participantes de un grupo a un archivo PDF
        public ActionResult ExportarTodosParticipantesPDF(string? searchTerm)
        {
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones();

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            var filePath = System.IO.Path.Combine(_environment.WebRootPath, "data", "Lista_de_Inscripciones.docx");
            PdfWriter writer = new PdfWriter(filePath);
            PdfDocument pdf = new PdfDocument(writer);

            PageSize pageSize = PageSize.A2;  // Puedes elegir PageSize.A3 para un tamaño más pequeño
            iText.Layout.Document document = new iText.Layout.Document(pdf, pageSize);

            // Establecer fuente en negrita para encabezado
            PdfFont boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
            PdfFont regularFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

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
            string fileName = "Lista_de_Inscripciones.pdf";
            return File(System.IO.File.ReadAllBytes(filePath), "application/pdf", fileName);
        }

        public ActionResult ExportarTodosParticipantesWord(string? searchTerm)
        {
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones();

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            var fileName = "Lista_de_Inscripciones.docx";

            XWPFDocument wordDoc = new XWPFDocument();

            // Crear un título
            XWPFParagraph titleParagraph = wordDoc.CreateParagraph();
            titleParagraph.Alignment = ParagraphAlignment.CENTER;
            XWPFRun titleRun = titleParagraph.CreateRun();
            titleRun.SetText("Lista de Inscripciones");
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
                        row.GetCell(5).SetText(inscripcion.nombreGrupo);
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

        public ActionResult ExportarTodosParticipantesExcel(string? searchTerm)
        {
            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones();

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                participantes = participantes.Where(p =>
                    p.unidadAcademica != null && p.unidadAcademica.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.primerApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.segundoApellido != null && p.segundoApellido.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.correo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            XSSFWorkbook workbook = new XSSFWorkbook();

            string sheetName = "Inscripciones";
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
                        row.CreateCell(5).SetCellValue(inscripcion.nombreGrupo); // Nombre del módulo
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
            string fileName = "Lista_de_Inscripciones.xlsx";
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