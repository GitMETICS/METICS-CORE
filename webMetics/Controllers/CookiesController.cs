using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace webMetics.Controllers
{
    /// <summary>
    /// Expone operaciones CRUD sobre las cookies del navegador. Utilizado internamente por
    /// JavaScript del cliente para crear, actualizar, eliminar y leer cookies de sesión.
    /// </summary>
    public class CookiesController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public CookiesController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
        }

        /// <summary>Crea una cookie con el nombre, valor y tiempo de expiración indicados.</summary>
        /// <param name="cookieName">Nombre de la cookie.</param>
        /// <param name="cookieValue">Valor de la cookie.</param>
        /// <param name="expirationTime">Fecha y hora de expiración.</param>
        /// <returns>HTTP 200 OK.</returns>
        public IActionResult CreateCookie(string cookieName, string cookieValue, DateTimeOffset expirationTime)
        {
            Response.Cookies.Append(cookieName, cookieValue, new CookieOptions
            {
                Expires = expirationTime.DateTime
            });

            return Ok();
        }

        /// <summary>Actualiza el valor de una cookie existente; si no existe la crea con expiración de 1 hora.</summary>
        /// <param name="cookieName">Nombre de la cookie a actualizar.</param>
        /// <param name="value">Nuevo valor para la cookie.</param>
        /// <returns>HTTP 200 OK.</returns>
        public IActionResult UpdateCookie(string cookieName, string value)
        {
            if (Request.Cookies.TryGetValue(cookieName, out _))
            {
                Response.Cookies.Append(cookieName, value);
            }
            else
            {
                Response.Cookies.Append(cookieName, value, new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddHours(1).DateTime
                });
            }

            return Ok();
        }

        /// <summary>Elimina la cookie con el nombre indicado.</summary>
        /// <param name="cookieName">Nombre de la cookie a eliminar.</param>
        /// <returns>HTTP 200 OK.</returns>
        public IActionResult DeleteCookie(string cookieName)
        {
            Response.Cookies.Delete(cookieName);

            return Ok();
        }

        /// <summary>Lee el valor de una cookie; retorna "0" si la cookie no existe.</summary>
        /// <param name="cookieName">Nombre de la cookie a leer.</param>
        /// <returns>Valor de la cookie o "0" si no existe.</returns>
        public string FetchCookieValue(string cookieName)
        {
            if (Request.Cookies.TryGetValue(cookieName, out var value))
            {
                return value;
            }

            return "0";
        }
    }
}
