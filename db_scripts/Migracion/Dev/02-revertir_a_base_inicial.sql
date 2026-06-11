-- ============================================================
-- Utilidad de prueba de METICS: revertir a la base inicial db_METICS.sql
--
-- Ejecutar esto sobre un RESPALDO RESTAURADO de la base de datos
-- actual para simular el estado previo a la migración antes de
-- ejecutar 03-aplicar_migracion.sql.
--
-- ADVERTENCIA: se descartan todos los datos de carrera,
-- correo_alternativo, grado_academico y participante_area_extra.
-- Ejecutar esto únicamente en una base de datos de prueba/copia,
-- nunca en producción.
-- ============================================================

SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    -- participante_area_extra debe ir primero (hijo con FK de participante)
    IF OBJECT_ID('dbo.participante_area_extra', 'U') IS NOT NULL
        DROP TABLE dbo.participante_area_extra;

    -- Eliminar la restricción CHECK de participante antes que su columna
    IF EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE name = 'CK_participante_grado_academico'
          AND parent_object_id = OBJECT_ID('dbo.participante')
    )
        EXEC sp_executesql N'ALTER TABLE dbo.participante DROP CONSTRAINT CK_participante_grado_academico;';

    IF COL_LENGTH('dbo.participante', 'correo_alternativo') IS NOT NULL
        EXEC sp_executesql N'ALTER TABLE dbo.participante DROP COLUMN correo_alternativo;';

    IF COL_LENGTH('dbo.participante', 'grado_academico') IS NOT NULL
        EXEC sp_executesql N'ALTER TABLE dbo.participante DROP COLUMN grado_academico;';

    IF COL_LENGTH('dbo.participante', 'carrera') IS NOT NULL
        EXEC sp_executesql N'ALTER TABLE dbo.participante DROP COLUMN carrera;';

    -- columnas de usuario (agregadas en el diseño intermedio, pueden o no estar presentes)
    IF EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE name = 'CK_usuario_grado_academico'
          AND parent_object_id = OBJECT_ID('dbo.usuario')
    )
        EXEC sp_executesql N'ALTER TABLE dbo.usuario DROP CONSTRAINT CK_usuario_grado_academico;';

    IF COL_LENGTH('dbo.usuario', 'correo_alternativo') IS NOT NULL
        EXEC sp_executesql N'ALTER TABLE dbo.usuario DROP COLUMN correo_alternativo;';

    IF COL_LENGTH('dbo.usuario', 'grado_academico') IS NOT NULL
        EXEC sp_executesql N'ALTER TABLE dbo.usuario DROP COLUMN grado_academico;';

    COMMIT TRANSACTION;
    PRINT 'Reversión al esquema base completada.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- Revertir los procedimientos almacenados a su forma base (sin carrera, sin campos nuevos)
CREATE OR ALTER PROCEDURE InsertParticipante
    @idUsuario            NVARCHAR(64),
    @idParticipante       NVARCHAR(64),
    @tipoIdentificacion   NVARCHAR(16)  = '',
    @numeroIdentificacion NVARCHAR(32),
    @correo               NVARCHAR(64),
    @nombre               NVARCHAR(64),
    @apellido1            NVARCHAR(64),
    @apellido2            NVARCHAR(64)  = '',
    @tipoParticipante     NVARCHAR(64)  = '',
    @condicion            NVARCHAR(64)  = '',
    @telefono             NVARCHAR(64)  = '',
    @area                 NVARCHAR(512) = '',
    @departamento         NVARCHAR(512) = '',
    @unidadAcademica      NVARCHAR(512) = '',
    @sede                 NVARCHAR(512) = '',
    @horasMatriculadas    INT           = 0,
    @horasAprobadas       INT           = 0
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO participante
    (id_usuario_FK, id_participante_PK, tipo_identificacion, numero_identificacion,
     correo, nombre, apellido_1, apellido_2, tipo_participante, condicion, telefono,
     area, departamento, unidad_academica, sede,
     total_horas_matriculadas, total_horas_aprobadas)
    VALUES
    (@idUsuario, @idParticipante, @tipoIdentificacion, @numeroIdentificacion,
     @correo, @nombre, @apellido1, @apellido2, @tipoParticipante, @condicion, @telefono,
     @area, @departamento, @unidadAcademica, @sede,
     @horasMatriculadas, @horasAprobadas);
END
GO

CREATE OR ALTER PROCEDURE UpdateParticipante
    @idUsuario            NVARCHAR(64),
    @idParticipante       NVARCHAR(64),
    @tipoIdentificacion   NVARCHAR(16)  = '',
    @numeroIdentificacion NVARCHAR(32),
    @correo               NVARCHAR(64),
    @nombre               NVARCHAR(64),
    @apellido1            NVARCHAR(64),
    @apellido2            NVARCHAR(64)  = '',
    @tipoParticipante     NVARCHAR(64)  = '',
    @condicion            NVARCHAR(64)  = '',
    @telefono             NVARCHAR(64)  = '',
    @area                 NVARCHAR(512) = '',
    @departamento         NVARCHAR(512) = '',
    @unidadAcademica      NVARCHAR(512) = '',
    @sede                 NVARCHAR(512) = '',
    @horasMatriculadas    INT           = 0,
    @horasAprobadas       INT           = 0
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE participante SET
        tipo_identificacion   = @tipoIdentificacion,
        numero_identificacion = @numeroIdentificacion,
        correo                = @correo,
        nombre                = @nombre,
        apellido_1            = @apellido1,
        apellido_2            = @apellido2,
        tipo_participante     = @tipoParticipante,
        condicion             = @condicion,
        telefono              = @telefono,
        area                  = @area,
        departamento          = @departamento,
        unidad_academica      = @unidadAcademica,
        sede                  = @sede,
        total_horas_matriculadas = @horasMatriculadas,
        total_horas_aprobadas    = @horasAprobadas
    WHERE id_usuario_FK = @idUsuario AND id_participante_PK = @idParticipante;
END
GO

CREATE OR ALTER PROCEDURE SelectParticipante
    @id NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM participante WHERE id_participante_PK = @id;
END
GO
