using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;

public class EmailSettingsController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    private static bool _isEmailEnabled = false;

    public EmailSettingsController(IWebHostEnvironment environment)
    {
        _environment = environment;
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
}
