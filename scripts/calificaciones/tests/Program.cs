using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using webMetics.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.FileProviders;
using webMetics.Controllers;

var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("METICS_TEST_CONNECTION")
    ?? throw new InvalidOperationException("Defina METICS_TEST_CONNECTION para SQL Server local."));
if (!builder.DataSource.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
    && !builder.DataSource.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Las pruebas solo admiten SQL Server local.");
var database = "METICS_Calificaciones_Test_" + Guid.NewGuid().ToString("N");
builder.InitialCatalog = "master";
var master = builder.ConnectionString;
builder.InitialCatalog = database;
var connection = builder.ConnectionString;
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    { ["ConnectionStrings:DefaultConnection"] = connection }).Build();
var service = new CalificacionParticipanteService(config);
var checks = 0;
void Check(bool value, string message)
{
    if (!value) throw new Exception("Fallo: " + message);
    checks++;
    Console.WriteLine("OK: " + message);
}
async Task<object?> Sql(string text, string? target = null)
{
    await using var cn = new SqlConnection(target ?? connection);
    await cn.OpenAsync();
    await using var command = new SqlCommand(text, cn);
    return await command.ExecuteScalarAsync();
}
void Fails(Action action, string message)
{
    try { action(); }
    catch { Check(true, message); return; }
    throw new Exception("Se esperaba error: " + message);
}
async Task<string> Snapshot() => Convert.ToString(await Sql("""
    SELECT CONCAT(horas_aprobadas, '|',horas_matriculadas,'|',calificacion,'|',estado)
    FROM dbo.inscripcion WHERE id_inscripcion_PK=1;
    """))!;
