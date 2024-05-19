using webMetics.Handlers;
using webMetics.Models;
using Microsoft.AspNetCore.Mvc;

/* 
 * Controlador de la entidad Participante
 */
namespace webMetics.Controllers
{
    public class ParticipanteController : Controller
    {
        private ParticipanteHandler accesoAParticipante;
        private GrupoHandler accesoAGrupo;
        private AsesorHandler accesoAAsesor;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public ParticipanteController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            accesoAParticipante = new ParticipanteHandler(environment, configuration);
            accesoAGrupo = new GrupoHandler(environment, configuration);
            accesoAAsesor = new AsesorHandler(environment, configuration);
        }

        public int GetRole()
        {
            int role = 0;

            if (HttpContext.Request.Cookies.ContainsKey("rolUsuario"))
            {
                role = Convert.ToInt32(Request.Cookies["rolUsuario"]);
            }

            return role;
        }

        public string GetId()
        {
            string id = "";

            if (HttpContext.Request.Cookies.ContainsKey("idUsuario"))
            {
                id = Convert.ToString(Request.Cookies["idUsuario"]);
            }

            return id;
        }

        /* Método para ver la lista de participantes de un grupo */
        public ActionResult ListaParticipantes(int? idGrupo)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                ViewBag.IdGrupo = idGrupo;
                ViewBag.ListaParticipantes = accesoAParticipante.ObtenerParticipantesDelGrupo(idGrupo.Value);
                
                ViewBag.Title = "Lista de participantes";
                GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo.Value);
                ViewBag.NombreGrupo = grupo.nombre;
                return View();
            }
            catch
            {
                // Si ocurre un error, redirigir a la lista de grupos disponibles
                return RedirectToAction("ListaDeGruposDisponibles", "Grupo");
            }
        }

        /* Método para ver todos los participantes y sus horas matriculadas y aprobadas (administrador) */
        public ActionResult VerParticipantes()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            List<ParticipanteModel> participantes = accesoAParticipante.ObtenerListaParticipantes();

            foreach (ParticipanteModel participante in participantes)
            {
                string idParticipante = participante.idParticipante;
                participante.gruposInscritos = accesoAGrupo.ObtenerListaGruposParticipante(idParticipante);
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

            return View();
        }

        public ActionResult MiUsuario(string idParticipante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

            ViewBag.Participante = participante;
            ViewBag.ListaGrupos = accesoAGrupo.ObtenerListaGruposParticipante(idParticipante);
            

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

        public ActionResult VerDatosParticipante(string idParticipante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);
            
            ViewBag.Participante = participante;
            ViewBag.ListaGrupos = accesoAGrupo.ObtenerListaGruposParticipante(idParticipante);

            if (HttpContext.Request.Cookies.ContainsKey("rolUsuario"))
            {
                int rolUsuario = Convert.ToInt32(Request.Cookies["rolUsuario"]);

                if (rolUsuario == 2)
                {
                    string idAsesor = Convert.ToString(Request.Cookies["idUsuario"]);
                    List<GrupoModel> gruposAsesor = accesoAAsesor.ObtenerListaGruposAsesor(idAsesor);
                    List<GrupoModel> gruposParticipante = ViewBag.ListaGrupos;

                    List<GrupoModel> gruposEnComun = gruposAsesor.Join(
                        gruposParticipante,
                        grupoAsesor => grupoAsesor.idGrupo,
                        grupoParticipante => grupoParticipante.idGrupo,
                        (grupoAsesor, grupoParticipante) => new GrupoModel
                        {
                            idGrupo = grupoParticipante.idGrupo,
                            nombre = grupoParticipante.nombre,
                            cantidadHoras = grupoParticipante.cantidadHoras
                        }
                    ).ToList();

                    ViewBag.ListaGrupos = gruposEnComun;
                }
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
        public ActionResult SubirHorasAprobadas(string idParticipante, int horasAprobadas)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);
            int nuevasHorasAprobadas = participante.horasAprobadas + horasAprobadas;

            if (nuevasHorasAprobadas <= participante.horasMatriculadas && nuevasHorasAprobadas >= 0)
            {
                accesoAParticipante.ActualizarHorasAprobadasParticipante(idParticipante, nuevasHorasAprobadas);

                TempData["successMessage"] = "Las horas fueron aprobadas.";
            }
            else
            {
                TempData["errorMessage"] = "No se pudo aprobar las horas.";
            }

            return RedirectToAction("VerDatosParticipante", "Participante", new { idParticipante });
        }

        // Vista para buscar y editar los datos de un participante
        public ActionResult ObtenerParticipante()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            return View(); // Muestra la vista para buscar al participante
        }

        // Método para obtener los datos de un participante según el ID proporcionado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ObtenerParticipante(string idParticipante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            // Busca al participante en la lista de participantes por su ID
            ParticipanteModel getParticipante = accesoAParticipante.ObtenerListaParticipantes().Find(participanteModel => participanteModel.idParticipante == idParticipante);
            if (getParticipante == null)
            {
                // Si no se encuentra el participante, redirige de nuevo a la vista para buscar
                return RedirectToAction("ObtenerParticipante");
            }
            else
            {
                // Si se encuentra el participante, redirige a la vista para editar sus datos
                return RedirectToAction("EditarParticipante", new { idParticipante = idParticipante });
            }
        }

        // Vista del formulario para editar los datos de un participante según el id proporcionado
        [HttpGet]
        public ActionResult EditarParticipante(string idParticipante)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                ViewBag.IdParticipante = idParticipante;
                ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

                if (participante != null)
                {
                    ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
                    return View(participante);
                }
                else
                {
                    return RedirectToAction("ObtenerParticipante");
                }
            }
            catch
            {
                return RedirectToAction("ObtenerParticipante");
            }
        }

        public ActionResult DevolverDatosParticipante(string idParticipante)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(idParticipante);

            return View("EditarParticipante", participante);
        }

        // Método para procesar el formulario de edición de datos del participante con los datos ingresados
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarParticipante(ParticipanteModel participante)
        {
            if (ModelState.IsValid)
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                // Si el modelo es válido, actualiza los datos del participante utilizando el controlador de acceso a participantes
                accesoAParticipante.EditarParticipante(participante);
                // Redirige a la lista de grupos disponibles después de editar el participante exitosamente
                TempData["successMessage"] = "Los datos fueron guardados.";

                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }
            else
            {
                // Si el modelo no es válido, muestra el formulario de edición con los errores
                ViewData["jsonDataAreas"] = accesoAParticipante.GetAllAreas();
                return View("EditarParticipante", participante);
            }
        }

        /* Método para que un administrador elimine un participante */
        [HttpGet]
        public ActionResult EliminarParticipante(string idParticipante)
        {
            try
            {
                ViewBag.Role = GetRole();
                ViewBag.Id = GetId();

                bool eliminadoExitosamente = accesoAParticipante.EliminarParticipante(idParticipante);

                if (eliminadoExitosamente)
                {
                    TempData["successMessage"] = "El participante fue eliminado.";
                }
                else
                {
                    TempData["errorMessage"] = "No se pudo eliminar al participante.";
                }
            }
            catch (Exception)
            {
                // Si ocurrió una excepción al intentar eliminar la inscripción, mostrar un mensaje y redirigir a la lista de participantes del grupo
                TempData["errorMessage"] = "Hubo un error y no se pudo eliminar al participante.";
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
    }
}