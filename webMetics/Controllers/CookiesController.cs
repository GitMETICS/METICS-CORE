using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace webMetics.Controllers
{
    public class CookiesController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public CookiesController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult CreateCookie(string cookieName, string cookieValue, DateTimeOffset expirationTime)
        {
            Response.Cookies.Append(cookieName, cookieValue, new CookieOptions
            {
                Expires = expirationTime.DateTime
            });

            return Ok();
        }

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

        public IActionResult DeleteCookie(string cookieName)
        {
            Response.Cookies.Delete(cookieName);

            return Ok();
        }

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
