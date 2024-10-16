using Microsoft.AspNetCore.Mvc;
using NETCore.MailKit.Core;
using Newtonsoft.Json.Linq;
using System;

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
