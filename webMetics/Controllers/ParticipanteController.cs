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
using Microsoft.EntityFrameworkCore;


namespace webMetics.Controllers
{
    /// <summary>
    /// Controlador para la entidad Participante (funcionario UCR).
    /// Gestiona la visualización, búsqueda, alta, edición y eliminación de participantes,
    /// la asignación de medallas, importación masiva desde Excel, exportaciones en PDF/Word/Excel
    /// y la recuperación de contraseña.
    /// </summary>
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

        /// <summary>
        /// Muestra la lista de participantes inscritos en un grupo específico.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <returns>
        /// View: ListaParticipantes —
        /// ViewBag.IdGrupo, ViewBag.NombreGrupo, ViewBag.NumeroGrupo,
        /// ViewBag.ListaParticipantes (List&lt;ParticipanteModel&gt;),
        /// ViewBag.Inscripciones, ViewBag.TodasLasMedallas,
        /// ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// Redirects to Grupo/ListaGruposDisponibles on exception.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, ParticipanteHandler, InscripcionHandler.
        /// Role required: Admin (1) o Asesor (2).
        /// </remarks>
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
                ViewBag.TodasLasMedallas = accesoAParticipante.ObtenerTodasMedallas();


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

        /// <summary>
        /// Muestra todos los participantes del sistema con sus horas matriculadas y aprobadas,
        /// incluyendo los grupos en los que cada uno está inscrito.
        /// </summary>
        /// <returns>
        /// View: VerParticipantes —
        /// ViewBag.ListaParticipantes (List&lt;ParticipanteModel&gt; con gruposInscritos cargados),
        /// ViewBag.Role, ViewBag.Id,
        /// ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, GrupoHandler.
        /// Role required: Admin (1).
        /// </remarks>
        public ActionResult VerParticipantes()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();

            if (participantes != null)
            {
                var areasExtraMap = accesoAParticipante.GetAreasExtraParticipantes();

                foreach (ParticipanteModel participante in participantes)
                {
                    string idParticipante = participante.idParticipante;
                    participante.gruposInscritos = accesoAGrupo.ObtenerListaGruposParticipante(idParticipante);
                    participante.areasExtra = areasExtraMap.TryGetValue(idParticipante, out var areas)
                        ? areas
                        : new List<string>();
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

        /// <summary>
        /// Filtra y muestra participantes cuyo nombre, apellidos, correo, unidad académica u horas
        /// contengan el término de búsqueda. Reutiliza la vista VerParticipantes.
        /// </summary>
        /// <param name="searchTerm">Texto libre para filtrar participantes.</param>
        /// <returns>
        /// View: VerParticipantes —
        /// ViewBag.ListaParticipantes (filtrada), ViewBag.Role, ViewBag.Id.
        /// </returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler.
        /// Role required: Admin (1).
        /// </remarks>
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

            if (participantes != null && participantes.Count > 0)
            {
                var areasExtraMap = accesoAParticipante.GetAreasExtraParticipantes();

                foreach (var participante in participantes)
                {
                    participante.areasExtra = areasExtraMap.TryGetValue(participante.idParticipante, out var areas)
                        ? areas
                        : new List<string>();
                }
            }

            ViewBag.ListaParticipantes = participantes;

            return View("VerParticipantes");
        }

        /// <summary>
        /// Filtra los participantes inscritos en un grupo por nombre, apellidos, correo o unidad académica.
        /// Reutiliza la vista ListaParticipantes.
        /// </summary>
        /// <param name="idGrupo">Identificador del grupo.</param>
        /// <param name="searchTerm">Texto libre para filtrar participantes del grupo.</param>
        /// <returns>
        /// View: ListaParticipantes —
        /// ViewBag.IdGrupo, ViewBag.NombreGrupo, ViewBag.NumeroGrupo,
        /// ViewBag.ListaParticipantes (filtrada), ViewBag.Inscripciones, ViewBag.TodasLasMedallas,
        /// ViewBag.Role, ViewBag.Id, ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// Redirects to Grupo/ListaGruposDisponibles on exception.
        /// </returns>
        /// <remarks>
        /// Handlers: GrupoHandler, ParticipanteHandler, InscripcionHandler.
        /// Role required: Admin (1) o Asesor (2).
        /// </remarks>
        public IActionResult BuscarParticipantesDelGrupo(int idGrupo, string searchTerm)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                GrupoModel grupo = accesoAGrupo.ObtenerGrupo(idGrupo);

                ViewBag.IdGrupo = idGrupo;
                ViewBag.NombreGrupo = grupo.nombre;
                ViewBag.NumeroGrupo = grupo.numeroGrupo;

                List<ParticipanteModel> participantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo);
                ViewBag.Inscripciones = accesoAInscripcion.ObtenerInscripcionesDelGrupo(idGrupo);
                ViewBag.TodasLasMedallas = accesoAParticipante.ObtenerTodasMedallas();

                // Filtrar por término de búsqueda
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

                ViewBag.ListaParticipantes = participantes;

