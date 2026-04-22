using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;
using webMetics.Models;

namespace webMetics.Controllers
{
    /// <summary>
    /// Controlador de inicio de la aplicación. Redirige a la lista de grupos disponibles.
    /// </summary>
    public class HomeController : Controller
    {
        private BaseDeDatosHandler accesoABaseDatos;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public HomeController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            accesoABaseDatos = new BaseDeDatosHandler(environment, configuration);
        }

        /// <summary>Redirige a la página de inicio de la aplicación (lista de grupos disponibles).</summary>
        /// <returns>Redirects to Grupo/ListaGruposDisponibles.</returns>
        public ActionResult Index()
        {
            return Redirect("~/Grupo/ListaGruposDisponibles");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}