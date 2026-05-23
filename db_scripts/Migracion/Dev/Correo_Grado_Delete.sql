SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    /* Quitar constraint viejo en usuario si existe */
    IF EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = 'CK_usuario_grado_academico'
          AND parent_object_id = OBJECT_ID('dbo.usuario')
    )
    BEGIN
        ALTER TABLE dbo.usuario
        DROP CONSTRAINT CK_usuario_grado_academico;
    END;

    /* Quitar constraint en participante si existiera por pruebas previas */
    IF EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = 'CK_participante_grado_academico'
          AND parent_object_id = OBJECT_ID('dbo.participante')
    )
    BEGIN
        ALTER TABLE dbo.participante
        DROP CONSTRAINT CK_participante_grado_academico;
    END;

    /* Eliminar columnas viejas de usuario */
    IF COL_LENGTH('dbo.usuario', 'correo_alternativo') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.usuario
        DROP COLUMN correo_alternativo;
    END;

    IF COL_LENGTH('dbo.usuario', 'grado_academico') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.usuario
        DROP COLUMN grado_academico;
    END;

    /* Eliminar columnas de participante si existieran por pruebas previas */
    IF COL_LENGTH('dbo.participante', 'correo_alternativo') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.participante
        DROP COLUMN correo_alternativo;
    END;

    IF COL_LENGTH('dbo.participante', 'grado_academico') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.participante
        DROP COLUMN grado_academico;
    END;

    COMMIT TRANSACTION;
    PRINT 'Limpieza completada correctamente.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

/* Dejar usuario limpio también a nivel de SP */
CREATE OR ALTER PROCEDURE InsertUsuario
    @id NVARCHAR(64),
    @rol INT = 0,
    @contrasena NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @salt UNIQUEIDENTIFIER = NEWID();

    INSERT INTO dbo.usuario
    (
        id_usuario_PK,
        rol_FK,
        hash_contrasena,
        salt
    )
    VALUES
    (
        @id,
        @rol,
        HASHBYTES('SHA2_512', @contrasena + CAST(@salt AS NVARCHAR(36))),
        @salt
    );
END
GO