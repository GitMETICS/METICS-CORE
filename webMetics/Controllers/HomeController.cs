using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;

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
    }
}