try
{
    await Sql($"CREATE DATABASE [{database}];", master);
    await Sql("""
        CREATE TABLE dbo.grupo(id_grupo_PK int PRIMARY KEY, cantidad_horas int NOT NULL);
        CREATE TABLE dbo.participante(id_participante_PK nvarchar(64) PRIMARY KEY,
            total_horas_aprobadas int NOT NULL, total_horas_matriculadas int NOT NULL);
        CREATE TABLE dbo.inscripcion(id_inscripcion_PK int PRIMARY KEY,id_grupo_FK int,id_participante_FK nvarchar(64),
            horas_aprobadas int,horas_matriculadas int,calificacion float,estado nvarchar(32));
        INSERT INTO dbo.grupo VALUES(1,10),(2,4);
        INSERT INTO dbo.participante VALUES(N'qa@ucr.ac.cr',4,0);
        INSERT INTO dbo.inscripcion VALUES(1,1,N'qa@ucr.ac.cr',5,5,85,N'Incompleto'),(2,2,N'qa@ucr.ac.cr',4,0,100,N'Aprobado');
        """);
    Check(!service.Guardar(1, "qa@ucr.ac.cr", null, null), "Campos vacíos devuelven aviso, sin guardar");
    Check(!service.Guardar(1, "qa@ucr.ac.cr", 5, 85), "Valores iguales devuelven aviso, sin guardar");
    Check(service.Guardar(1, "qa@ucr.ac.cr", 0, 0), "Acepta horas y nota en cero");
    Check(await Snapshot() == "0|10|0|Inscrito", "El cero persiste y recalcula horas/estado");
    Check(Convert.ToInt32(await Sql("SELECT total_horas_aprobadas FROM dbo.participante;")) == 4
        && Convert.ToInt32(await Sql("SELECT total_horas_matriculadas FROM dbo.participante;")) == 10,
        "Acumulados conservan otras inscripciones");
    Check(service.Guardar(1, "qa@ucr.ac.cr", null, 90), "Permite actualizar solamente la nota");
    Check(await Snapshot() == "0|10|90|Inscrito", "Nota sola no altera horas");
    Check(service.Guardar(1, "qa@ucr.ac.cr", 10, null), "Permite actualizar solamente horas");
    Check(await Snapshot() == "10|0|90|Aprobado", "Horas solas no alteran la nota");
    Check(Convert.ToInt32(await Sql("SELECT total_horas_aprobadas FROM dbo.participante;")) == 14,
        "La aprobación actualiza el acumulado total");
    foreach (var (hours, grade, message) in new[] {
        ((int?)11,(int?)50,"Rechaza horas superiores al módulo antes de guardar la nota"),
        ((int?)5,(int?)101,"Rechaza nota superior a 100 antes de guardar horas"),
        ((int?)-1,(int?)40,"Rechaza horas negativas"),
        ((int?)5,(int?)-1,"Rechaza notas negativas") })
    {
        var before = await Snapshot();
        Fails(() => service.Guardar(1, "qa@ucr.ac.cr", hours, grade), message);
        Check(await Snapshot() == before, "El rechazo no aplica cambios parciales");
    }
    Fails(() => service.Guardar(999, "qa@ucr.ac.cr", 1, 1), "Rechaza módulo/inscripción inexistente");
    Fails(() => service.Guardar(1, "missing@ucr.ac.cr", 1, 1), "Rechaza participante inexistente");
    Check(service.Guardar(1, "qa@ucr.ac.cr", 5, 80), "Permite corregir una aprobación a incompleto");
    await Sql("ALTER TABLE dbo.participante ADD CONSTRAINT CK_test_rollback CHECK(total_horas_aprobadas<10);");
    var rollbackBefore = await Snapshot();
    Fails(() => service.Guardar(1, "qa@ucr.ac.cr", 10, 99), "Un fallo al guardar acumulados produce error");
    Check(await Snapshot() == rollbackBefore, "El fallo SQL revierte también horas, estado y nota");
    await Sql("ALTER TABLE dbo.participante DROP CONSTRAINT CK_test_rollback;");
    await Task.WhenAll(Task.Run(() => service.Guardar(1, "qa@ucr.ac.cr", 10, 70)),
        Task.Run(() => service.Guardar(2, "qa@ucr.ac.cr", 0, 0)));
    Check(Convert.ToInt32(await Sql("SELECT total_horas_aprobadas FROM dbo.participante;")) == 10
        && Convert.ToInt32(await Sql("SELECT total_horas_matriculadas FROM dbo.participante;")) == 4,
        "Guardados concurrentes del mismo participante mantienen acumulados coherentes");
    CalificacionesController Controller(int role = 1)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"rolUsuario={role}; idUsuario=admin@ucr.ac.cr";
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        return new CalificacionesController(new TestEnvironment(), config, new EmailService(config))
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, new TestTempDataProvider())
        };
    }
    var emptyController = Controller();
    emptyController.SubirHorasAprobadasYCalificacion(1, "qa@ucr.ac.cr", null, null);
    Check(emptyController.TempData.ContainsKey("warningMessage") && !emptyController.TempData.ContainsKey("successMessage"),
        "Controlador: envío vacío genera aviso amarillo, no éxito");
    var invalidController = Controller();
    invalidController.SubirHorasAprobadasYCalificacion(1, "qa@ucr.ac.cr", 11, 30);
    Check(invalidController.TempData.ContainsKey("errorMessage") && !invalidController.TempData.ContainsKey("successMessage"),
        "Controlador: exceso de horas genera error rojo, no éxito");
    var malformedController = Controller();
    malformedController.ModelState.AddModelError("calificacion", "Valor no numérico");
    malformedController.SubirHorasAprobadasYCalificacion(1, "qa@ucr.ac.cr", 5, null);
    Check(malformedController.TempData.ContainsKey("errorMessage"), "Controlador: datos mal formados no se guardan");
    var deniedController = Controller(0);
    deniedController.SubirHorasAprobadasYCalificacion(1, "qa@ucr.ac.cr", 5, 20);
    Check(deniedController.TempData.ContainsKey("errorMessage"), "Controlador: participante no puede editar calificaciones");
    var savedController = Controller();
    savedController.SubirHorasAprobadasYCalificacion(1, "qa@ucr.ac.cr", 0, 0);
    Check(savedController.TempData.ContainsKey("successMessage") && await Snapshot() == "0|10|0|Inscrito",
        "Controlador: guardar ceros persiste y genera éxito verde");
    var repeatedController = Controller();
    repeatedController.SubirHorasAprobadasYCalificacion(1, "qa@ucr.ac.cr", 0, 0);
    Check(repeatedController.TempData.ContainsKey("warningMessage"), "Controlador: valores sin cambios generan aviso amarillo");
    Console.WriteLine($"{checks} comprobaciones correctas.");
}

finally
{
    SqlConnection.ClearAllPools();
    await Sql($"IF DB_ID(N'{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END;", master);
}

sealed class TestTempDataProvider : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
    public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
}
sealed class TestEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "CalificacionesChecks";
    public string EnvironmentName { get; set; } = "Development";
    public string WebRootPath { get; set; } = Path.GetTempPath();
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = Path.GetTempPath();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}