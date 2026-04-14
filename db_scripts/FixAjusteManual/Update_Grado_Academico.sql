-- =============================================
-- Script para agregar el campo grado_academico a usuario
-- =============================================

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.usuario', 'grado_academico') IS NULL
    BEGIN
        ALTER TABLE dbo.usuario
        ADD grado_academico NVARCHAR(32) NULL;
    END;

    COMMIT TRANSACTION;
    PRINT 'Columna grado_academico agregada correctamente en dbo.usuario.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_usuario_grado_academico'
      AND parent_object_id = OBJECT_ID('dbo.usuario')
)
BEGIN
    ALTER TABLE dbo.usuario
    ADD CONSTRAINT CK_usuario_grado_academico
    CHECK (
        grado_academico IS NULL OR
        grado_academico IN (
            N'Doctorado - PhD',
            N'Maestría - MSc',
            N'Licenciatura - Lic',
            N'Bachillerato - Bach'
        )
    );

    PRINT 'Constraint CK_usuario_grado_academico agregado correctamente.';
END
GO