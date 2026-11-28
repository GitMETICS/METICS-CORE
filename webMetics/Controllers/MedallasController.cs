using Microsoft.AspNetCore.Mvc;
using webMetics.Models;
using webMetics.Handlers;
using webMetics.Services;

namespace webMetics.Controllers
{
    /// <summary>
    /// Gestiona las medallas del sistema: permite subir nuevas imágenes de medallas, editarlas,
    /// eliminarlas y desasociarlas de participantes específicos.
    /// </summary>
    public class MedallasController : Controller
    {
        private protected UsuarioHandler accesoAUsuario;
        private protected ParticipanteHandler accesoAParticipante;
        private protected InscripcionHandler accesoAInscripcion;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly MedallasCatalogoService catalogo;

        public MedallasController(IWebHostEnvironment environment, IConfiguration configuration, EmailService emailService)
        {
            _environment = environment;
            _configuration = configuration;
            _emailService = emailService;
            catalogo = new MedallasCatalogoService(environment, configuration);

            accesoAUsuario = new UsuarioHandler(environment, configuration);
            accesoAParticipante = new ParticipanteHandler(environment, configuration);
            accesoAInscripcion = new InscripcionHandler(environment, configuration);
        }

        /// <summary>Obtiene el rol del usuario autenticado desde la cookie "rolUsuario".</summary>
        private int GetRole()
        {
            int role = 0;

            if (HttpContext.Request.Cookies.ContainsKey("rolUsuario"))
            {
                role = Convert.ToInt32(Request.Cookies["rolUsuario"]);
            }

            return role;
        }

        /// <summary>Obtiene el identificador del usuario autenticado desde la cookie "idUsuario".</summary>
        private string GetId()
        {
            string id = "";

            if (HttpContext.Request.Cookies.ContainsKey("idUsuario"))
            {
                id = Convert.ToString(Request.Cookies["idUsuario"]);
            }

            return id;
        }

        /// <summary>Muestra el catálogo de medallas y la lista de participantes del sistema.</summary>
        /// <returns>
        /// View: ListaMedallas —
        /// ViewBag.TodasLasMedallas, ViewBag.TodosLosParticipantes,
        /// ViewBag.Role, ViewBag.Id, ViewBag.ErrorMessage, ViewBag.SuccessMessage.
        /// </returns>
        /// <remarks>Handlers: ParticipanteHandler. Role required: Admin (1).</remarks>
        public ActionResult ListaMedallas()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewBag.MedallasRegistradas = accesoAParticipante.ObtenerTodasMedallas() ?? new List<string>();

            ViewBag.TodasLasMedallas = ViewBag.MedallasRegistradas;
            ViewBag.TodosLosParticipantes = accesoAParticipante.ObtenerListaParticipantes();

            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View();
        }

        /// <summary>
        /// Sube una nueva imagen de medalla al servidor y la registra en la base de datos,
        /// siempre que no exista ya una medalla con el mismo nombre de archivo.
        /// </summary>
        /// <param name="imageFile">Archivo de imagen a subir (JPEG/PNG). El nombre del archivo se usa como identificador.</param>
        /// <returns>
        /// Redirects to ListaMedallas. Sets TempData["successMessage"] on success or
        /// TempData["errorMessage"] si el archivo es inválido o ya existe.
        /// </returns>
        /// <remarks>Handlers: ParticipanteHandler. Role required: Admin (1).</remarks>
        [HttpPost]
        public async Task<IActionResult> SubirMedalla(IFormFile imageFile)
        {
            if (GetRole() != 1 || string.IsNullOrWhiteSpace(GetId())) return RedirectToAction("CerrarSesion", "Usuario");
            try
            {
                if (imageFile == null || imageFile.Length == 0)
                    TempData["errorMessage"] = "Elija una imagen válida.";
                else
                {
                    await catalogo.GuardarImagenAsync(imageFile);
                    TempData["successMessage"] = "Se subió la medalla.";
                }
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "No se pudo subir la medalla. Verifique el formato, el nombre y que no exista ya en el catálogo.";
            }
            return RedirectToAction("ListaMedallas");
        }

