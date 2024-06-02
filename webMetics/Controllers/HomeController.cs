using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;

namespace webMetics.Controllers
{
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

        public ActionResult Index()
        {
            //La página de inicio es la lista de los grupos disponibles
            return Redirect("~/Grupo/ListaGruposDisponibles");
        }
    }
}