using System.Data;
using Microsoft.Data.SqlClient;

namespace webMetics.Services;

/// <summary>Catálogo independiente de los reconocimientos otorgados a participantes.</summary>
public sealed class MedallasCatalogoService
{
    private readonly string connectionString;
    private readonly string imageDirectory;

    public MedallasCatalogoService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta DefaultConnection.");
        imageDirectory = Path.Combine(environment.WebRootPath, "images", "medallas");
    }

    public static bool EsNombreValido(string? name) => !string.IsNullOrWhiteSpace(name)
        && name.Length <= 450 && !name.Contains('/') && !name.Contains('\\')
        && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
        && new[] { ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase);

    public string RutaImagen(string name)
    {
        if (!EsNombreValido(name)) throw new ArgumentException("El nombre de la imagen no es válido.", nameof(name));
        return Path.Combine(imageDirectory, name);
    }

    public async Task<int> SincronizarAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(imageDirectory))
            throw new DirectoryNotFoundException($"Falta la carpeta de imágenes de medallas: {imageDirectory}");
        var files = Directory.EnumerateFiles(imageDirectory).Select(Path.GetFileName)
            .Where(EsNombreValido).Select(name => name!).ToArray();
        if (files.Distinct(StringComparer.OrdinalIgnoreCase).Count() != files.Length)
            throw new InvalidOperationException("Hay imágenes de medallas con nombres que solo difieren en mayúsculas/minúsculas.");

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        Bloquear(connection, transaction);
        using var check = new SqlCommand("SELECT OBJECT_ID(N'dbo.catalogo_medallas', N'U');", connection, transaction);
        if (await check.ExecuteScalarAsync(cancellationToken) is null or DBNull)
            throw new InvalidOperationException("Antes de iniciar METICS, aplique scripts/medallas/001_catalogo_medallas.sql.");
        var added = 0;
        foreach (var name in files)
        {
            using var command = CrearComando(connection, transaction, """
                INSERT INTO dbo.catalogo_medallas (nombre_medalla, activa)
                SELECT @nombre, 1 WHERE NOT EXISTS (
                    SELECT 1 FROM dbo.catalogo_medallas WHERE nombre_medalla = @nombre
                );
                """, name);
            added += await command.ExecuteNonQueryAsync(cancellationToken);
        }
        transaction.Commit();
        return added;
    }

    public List<string> ObtenerNombres()
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = new SqlCommand("SELECT nombre_medalla FROM dbo.catalogo_medallas WHERE activa = 1 ORDER BY nombre_medalla;", connection);
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
        {
            var name = reader.GetString(0);
            if (EsNombreValido(name) && File.Exists(RutaImagen(name))) result.Add(name);
        }
        return result;
    }

    public bool Existe(string name)
    {
        RutaImagen(name);
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = CrearComando(connection, null,
            "SELECT COUNT(*) FROM dbo.catalogo_medallas WHERE nombre_medalla = @nombre AND activa = 1;", name);
        return (int)command.ExecuteScalar()! > 0;
    }

    public bool Registrar(string name) => Mutar((connection, transaction) =>
    {
        if (!File.Exists(RutaImagen(name))) throw new FileNotFoundException("Falta la imagen de la medalla.");
        RegistrarEnTransaccion(connection, transaction, name);
        return true;
    });

    private static void RegistrarEnTransaccion(SqlConnection connection, SqlTransaction transaction, string name)
    {
        using var command = CrearComando(connection, transaction, """
            IF EXISTS (SELECT 1 FROM dbo.catalogo_medallas WHERE nombre_medalla = @nombre AND activa = 1)
                THROW 51003, 'Ya existe una medalla con ese nombre.', 1;
            UPDATE dbo.catalogo_medallas SET activa = 1 WHERE nombre_medalla = @nombre;
            IF @@ROWCOUNT = 0 INSERT INTO dbo.catalogo_medallas (nombre_medalla, activa) VALUES (@nombre, 1);
            """, name);
        command.ExecuteNonQuery();
    }

    public bool Reemplazar(string previousName, string name) => Mutar((connection, transaction) =>
    {
        RutaImagen(previousName);
        if (!File.Exists(RutaImagen(name))) throw new FileNotFoundException("Falta la nueva imagen de la medalla.");
        ReemplazarEnTransaccion(connection, transaction, previousName, name);
        return true;
    });

    private static void ReemplazarEnTransaccion(SqlConnection connection, SqlTransaction transaction, string previousName, string name)
    {
        using var command = CrearComando(connection, transaction, """
            IF NOT EXISTS (SELECT 1 FROM dbo.catalogo_medallas WHERE nombre_medalla = @anterior AND activa = 1)
                THROW 51004, 'La medalla original no existe en el catálogo.', 1;
            IF @anterior <> @nombre
            BEGIN
                IF EXISTS (SELECT 1 FROM dbo.catalogo_medallas WHERE nombre_medalla = @nombre)
                    THROW 51003, 'El nombre de destino ya existe en el catálogo.', 1;
                INSERT INTO dbo.catalogo_medallas (nombre_medalla, activa) VALUES (@nombre, 1);
                UPDATE dbo.medallas SET nombre_medalla = @nombre WHERE nombre_medalla = @anterior;
                UPDATE dbo.catalogo_medallas SET activa = 0 WHERE nombre_medalla = @anterior;
            END;
            """, name);
        command.Parameters.Add("@anterior", SqlDbType.NVarChar, 450).Value = previousName;
        command.ExecuteNonQuery();
    }

    /// <summary>Publica la imagen sin sobrescribir otro registro y restaura el archivo si falla la operación SQL.</summary>
    public async Task GuardarImagenAsync(IFormFile image, string? previousName = null)
    {
        if (image.Length == 0) throw new ArgumentException("Elija una imagen válida.");
        var destination = RutaImagen(image.FileName);
        if (previousName != null)
        {
            RutaImagen(previousName);
            if (previousName.Equals(image.FileName, StringComparison.OrdinalIgnoreCase)
                && !previousName.Equals(image.FileName, StringComparison.Ordinal))
                throw new ArgumentException("Conserve las mayúsculas y minúsculas del nombre original.");
        }
        Directory.CreateDirectory(imageDirectory);
        var temporary = Path.Combine(imageDirectory, $".{Guid.NewGuid():N}.tmp");
        var backup = Path.Combine(imageDirectory, $".{Guid.NewGuid():N}.bak");
        var published = false;
        var hadImage = false;
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew))
                await image.CopyToAsync(stream);
            Mutar((connection, transaction) =>
            {
                using var check = CrearComando(connection, transaction,
                    "SELECT COUNT(*) FROM dbo.catalogo_medallas WHERE nombre_medalla = @nombre AND activa = 1;", image.FileName);
                var destinationExists = (int)check.ExecuteScalar()! > 0;
                if (destinationExists && previousName != image.FileName)
                    throw new InvalidOperationException("Ya existe una medalla con ese nombre.");
                hadImage = File.Exists(destination);
                if (hadImage) File.Copy(destination, backup);
                File.Move(temporary, destination, true);
                published = true;
                if (previousName == null) RegistrarEnTransaccion(connection, transaction, image.FileName);
                else ReemplazarEnTransaccion(connection, transaction, previousName, image.FileName);
                return true;
            });
        }
        catch
        {
            if (published)
            {
                if (hadImage) File.Move(backup, destination, true);
                else File.Delete(destination);
            }
            throw;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
            if (File.Exists(backup)) File.Delete(backup);
        }
    }

    public bool Eliminar(string name) => Mutar((connection, transaction) =>
    {
        RutaImagen(name);
        using var command = CrearComando(connection, transaction, """
            UPDATE dbo.catalogo_medallas SET activa = 0 WHERE nombre_medalla = @nombre AND activa = 1;
            DECLARE @changed int = @@ROWCOUNT;
            IF @changed > 0 DELETE FROM dbo.medallas WHERE nombre_medalla = @nombre;
            SELECT @changed;
            """, name);
        return (int)command.ExecuteScalar()! > 0;
    });

    public void Asignar(string name, IEnumerable<string> participants) => Mutar((connection, transaction) =>
    {
        if (!File.Exists(RutaImagen(name))) throw new FileNotFoundException("Falta la imagen de la medalla.");
        using var check = CrearComando(connection, transaction,
            "SELECT COUNT(*) FROM dbo.catalogo_medallas WHERE nombre_medalla = @nombre AND activa = 1;", name);
        if ((int)check.ExecuteScalar()! == 0) throw new InvalidOperationException("La medalla no está activa en el catálogo.");
        foreach (var participant in participants.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            using var command = CrearComando(connection, transaction, """
                INSERT INTO dbo.medallas (id_participante_FK, nombre_medalla)
                SELECT @participante, @nombre WHERE NOT EXISTS (
                    SELECT 1 FROM dbo.medallas WHERE id_participante_FK = @participante AND nombre_medalla = @nombre
                );
                """, name);
            command.Parameters.Add("@participante", SqlDbType.NVarChar, 64).Value = participant;
            command.ExecuteNonQuery();
        }
        return true;
    });

    private T Mutar<T>(Func<SqlConnection, SqlTransaction, T> operation)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        Bloquear(connection, transaction);
        var result = operation(connection, transaction);
        transaction.Commit();
        return result;
    }

    private static void Bloquear(SqlConnection connection, SqlTransaction transaction)
    {
        using var command = new SqlCommand("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = N'METICS.CatalogoMedallas',
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000;
            IF @result < 0 THROW 51000, 'No se pudo bloquear el catálogo de medallas.', 1;
            """, connection, transaction);
        command.CommandTimeout = 40;
        command.ExecuteNonQuery();
    }

    private static SqlCommand CrearComando(SqlConnection connection, SqlTransaction? transaction, string sql, string name)
    {
        var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@nombre", SqlDbType.NVarChar, 450).Value = name;
        return command;
    }
}