        /// <summary>Muestra el formulario para reemplazar la imagen de una medalla existente.</summary>
        /// <param name="nombreMedalla">Nombre de archivo de la medalla a editar.</param>
        /// <returns>
        /// View: EditarMedalla — ViewBag.NombreMedalla, ViewBag.Role, ViewBag.Id.
        /// </returns>
        /// <remarks>Role required: Admin (1).</remarks>
        public ActionResult EditarMedalla(string nombreMedalla)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewBag.NombreMedalla = nombreMedalla;

            return View();
        }

        /// <summary>
        /// Reemplaza la imagen de una medalla conservando sus asignaciones; el nombre anterior queda inactivo.
        /// </summary>
        /// <param name="nombreMedalla">Nombre de la medalla a reemplazar.</param>
        /// <param name="imageFile">Nueva imagen de la medalla.</param>
        /// <returns>
        /// Redirects to ListaMedallas. Sets TempData["successMessage"] on success or
        /// TempData["errorMessage"] si el archivo es inválido.
        /// </returns>
        /// <remarks>Handlers: ParticipanteHandler. Role required: Admin (1).</remarks>
        [HttpPost]
        public async Task<IActionResult> EditarMedalla(string nombreMedalla, IFormFile imageFile)
        {
            if (GetRole() != 1 || string.IsNullOrWhiteSpace(GetId())) return RedirectToAction("CerrarSesion", "Usuario");
            try
            {
                if (imageFile == null || imageFile.Length == 0)
                    TempData["errorMessage"] = "Elija una imagen válida.";
                else
                {
                    await catalogo.GuardarImagenAsync(imageFile, nombreMedalla);
                    TempData["successMessage"] = "Se editó la medalla.";
                }
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "No se pudo editar la medalla. Verifique el nombre y que no pertenezca a otra medalla.";
            }
            return RedirectToAction("ListaMedallas");
        }

        /// <summary>Desactiva una medalla del catálogo y elimina sus asignaciones. Conserva la imagen para recuperación.</summary>
        /// <param name="nombreMedalla">Nombre de archivo de la medalla a eliminar.</param>
        /// <returns>
        /// Redirects to ListaMedallas. Sets TempData["successMessage"] on success or
        /// TempData["errorMessage"] on failure.
        /// </returns>
        /// <remarks>Handlers: ParticipanteHandler. Role required: Admin (1).</remarks>
        public ActionResult EliminarMedalla(string nombreMedalla)
        {
            if (GetRole() != 1 || string.IsNullOrWhiteSpace(GetId())) return RedirectToAction("CerrarSesion", "Usuario");
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                bool exito = accesoAParticipante.EliminarMedalla(nombreMedalla);
                if (exito)
                {
                    TempData["successMessage"] = "Se eliminó la medalla.";
                }
                else
                {
                    TempData["errorMessage"] = "No se pudo eliminar la medalla.";
                }
            }
            catch
            {
                TempData["errorMessage"] = "No se pudo eliminar la medalla.";
            }

            return RedirectToAction("ListaMedallas");
        }

        /// <summary>Desasigna una medalla de un participante específico sin eliminarla del catálogo.</summary>
        /// <param name="idParticipante">Correo/ID del participante.</param>
        /// <param name="nombreMedalla">Nombre de archivo de la medalla a desasignar.</param>
        /// <returns>
        /// Redirects to Participante/VerDatosParticipante. Sets TempData["successMessage"] on
        /// success or TempData["errorMessage"] on failure.
        /// </returns>
        /// <remarks>Handlers: ParticipanteHandler. Role required: Admin (1).</remarks>
        [HttpPost]
        public IActionResult EliminarMedallaParticipante(string idParticipante, string nombreMedalla)
        {
            try
            {
                accesoAParticipante.EliminarMedallaParticipante(idParticipante, nombreMedalla);

                TempData["successMessage"] = "Se eliminó la medalla.";
            }
            catch
            {
                TempData["errorMessage"] = "No se pudo eliminar la medalla.";
            }

            return RedirectToAction("VerDatosParticipante", "Participante", new { idParticipante });
        }
    }
}
