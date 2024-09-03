using Microsoft.AspNetCore.Mvc;
using webMetics.Models;
using webMetics.Handlers;
using MimeKit;
using OfficeOpenXml;
using Microsoft.AspNetCore.Mvc.Rendering;
using NPOI.XSSF.UserModel;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using NPOI.XWPF.UserModel;


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
            table.AddHeaderCell("Correo institucional").SetFontSize(8);
            table.AddHeaderCell("Condición").SetFontSize(8);
            table.AddHeaderCell("Unidad académica").SetFontSize(8);
            table.AddHeaderCell("Teléfono").SetFontSize(8);

            foreach (var participante in participantes)
            {
                table.AddCell(participante.numeroIdentificacion);
                table.AddCell(participante.nombre + " " + participante.primerApellido + " " + participante.segundoApellido);
                table.AddCell(participante.idParticipante);
                table.AddCell(participante.condicion);
                table.AddCell(participante.unidadAcademica);
                table.AddCell(participante.telefono);
            }

            document.Add(table);
            table.Complete();
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
            headerRow.GetCell(2).SetText("Correo institucional");
            headerRow.GetCell(3).SetText("Condición");
            headerRow.GetCell(4).SetText("Unidad académica");
            headerRow.GetCell(5).SetText("Teléfono");


            for (int i = 1; i < participantes.Count; i++)
            {
                var row = table.Rows[i];
                row.GetCell(0).SetText(participantes[i].numeroIdentificacion.ToString());
                row.GetCell(1).SetText(participantes[i].nombre + " " + participantes[i].primerApellido + " " + participantes[i].segundoApellido);
                row.GetCell(2).SetText(participantes[i].idParticipante.ToString());
                row.GetCell(3).SetText(participantes[i].condicion.ToString());
                row.GetCell(4).SetText(participantes[i].unidadAcademica);
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

            NPOI.SS.UserModel.ICell cell35 = row3.CreateCell(2);
            cell35.SetCellValue("Correo institucional");

            NPOI.SS.UserModel.ICell cell33 = row3.CreateCell(3);
            cell33.SetCellValue("Condición");

            NPOI.SS.UserModel.ICell cell34 = row3.CreateCell(4);
            cell34.SetCellValue("Unidad académica");

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

                NPOI.SS.UserModel.ICell cell5 = row.CreateCell(2);
                cell5.SetCellValue(participante.idParticipante);

                NPOI.SS.UserModel.ICell cell3 = row.CreateCell(3);
                cell3.SetCellValue(participante.condicion);

                NPOI.SS.UserModel.ICell cell4 = row.CreateCell(4);
                cell4.SetCellValue(participante.unidadAcademica);

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
            cell36.SetCellValue("Horas aprobadas");

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
        public ActionResult SubirHorasAprobadas(int? idGrupo, string idParticipante, int horasAprobadas)
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

                ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);
                int nuevoTotalHorasAprobadas = participante.horasAprobadas + horasAprobadas;

                InscripcionModel inscripcion = accesoAInscripcion.ObtenerInscripcionParticipante(idGrupo.Value, idParticipante);
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
        public static List<SelectListItem> GetAreas()
        {
            var areas = new List<SelectListItem>();
            areas.Add(new SelectListItem() { Text = "Área de Artes y Letras" });
            areas.Add(new SelectListItem() { Text = "Área de Ciencias Agroalimentarias" });
            areas.Add(new SelectListItem() { Text = "Área de Ciencias Básicas" });
            areas.Add(new SelectListItem() { Text = "Área de Ciencias Sociales" });
            areas.Add(new SelectListItem() { Text = "Área de Ingeniería" });
            areas.Add(new SelectListItem() { Text = "Área de Salud" });
            areas.Add(new SelectListItem() { Text = "Sistema de Estudios de Posgrado" });

            return areas;
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
                    break;
                case "Sistema de Estudios de Posgrado":
                    departamentos.Add(new SelectListItem() { Text = "Especialidad de Posgrado" });
                    departamentos.Add(new SelectListItem() { Text = "Programa de Doctorado" });
                    departamentos.Add(new SelectListItem() { Text = "Programa de Estudios de Posgrado" });
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