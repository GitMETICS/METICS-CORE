using Microsoft.AspNetCore.Mvc;
using webMetics.Models;
using webMetics.Handlers;
using System.Security.Claims;

namespace webMetics.Controllers
{
    public class MedallasController : Controller
    {
        private protected UsuarioHandler accesoAUsuario;
        private protected ParticipanteHandler accesoAParticipante;
        private protected InscripcionHandler accesoAInscripcion;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public MedallasController(IWebHostEnvironment environment, IConfiguration configuration, EmailService emailService)
        {
            _environment = environment;
            _configuration = configuration;
            _emailService = emailService;

            accesoAUsuario = new UsuarioHandler(environment, configuration);
            accesoAParticipante = new ParticipanteHandler(environment, configuration);
            accesoAInscripcion = new InscripcionHandler(environment, configuration);
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

        public ActionResult ListaMedallas()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewBag.Medallas = accesoAParticipante.ObtenerTodasMedallas();

            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubirMedalla(IFormFile imageFile)
        {
            if (!accesoAParticipante.ExisteMedalla(imageFile.FileName))
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "medallas", imageFile.FileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Guardar la ruta de la medalla en la base de datos
                    accesoAParticipante.AgregarMedalla(GetId(), imageFile.FileName);

                    TempData["successMessage"] = "Se subió la medalla.";
                }
                else
                {
                    TempData["errorMessage"] = "Elija una imagen válida.";
                }
            }
            else
            {
                TempData["errorMessage"] = "Ya existe una medalla con el mismo nombre.";
            }
                
            return RedirectToAction("ListaMedallas");
        }

        public ActionResult EditarMedalla(string nombreMedalla)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewBag.NombreMedalla = nombreMedalla;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EditarMedalla(string nombreMedalla, IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "medallas", imageFile.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Eliminar la medalla existente
                accesoAParticipante.EliminarMedalla(nombreMedalla);
                
                // Guardar la ruta de la medalla en la base de datos
                accesoAParticipante.AgregarMedalla(GetId(), imageFile.FileName);

                TempData["successMessage"] = "Se editó la medalla.";
            }
            else
            {
                TempData["errorMessage"] = "Elija una imagen válida.";
            }

            return RedirectToAction("ListaMedallas");
        }

        public ActionResult EliminarMedalla(string nombreMedalla)
        {
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