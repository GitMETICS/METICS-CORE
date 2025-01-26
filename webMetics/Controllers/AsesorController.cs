using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;
using webMetics.Models;

/* 
 * Controlador de la entidad Asesor
 * Los asesores son los profesores a cargo de impartir el grupo
 * En esta clase se puede retornar todos los asesores, editar, agregar y eliminar algun asesor 
 */
namespace webMetics.Controllers
{
    // Controlador para gestionar las operaciones relacionadas con los asesores
    public class AsesorController : Controller
    {
        private protected UsuarioHandler accesoAUsuario;
        private protected TemaHandler accesoATema;
        private protected AsesorHandler accesoAAsesor;
        private protected ParticipanteHandler accesoAParticipante;
        private protected GrupoHandler accesoAGrupo;
        private protected InscripcionHandler accesoAInscripcion;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public AsesorController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            accesoATema = new TemaHandler(environment, configuration);
            accesoAUsuario = new UsuarioHandler(environment, configuration);
            accesoAAsesor = new AsesorHandler(environment, configuration);
            accesoAParticipante = new ParticipanteHandler(environment, configuration);
            accesoAGrupo = new GrupoHandler(environment, configuration);
            accesoAInscripcion = new InscripcionHandler(environment, configuration);
        }

        public ActionResult MisModulos()
        {
            const int RolUsuarioParticipante = 0;
            const int RolUsuarioAdmin = 1;
            const int RolUsuarioAsesor = 2;

            int rolUsuario = GetRole();
            string idUsuario = GetId();

            // Obtener la lista de grupos y participantes
            List<GrupoModel> listaGrupos = accesoAGrupo.ObtenerListaGruposAsesor(idUsuario);
            List<InscripcionModel> participantesEnGrupos = accesoAInscripcion.ObtenerInscripciones();

            // Inicializar las propiedades de ViewBag
            ViewBag.ListaGrupos = listaGrupos;
            ViewBag.ParticipantesEnGrupos = participantesEnGrupos;
            ViewBag.IdParticipante = string.Empty;

            // Manejar los roles de usuario y las suscripciones a grupos
            if (!string.IsNullOrEmpty(idUsuario))
            {
                ViewBag.IdParticipante = idUsuario;

                switch (rolUsuario)
                {
                    case RolUsuarioParticipante:
                        var gruposInscritos = accesoAGrupo.ObtenerListaGruposParticipante(idUsuario);
                        ViewBag.GruposInscritos = gruposInscritos;

                        if (gruposInscritos != null)
                        {
                            ViewBag.ListaGrupos = listaGrupos
                                .Where(grupo => !gruposInscritos.Any(inscrito => inscrito.idGrupo == grupo.idGrupo))
                                .ToList();
                        }
                        break;

                    case RolUsuarioAdmin:
                        ViewBag.ListaGrupos = listaGrupos;
                        break;

                    case RolUsuarioAsesor:
                        var gruposAsesor = accesoAGrupo.ObtenerListaGruposAsesor(idUsuario);
                        ViewBag.ListaGrupos = gruposAsesor;

                        break;
                }
            }

            // Definir propiedades adicionales de ViewBag
            ViewBag.DateNow = DateTime.Now;
            ViewBag.Role = rolUsuario;
            ViewBag.Id = idUsuario;

            // Gestionar mensajes de TempData
            ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
            ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

            return View();
        }

