-- ============================================================
-- METICS migration: base schema → current
--
-- Applies all post-baseline changes to the participante table:
--   1. carrera column
--   2. participante_area_extra junction table
--   3. correo_alternativo column
--   4. grado_academico column + CHECK constraint
--   5. InsertParticipante / UpdateParticipante / SelectParticipante
--      stored procedures — full parameter list, explicit column list
--
-- Idempotent: safe to run on any state between the baseline
-- db_METICS.sql and the current schema.
-- ============================================================

SET XACT_ABORT ON;
GO

-- ----------------------------------------------------------
-- Schema changes (DDL inside a transaction for atomicity)
-- ----------------------------------------------------------
BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.participante', 'carrera') IS NULL
    BEGIN
        ALTER TABLE dbo.participante ADD carrera NVARCHAR(512) NULL;
    END;

    IF COL_LENGTH('dbo.participante', 'correo_alternativo') IS NULL
    BEGIN
        ALTER TABLE dbo.participante ADD correo_alternativo NVARCHAR(64) NULL;
    END;

    IF COL_LENGTH('dbo.participante', 'grado_academico') IS NULL
    BEGIN
        ALTER TABLE dbo.participante ADD grado_academico NVARCHAR(32) NULL;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE name = 'CK_participante_grado_academico'
          AND parent_object_id = OBJECT_ID('dbo.participante')
    )
    BEGIN
        EXEC sp_executesql N'
            ALTER TABLE dbo.participante
            ADD CONSTRAINT CK_participante_grado_academico
            CHECK (
                grado_academico IS NULL OR
                grado_academico IN (
                    N''Doctorado - PhD'',
                    N''Maestría - MSc'',
                    N''Licenciatura - Lic'',
                    N''Bachillerato - Bach''
                )
            );
        ';
    END;

    IF OBJECT_ID('dbo.participante_area_extra', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.participante_area_extra (
            id_participante_FK NVARCHAR(64) NOT NULL,
            area_extra         NVARCHAR(256) NOT NULL,
            CONSTRAINT PK_participante_area_extra
                PRIMARY KEY (id_participante_FK, area_extra),
            CONSTRAINT FK_participante_area_extra_participante
                FOREIGN KEY (id_participante_FK)
                REFERENCES dbo.participante(id_participante_PK)
                ON DELETE CASCADE
                ON UPDATE CASCADE
        );
    END;

    COMMIT TRANSACTION;
    PRINT 'Schema migration completed successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- ----------------------------------------------------------
-- Stored procedures (CREATE OR ALTER is inherently idempotent)
-- ----------------------------------------------------------

CREATE OR ALTER PROCEDURE InsertParticipante
    @idUsuario           NVARCHAR(64),
    @idParticipante      NVARCHAR(64),
    @tipoIdentificacion  NVARCHAR(16)  = '',
    @numeroIdentificacion NVARCHAR(32),
    @correo              NVARCHAR(64),
    @correoAlternativo   NVARCHAR(64)  = NULL,
    @gradoAcademico      NVARCHAR(32)  = NULL,
    @nombre              NVARCHAR(64),
    @apellido1           NVARCHAR(64),
    @apellido2           NVARCHAR(64)  = '',
    @tipoParticipante    NVARCHAR(64)  = '',
    @condicion           NVARCHAR(64)  = '',
    @telefono            NVARCHAR(64)  = '',
    @area                NVARCHAR(512) = '',
    @departamento        NVARCHAR(512) = '',
    @unidadAcademica     NVARCHAR(512) = '',
    @sede                NVARCHAR(512) = '',
    @carrera             NVARCHAR(512) = '',
    @horasMatriculadas   INT           = 0,
    @horasAprobadas      INT           = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.participante
    (
        id_usuario_FK,
        id_participante_PK,
        tipo_identificacion,
        numero_identificacion,
        correo,
        correo_alternativo,
        grado_academico,
        nombre,
        apellido_1,
        apellido_2,
        tipo_participante,
        condicion,
        telefono,
        area,
        departamento,
        unidad_academica,
        sede,
        carrera,
        total_horas_matriculadas,
        total_horas_aprobadas
    )
    VALUES
    (
        @idUsuario,
        @idParticipante,
        @tipoIdentificacion,
        @numeroIdentificacion,
        @correo,
        NULLIF(LTRIM(RTRIM(@correoAlternativo)), N''),
        NULLIF(LTRIM(RTRIM(@gradoAcademico)),    N''),
        @nombre,
        @apellido1,
        @apellido2,
        @tipoParticipante,
        @condicion,
        @telefono,
        @area,
        @departamento,
        @unidadAcademica,
        @sede,
        @carrera,
        @horasMatriculadas,
        @horasAprobadas
    );

    SELECT @@ROWCOUNT AS FilasAfectadas;
END
GO

CREATE OR ALTER PROCEDURE UpdateParticipante
    @idUsuario           NVARCHAR(64),
    @idParticipante      NVARCHAR(64),
    @tipoIdentificacion  NVARCHAR(16)  = '',
    @numeroIdentificacion NVARCHAR(32),
    @correo              NVARCHAR(64),
    @correoAlternativo   NVARCHAR(64)  = NULL,
    @gradoAcademico      NVARCHAR(32)  = NULL,
    @nombre              NVARCHAR(64),
    @apellido1           NVARCHAR(64),
    @apellido2           NVARCHAR(64)  = '',
    @tipoParticipante    NVARCHAR(64)  = '',
    @condicion           NVARCHAR(64)  = '',
    @telefono            NVARCHAR(64)  = '',
    @area                NVARCHAR(512) = '',
    @departamento        NVARCHAR(512) = '',
    @unidadAcademica     NVARCHAR(512) = '',
    @sede                NVARCHAR(512) = '',
    @carrera             NVARCHAR(512) = '',
    @horasMatriculadas   INT           = 0,
    @horasAprobadas      INT           = 0
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.participante
    SET
        tipo_identificacion   = @tipoIdentificacion,
        numero_identificacion = @numeroIdentificacion,
        correo                = @correo,
        correo_alternativo    = NULLIF(LTRIM(RTRIM(@correoAlternativo)), N''),
        grado_academico       = NULLIF(LTRIM(RTRIM(@gradoAcademico)),    N''),
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
        carrera               = @carrera,
        total_horas_matriculadas = @horasMatriculadas,
        total_horas_aprobadas    = @horasAprobadas
    WHERE
        id_usuario_FK      = @idUsuario
        AND id_participante_PK = @idParticipante;

    SELECT @@ROWCOUNT AS FilasAfectadas;
END
GO

CREATE OR ALTER PROCEDURE SelectParticipante
    @id NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        id_usuario_FK,
        id_participante_PK,
        nombre,
        apellido_1,
        apellido_2,
        tipo_identificacion,
        numero_identificacion,
        correo,
        correo_alternativo,
        grado_academico,
        tipo_participante,
        condicion,
        telefono,
        area,
        departamento,
        unidad_academica,
        sede,
        carrera,
        total_horas_matriculadas,
        total_horas_aprobadas,
        correo_notificacion_enviado
    FROM dbo.participante
    WHERE id_participante_PK = @id;
END
GO
