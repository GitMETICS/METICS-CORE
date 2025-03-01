using Microsoft.AspNetCore.Mvc;
using NETCore.MailKit.Core;
using webMetics.Handlers;
using webMetics.Models;

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

        public ActionResult VerMedallas()
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
            if (imageFile != null && imageFile.Length > 0)
            {
                var filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "medallas", imageFile.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Guardar la ruta de la medalla en la base de datos
                accesoAParticipante.AgregarMedalla(imageFile.FileName);

                TempData["successMessage"] = "Se subió la medalla.";
            }
            else
            {
                TempData["errorMessage"] = "Elija una imagen válida.";
            }

            return RedirectToAction("VerMedallas");
        }

        public ActionResult EditarMedalla(IFormFile imageFile)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewBag.Medalla = imageFile;

            return View();
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

            return RedirectToAction("ListaCategorias");
        }
    }
}