                if (TempData["errorMessage"] != null)
                {
                    ViewBag.ErrorMessage = TempData["errorMessage"].ToString();
                }
                if (TempData["successMessage"] != null)
                {
                    ViewBag.SuccessMessage = TempData["successMessage"].ToString();
                }

                return View("ListaParticipantes");
            }
            catch
            {
                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }
        }


        /// <summary>
        /// Asigna una o varias medallas a un participante específico.
        /// Solo agrega las medallas que el participante aún no posee.
        /// </summary>
        /// <param name="idParticipante">Identificador del participante.</param>
        /// <param name="selectedMedallas">Lista de nombres de medallas a asignar.</param>
        /// <returns>
        /// Redirects to VerDatosParticipante. Sets TempData["successMessage"] or TempData["errorMessage"].
        /// Redirects to VerDatosParticipante with TempData["errorMessage"] si el rol no es Admin (1).
        /// </returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler.
        /// Role required: Admin (1).
        /// </remarks>
        [HttpPost]
        public IActionResult AsignarMedallas(string idParticipante, List<string> selectedMedallas)
        {
            // Validar que solo administradores puedan asignar medallas
            if (GetRole() != 1)  // Si no es administrador
            {
                TempData["errorMessage"] = "No tiene permisos para asignar medallas.";
                return RedirectToAction("VerDatosParticipante", new { idParticipante });
            }
            try
            {
                var participante = accesoAParticipante.ObtenerParticipante(idParticipante);

                if (participante == null)
                {
                    ViewBag.ErrorMessage = "Participante no encontrado.";
                    return RedirectToAction("VerDatosParticipante", "Participante", new { idParticipante });
                }

                if (selectedMedallas != null && selectedMedallas.Any())
                {
                    foreach (var medalla in selectedMedallas)
                    {
                        if (!accesoAParticipante.ParticipanteTieneMedalla(idParticipante, medalla)) {
                            accesoAParticipante.AgregarMedallaParticipante(idParticipante, medalla);
                        }
                    }
                }

                TempData["successMessage"] = "Medallas asignadas correctamente.";
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = $"Error al asignar medallas.";
            }

            return RedirectToAction("VerDatosParticipante", new { idParticipante = idParticipante });
        }

        /// <summary>
        /// Asigna una medalla a múltiples participantes a la vez desde la lista o una vista de grupo.
        /// </summary>
        /// <param name="nombreMedalla">Nombre de la medalla a asignar.</param>
        /// <param name="participantesSeleccionados">Lista de IDs de participantes que recibirán la medalla.</param>
        /// <returns>
        /// Redirects to la URL referente (Referer) si existe; de lo contrario a Grupo/ListaGruposDisponibles.
        /// Sets TempData["successMessage"] o TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler.
        /// Role required: Admin (1).
        /// </remarks>
        [HttpPost]
        public IActionResult AsignarMedallaMasiva(string nombreMedalla, List<string> participantesSeleccionados)
        {
            // Validar que solo administradores puedan asignar medallas
            if (GetRole() != 1)  // Si no es administrador
            {
                TempData["errorMessage"] = "No tiene permisos para asignar medallas.";
            }
            try
            {
                if (participantesSeleccionados != null && participantesSeleccionados.Any() && !string.IsNullOrEmpty(nombreMedalla))
                {
                    accesoAParticipante.AsignarMedallaAParticipantes(nombreMedalla, participantesSeleccionados);
                    TempData["successMessage"] = $"Se asignó la medalla '{nombreMedalla}' a {participantesSeleccionados.Count} participantes.";
                }
                else
                {
                    TempData["errorMessage"] = "Seleccione al menos un participante y una medalla.";
                }
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = $"Ocurrió un error al asignar las medallas: {ex.Message}";
            }

            var refererUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(refererUrl))
            {
                return Redirect(refererUrl);
            }

            return RedirectToAction("ListaGruposDisponibles", "Grupo");
        }

        /// <summary>
        /// Importa participantes desde un archivo Excel. Por cada fila crea un usuario y un participante
        /// si aún no existen, enviando la contraseña generada por correo.
        /// </summary>
        /// <param name="file">Archivo Excel (.xlsx) con columnas: Unidad Académica, Nombre, Primer Apellido,
        /// Segundo Apellido, Correo Institucional, Horas Aprobadas.</param>
        /// <returns>
        /// Redirects to VerParticipantes. Sets TempData["successMessage"] or TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: UsuarioHandler, ParticipanteHandler (vía IngresarParticipante).
        /// Role required: Admin (1).
        /// </remarks>
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



        /// <summary>
        /// Envía un correo de notificación al responsable de METICS cuando el participante supera 30 horas aprobadas
        /// y marca el envío en la base de datos.
        /// </summary>
        /// <param name="idParticipante">Correo institucional del participante.</param>
        /// <returns>Redirects to VerParticipantes.</returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, InscripcionHandler (para obtener correo destino).
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>Envía un correo al responsable cuando un participante supera el límite de 30 horas aprobadas.</summary>
        private async Task<IActionResult> EnviarCorreoNotificacion(string idParticipante)
        {
            string subject = "Notificación Límite de Horas Aprobadas - SISTEMA DE INSCRIPCIONES METICS";
            string message = $"El usuario con correo institucional {idParticipante} ha superado las 30 horas aprobadas en el SISTEMA DE COMPETENCIAS DIGITALES PARA LA DOCENCIA - METICS.";
            string receiver = accesoAInscripcion.ObtenerCorreoLimiteHoras() ?? "soporte.metics@ucr.ac.cr";

            await _emailService.SendEmailAsync(receiver, subject, message);
            return Ok();
        }

        /// <summary>
        /// Genera y descarga un PDF en formato A2 con la lista de participantes y sus módulos inscritos.
        /// Aplica el mismo filtro de búsqueda de texto que VerParticipantes.
        /// </summary>
        /// <param name="searchTerm">Texto opcional para filtrar participantes antes de exportar.</param>
        /// <returns>FileResult (application/pdf) — "Lista_de_Participantes_Módulos.pdf".</returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, InscripcionHandler.
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Genera y descarga un documento Word (.docx) con la lista de participantes y sus módulos inscritos.
        /// </summary>
        /// <param name="searchTerm">Texto opcional para filtrar participantes antes de exportar.</param>
        /// <returns>FileResult (application/vnd.openxmlformats-officedocument.wordprocessingml.document) — "Lista_de_Participantes_Módulos.docx".</returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, InscripcionHandler.
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Genera y descarga un Excel (.xlsx) con la lista completa de participantes y todos sus módulos inscritos
        /// (columnas: identificación, nombre, correo, condición, unidad académica, teléfono, módulo, horas, calificación).
        /// </summary>
        /// <param name="searchTerm">Texto opcional para filtrar participantes antes de exportar.</param>
        /// <returns>FileResult (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet) — "Lista_de_Participantes_Módulos.xlsx".</returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, InscripcionHandler.
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Genera y descarga un Excel (.xlsx) simplificado con totales de horas por participante
        /// (columnas: unidad académica, nombre, apellidos, correo, total horas inscritas, total horas aprobadas).
        /// Soporta filtrado opcional igual que ExportarParticipantesExcel.
        /// </summary>
        /// <param name="searchTerm">Texto opcional para filtrar participantes antes de exportar.</param>
        /// <returns>FileResult (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet) — "Lista_de_Participantes_Módulos.xlsx".</returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, InscripcionHandler.
        /// Role required: Admin (1).
        /// </remarks>
        public ActionResult ExportarParticipantesExcel2(string? searchTerm)
        {
            try
            {
                // Optimized Database Query (if possible)
                List<ParticipanteModel> participantes;
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    participantes = accesoAParticipante.ObtenerListaParticipantesFiltrada(searchTerm); // Assuming this method exists
                }
                else
                {
                    participantes = accesoAParticipante.ObtenerListaParticipantes();
                }

                // Optimized Inscripciones retrieval if needed.
                List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripciones();

                // Excel Workbook Setup
                XSSFWorkbook workbook = new XSSFWorkbook();
                var sheet = workbook.CreateSheet("Lista_de_Participantes_Módulos");

                // Header Style
                ICellStyle headerStyle = workbook.CreateCellStyle();
                headerStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                headerStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                headerStyle.BorderBottom = BorderStyle.Thin;
                headerStyle.BorderTop = BorderStyle.Thin;
                headerStyle.BorderLeft = BorderStyle.Thin;
                headerStyle.BorderRight = BorderStyle.Thin;
                IFont headerFont = workbook.CreateFont();
                headerStyle.SetFont(headerFont);

                // Body Style
                ICellStyle bodyStyle = workbook.CreateCellStyle();
                bodyStyle.BorderBottom = BorderStyle.Thin;
                bodyStyle.BorderTop = BorderStyle.Thin;
                bodyStyle.BorderLeft = BorderStyle.Thin;
                bodyStyle.BorderRight = BorderStyle.Thin;

                // Headers
                string[] headers = { "Unidad Académica", "Nombre", "Primer Apellido", "Segundo Apellido", "Correo Institucional", "Total Horas Inscritas", "Total Horas Aprobadas" };
                IRow headerRow = sheet.CreateRow(3);

                for (int i = 0; i < headers.Length; i++)
                {
                    NPOI.SS.UserModel.ICell cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                // Populate Data
                int rowNumber = 4;
                foreach (var participante in participantes)
                {
                    IRow dataRow = sheet.CreateRow(rowNumber++);
                    dataRow.CreateCell(0).SetCellValue(participante.unidadAcademica);
                    dataRow.CreateCell(1).SetCellValue(participante.nombre);
                    dataRow.CreateCell(2).SetCellValue(participante.primerApellido);
                    dataRow.CreateCell(3).SetCellValue(participante.segundoApellido);
                    dataRow.CreateCell(4).SetCellValue(participante.correo); // changed from idParticipante to correo
                    dataRow.CreateCell(5).SetCellValue(participante.horasMatriculadas);
                    dataRow.CreateCell(6).SetCellValue(participante.horasAprobadas);

                    for (int i = 0; i < headers.Length; i++)
                    {
                        dataRow.GetCell(i).CellStyle = bodyStyle;
                    }
                }

                // Auto-size columns after all rows are added
                for (int i = 0; i < headers.Length; i++)
                {
                    sheet.AutoSizeColumn(i);
                }

                // Create and return the Excel file
                string fileName = "Lista_de_Participantes_Módulos.xlsx";
                using (var stream = new MemoryStream())
                {
                    workbook.Write(stream);
                    var file = stream.ToArray();
                    return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                // Basic error handling (log or return an error view)
                Console.WriteLine($"Error exporting Excel: {ex.Message}");
                return Content("An error occurred while exporting the Excel file.");
            }
        }

        /// <summary>
        /// Genera y descarga la plantilla Excel (.xlsx) para la importación masiva de participantes.
        /// Columnas: Unidad Académica, Nombre, Primer Apellido, Segundo Apellido, Correo Institucional, Horas Aprobadas.
        /// </summary>
        /// <returns>FileResult (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet) — "Plantilla_Lista_Participantes.xlsx".</returns>
        /// <remarks>
        /// Handlers: ninguno.
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Muestra el perfil completo de un participante: datos personales, inscripciones y medallas.
        /// Para asesores (rol 2) solo muestra las inscripciones en grupos que el asesor imparte.
        /// </summary>
        /// <param name="idParticipante">Correo institucional del participante.</param>
        /// <param name="idGrupo">Opcional. Si se provee, se guarda en ViewBag.IdGrupo para el botón "Volver al grupo".</param>
        /// <returns>
        /// View: VerDatosParticipante —
        /// ViewBag.Participante (ParticipanteModel), ViewBag.Inscripciones, ViewBag.Medallas,
        /// ViewBag.TodasMedallas, ViewBag.IdGrupo (opcional),
        /// ViewBag.Role, ViewBag.Id, ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, InscripcionHandler, GrupoHandler (solo rol 2).
        /// Role required: Admin (1) o Asesor (2).
        /// </remarks>
        public ActionResult VerDatosParticipante(string idParticipante, int? idGrupo)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);
            List<InscripcionModel> inscripciones = accesoAInscripcion.ObtenerInscripcionesParticipante(idParticipante);

            ViewBag.Participante = participante;
            ViewBag.Inscripciones = inscripciones;
            ViewBag.Medallas = accesoAParticipante.ObtenerMedallas(idParticipante);
            ViewBag.TodasMedallas = accesoAParticipante.ObtenerTodasMedallas();

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

            // Si se pasa un idGrupo, se guarda en ViewBag para usarlo en la vista cuando se regresa al grupo
            if (idGrupo != null)
            {
                ViewBag.IdGrupo = idGrupo;
            }

            return View();
        }

        /// <summary>
        /// Muestra el formulario vacío para agregar manualmente un nuevo participante.
        /// </summary>
        /// <returns>
        /// View: FormularioParticipante —
        /// ViewData["jsonDataAreas"] (JSON de la jerarquía de áreas UCR),
        /// ViewBag.Id, ViewBag.Role.
        /// </returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler.
        /// Role required: Admin (1).
        /// </remarks>
        public ActionResult FormularioParticipante()
        {
            ViewBag.Id = GetId();
            ViewBag.Role = GetRole();

            try
            {
                ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
                return View("FormularioParticipante");
            }
            catch
            {
                TempData["errorMessage"] = "Error al cargar el formulario de participante.";
                return RedirectToAction("VerParticipantes");
            }
        }

        /// <summary>
        /// Procesa el formulario de alta manual de un participante.
        /// Crea usuario y participante si no existen, enviando contraseña por correo.
        /// </summary>
        /// <param name="participante">Modelo con los datos del nuevo participante.</param>
        /// <returns>
        /// Redirects to VerParticipantes on success or error; sets TempData["successMessage"] o TempData["errorMessage"].
        /// View: FormularioParticipante con errores de validación si ModelState es inválido.
        /// </returns>
        /// <remarks>
        /// Handlers: UsuarioHandler, ParticipanteHandler (vía IngresarParticipante).
        /// Role required: Admin (1).
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FormularioParticipante(ParticipanteModel participante)
        {
            ViewBag.Id = GetId();
            ViewBag.Role = GetRole();
            bool isAjaxRequest = IsAjaxRequest();

            ValidarAreasExtra(participante);

            if (!ModelState.IsValid)
            {
                if (isAjaxRequest)
                {
                    return BadRequest(BuildAjaxValidationErrorResponse());
                }

                ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
                return View("FormularioParticipante", participante);
            }

            participante.idParticipante = participante.correo;

            try
            {
                bool exito = IngresarParticipante(participante);

                if (isAjaxRequest)
                {
                    if (exito)
                    {
                        return Json(new
                        {
                            success = true,
                            redirectUrl = Url.Action("VerParticipantes", "Participante")
                        });
                    }

                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Participante creado, pero ocurrió un error al guardar las áreas extra."
                    });
                }

                if (exito)
                {
                    TempData["successMessage"] = "Participante agregado.";
                }
                else
                {
                    TempData["errorMessage"] = "Participante creado, pero ocurrió un error al guardar las áreas extra.";
                }
            }
            catch
            {
                if (isAjaxRequest)
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Error al agregar al participante."
                    });
                }

                TempData["errorMessage"] = "Error al agregar al participante.";
            }

            return RedirectToAction("VerParticipantes");
        }

        /// <summary>
        /// Registra un nuevo participante en el sistema.
        /// Si el usuario no existe, crea una cuenta con una contraseña generada aleatoriamente
        /// y la envía por correo. Si el participante no existe, lo crea.
        /// </summary>
        /// <param name="participante">Modelo con los datos del participante a registrar.</param>
        private bool IngresarParticipante(ParticipanteModel participante)
        {
            if (!accesoAUsuario.ExisteUsuario(participante.idParticipante))
            {
                string contrasena = GenerateRandomPassword();

                accesoAUsuario.CrearUsuario(
                    participante.idParticipante,
                    contrasena,
                    0,
                    participante.correoAlternativo);

                EnviarContrasenaPorCorreo(participante.idParticipante, contrasena);
            }

            if (!accesoAParticipante.ExisteParticipante(participante.idParticipante))
            {
                bool participanteCreado = accesoAParticipante.CrearParticipante(participante);

                if (participanteCreado)
                {
                    List<string> areasExtra = FiltrarAreasExtraValidas(participante.areasExtra, participante.area);
                    return accesoAParticipante.GuardarAreasExtraParticipante(participante.idParticipante, areasExtra);
                }

                return false;
            }

            return true;
        }

        private void ValidarAreasExtra(ParticipanteModel participante)
        {
            if (participante.areasExtra == null || participante.areasExtra.Count == 0)
            {
                return;
            }

            var areasValidas = new HashSet<string>(accesoAParticipante.GetAllAreas(), StringComparer.OrdinalIgnoreCase);

            bool contieneInvalida = participante.areasExtra
                .Any(area => !areasValidas.Contains(area));

            if (contieneInvalida)
            {
                ModelState.AddModelError(nameof(ParticipanteModel.areasExtra), "Se detectaron áreas extra inválidas.");
            }
            }

        private List<string> FiltrarAreasExtraValidas(List<string>? areasExtraSeleccionadas, string? areaPrincipal)
        {
            if (areasExtraSeleccionadas == null || areasExtraSeleccionadas.Count == 0)
            {
                return new List<string>();
        }

            var areasValidas = new HashSet<string>(accesoAParticipante.GetAllAreas(), StringComparer.OrdinalIgnoreCase);

            return areasExtraSeleccionadas
                .Where(area => !string.IsNullOrWhiteSpace(area))
                .Where(area => areasValidas.Contains(area))
                .Where(area => !string.Equals(area, areaPrincipal, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }


        /// <summary>
        /// Muestra el formulario de edición de un participante con sus datos actuales
        /// y la jerarquía de áreas/departamentos/secciones preseleccionada.
        /// </summary>
        /// <param name="idParticipante">Correo institucional del participante a editar.</param>
        /// <returns>
        /// View: EditarParticipante con el modelo ParticipanteModel —
        /// ViewData["jsonDataAreas"], ViewData["jsonDataDepartamentos"], ViewData["jsonDataUnidadesAcademicas"],
        /// ViewBag.Role, ViewBag.Id.
        /// Redirects to VerParticipantes on error; sets TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler.
        /// Role required: Admin (1).
        /// </remarks>
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
                ViewData["jsonDataUnidadesAcademicas"] = accesoAParticipante.GetSeccionesByDepartamento(participante.area, participante.departamento);

                return View(participante);
            }
            catch
            {
                TempData["errorMessage"] = "Ocurrió un error al obtener los datos solicitados.";
                return RedirectToAction("VerParticipantes");
            }
        }

        /// <summary>
        /// Persiste la edición de los datos de un participante. Si el participante también es asesor,
        /// sincroniza los datos en la tabla de asesores.
        /// </summary>
        /// <param name="participante">Modelo con los datos actualizados del participante.</param>
        /// <returns>
        /// Redirects to VerParticipantes on success; sets TempData["successMessage"].
        /// View: EditarParticipante con errores si ModelState es inválido.
        /// Redirects to VerParticipantes on exception; sets TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, AsesorHandler.
        /// Role required: Admin (1).
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarParticipante(ParticipanteModel participante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();
            bool isAjaxRequest = IsAjaxRequest();

            ValidarAreasExtra(participante);

            try
            {
                if (!ModelState.IsValid)
                {
                    if (isAjaxRequest)
                    {
                        return BadRequest(BuildAjaxValidationErrorResponse());
                    }

                    ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
                    ViewData["jsonDataDepartamentos"] = accesoAParticipante.GetDepartamentosByArea(participante.area);
                    ViewData["jsonDataUnidadesAcademicas"] = accesoAParticipante.GetSeccionesByDepartamento(participante.area, participante.departamento);

                    return View("EditarParticipante", participante);
                }

                participante.idParticipante = participante.correo;
                bool participanteEditado = accesoAParticipante.EditarParticipante(participante);
                if (!participanteEditado)
                {
                    throw new Exception("No se pudo actualizar la información del participante.");
                }

                List<string> areasExtra = FiltrarAreasExtraValidas(participante.areasExtra, participante.area);
                bool areasExtraGuardadas = accesoAParticipante.GuardarAreasExtraParticipante(participante.idParticipante, areasExtra);

                AsesorModel asesorAsociado = accesoAAsesor.ObtenerAsesor(participante.idParticipante);
                if (asesorAsociado != null)
                {
                    asesorAsociado.nombre = participante.nombre;
                    asesorAsociado.primerApellido = participante.primerApellido;
                    asesorAsociado.segundoApellido = participante.segundoApellido;
                    asesorAsociado.correo = participante.correo;
                    asesorAsociado.correoAlternativo = participante.correoAlternativo;
                    asesorAsociado.tipoIdentificacion = participante.tipoIdentificacion;
                    asesorAsociado.numeroIdentificacion = participante.numeroIdentificacion;
                    asesorAsociado.telefono = participante.telefono;

                    accesoAAsesor.EditarAsesor(asesorAsociado);
                }

                if (isAjaxRequest)
                {
                    if (!areasExtraGuardadas)
                    {
                        return StatusCode(500, new
                        {
                            success = false,
                            message = "Los datos se actualizaron, pero ocurrió un error al guardar las áreas extra."
                        });
                    }

                    return Json(new
                    {
                        success = true,
                        redirectUrl = Url.Action("VerParticipantes", "Participante")
                    });
                }

                if (areasExtraGuardadas)
                {
                    TempData["successMessage"] = "Los datos fueron guardados.";
                }
                else
                {
                    TempData["errorMessage"] = "Los datos se actualizaron, pero ocurrió un error al guardar las áreas extra.";
                }

                return RedirectToAction("VerParticipantes", "Participante");
            }
            catch
            {
                if (isAjaxRequest)
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Ocurrió un error al editar los datos."
                    });
                }

                TempData["errorMessage"] = "Ocurrió un error al editar los datos.";
                return RedirectToAction("VerParticipantes", "Participante");
            }
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }

        private object BuildAjaxValidationErrorResponse()
        {
            var fieldErrors = ModelState
                .Where(entry => entry.Value != null && entry.Value.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Valor inválido." : error.ErrorMessage)
                        .ToList());

            var globalErrors = new List<string>();

            if (fieldErrors.TryGetValue(string.Empty, out var modelLevelErrors))
            {
                globalErrors.AddRange(modelLevelErrors);
                fieldErrors.Remove(string.Empty);
            }

            return new
            {
                success = false,
                fieldErrors,
                globalErrors
            };
        }

        /// <summary>
        /// Elimina el participante y su cuenta de usuario del sistema.
        /// </summary>
        /// <param name="idParticipante">Correo institucional del participante a eliminar.</param>
        /// <returns>
        /// Redirects to VerParticipantes. Sets TempData["successMessage"] or TempData["errorMessage"].
        /// </returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, UsuarioHandler.
        /// Role required: Admin (1).
        /// </remarks>
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

        /// <summary>
        /// Devuelve la vista parcial del modal con los grupos inscritos de un participante.
        /// </summary>
        /// <param name="idParticipante">Correo institucional del participante.</param>
        /// <returns>PartialView: _Modal — ViewBag.ListaGrupos, ViewBag.IdParticipante.</returns>
        /// <remarks>
        /// Handlers: GrupoHandler.
        /// Role required: Admin (1) o Asesor (2).
        /// </remarks>
        [HttpPost]
        public ActionResult DisplayModal(string idParticipante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewBag.ListaGrupos = accesoAGrupo.ObtenerListaGruposParticipante(idParticipante);
            ViewBag.IdParticipante = idParticipante;

            return PartialView("_Modal");
        }

        /// <summary>Devuelve la lista de tipos de participante definidos en el enum TipoDeParticipantes.</summary>
        [HttpGet]
        public JsonResult GetTiposParticipante(string tipoParticipante)
        {
            string[] tipoDeParticipantes = Enum.GetNames(typeof(TipoDeParticipantes));
            List<string> tipoDeParticipantesLista = new List<string>(tipoDeParticipantes);

            return Json(tipoDeParticipantesLista);
        }

        /// <summary>Devuelve los departamentos disponibles para un área UCR, consultando dataAreas.json vía ParticipanteHandler.</summary>
        [HttpGet]
        public IActionResult GetDepartamentosByArea(string areaName)
        {
            try
            {
                List<string> departamentos = accesoAParticipante.GetDepartamentosByArea(areaName);
                return Json(departamentos);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        /// <summary>Devuelve las secciones/unidades académicas disponibles para un área y departamento UCR.</summary>
        [HttpGet]
        public IActionResult GetSeccionesByDepartamento(string areaName, string departamentoName)
        {
            try
            {
                List<string> secciones = accesoAParticipante.GetSeccionesByDepartamento(areaName, departamentoName);
                return Json(secciones);
            }
            catch
            {
                return StatusCode(500);
            }
        }


        /// <summary>
        /// Devuelve en JSON la estructura completa de áreas, departamentos y secciones UCR
        /// para poblar los desplegables del formulario de participante.
        /// </summary>
        [HttpGet]
        public IActionResult GetAllAreasData()
        {
            try
            {
                var allAreas = accesoAParticipante.GetAllAreas();
                var departamentosByArea = new Dictionary<string, List<string>>();
                var seccionesByDepartamento = new Dictionary<string, List<string>>();
                var carrerasBySeccionAndSede = new Dictionary<string, Dictionary<string, List<string>>>();

                foreach (var areaName in allAreas)
                {
                    var departamentos = accesoAParticipante.GetDepartamentosByArea(areaName);
                    departamentosByArea[areaName] = departamentos;

                    foreach (var departamentoName in departamentos)
                    {
                        var key = $"{areaName}|{departamentoName}";
                        var secciones = accesoAParticipante.GetSeccionesByDepartamento(areaName, departamentoName);
                        seccionesByDepartamento[key] = secciones;
                    }
                }

                return Json(new
                {
                    areas = allAreas,
                    departamentosByArea,
                    seccionesByDepartamento,
                    carrerasBySeccionAndSede
                });
            }
            catch
            {
                return StatusCode(500);
            }
        }

        /// <summary>Devuelve las carreras disponibles para una sección/unidad académica y sede UCR.</summary>
        [HttpGet]
        public IActionResult GetCarrerasBySeccionAndSede(string areaName, string departamentoName, string unidadAcademica, string sede)
        {
            if (string.IsNullOrEmpty(areaName) || string.IsNullOrEmpty(departamentoName) || string.IsNullOrEmpty(unidadAcademica) || string.IsNullOrEmpty(sede))
                return Json(new List<string>());

            try
            {
                var carreras = accesoAParticipante.GetCarrerasBySeccionAndSede(areaName, departamentoName, unidadAcademica, sede);
                return Json(carreras);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        /// <summary>Wrapper JSON que delega en el método estático GetDepartamento para retornar departamentos por área.</summary>
        [HttpGet]
        public JsonResult GetDepartamentoJSON(string areaName)
        {
            return Json(GetDepartamento(areaName));
        }

        /// <summary>Devuelve la lista de departamentos/facultades para un área UCR como SelectListItem (datos estáticos).</summary>
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
                    departamentos.Add(new SelectListItem() { Text = "Programa de Estudios de Posgrado" });
                    break;
                case "Sedes Regionales":
                    departamentos.Add(new SelectListItem() { Text = "Ciudad Universitaria Rodrigo Facio" });
                    departamentos.Add(new SelectListItem() { Text = "Sede Interuniversitaria de Alajuela" });
                    departamentos.Add(new SelectListItem() { Text = "Sede Regional de Guanacaste" });
                    departamentos.Add(new SelectListItem() { Text = "Sede Regional de Occidente" });
                    departamentos.Add(new SelectListItem() { Text = "Sede Regional del Atlántico" });
                    departamentos.Add(new SelectListItem() { Text = "Sede Regional del Caribe" });
                    departamentos.Add(new SelectListItem() { Text = "Sede Regional del Pacífico" });
                    departamentos.Add(new SelectListItem() { Text = "Sede Regional del Sur" });
                    break;
                case "Otros":
                    departamentos.Add(new SelectListItem() { Text = "Centros de Investigación" });
                    departamentos.Add(new SelectListItem() { Text = "Otro" });
                    break;
                case "Oficinas Administrativas":
                    departamentos.Add(new SelectListItem() { Text = "Vicerrectoría" });
                    departamentos.Add(new SelectListItem() { Text = "Otro" });
                    break;
            }
            return departamentos;
        }
        /// <summary>Devuelve la lista de unidades académicas/escuelas para un departamento/facultad UCR (datos estáticos).</summary>
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
                case "Vicerrectoría":
                    unidades.Add(new SelectListItem() { Text = "Vicerrectoría de Acción Social" });
                    unidades.Add(new SelectListItem() { Text = "Vicerrectoría de Administración" });
                    unidades.Add(new SelectListItem() { Text = "Vicerrectoría de Docencia" });
                    unidades.Add(new SelectListItem() { Text = "Vicerrectoría de Vida Estudiantil" });
                    unidades.Add(new SelectListItem() { Text = "Vicerrectoría de Investigación" });
                    unidades.Add(new SelectListItem() { Text = "Otro" });
                    break;
                case "Centros de Investigación":
                    unidades.Add(new SelectListItem() { Text = "Centros de Investigación" });
                    unidades.Add(new SelectListItem() { Text = "Otro" });
                    break;
                case "Otro":
                    unidades.Add(new SelectListItem() { Text = "Otro" });
                    break;
            }
            return unidades;
        }

        /// <summary>Devuelve la lista de sedes y recintos UCR como SelectListItem agrupados por sede (datos estáticos).</summary>
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
            sedes.Add(new SelectListItem() { Text = "Recinto de Limón", Group = group3 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Siquirres", Group = group3 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Liberia", Group = group4 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Santa Cruz", Group = group4 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Turrialba", Group = group5 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Paraíso", Group = group5 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Guápiles", Group = group5 });
            sedes.Add(new SelectListItem() { Text = "Recinto de San Ramón", Group = group6 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Tacáres", Group = group6 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Alajuela", Group = group7 });
            sedes.Add(new SelectListItem() { Text = "Recinto de Puntarenas", Group = group8 });


            return sedes;
        }

        /// <summary>Busca el índice (base-1) de una columna en la primera fila del worksheet, ignorando acentos y mayúsculas.</summary>
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

        /// <summary>Elimina diacríticos (acentos) de un texto normalizando a Unicode FormD.</summary>
        private string RemoveAccents(string text)
        {
            return string.Concat(text.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
                .Normalize(NormalizationForm.FormC);
        }

        /// <summary>Envía al nuevo participante un correo con su contraseña temporal.</summary>
        private async Task<IActionResult> EnviarContrasenaPorCorreo(string correo, string contrasena)
        {
            string subject = "Nuevo Usuario en el SISTEMA DE INSCRIPCIONES METICS";
            string message = $"<p>Se ha creado al usuario con correo institucional {correo} en el Sistema de Competencias Digitales para la Docencia - METICS.</p>" +
                $"</p>Su contraseña temporal es <strong>{contrasena}</strong></p>" +
                $"<p>Recuerde que puede cambiar la contraseña al iniciar sesión en el sistema desde el ícono de usuario.</p>";

            await _emailService.SendEmailAsync(correo, subject, message);
            return Ok();
        }

        /// <summary>Genera una contraseña aleatoria de 10 caracteres alfanuméricos.</summary>
        private string GenerateRandomPassword()
        {
            int length = 10;
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            string password = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return password;
        }

        /// <summary>Obtiene el rol del usuario actual desde la cookie "rolUsuario".</summary>
        private int GetRole()
        {
            int role = 0;

            if (HttpContext.Request.Cookies.ContainsKey("rolUsuario"))
            {
                role = Convert.ToInt32(Request.Cookies["rolUsuario"]);
            }

            return role;
        }

        /// <summary>Obtiene el identificador del usuario actual desde la cookie "idUsuario".</summary>
        private string GetId()
        {
            string id = "";

            if (HttpContext.Request.Cookies.ContainsKey("idUsuario"))
            {
                id = Convert.ToString(Request.Cookies["idUsuario"]);
            }

            return id;
        }

        /// <summary>Muestra el formulario de recuperación de contraseña.</summary>
        /// <returns>View: FormularioRecuperarContrasena.</returns>
        public ActionResult FormularioRecuperarContrasena()
        {
            return View("FormularioRecuperarContrasena");
        }

        /// <summary>
        /// Genera una nueva contraseña aleatoria para el participante y la envía por correo.
        /// </summary>
        /// <param name="participante">Modelo que contiene el correo institucional del participante.</param>
        /// <returns>
        /// View: FormularioRecuperarContrasena —
        /// ViewBag.SuccessMessage o ViewBag.ErrorMessage según resultado.
        /// </returns>
        /// <remarks>
        /// Handlers: ParticipanteHandler, UsuarioHandler.
        /// Role required: Any (autoservicio).
        /// </remarks>
        [HttpPost]
        public async Task<IActionResult> RecuperarContrasena(ParticipanteModel participante)
        {
            var correo = participante.correo;

            // Obtener la lista de participantes (solo el correo importa)
            var listaParticipantes = accesoAParticipante.ObtenerListaParticipantesFiltrada(correo);

            // Si la lista de búsqueda no esta vacia
            if (listaParticipantes != null)
            {
                // Obtener el participante de la lista
                var participanteEncontrado = listaParticipantes.First();

                // Generar una nueva contraseña aleatoria
                string nuevaContrasena = GenerateRandomPassword();
                // Actualizar la contraseña del usuario en la base de datos
                if (!accesoAUsuario.ActualizarContrasena(correo, nuevaContrasena))
                {
                    // Enviar un correo electrónico al usuario con la nueva contraseña
                    string subject = "Recuperación de contraseña SISTEMA DE INSCRIPCIONES METICS";
                    string message = $"<p>Se ha solicitado la recuperación de la contraseña para el usuario con correo institucional {correo} en el Sistema de Competencias Digitales para la Docencia - METICS.</p>" +
                        $"</p>Su contraseña temporal es <strong>{nuevaContrasena}</strong></p>" +
                        $"<p>Si no ha solicitado este cambio, ignore este mensaje.</p>";

                    await _emailService.SendEmailAsync(correo, subject, message);
                    TempData["successMessage"] = "Se ha enviado un correo con su nueva contraseña.";
                }
                else
                {
                    TempData["errorMessage"] = "Error al actualizar la contraseña.";
                }
            }
            else
            {
                TempData["errorMessage"] = "No existe un usuario con ese correo y número de identificación.";
            }

            if (TempData["errorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["errorMessage"].ToString();
            }
            if (TempData["successMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["successMessage"].ToString();
            }

            return View("FormularioRecuperarContrasena");
        }
    }
}