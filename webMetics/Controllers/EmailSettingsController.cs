using Microsoft.AspNetCore.Mvc;
using NETCore.MailKit.Core;
using Newtonsoft.Json.Linq;
using System;
using Microsoft.AspNetCore.Mvc;
using webMetics.Handlers;

namespace webMetics.Controllers
{
    /// <summary>
    /// Permite a los administradores habilitar/deshabilitar el envío de correos electrónicos y
    /// configurar el correo de notificación para el límite de horas. Los cambios se persisten
    /// en <c>appsettings.json</c>.
    /// </summary>
    public class EmailSettingsController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    private protected InscripcionHandler accesoAInscripcion;

    private static bool _isEmailEnabled = false;

    public EmailSettingsController(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;

        accesoAInscripcion = new InscripcionHandler(environment, configuration);
    }

    /// <summary>Obtiene el rol del usuario autenticado desde la cookie "rolUsuario".</summary>
    private int GetRole()
    {
        int role = 0;

        if (HttpContext.Request.Cookies.ContainsKey("rolUsuario"))
        {
            role = Convert.ToInt32(Request.Cookies["rolUsuario"]);
        }

        return role;
    }

    /// <summary>Obtiene el identificador del usuario autenticado desde la cookie "idUsuario".</summary>
    private string GetId()
    {
        string id = "";

        if (HttpContext.Request.Cookies.ContainsKey("idUsuario"))
        {
            id = Convert.ToString(Request.Cookies["idUsuario"]);
        }

        return id;
    }

    /// <summary>Muestra la pantalla de configuración de correo electrónico con el estado actual del envío.</summary>
    /// <returns>
    /// View: Index (model: bool — isEnabled) —
    /// ViewBag.CorreoNotificacion, ViewBag.Role, ViewBag.Id,
    /// ViewBag.ErrorMessage, ViewBag.SuccessMessage.
    /// </returns>
    /// <remarks>Handlers: InscripcionHandler. Role required: Admin (1).</remarks>
    public IActionResult Index()
    {
        ViewBag.Id = GetId();
        ViewBag.Role = GetRole();

        // Gestionar mensajes de TempData
        ViewBag.ErrorMessage = TempData["errorMessage"]?.ToString();
        ViewBag.SuccessMessage = TempData["successMessage"]?.ToString();

        ViewBag.CorreoNotificacion = accesoAInscripcion.ObtenerCorreoLimiteHoras();

        return View(_isEmailEnabled);
    }

    /// <summary>
    /// Habilita o deshabilita el envío de correos electrónicos y persiste el cambio en
    /// <c>appsettings.json</c>.
    /// </summary>
    /// <param name="isEnabled"><c>true</c> para habilitar el envío; <c>false</c> para deshabilitarlo.</param>
    /// <returns>
    /// Redirects to Index. Sets TempData["successMessage"] si se habilitó o
    /// TempData["errorMessage"] si se deshabilitó.
    /// </returns>
    /// <remarks>Role required: Admin (1).</remarks>
    [HttpPost]
    public IActionResult ToggleEmailSending(bool isEnabled)
    {
        ViewBag.Id = GetId();
        ViewBag.Role = GetRole();

        _isEmailEnabled = isEnabled;

        SaveSettingsToJsonFile();

        if (_isEmailEnabled)
        {
            TempData["successMessage"] = "Envío de correos habilitado.";
        } else
        {
            TempData["errorMessage"] = "Envío de correos deshabilitado.";
        }

        return RedirectToAction("Index");
    }

    /// <summary>Persiste el valor de <c>EmailSettings:IsEnabled</c> en el archivo <c>appsettings.json</c>.</summary>
    private void SaveSettingsToJsonFile()
    {
        // Path to the appsettings.json file
        var filePath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
        var json = System.IO.File.ReadAllText(filePath);

        // Deserialize the JSON into a JObject
        JObject jsonObj = JObject.Parse(json);

        // Update the EmailSettings in the JObject
        jsonObj["EmailSettings"]["IsEnabled"] = _isEmailEnabled;

        // Serialize the updated JObject back to JSON
        var output = jsonObj.ToString();

        // Write the updated JSON back to the file
        System.IO.File.WriteAllText(filePath, output);
    }

    /// <summary>
    /// Crea o actualiza el correo de notificación utilizado cuando un participante alcanza el límite de horas.
    /// </summary>
    /// <param name="correoLimiteHoras">Dirección de correo electrónico de notificación.</param>
    /// <returns>
    /// Redirects to la URL referente (Referer) si está disponible; de lo contrario redirige a
    /// EmailSettings/Index. Sets TempData["successMessage"] on success or TempData["errorMessage"].
    /// </returns>
    /// <remarks>Handlers: InscripcionHandler. Role required: Admin (1).</remarks>
    [HttpPost]
    public ActionResult UpdateNotificationEmail(string correoLimiteHoras)
    {
        ViewBag.Role = GetRole();
        ViewBag.Id = GetId();

        string correo = accesoAInscripcion.ObtenerCorreoLimiteHoras();

        bool exito = (string.IsNullOrEmpty(correo)) ? accesoAInscripcion.IngresarCorreoLimiteHoras(correoLimiteHoras) : accesoAInscripcion.ActualizarCorreoLimiteHoras(correoLimiteHoras);

        if (exito)
        {
            TempData["successMessage"] = "Se actualizó el correo de notificación.";
        }
        else
        {
            TempData["errorMessage"] = "Debe introducir un correo de notificación válido.";
        }

        var refererUrl = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(refererUrl))
        {
            return Redirect(refererUrl);
        }

        return RedirectToAction("Index", "EmailSettings");
    }
}
}