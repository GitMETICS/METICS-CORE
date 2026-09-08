using System.Text;
using webMetics.Services;

// Se enlaza el mismo codigo que usa la aplicacion, sin copiar su algoritmo.
var root = new DirectoryInfo(AppContext.BaseDirectory);
while (root != null && !File.Exists(Path.Combine(root.FullName, "webMetics.sln")))
    root = root.Parent;
if (root == null) throw new InvalidOperationException("No se encontro webMetics.sln.");

var mappings = new SortedDictionary<string, List<int>>(StringComparer.Ordinal);
for (int code = 0; code <= char.MaxValue; code++)
{
    char character = (char)code;
    if (char.IsSurrogate(character)) continue; // SQL recorre unidades UTF-16, como el foreach de C#.
    string normalized;
    try { normalized = CarreraResolver.Normalizar("A" + character + "B"); }
    catch (ArgumentException) { continue; } // UTF-16 que .NET no admite en Normalize.
    string replacement = normalized[1..^1];
    if (replacement == " ") continue;
    if (replacement.Length > 1 || (replacement.Length == 1 && !char.IsAsciiLetterOrDigit(replacement[0])))
        throw new InvalidOperationException($"Mapeo inesperado U+{code:X4}: {normalized}");
    if (!mappings.TryGetValue(replacement, out var codes)) mappings[replacement] = codes = new();
    codes.Add(code);
}

var sql = new StringBuilder();
sql.AppendLine("-- BEGIN GENERATED NORMALIZAR CARRERA");
sql.AppendLine("-- Generado por scripts/carreras/NormalizacionSql; no editar este bloque a mano.");
sql.AppendLine("-- Mapeo Unicode derivado de CarreraResolver, incluidas marcas combinantes.");
sql.AppendLine("-- BIN2 y UNICODE evitan depender de UPPER, rangos o colacion de la base.");
sql.AppendLine("CREATE OR ALTER FUNCTION dbo.fn_NormalizarCarrera (@entrada NVARCHAR(512))");
sql.AppendLine("RETURNS NVARCHAR(512)");
sql.AppendLine("AS");
sql.AppendLine("BEGIN");
sql.AppendLine("    DECLARE @resultado NVARCHAR(512) = N'', @pos INT = 1,");
sql.AppendLine("            @longitud INT = DATALENGTH(@entrada) / 2, @codigo INT,");
sql.AppendLine("            @letra NVARCHAR(1), @separador BIT = 0;");
sql.AppendLine("    WHILE @pos <= @longitud");
sql.AppendLine("    BEGIN");
sql.AppendLine("        SET @codigo = UNICODE(SUBSTRING(@entrada COLLATE Latin1_General_BIN2, @pos, 1));");
sql.AppendLine("        SET @letra = CASE");
foreach (var (replacement, codes) in mappings)
{
    // Rangos contiguos mantienen el SQL pequeno y revisable.
    var ranges = new List<string>();
    for (int i = 0; i < codes.Count; i++)
    {
        int start = codes[i], end = start;
        while (i + 1 < codes.Count && codes[i + 1] == end + 1) end = codes[++i];
        ranges.Add(start == end ? $"@codigo = {start}" : $"@codigo BETWEEN {start} AND {end}");
    }
    sql.AppendLine("            WHEN " + string.Join("\n              OR ", ranges) + $" THEN N'{replacement}'");
}
sql.AppendLine("            ELSE N' ' END;");
sql.AppendLine("        IF DATALENGTH(@letra) = 0");
sql.AppendLine("            SET @pos = @pos + 1; -- Una marca se elimina; no separa palabras.");
sql.AppendLine("        ELSE");
sql.AppendLine("        BEGIN");
sql.AppendLine("            IF @letra = N' '");
sql.AppendLine("                SET @separador = 1;");
sql.AppendLine("            ELSE");
sql.AppendLine("            BEGIN");
sql.AppendLine("                IF @separador = 1 AND DATALENGTH(@resultado) > 0");
sql.AppendLine("                    SET @resultado = @resultado + N' ';");
sql.AppendLine("                SET @resultado = @resultado + @letra;");
sql.AppendLine("                SET @separador = 0;");
sql.AppendLine("            END;");
sql.AppendLine("            SET @pos = @pos + 1;");
sql.AppendLine("        END;");
sql.AppendLine("    END;");
sql.AppendLine("    RETURN @resultado;");
sql.AppendLine("END");
sql.AppendLine("-- END GENERATED NORMALIZAR CARRERA");
string block = sql.ToString().Replace("\r\n", "\n").TrimEnd();
bool check = args.Contains("--check");
foreach (string environment in new[] { "Dev", "Prod" })
{
    string path = Path.Combine(root.FullName, "db_scripts", "Migracion", environment, "01-aplicar_normalizacion_carreras.sql");
    string text = File.ReadAllText(path).Replace("\r\n", "\n");
    const string begin = "-- BEGIN GENERATED NORMALIZAR CARRERA";
    const string end = "-- END GENERATED NORMALIZAR CARRERA";
    int first = text.IndexOf(begin, StringComparison.Ordinal), last = text.IndexOf(end, StringComparison.Ordinal);
    if (first < 0 || last < first) throw new InvalidOperationException("Faltan marcadores en " + path);
    string updated = text[..first] + block + text[(last + end.Length)..];
    if (check && updated != text) throw new InvalidOperationException("Regenerar mapeo Unicode: " + path);
    if (!check) File.WriteAllText(path, updated, new UTF8Encoding(false));
}
Console.WriteLine(check ? "Mapeo SQL vigente en Dev y Prod." : "Mapeo SQL regenerado en Dev y Prod.");
