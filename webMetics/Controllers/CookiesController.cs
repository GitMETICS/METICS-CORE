using Microsoft.AspNetCore.Mvc;
using System;

namespace webMetics.Controllers
{
    public class CookiesController : Controller
    {
        public IActionResult CreateCookie(string cookieName, string cookieValue, DateTimeOffset expirationTime)
        {
            Response.Cookies.Append(cookieName, cookieValue, new Microsoft.AspNetCore.Http.CookieOptions
            {
                Expires = expirationTime.DateTime
            });

            return Ok();
        }

        public IActionResult UpdateCookie(string cookieName, string value)
        {
            if (Request.Cookies.TryGetValue(cookieName, out string currentValue))
            {
                Response.Cookies.Append(cookieName, value);
            }
            else
            {
                Response.Cookies.Append(cookieName, value, new Microsoft.AspNetCore.Http.CookieOptions
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
            if (Request.Cookies.TryGetValue(cookieName, out string value))
            {
                return value;
            }

            return "0";
        }
    }
}
