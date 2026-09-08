using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using webMetics.Services;

// Sin fallback a appsettings.json: requiere un SQL Server de pruebas explicito.
string connection = Environment.GetEnvironmentVariable("METICS_TEST_SQL_CONNECTION")
    ?? throw new InvalidOperationException("Definir METICS_TEST_SQL_CONNECTION hacia un servidor desechable.");
string expectedServer = Environment.GetEnvironmentVariable("METICS_TEST_SQL_SERVER")
    ?? throw new InvalidOperationException("Definir METICS_TEST_SQL_SERVER con @@SERVERNAME del servidor desechable.");
var settings = new SqlConnectionStringBuilder(connection) { InitialCatalog = "master" };
using var master = new SqlConnection(settings.ConnectionString);
await master.OpenAsync();
if (!Equals(await Scalar(master, "SELECT CONVERT(nvarchar(128), @@SERVERNAME)"), expectedServer))
    throw new InvalidOperationException("El servidor no coincide. No se crearon ni modificaron bases.");
var root = new DirectoryInfo(AppContext.BaseDirectory);
while (root != null && !File.Exists(Path.Combine(root.FullName, "webMetics.sln"))) root = root.Parent;
if (root == null) throw new InvalidOperationException("No se encontro el repositorio.");
int assertions = 0;
void Equal(string name, object? actual, object? expected)
{
    assertions++;
    if (!Equals(actual, expected)) throw new InvalidOperationException($"{name}: esperado {JsonSerializer.Serialize(expected)}, obtenido {JsonSerializer.Serialize(actual)}");
}

