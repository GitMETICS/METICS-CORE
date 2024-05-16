using System;
using System.Web;
using System.Web.Mvc;

/* 
 * Controlador de las cookies para administar los roles de los usuarios 
 * El rol de los usuarios se guardan como cookies para mantener la sesión activa y administracion de los permisos
 * En esta clase se puede crear, actualizar y eliminar las cookies
 */
namespace webMetics.Controllers
{
    public class CookiesController : Controller
    {
        // Método para crear una nueva cookie con el nombre, valor y tiempo de expiración proporcionados
        public HttpCookie CreateCookie(string cookieName, string cookieValue, DateTime expirationTime)
        {
            // Crear una nueva instancia de HttpCookie con el nombre especificado
            HttpCookie cookie = new HttpCookie(cookieName);

            // Asignar el valor de la cookie
            cookie.Value = cookieValue;

            // Establecer la fecha y hora de expiración de la cookie
            cookie.Expires = expirationTime;

            // Devolver la cookie creada
            return cookie;
        }

        // Método para actualizar el valor de una cookie existente o crear una nueva si no existe
        public HttpCookie UpdateCookie(string cookieName, string value)
        {
            HttpCookie cookie = null;

            // Obtener el valor actual de la cookie
            string currentValue = FetchCookieValue(cookieName);

            // Si el valor actual es "0", significa que la cookie no existe, entonces se crea una nueva con el valor proporcionado y expiración de 1 hora.
            if (currentValue == "0")
            {
                cookie = CreateCookie(cookieName, value, DateTime.Now.AddHours(1));
            }
            else
            {
                // Si la cookie ya existe, se obtiene la cookie actual
                cookie = Request.Cookies[cookieName];

                // Se actualiza el valor de la cookie con el valor proporcionado
                cookie.Value = value;
            }

            // Devolver la cookie actualizada o la nueva creada
            return cookie;
        }

        // Método para eliminar una cookie con el nombre proporcionado
        public void DeleteCookie(string cookieName)
        {
            // Establecer la fecha de expiración de la cookie a una fecha anterior a la actual para eliminarla
            Request.Cookies[cookieName].Expires = DateTime.Now.AddDays(-1);
        }

        // Método para obtener el valor de una cookie con el nombre proporcionado
        public string FetchCookieValue(string cookieName)
        {
            string value = null;

            try
            {
                // Intentar obtener el valor de la cookie con el nombre proporcionado
                value = Request.Cookies[cookieName].Value;
            }
            catch
            {
                // Si la cookie no existe o hay algún error al obtenerla, se asigna "0" al valor
                value = "0";
            }

            // Devolver el valor de la cookie
            return value;
        }
    }

}