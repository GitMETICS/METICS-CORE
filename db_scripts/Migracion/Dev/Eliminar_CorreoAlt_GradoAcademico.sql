SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    -- Disable triggers that use scalar subqueries on inserted — they break on multi-row updates
    --DISABLE  TRIGGER TR_ActualizarIdParticipante ON dbo.usuario;
    --DISABLE  TRIGGER TR_ActualizarIdAsesor ON dbo.usuario;
    -- Remember to re-enable them after the update

    IF COL_LENGTH('dbo.usuario', 'correo_alternativo') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            UPDATE dbo.usuario
            SET correo_alternativo = NULL
            WHERE correo_alternativo IS NOT NULL;
        ';
    END;

    IF COL_LENGTH('dbo.usuario', 'grado_academico') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            UPDATE dbo.usuario
            SET grado_academico = NULL
            WHERE grado_academico IS NOT NULL;
        ';
    END;




    IF COL_LENGTH('dbo.participante', 'correo_alternativo') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            UPDATE dbo.participante
            SET correo_alternativo = NULL
            WHERE correo_alternativo IS NOT NULL;
        ';
    END;

    IF COL_LENGTH('dbo.participante', 'grado_academico') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            UPDATE dbo.participante
            SET grado_academico = NULL
            WHERE grado_academico IS NOT NULL;
        ';
    END;


    IF EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = 'CK_usuario_grado_academico'
          AND parent_object_id = OBJECT_ID('dbo.usuario')
    )
    BEGIN
        EXEC sp_executesql N'
            ALTER TABLE dbo.usuario
            DROP CONSTRAINT CK_usuario_grado_academico;
        ';
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = 'CK_participante_grado_academico'
          AND parent_object_id = OBJECT_ID('dbo.participante')
    )
    BEGIN
        EXEC sp_executesql N'
            ALTER TABLE dbo.participante
            DROP CONSTRAINT CK_participante_grado_academico;
        ';
    END;


    IF COL_LENGTH('dbo.usuario', 'correo_alternativo') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            ALTER TABLE dbo.usuario
            DROP COLUMN correo_alternativo;
        ';
    END;

    IF COL_LENGTH('dbo.usuario', 'grado_academico') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            ALTER TABLE dbo.usuario
            DROP COLUMN grado_academico;
        ';
    END;


    IF COL_LENGTH('dbo.participante', 'correo_alternativo') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            ALTER TABLE dbo.participante
            DROP COLUMN correo_alternativo;
        ';
    END;

    IF COL_LENGTH('dbo.participante', 'grado_academico') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            ALTER TABLE dbo.participante
            DROP COLUMN grado_academico;
        ';
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

CREATE OR ALTER PROCEDURE UpdateUsuario
    @id NVARCHAR(64),
    @rol INT = 0,
    @contrasena NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @salt UNIQUEIDENTIFIER = NEWID();

    UPDATE dbo.usuario
    SET
        id_usuario_PK = @id,
        rol_FK = @rol,
        hash_contrasena = HASHBYTES('SHA2_512', @contrasena + CAST(@salt AS NVARCHAR(36))),
        salt = @salt
    WHERE id_usuario_PK = @id;
END
GO

CREATE OR ALTER PROCEDURE SelectUsuario
    @id NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        id_usuario_PK,
        rol_FK
    FROM dbo.usuario
    WHERE id_usuario_PK = @id;
END
GO