foreach (string environment in new[] { "Dev", "Prod" })
{
    // Nombre propio aleatorio. Solo se elimina esta base, creada por esta ejecucion.
    string database = "MeticsCarrerasTest_" + Guid.NewGuid().ToString("N");
    await Scalar(master, $"CREATE DATABASE [{database}] COLLATE Latin1_General_100_CI_AI_SC;");
    try
    {
        var dbSettings = new SqlConnectionStringBuilder(settings.ConnectionString) { InitialCatalog = database };
        using var db = new SqlConnection(dbSettings.ConnectionString);
        await db.OpenAsync();
        string folder = Path.Combine(root.FullName, "db_scripts", "Migracion", environment);
        string apply = File.ReadAllText(Path.Combine(folder, "01-aplicar_normalizacion_carreras.sql"));
        string revert = File.ReadAllText(Path.Combine(folder, "03-revertir_normalizacion_carreras.sql"));
        string verify = File.ReadAllText(Path.Combine(folder, "02-verificar_normalizacion_carreras.sql"));
        const string table = "CREATE TABLE dbo.participante (id_participante_PK nvarchar(64) PRIMARY KEY, carrera nvarchar(512) NULL);";

        // El guard no deja objetos creados, aun si el cliente continua tras el error.
        int guardErrors = 0;
        foreach (string batch in Batches(apply))
        {
            try { await Scalar(db, batch); }
            catch (SqlException ex) when (ex.Number == 50000) { guardErrors++; }
        }
        Equal("guard sin columna", guardErrors, 1);
        Equal("guard no crea funcion", await Scalar(db, "SELECT OBJECT_ID('dbo.fn_NormalizarCarrera')"), null);
        Equal("guard no crea respaldo", await Scalar(db, "SELECT OBJECT_ID('dbo.participante_carrera_respaldo')"), null);
        await Scalar(db, table);

        var fixtures = new Dictionary<string, string?>
        {
            ["catalogo"] = "Diseño Gráfico", ["puntuacion"] = "DISENO/MODAS",
            ["espacios"] = "   varios    espacios   ", ["final"] = "MEDICINA ",
            ["blancos"] = "   ", ["nfd"] = "Disen\u0303o", ["extendido"] = "Ingeniería Łódź",
            ["nulo"] = null, ["vacio"] = "", ["normalizado"] = "MEDICINA",
            ["maximo"] = new string('á', 512), ["emoji"] = "Diseño🎨modas",
            ["marca_final"] = "ABC\u0301", ["marca_tras_espacio"] = "ABC \u0301 D",
            ["turco"] = "idioma İ ı", ["nul"] = "AB\0CD"
        };
        foreach (var (id, value) in fixtures) await Insert(db, id, value);
        await Script(db, apply);
        foreach (var (id, value) in fixtures)
            Equal("normalizacion " + id, await Career(db, id), value == null ? null : CarreraResolver.Normalizar(value));
        int changed = fixtures.Count(f => f.Value != null && f.Value != CarreraResolver.Normalizar(f.Value));
        Equal("respaldo exacto", await Scalar(db, "SELECT COUNT(*) FROM participante_carrera_respaldo"), changed);
        Equal("valor escrito registrado", await Scalar(db, "SELECT COUNT(*) FROM participante_carrera_respaldo r JOIN participante p ON p.id_participante_PK=r.id_participante_FK WHERE CONVERT(varbinary(max),p.carrera)<>CONVERT(varbinary(max),r.carrera_normalizada) OR r.carrera_normalizada IS NULL"), 0);
        await Script(db, apply);
        Equal("idempotencia", await Scalar(db, "SELECT COUNT(*) FROM participante_carrera_respaldo"), changed);
        await Verify(db, verify);

        // Paridad con el codigo real para cada caracter BMP valido y cadenas mixtas.
        var samples = new DataTable();
        samples.Columns.Add("entrada", typeof(string)); samples.Columns.Add("esperado", typeof(string));
        for (int code = 0; code <= char.MaxValue; code++)
        {
            if (char.IsSurrogate((char)code)) continue;
            string value = "A" + (char)code + "B";
            try { samples.Rows.Add(value, CarreraResolver.Normalizar(value)); }
            catch (ArgumentException) { }
        }
        var random = new Random(7349);
        string alphabet = "ABCxyz019 áéíóúñŁźİı\u0301\u0303\u0345\u1AB0\u093E\u1100\u1161\u11A8/.-";
        for (int i = 0; i < 500; i++)
        {
            string value = new(Enumerable.Range(0, random.Next(1, 513)).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());
            samples.Rows.Add(value, CarreraResolver.Normalizar(value));
        }
        using (var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "webMetics/wwwroot/data/dataAreas.json"))))
        {
            foreach (var area in catalog.RootElement.GetProperty("areas").EnumerateArray())
            foreach (var department in area.GetProperty("departamentos").EnumerateArray())
            if (department.TryGetProperty("secciones", out var sections)) foreach (var section in sections.EnumerateArray())
            if (section.TryGetProperty("carreras", out var careers)) foreach (var campus in careers.EnumerateObject())
            foreach (var career in campus.Value.EnumerateArray())
            {
                string value = career.GetString()!;
                samples.Rows.Add(value, CarreraResolver.Normalizar(value));
                samples.Rows.Add(value.Normalize(NormalizationForm.FormD), CarreraResolver.Normalizar(value));
            }
        }
        await Scalar(db, "CREATE TABLE #paridad(entrada nvarchar(512), esperado nvarchar(512));");
        using (var bulk = new SqlBulkCopy(db)) { bulk.DestinationTableName = "#paridad"; await bulk.WriteToServerAsync(samples); }
        using (var command = new SqlCommand("SELECT TOP(1) entrada, esperado, dbo.fn_NormalizarCarrera(entrada) FROM #paridad WHERE CONVERT(varbinary(max),esperado)<>CONVERT(varbinary(max),dbo.fn_NormalizarCarrera(entrada))", db) { CommandTimeout = 180 })
        using (var reader = await command.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync()) throw new Exception($"Paridad Unicode: {JsonSerializer.Serialize(reader.GetString(0))}, C#={reader.GetString(1)}, SQL={reader.GetString(2)}");
        }
        assertions += samples.Rows.Count;
        Console.WriteLine($"{environment}: paridad SQL/C# aprobada para {samples.Rows.Count} entradas.");

        // Cambios posteriores diferentes solo por espacios y NULL deben conservarse.
        await Scalar(db, "UPDATE participante SET carrera=N'DISENO GRAFICO ' WHERE id_participante_PK='catalogo'; UPDATE participante SET carrera=NULL WHERE id_participante_PK='blancos';");
        await Script(db, revert);
        Equal("conservar edicion con espacio", await Career(db, "catalogo"), "DISENO GRAFICO ");
        Equal("conservar edicion NULL", await Career(db, "blancos"), null);
        Equal("restaurar espacio original", await Career(db, "final"), "MEDICINA ");
        Equal("restaurar Unicode", await Career(db, "nfd"), fixtures["nfd"]);
        await Script(db, revert);
        Equal("reversion idempotente", await Career(db, "final"), "MEDICINA ");
        await Scalar(db, "UPDATE participante SET carrera=N'Nuevo diseño!' WHERE id_participante_PK='puntuacion';");
        await Script(db, apply);
        await Script(db, revert);
        Equal("ultimo respaldo", await Career(db, "puntuacion"), "Nuevo diseño!");

        await Scalar(db, "DELETE participante; DELETE participante_carrera_respaldo;");
        await Insert(db, "fallo", "Diseño!");
        await Scalar(db, "CREATE TRIGGER dbo.FalloPrueba ON participante AFTER UPDATE AS THROW 51000, 'Fallo simulado', 1;");
        bool failed = false;
        try { await Script(db, apply); } catch (SqlException ex) when (ex.Number == 51000) { failed = true; }
        Equal("fallo provocado", failed, true);
        Equal("fallo revierte datos", await Career(db, "fallo"), "Diseño!");
        Equal("fallo revierte respaldo", await Scalar(db, "SELECT COUNT(*) FROM participante_carrera_respaldo"), 0);
        await Scalar(db, "DROP TRIGGER dbo.FalloPrueba; DELETE participante;");

        // Solapamiento real: un escritor retiene el bloqueo hasta que la migracion espera.
        await Insert(db, "concurrente", "Diseño primero!");
        using (var writer = new SqlConnection(dbSettings.ConnectionString))
        using (var migration = new SqlConnection(dbSettings.ConnectionString))
        {
            await writer.OpenAsync(); await migration.OpenAsync();
            int migrationId = Convert.ToInt32(await Scalar(migration, "SELECT @@SPID"));
            using var transaction = writer.BeginTransaction();
            using (var command = new SqlCommand("UPDATE participante SET carrera=N'Diseño segundo!' WHERE id_participante_PK='concurrente'", writer, transaction)) await command.ExecuteNonQueryAsync();
            string updateBatch = Batches(apply).Single(b => b.Contains("OUTPUT deleted.id_participante_PK"));
            Task<object?> running = Scalar(migration, updateBatch);
            var timeout = Stopwatch.StartNew();
            bool blocked = false;
            while (timeout.Elapsed < TimeSpan.FromSeconds(10))
            {
                if ((int)(await Scalar(master, $"SELECT COUNT(*) FROM sys.dm_exec_requests WHERE session_id={migrationId} AND blocking_session_id>0"))! > 0) { blocked = true; break; }
                if (running.IsCompleted) break;
                await Task.Delay(25);
            }
            transaction.Commit();
            await running;
            Equal("se comprobo solapamiento", blocked, true);
        }
        Equal("normalizar ultima escritura", await Career(db, "concurrente"), "DISENO SEGUNDO");
        Equal("respaldar ultima escritura", await Scalar(db, "SELECT carrera_original FROM participante_carrera_respaldo WHERE id_participante_FK='concurrente'"), "Diseño segundo!");
        await Script(db, revert);
        Equal("revertir escritura concurrente", await Career(db, "concurrente"), "Diseño segundo!");
        await Scalar(db, "DELETE participante_carrera_respaldo;");
        await Task.WhenAll(RunSeparate(dbSettings.ConnectionString, apply), RunSeparate(dbSettings.ConnectionString, apply));
        Equal("dos migraciones no duplican respaldo", await Scalar(db, "SELECT COUNT(*) FROM participante_carrera_respaldo"), 1);
        await Scalar(db, "DELETE participante;");
        Equal("borrado conserva respaldo", await Scalar(db, "SELECT COUNT(*) FROM participante_carrera_respaldo"), 1);

        // Compatibilidad con el esquema historico y la version de una fila por persona.
        foreach (bool oldPrimaryKey in new[] { false, true })
        {
            await Scalar(db, "DROP TABLE participante_carrera_respaldo; DELETE participante;");
            await Scalar(db, "CREATE TABLE participante_carrera_respaldo (" +
                (oldPrimaryKey ? "id_participante_FK nvarchar(64) PRIMARY KEY," : "id_respaldo int IDENTITY PRIMARY KEY, id_participante_FK nvarchar(64),") +
                "carrera_original nvarchar(512), fecha_respaldo datetime2(3) DEFAULT SYSUTCDATETIME());");
            await Insert(db, "anterior", "DISEN O");
            await Scalar(db, "INSERT participante_carrera_respaldo(id_participante_FK,carrera_original) VALUES(N'anterior',N'Disen'+NCHAR(771)+N'o');");
            await Scalar(db, "CREATE OR ALTER FUNCTION dbo.fn_NormalizarCarrera(@entrada nvarchar(512)) RETURNS nvarchar(512) AS BEGIN RETURN N'DISEN O'; END;");
            await Script(db, apply);
            Equal("capturar normalizacion anterior", await Scalar(db, "SELECT carrera_normalizada FROM participante_carrera_respaldo"), "DISEN O");
            await Script(db, revert);
            Equal("revertir usando version anterior", await Career(db, "anterior"), "Disen\u0303o");
            await Script(db, apply); await Script(db, revert);
            Equal("historial despues de actualizar esquema", await Career(db, "anterior"), "Disen\u0303o");
        }
        await Scalar(db, "DROP TABLE participante_carrera_respaldo; DROP FUNCTION dbo.fn_NormalizarCarrera; DELETE participante;");
        await Scalar(db, "CREATE TABLE participante_carrera_respaldo(id_participante_FK nvarchar(64) PRIMARY KEY,carrera_original nvarchar(512),fecha_respaldo datetime2(3) DEFAULT SYSUTCDATETIME()); INSERT participante_carrera_respaldo(id_participante_FK,carrera_original) VALUES(N'desconocido',N'Diseño');");
        await Insert(db, "desconocido", "DISENO");
        await Script(db, apply); await Script(db, revert);
        Equal("no inferir respaldo sin funcion antigua", await Career(db, "desconocido"), "DISENO");
        Equal("valor escrito desconocido", await Scalar(db, "SELECT carrera_normalizada FROM participante_carrera_respaldo"), null);
        Console.WriteLine($"{environment}: migracion, verificacion, reversion, fallos, concurrencia y compatibilidad aprobados.");
    }
    finally
    {
        SqlConnection.ClearAllPools();
        await Scalar(master, $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}];");
    }
}
Console.WriteLine($"PASS: {assertions} comprobaciones. Solo se usaron bases temporales creadas por esta ejecucion.");

