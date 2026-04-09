-- =============================================
-- Script para agregar el campo correo alternativo a usuario
-- y actualizar el procedimiento InsertUsuario
-- =============================================

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.usuario', 'correo_alternativo') IS NULL
    BEGIN
        ALTER TABLE dbo.usuario
        ADD correo_alternativo NVARCHAR(64) NULL;
    END;

    COMMIT TRANSACTION;
    PRINT 'Script ejecutado exitosamente: columna correo_alternativo agregada en dbo.usuario.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

CREATE OR ALTER PROCEDURE InsertUsuario
    @id NVARCHAR(64),
	@rol INT = 0,
    @contrasena NVARCHAR(64),
	@correoAlternativo NVARCHAR(64) = NULL
AS
BEGIN
	DECLARE @salt UNIQUEIDENTIFIER=NEWID()

    INSERT INTO usuario (id_usuario_PK, correo_alternativo, rol_FK, hash_contrasena, salt)
    VALUES(@id, @correoAlternativo, @rol, HASHBYTES('SHA2_512', @contrasena + CAST(@salt AS NVARCHAR(36))), @salt)
END
GO