        public IActionResult BuscarGrupos(string searchTerm)
        {
            int rolUsuario = GetRole();
            string idUsuario = GetId();

            // Obtener la lista de participantes
            var grupos = accesoAGrupo.ObtenerListaGruposAsesor(idUsuario);

            // Filtrar la lista si se ha ingresado un término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                grupos = grupos.Where(p =>
                    p.nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.descripcion.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombreAsesor.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.nombreCategoria.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.modalidad.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.lugar.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.TemasSeleccionadosNombres.Any(t => t.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            ViewBag.ListaGrupos = grupos;
            ViewBag.Role = rolUsuario;
            ViewBag.Id = idUsuario;

            return View("MisModulos");
        }

        /* Método de la vista ListaAsesores que muestra todos los asesores */
        public ActionResult ListaAsesores()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            if (TempData["errorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["errorMessage"].ToString();
            }
            if (TempData["successMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["successMessage"].ToString();
            }

            // Obtener la lista de asesores y enviarla a la vista a través de ViewBag
            ViewBag.Asesores = accesoAAsesor.ObtenerAsesores();
            return View();
        }

        /* Método de la vista del formulario para crear un asesor */
        public ActionResult CrearAsesor()
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            if (TempData["errorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["errorMessage"].ToString();
            }
            if (TempData["successMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["successMessage"].ToString();
            }

            ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();

            return View();
        }

        // Método para procesar el formulario con los datos necesarios para crear un asesor
        [HttpPost]
        public ActionResult AgregarAsesor(AsesorModel asesor)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                if (ModelState.IsValid)
                {
                    // Aquí se define que el correo es la identificación.
                    asesor.idAsesor = asesor.correo;

                    if (!accesoAUsuario.ExisteUsuario(asesor.idAsesor))
                    {
                        if (!string.IsNullOrEmpty(asesor.contrasena) && asesor.contrasena == asesor.confirmarContrasena)
                        {
                            accesoAUsuario.CrearUsuario(asesor.idAsesor, asesor.contrasena);
                        }
                        else
                        {
                            ViewBag.ErrorMessage = "Las contraseñas ingresadas deben coincidir.";
                            ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                            return View("CrearAsesor", asesor);
                        }
                    }

                    if (!accesoAAsesor.ExisteAsesor(asesor.idAsesor))
                    {
                        accesoAAsesor.CrearAsesor(asesor);

                        TempData["successMessage"] = "El/la facilitador(a) se agregó con éxito.";
                        return RedirectToAction("ListaAsesores");

                    }
                    else
                    {
                        TempData["errorMessage"] = "Ya existe un(a) facilitador(a) con los mismos datos.";
                        return RedirectToAction("ListaAsesores");
                    }

                }
                else
                {
                    // Si el formulario no es válido o hubo algún problema, regresar a la vista del formulario
                    ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                    return View("CrearAsesor", asesor);
                }
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "Ocurrió un error crear el/la facilitador(a).";
                return RedirectToAction("ListaAsesores");
            }
        }


        // Método de la vista del formulario para editar a un asesor
        public ActionResult EditarAsesor(string idAsesor)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            try
            {
                AsesorModel asesor = accesoAAsesor.ObtenerAsesor(idAsesor);
                ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                return View(asesor);
            }
            catch
            {
                TempData["errorMessage"] = "Ocurrió un error al obtener los datos del/la facilitador(a).";
                return RedirectToAction("ListaAsesores");
            }
        }

        [HttpPost]
        public ActionResult ActualizarAsesor(AsesorModel asesor)
        {
            ViewBag.Id = GetId();
            ViewBag.Role = GetRole();

            if (!ModelState.IsValid)
            {
                ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                return View("EditarAsesor", asesor);
            }
            else
            {
                try
                {
                    if (GetRole() == 1)
                    {
                        if (asesor.contrasena == asesor.confirmarContrasena)
                        {
                            accesoAAsesor.EditarAsesor(asesor);

                            NewLoginModel usuario = new NewLoginModel
                            {
                                oldId = asesor.idAsesor,
                                id = asesor.correo,
                                role = 2,
                                nuevaContrasena = asesor.contrasena
                            };

                            if (asesor.idAsesor != asesor.correo)
                            {
                                EditarIdUsuario(usuario);
                            }

                            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(usuario.id);

                            if (participante != null)
                            {
                                AsesorModel asesorActualizado = accesoAAsesor.ObtenerAsesor(usuario.id);

                                participante.nombre = asesorActualizado.nombre;
                                participante.primerApellido = asesorActualizado.primerApellido;
                                participante.segundoApellido = asesorActualizado.segundoApellido;
                                participante.correo = asesorActualizado.correo;
                                participante.tipoIdentificacion = asesorActualizado.tipoIdentificacion;
                                participante.numeroIdentificacion = asesorActualizado.numeroIdentificacion;
                                participante.telefono = asesorActualizado.telefono;

                                accesoAParticipante.EditarParticipante(participante);
                            } 

                            TempData["successMessage"] = "Los datos de/la facilitador(a) fueron guardados.";
                            return RedirectToAction("ListaAsesores");
                        }
                        else
                        {
                            ViewBag.ErrorMessage = "Las contraseñas ingresadas deben coincidir.";

                            ViewData["Temas"] = accesoATema.ObtenerListaSeleccionTemas();
                            return View("EditarAsesor", asesor);
                        }
                    }
                }
                catch
                {
                    TempData["errorMessage"] = "Ocurrió un error al editar los datos del/la facilitador(a).";
                }

                return RedirectToAction("ListaGruposDisponibles", "Grupo");
            }
        }

