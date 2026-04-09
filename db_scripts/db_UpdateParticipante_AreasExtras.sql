-- Script de actualización para soportar área principal + áreas extra (múltiples)
-- Incluye ajuste de carrera y corrección de parámetros del SP InsertParticipante.

--GO
--DELETE FROM dbo.participante_area_extra;
--GO

GO
IF COL_LENGTH('participante', 'carrera') IS NULL
BEGIN
    ALTER TABLE participante ADD carrera NVARCHAR(512) NULL;
END

GO
IF OBJECT_ID('dbo.participante_area_extra', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.participante_area_extra (
        id_participante_FK NVARCHAR(64) NOT NULL,
        area_extra NVARCHAR(256) NOT NULL,
        CONSTRAINT PK_participante_area_extra PRIMARY KEY (id_participante_FK, area_extra),
        CONSTRAINT FK_participante_area_extra_participante FOREIGN KEY (id_participante_FK)
            REFERENCES dbo.participante(id_participante_PK)
            ON DELETE CASCADE
            ON UPDATE CASCADE
    );
END

GO
CREATE OR ALTER PROCEDURE InsertParticipante
    @idUsuario NVARCHAR(64),
    @idParticipante NVARCHAR(64),
    @tipoIdentificacion NVARCHAR(16) = '',
    @numeroIdentificacion NVARCHAR(32),
    @correo NVARCHAR(64),
    @nombre NVARCHAR(64),
    @apellido1 NVARCHAR(64),
    @apellido2 NVARCHAR(64) = '',
    @tipoParticipante NVARCHAR(64) = '',
    @condicion NVARCHAR(64) = '',
    @telefono NVARCHAR(64) = '',
    @area NVARCHAR(512) = '',
    @departamento NVARCHAR(512) = '',
    @unidadAcademica NVARCHAR(512) = '',
    @sede NVARCHAR(512) = '',
    @carrera NVARCHAR(512) = '',
    @horasMatriculadas INT = 0,
    @horasAprobadas INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO participante
    (
        id_usuario_FK,
        id_participante_PK,
        tipo_identificacion,
        numero_identificacion,
        correo,
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
    @idUsuario NVARCHAR(64),
    @idParticipante NVARCHAR(64),
    @tipoIdentificacion NVARCHAR(16) = '',
    @numeroIdentificacion NVARCHAR(32),
    @correo NVARCHAR(64),
    @nombre NVARCHAR(64),
    @apellido1 NVARCHAR(64),
    @apellido2 NVARCHAR(64) = '',
    @tipoParticipante NVARCHAR(64) = '',
    @condicion NVARCHAR(64) = '',
    @telefono NVARCHAR(64) = '',
    @area NVARCHAR(512) = '',
    @departamento NVARCHAR(512) = '',
    @unidadAcademica NVARCHAR(512) = '',
    @sede NVARCHAR(512) = '',
    @carrera NVARCHAR(512) = '',
    @horasMatriculadas INT = 0,
    @horasAprobadas INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE participante
    SET
        tipo_identificacion = @tipoIdentificacion,
        numero_identificacion = @numeroIdentificacion,
        correo = @correo,
        nombre = @nombre,
        apellido_1 = @apellido1,
        apellido_2 = @apellido2,
        tipo_participante = @tipoParticipante,
        condicion = @condicion,
        telefono = @telefono,
        area = @area,
        departamento = @departamento,
        unidad_academica = @unidadAcademica,
        sede = @sede,
        carrera = @carrera,
        total_horas_matriculadas = @horasMatriculadas,
        total_horas_aprobadas = @horasAprobadas
    WHERE
        id_usuario_FK = @idUsuario
        AND id_participante_PK = @idParticipante;
END
