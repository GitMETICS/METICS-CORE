using Microsoft.AspNetCore.Mvc;

namespace webMetics.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            //La página de inicio es la lista de los grupos disponibles
            return Redirect("~/Grupo/ListaGruposDisponibles");
        }
    }
}