        private bool EditarIdUsuario(NewLoginModel usuario)
        {
            bool exito = accesoAUsuario.CrearUsuario(usuario.id, usuario.nuevaContrasena, usuario.role);

            if (usuario.role == 0 && exito)
            {
                EditarIdParticipante(usuario);
            }

            if (usuario.role == 2 && exito)
            {
                EditarIdAsesor(usuario);
            }

            exito = accesoAUsuario.EliminarUsuario(usuario.oldId);

            return exito;
        }

        private void EditarIdParticipante(NewLoginModel usuario)
        {
            ParticipanteModel participante = accesoAParticipante.ObtenerParticipante(usuario.oldId);

            if (participante != null)
            {
                participante.idParticipante = usuario.id;
                participante.correo = usuario.oldId;

                accesoAParticipante.EditarIdParticipante(participante);
            }
        }

        private void EditarIdAsesor(NewLoginModel usuario)
        {
            EditarIdParticipante(usuario);

            AsesorModel existingAsesor = accesoAAsesor.ObtenerAsesor(usuario.oldId);

            if (existingAsesor != null)
            {
                AsesorModel newAsesor = new AsesorModel
                {
                    idAsesor = usuario.id,
                    correo = usuario.id,
                    nombre = existingAsesor.nombre,
                    primerApellido = existingAsesor.primerApellido,
                    segundoApellido = existingAsesor.segundoApellido,
                    tipoIdentificacion = existingAsesor.tipoIdentificacion,
                    numeroIdentificacion = existingAsesor.numeroIdentificacion,
                    descripcion = existingAsesor.descripcion,
                    telefono = existingAsesor.telefono
                };

                accesoAAsesor.CrearAsesor(newAsesor);

                List<GrupoModel> gruposAsesor = accesoAGrupo.ObtenerListaGruposAsesor(usuario.oldId);

                if (gruposAsesor != null)
                {
                    accesoAGrupo.EditarIdGruposAsesor(usuario.id, usuario.oldId);
                }

                accesoAAsesor.EliminarAsesor(usuario.oldId);
            }
        }

        public ActionResult EliminarAsesor(string idAsesor)
        {
            ViewBag.Role = GetRole();
            ViewBag.Id = GetId();

            // Verificar si se puede eliminar el asesor (no está asignado a ningún grupo)
            if (PuedeEliminarAsesor(idAsesor))
            {
                bool exito = accesoAAsesor.EliminarAsesor(idAsesor);
                if (exito)
                {
                    TempData["successMessage"] = "El/la facilitador(a) se eliminó.";
                }
                else
                {
                    TempData["errorMessage"] = "Hubo un error y no se pudo eliminar el/la facilitador(a).";
                }
            }
            else
            {
                TempData["errorMessage"] = "No se puede eliminar el/la facilitador(a) porque está asignado(a) a un módulo.";
            }

            return RedirectToAction("ListaAsesores");
        }

        // Método para verificar si un asesor puede ser eliminado
        public bool PuedeEliminarAsesor(string idAsesor)
        {
            bool eliminar = true;

            try
            {
                List<GrupoModel> gruposAsesor = accesoAGrupo.ObtenerListaGruposAsesor(idAsesor);
                if (gruposAsesor != null && gruposAsesor.Any(g => g.esVisible == true))
                {
                    eliminar = false;
                }
            }
            catch
            {
                eliminar = false;
            }

            return eliminar;
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
    }
}