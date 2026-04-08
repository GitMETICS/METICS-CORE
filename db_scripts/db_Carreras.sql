-- Script para agregar la columna carrera a la tabla participante y actualizar el stored procedure InsertParticipante

GO
-- Agregar columna carrera a la tabla participante
ALTER TABLE participante ADD carrera NVARCHAR(512);

GO
-- Actualizar stored procedure InsertParticipante
ALTER PROCEDURE InsertParticipante
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
END