static async Task<object?> Scalar(SqlConnection connection, string sql)
{
    using var command = new SqlCommand(sql, connection) { CommandTimeout = 180 };
    var result = await command.ExecuteScalarAsync();
    return result is DBNull ? null : result;
}
static IEnumerable<string> Batches(string script) => Regex.Split(script, @"^\s*GO\s*\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Where(b => !string.IsNullOrWhiteSpace(b));
static async Task Script(SqlConnection db, string script) { foreach (string batch in Batches(script)) await Scalar(db, batch); }
static async Task RunSeparate(string connection, string script) { using var db = new SqlConnection(connection); await db.OpenAsync(); await Script(db, script); }
static async Task Insert(SqlConnection db, string id, string? value)
{
    using var command = new SqlCommand("INSERT participante VALUES(@id,@carrera)", db);
    command.Parameters.Add("@id", SqlDbType.NVarChar, 64).Value = id;
    command.Parameters.Add("@carrera", SqlDbType.NVarChar, 512).Value = (object?)value ?? DBNull.Value;
    await command.ExecuteNonQueryAsync();
}
static async Task<object?> Career(SqlConnection db, string id)
{
    using var command = new SqlCommand("SELECT carrera FROM participante WHERE id_participante_PK=@id", db);
    command.Parameters.Add("@id", SqlDbType.NVarChar, 64).Value = id;
    object? result = await command.ExecuteScalarAsync(); return result is DBNull ? null : result;
}
static async Task Verify(SqlConnection db, string script)
{
    using var command = new SqlCommand(script, db) { CommandTimeout = 180 };
    using var reader = await command.ExecuteReaderAsync();
    do
    {
        while (await reader.ReadAsync())
        for (int i = 0; i < reader.FieldCount; i++)
        if (reader.GetValue(i) is string state && state is "FAIL" or "MISSING")
            throw new Exception("Fallo en 02-verificar: " + reader.GetName(i));
    } while (await reader.NextResultAsync());
}
