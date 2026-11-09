using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using webMetics.Services;

var source = Environment.GetEnvironmentVariable("METICS_TEST_CONNECTION")
    ?? throw new InvalidOperationException("Defina METICS_TEST_CONNECTION para SQL Server local; no se usan credenciales del repositorio automáticamente.");
var builder = new SqlConnectionStringBuilder(source);
if (!(builder.DataSource.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
    || builder.DataSource.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase)))
    throw new InvalidOperationException("Las pruebas solo admiten SQL Server en localhost/127.0.0.1.");
var database = "METICS_Medallas_Test_" + Guid.NewGuid().ToString("N");
builder.InitialCatalog = "master";
var masterConnection = builder.ConnectionString;
var temporaryDirectory = Path.Combine(Path.GetTempPath(), database);
Directory.CreateDirectory(Path.Combine(temporaryDirectory, "images", "medallas"));
var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception("Fallo: " + message);
    checks++;
    Console.WriteLine("OK: " + message);
}
async Task<object?> Sql(string sql, string connectionString)
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new SqlCommand(sql, connection);
    return await command.ExecuteScalarAsync();
}
async Task MustFail(Func<Task> operation, string message)
{
    try { await operation(); }
    catch { Check(true, message); return; }
    throw new Exception("Se esperaba un error: " + message);
}
FormFile Image(string name, string contents = "test image")
{
    var bytes = System.Text.Encoding.UTF8.GetBytes(contents);
    return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "imageFile", name);
}
try
{
    await Sql($"CREATE DATABASE [{database}];", masterConnection);
    builder.InitialCatalog = database;
    var connectionString = builder.ConnectionString;
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:DefaultConnection"] = connectionString }).Build();
    var environment = new TestEnvironment { WebRootPath = temporaryDirectory };
    var service = new MedallasCatalogoService(environment, configuration);
    string Asset(string name) => Path.Combine(temporaryDirectory, "images", "medallas", name);
    await File.WriteAllTextAsync(Asset("legacy.png"), "legacy");
    await File.WriteAllTextAsync(Asset("new.PNG"), "new");
    await File.WriteAllTextAsync(Asset("ignored.txt"), "not an image");
    await Sql("""
        CREATE TABLE dbo.medallas (id_participante_FK nvarchar(64) NOT NULL, nombre_medalla nvarchar(512) NOT NULL);
        INSERT INTO dbo.medallas VALUES (N'participant@ucr.ac.cr', N'legacy.png');
        """, connectionString);
    await MustFail(() => service.SincronizarAsync(), "Arranque sin migración exige aplicar el SQL");
    var migration = await File.ReadAllTextAsync("scripts/medallas/001_catalogo_medallas.sql");
    await Sql(migration, connectionString);
    await Sql(migration, connectionString);
    Check(Convert.ToInt32(await Sql("SELECT COUNT(*) FROM dbo.medallas;", connectionString)) == 1,
        "Migración repetible conserva asignaciones históricas");
    var results = await Task.WhenAll(service.SincronizarAsync(), service.SincronizarAsync());
    Check(results.Sum() == 1, "Arranques concurrentes registran una sola vez cada imagen nueva");
    Check(service.ObtenerNombres().Count == 2, "El catálogo incluye imágenes válidas sin requerir asignaciones");
    Check(Convert.ToInt32(await Sql("SELECT COUNT(*) FROM dbo.medallas;", connectionString)) == 1,
        "El seed no otorga medallas a nadie");
    service.Asignar("new.PNG", new[] { "participant@ucr.ac.cr", "participant@ucr.ac.cr" });
    await Task.WhenAll(Task.Run(() => service.Asignar("new.PNG", new[] { "participant@ucr.ac.cr" })),
        Task.Run(() => service.Asignar("new.PNG", new[] { "participant@ucr.ac.cr" })));
    Check(Convert.ToInt32(await Sql("SELECT COUNT(*) FROM dbo.medallas WHERE nombre_medalla=N'new.PNG';", connectionString)) == 1,
        "La asignación repetida/concurrente no duplica premios");
    await service.GuardarImagenAsync(Image("renamed.png"), "legacy.png");
    Check(Convert.ToInt32(await Sql("SELECT COUNT(*) FROM dbo.medallas WHERE nombre_medalla=N'renamed.png';", connectionString)) == 1,
        "Editar con otro nombre conserva las asignaciones");
    await service.SincronizarAsync();
    Check(!service.Existe("legacy.png"), "El seed no revive el nombre anterior aunque siga publicado");
    await service.GuardarImagenAsync(Image("renamed.png", "replacement"), "renamed.png");
    Check(await File.ReadAllTextAsync(Asset("renamed.png")) == "replacement"
        && Convert.ToInt32(await Sql("SELECT COUNT(*) FROM dbo.medallas WHERE nombre_medalla=N'renamed.png';", connectionString)) == 1,
        "Reemplazar la imagen con el mismo nombre conserva los premios");
    await MustFail(() => service.GuardarImagenAsync(Image("new.PNG", "must not overwrite"), "renamed.png"),
        "La edición rechaza nombres de otra medalla");
    Check(await File.ReadAllTextAsync(Asset("new.PNG")) == "new", "Una colisión no sobrescribe la imagen existente");
    await MustFail(() => service.GuardarImagenAsync(Image("failed.png"), "missing.png"),
        "Una edición con origen inexistente revierte la operación SQL");
    Check(!File.Exists(Asset("failed.png")) && !service.Existe("failed.png"),
        "Si falla SQL se retira la imagen nueva y no queda registrada");
    var previousImage = await File.ReadAllTextAsync(Asset("legacy.png"));
    await MustFail(() => service.GuardarImagenAsync(Image("legacy.png", "must roll back"), "missing.png"),
        "Un error SQL también revierte una imagen sobrescrita");
    Check(await File.ReadAllTextAsync(Asset("legacy.png")) == previousImage && !service.Existe("legacy.png"),
        "Restaura la imagen previa y conserva su estado inactivo");
    Check(service.Eliminar("new.PNG"), "Se puede eliminar una medalla del catálogo");
    Check(Convert.ToInt32(await Sql("SELECT COUNT(*) FROM dbo.medallas WHERE nombre_medalla=N'new.PNG';", connectionString)) == 0,
        "Eliminar catálogo conserva el comportamiento de retirar sus asignaciones");
    await service.SincronizarAsync();
    await Sql(migration, connectionString);
    Check(!service.Existe("new.PNG"), "Ni el seed ni repetir la migración reviven medallas eliminadas");
    await MustFail(() => Task.Run(() => service.Asignar("new.PNG", new[] { "participant@ucr.ac.cr" })),
        "No permite asignar medallas inactivas");
    await service.GuardarImagenAsync(Image("new.PNG", "reactivated"));
    Check(service.Existe("new.PNG") && Convert.ToInt32(await Sql("SELECT COUNT(*) FROM dbo.medallas WHERE nombre_medalla=N'new.PNG';", connectionString)) == 0,
        "Una subida explícita puede reactivar el catálogo sin otorgar la medalla al administrador");
    await MustFail(() => service.GuardarImagenAsync(Image("../outside.png")), "Rechaza rutas fuera de la carpeta de medallas");
    await MustFail(() => service.GuardarImagenAsync(Image("not-image.txt")), "Rechaza extensiones no soportadas");
    await service.GuardarImagenAsync(Image("only-catalog.png"));
    Check(service.Eliminar("only-catalog.png"), "Eliminar funciona aun sin asignaciones");
    File.Delete(Asset("renamed.png"));
    Check(!service.ObtenerNombres().Contains("renamed.png")
        && Convert.ToInt32(await Sql("SELECT COUNT(*) FROM dbo.medallas WHERE nombre_medalla=N'renamed.png';", connectionString)) == 1,
        "Una imagen ausente no se ofrece para asignar y no borra premios históricos");
    await service.GuardarImagenAsync(Image("legacy.png"));
    await Sql("DELETE FROM dbo.medallas;", connectionString);
    Check(service.ObtenerNombres().Count > 0, "Retirar todos los premios no vacía el catálogo");
    Console.WriteLine($"{checks} comprobaciones correctas. Base temporal: {database}");
}
finally
{
    SqlConnection.ClearAllPools();
    await Sql($"IF DB_ID(N'{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END;", masterConnection);
    if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, recursive: true);
}

sealed class TestEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "MedallasChecks";
    public string EnvironmentName { get; set; } = "Development";
    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
