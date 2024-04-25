using Microsoft.AspNetCore.Mvc;
using webMetics.Models;
using webMetics.Handlers;

namespace webMetics.Controllers
{
    public class CalificacionesController : Controller
    {
        private InscripcionHandler accesoAInscripcion;
        private GrupoHandler accesoAGrupo;
        private ParticipanteHandler accesoAParticipante;
        private CalificacionesHandler accesoACalificaciones;

        public CalificacionesController()
        {
            accesoAInscripcion = new InscripcionHandler();
            accesoAGrupo = new GrupoHandler();
            accesoAParticipante = new ParticipanteHandler();
            accesoACalificaciones = new CalificacionesHandler();
        }

        public ActionResult VerCalificaciones(int idGrupo)
        {
            List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            ViewBag.ListaCalificaciones = calificaciones;
            ViewBag.IdGrupo = idGrupo;
            GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo);
            ViewBag.Title = "Calificaciones";
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

        public ActionResult EditarCalificacion(int idGrupo, string idParticipante)
        {
            List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            CalificacionModel calificacion = calificaciones.Find(calificacionModel => calificacionModel.participante.idParticipante == idParticipante);
            GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo);

            ViewBag.Calificacion = calificacion;
            ViewBag.NombreGrupo = grupo.nombre;

            return View();
        }

        [HttpPost]
        public ActionResult SubirCalificacion(int idGrupo, string idParticipante, int calificacion)
        {
            try
            {
                accesoACalificaciones.IngresarNota(idGrupo, idParticipante, calificacion);
            }
            catch
            {
                TempData["errorMessage"] = "Ocurrió un error y no se pudo actualizar la calificación.";
            }

            return RedirectToAction("VerCalificaciones", "Calificaciones", new { idGrupo = idGrupo });
        }


        [HttpPost]
        public ActionResult EnviarCalificaciones(int idGrupo) //ToDo
        {
            //List<CalificacionModel> calificaciones = accesoACalificaciones.ObtenerListaCalificaciones(idGrupo);
            //GrupoModel grupo = accesoAGrupo.ObtenerInfoGrupo(idGrupo);

            //string mensaje;
            //try
            //{
            //    foreach (CalificacionModel calificacion in calificaciones)
            //    {
            //        mensaje = ConstructorDelMensaje(grupo, calificacion);
            //        SendEmail(grupo.nombre, mensaje, calificacion.participante.correo);
            //    }

            //    TempData["successMessage"] = "Las calificaciones fueron enviadas éxitosamente al correo de cada participante.";
            //}
            //catch (Exception ex)
            //{
            //    TempData["errorMessage"] = "Error al enviar las calificaciones: " + ex.Message;
            //}

            //// Redirige a la vista adecuada.
            return RedirectToAction("VerCalificaciones", "Calificaciones", new { idGrupo = idGrupo });
        }
    }
}