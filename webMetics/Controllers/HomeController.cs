using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;

namespace webMetics.Controllers
{
    public class HomeController : Controller
    {
        private BaseDeDatosHandler accesoABaseDatos;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HomeController(IWebHostEnvironment environment, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _environment = environment;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;

            accesoABaseDatos = new BaseDeDatosHandler(environment, configuration);
        }

        public ActionResult Index()
        {
            //La página de inicio es la lista de los grupos disponibles
            return Redirect("~/Grupo/ListaGruposDisponibles");
        }
    }
}