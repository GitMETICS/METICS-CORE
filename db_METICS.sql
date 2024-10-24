/*
	SCRIPT PARA CREAR LA BASE DE DATOS
*/



--Creacion de la tabla rol
CREATE TABLE rol (
	-- null = usuario sin registrar
	-- 0 = Rol de participante (Usuario general)
	-- 1 = Rol de Administrador
	-- 2 = Rol de Asesor
	rol_PK INT PRIMARY KEY NOT NULL,
	nombre_rol NVARCHAR(16) NOT NULL,
);

--Creación del correo de notificación
CREATE TABLE correo_notificacion (
	correo NVARCHAR(64) NOT NULL,
);

--Creación de la tabla usuario
CREATE TABLE usuario (
	id_usuario_PK NVARCHAR(64) PRIMARY KEY NOT NULL,
	rol_FK INT FOREIGN KEY REFERENCES rol(rol_PK) ON DELETE NO ACTION DEFAULT 0,
	hash_contrasena BINARY(64) NOT NULL,
	salt UNIQUEIDENTIFIER,
	registrado_por_usuario INT DEFAULT 0,
);

--Creación de la tabla asesor
CREATE TABLE asesor (
    id_usuario_FK NVARCHAR(64) NOT NULL,
    id_asesor_PK NVARCHAR(64) PRIMARY KEY NOT NULL,
    nombre NVARCHAR(64) NOT NULL,
    apellido_1 NVARCHAR(64) NOT NULL,
    apellido_2 NVARCHAR(64),
    tipo_identificacion NVARCHAR(16),
    numero_identificacion NVARCHAR(32),
    correo NVARCHAR(64) NOT NULL,
	descripcion NVARCHAR(256),
    telefono NVARCHAR(15),

    FOREIGN KEY (id_usuario_FK) REFERENCES usuario(id_usuario_PK)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

--Creación de la tabla participante
CREATE TABLE participante (
    id_usuario_FK NVARCHAR(64) NOT NULL,
    id_participante_PK NVARCHAR(64) PRIMARY KEY NOT NULL,
    nombre NVARCHAR(64) NOT NULL,
    apellido_1 NVARCHAR(64) NOT NULL,
    apellido_2 NVARCHAR(64),
	tipo_identificacion NVARCHAR(16),
    numero_identificacion NVARCHAR(32),
	correo NVARCHAR(64) NOT NULL,
	tipo_participante NVARCHAR(64),
    condicion NVARCHAR(64),
	telefono NVARCHAR(64),
    area NVARCHAR(64) NOT NULL,
    departamento NVARCHAR(64) NOT NULL,
    unidad_academica NVARCHAR(64) NOT NULL,
    sede NVARCHAR(64),
    total_horas_matriculadas INT DEFAULT 0,
    total_horas_aprobadas INT DEFAULT 0,
	correo_notificacion_enviado INT DEFAULT 0,

    FOREIGN KEY (id_usuario_FK) REFERENCES usuario(id_usuario_PK)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

--Creación de la tabla categoria
CREATE TABLE categoria (
	id_categoria_PK INT IDENTITY(1,1) PRIMARY KEY,
	nombre NVARCHAR(256) NOT NULL UNIQUE,
	descripcion NVARCHAR(512),
);

--Creación de la tabla tema
CREATE TABLE tema (
    id_tema_PK INT IDENTITY(1, 1) PRIMARY KEY,
    nombre NVARCHAR(256) NOT NULL
);

--Creación de la tabla grupo
CREATE TABLE grupo (
    id_grupo_PK INT IDENTITY(1, 1) PRIMARY KEY,
	id_categoria_FK INT NOT NULL,
	id_asesor_FK NVARCHAR(64),
	nombre NVARCHAR(256) NOT NULL,
    horario NVARCHAR(256) NOT NULL,
    fecha_inicio_grupo DATE NOT NULL,
    fecha_finalizacion_grupo DATE NOT NULL,
    fecha_inicio_inscripcion DATETIME NOT NULL,
    fecha_finalizacion_inscripcion DATETIME NOT NULL,
    cantidad_horas TINYINT NOT NULL,
    modalidad NVARCHAR(64) NOT NULL,
    cupo INT NOT NULL,
	numero_grupo INT DEFAULT 1,
    descripcion NVARCHAR(3000),
    lugar NVARCHAR(512),
    es_visible BIT,
    nombre_archivo NVARCHAR(256),
	adjunto VARBINARY(MAX),
    enlace NVARCHAR(64),
    clave_inscripcion NVARCHAR(64),


	FOREIGN KEY (id_categoria_FK) REFERENCES categoria(id_categoria_PK)
        ON DELETE NO ACTION,

	FOREIGN KEY (id_asesor_FK) REFERENCES asesor(id_asesor_PK)
        ON DELETE NO ACTION
);

--Creacion de la tabla inscripcion
CREATE TABLE inscripcion (
    id_inscripcion_PK INT IDENTITY(1, 1) PRIMARY KEY,
    estado NVARCHAR(16) NOT NULL,
    observaciones NVARCHAR(512),
    id_grupo_FK INT,
	numero_grupo INT,
	nombre_grupo NVARCHAR(256),
    id_participante_FK NVARCHAR(64) NOT NULL,
	horas_aprobadas INT DEFAULT 0,
	horas_matriculadas INT DEFAULT 0,
	calificacion FLOAT DEFAULT 0.0,

    FOREIGN KEY (id_participante_FK) REFERENCES participante(id_participante_PK)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

--Creacion de la tabla grupo_tema
CREATE TABLE grupo_tema (
    id_grupo_FK INT NOT NULL,
    id_tema_FK INT NOT NULL,
    PRIMARY KEY (id_grupo_FK, id_tema_FK),
    FOREIGN KEY (id_grupo_FK) REFERENCES grupo(id_grupo_PK) ON DELETE CASCADE,
    FOREIGN KEY (id_tema_FK) REFERENCES tema(id_tema_PK) ON DELETE CASCADE
);

/*
	Script para crear los triggers de la base de datos
*/

GO
--Creación de trigger cuando se cambia la identificación de un usuario en tabla de participantes
CREATE OR ALTER TRIGGER TR_ActualizarIdParticipante
ON usuario
AFTER UPDATE
AS
BEGIN
	DECLARE @id NVARCHAR(64) = (SELECT id_usuario_PK FROM inserted)

	UPDATE participante SET id_participante_PK =  @id WHERE id_usuario_FK = @id
END

GO
--Creación de trigger para cambiar rol de usuario cuando se agrega un asesor
CREATE OR ALTER TRIGGER TR_ActualizarRolAsesor
ON asesor
AFTER INSERT
AS
BEGIN
	DECLARE @id NVARCHAR(64) = (SELECT id_asesor_PK FROM inserted)

	UPDATE usuario SET rol_FK =  2 WHERE id_usuario_PK = @id
END

GO
--Creación de trigger cuando se cambia la identificación de un usuario en tabla de asesores
CREATE OR ALTER TRIGGER TR_ActualizarIdAsesor
ON usuario
AFTER UPDATE
AS
BEGIN
	DECLARE @id NVARCHAR(64) = (SELECT id_usuario_PK FROM inserted)

	UPDATE asesor SET id_asesor_PK =  @id WHERE id_usuario_FK = @id
END

GO
--Creación de trigger al eliminar participantes
CREATE OR ALTER TRIGGER TR_EliminarParticipante
ON participante
AFTER DELETE
AS
BEGIN
	DELETE FROM usuario
	WHERE id_usuario_PK IN (SELECT id_usuario_FK FROM deleted)
END

GO
--Creación de trigger al eliminar asesores
CREATE OR ALTER TRIGGER TR_EliminarAsesor
ON asesor
AFTER DELETE
AS
BEGIN
	DELETE FROM usuario
	WHERE id_usuario_PK IN (SELECT id_usuario_FK FROM deleted)
END



/*
	Script para crear los procedimientos almacenados de la base de datos
*/

GO
--Creación de procedimiento para crear un usuario
CREATE OR ALTER PROCEDURE InsertUsuario
    @id NVARCHAR(64),
	@rol INT = 0,
    @contrasena NVARCHAR(64)
AS
BEGIN
	DECLARE @salt UNIQUEIDENTIFIER=NEWID()

    INSERT INTO usuario (id_usuario_PK, rol_FK, hash_contrasena, salt)
    VALUES(@id, @rol, HASHBYTES('SHA2_512', @contrasena + CAST(@salt AS NVARCHAR(36))), @salt)
END

GO
--Creación de procedimiento para editar un usuario
CREATE OR ALTER PROCEDURE UpdateUsuario
    @id NVARCHAR(64),
	@rol INT = 0,
    @contrasena NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON

	DECLARE @salt UNIQUEIDENTIFIER=NEWID()

    UPDATE usuario SET id_usuario_PK = @id, rol_FK = @rol, hash_contrasena = HASHBYTES('SHA2_512', @contrasena + CAST(@salt AS NVARCHAR(36))), salt = @salt
    WHERE id_usuario_PK = @id
END

GO
--Creación de procedimiento para obtener los datos de un usuario
CREATE OR ALTER PROCEDURE SelectUsuario
    @id NVARCHAR(64)
AS
BEGIN
	SELECT id_usuario_PK, rol_FK FROM usuario WHERE id_usuario_PK = @id
END

GO
--Creación de procedimiento para verificar si existe un usuario
CREATE OR ALTER PROCEDURE ExistsUsuario
    @id NVARCHAR(64),
    @exists INT=0 OUTPUT
AS
BEGIN
    IF EXISTS (SELECT TOP 1 id_usuario_PK FROM usuario WHERE id_usuario_PK = @id)
		SET @exists = 1
	ELSE
		SET @exists = 0

	RETURN
END

GO
--Creación de procedimiento para verificar datos de inicio de sesión
CREATE OR ALTER PROCEDURE AuthUsuario
    @id NVARCHAR(64),
    @contrasena NVARCHAR(64),
    @auth INT=0 OUTPUT
AS
BEGIN
    SET NOCOUNT ON

    DECLARE @userID NVARCHAR(64)
    DECLARE @salt UNIQUEIDENTIFIER

    SET @salt = (SELECT TOP 1 salt FROM usuario WHERE id_usuario_PK = @id)

    IF EXISTS (SELECT TOP 1 id_usuario_PK FROM usuario WHERE id_usuario_PK = @id)
    BEGIN
        SET @userID = (SELECT id_usuario_PK FROM usuario WHERE id_usuario_PK = @id AND hash_contrasena = HASHBYTES('SHA2_512', @contrasena + CAST(@salt AS NVARCHAR(36))))

        IF(@userID IS NULL)
            SET @auth = 0
        ELSE 
            SET @auth = 1
    END
    ELSE
        SET @auth = 0

    RETURN
END

GO
--Creación de procedimiento para insertar un asesor
CREATE OR ALTER PROCEDURE InsertAsesor
    @idUsuario NVARCHAR(64),
    @idAsesor NVARCHAR(64),
    @tipoIdentificacion NVARCHAR(16) = '',
    @numeroIdentificacion NVARCHAR(32),
    @correo NVARCHAR(64),
    @nombre NVARCHAR(64),
    @apellido1 NVARCHAR(64),
    @apellido2 NVARCHAR(64) = '',
    @descripcion NVARCHAR(256) = '',
    @telefono NVARCHAR(64) = '',
	@unidadAcademica NVARCHAR(64) = '',
	@sede NVARCHAR(64) = ''
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO asesor
    (
        id_usuario_FK,
        id_asesor_PK,
        nombre,
        apellido_1,
        apellido_2,
        tipo_identificacion,
        numero_identificacion,
        correo,
        telefono,
        descripcion
    )
    VALUES
    (
        @idUsuario,
        @idAsesor,
        @nombre,
        @apellido1,
        @apellido2,
        @tipoIdentificacion,
        @numeroIdentificacion,
        @correo,
        @telefono,
        @descripcion
    );
END

GO
--Creación de procedimiento para actualizar los datos de un asesor
CREATE OR ALTER PROCEDURE UpdateAsesor
    @idUsuario NVARCHAR(64),
    @idAsesor NVARCHAR(64),
    @tipoIdentificacion NVARCHAR(16) = '',
    @numeroIdentificacion NVARCHAR(32),
    @correo NVARCHAR(64),
    @nombre NVARCHAR(64),
    @apellido1 NVARCHAR(64),
    @apellido2 NVARCHAR(64) = '',
    @descripcion NVARCHAR(256) = '',
    @telefono NVARCHAR(64) = '',
	@unidadAcademica NVARCHAR(64) = '',
	@sede NVARCHAR(64) = ''
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE asesor
    SET
        tipo_identificacion = @tipoIdentificacion,
        numero_identificacion = @numeroIdentificacion,
        correo = @correo,
        nombre = @nombre,
        apellido_1 = @apellido1,
        apellido_2 = @apellido2,
        descripcion = @descripcion,
        telefono = @telefono
    WHERE
        id_usuario_FK = @idUsuario
        AND id_asesor_PK = @idAsesor;
END

GO
--Creación de procedimiento para obtener los datos de un asesor
CREATE OR ALTER PROCEDURE SelectAsesor
    @idAsesor NVARCHAR(64)
AS
BEGIN
	SELECT * FROM asesor WHERE id_asesor_PK = @idAsesor
END

GO
--Creación de procedimiento para verificar si existe un asesor
CREATE OR ALTER PROCEDURE ExistsAsesor
    @idAsesor NVARCHAR(64),
    @exists INT=0 OUTPUT
AS
BEGIN
    IF EXISTS (SELECT TOP 1 id_asesor_PK FROM asesor WHERE id_asesor_PK = @idAsesor)
		SET @exists = 1
	ELSE
		SET @exists = 0

	RETURN
END

GO
--Creación de procedimiento para insertar un participante
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
    @area NVARCHAR(64) = '',
    @departamento NVARCHAR(64) = '',
	@unidadAcademica NVARCHAR(64) = '',
	@sede NVARCHAR(64) = '',
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
		@horasMatriculadas,
		@horasAprobadas
    );
END

GO
--Creación de procedimiento para actualizar los datos de un participante
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
    @area NVARCHAR(64) = '',
    @departamento NVARCHAR(64) = '',
    @unidadAcademica NVARCHAR(64) = '',
    @sede NVARCHAR(64) = '',
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
        total_horas_matriculadas = @horasMatriculadas,
        total_horas_aprobadas = @horasAprobadas
    WHERE
        id_usuario_FK = @idUsuario
        AND id_participante_PK = @idParticipante;
END

GO


--Creación de procedimiento para obtener los datos de un participante
CREATE OR ALTER PROCEDURE SelectParticipante
    @id NVARCHAR(64)
AS
BEGIN
	SELECT * FROM participante WHERE id_participante_PK = @id
END

GO
--Creación de procedimiento para verificar si existe un participante
CREATE OR ALTER PROCEDURE ExistsParticipante
    @id NVARCHAR(64),
    @exists INT=0 OUTPUT
AS
BEGIN
    IF EXISTS (SELECT TOP 1 id_participante_PK FROM participante WHERE id_participante_PK = @id)
		SET @exists = 1
	ELSE
		SET @exists = 0

	RETURN
END

GO
--Creación de procedimiento para insertar un grupo
CREATE OR ALTER PROCEDURE InsertGrupo
	@idCategoria INT,
	@idAsesor NVARCHAR(64) = NULL,
	@nombre NVARCHAR(256),
	@modalidad NVARCHAR(64),
	@cantidad_horas INT,
	@cupo INT,
	@numeroGrupo INT,
    @fecha_inicio_grupo DATE,
    @fecha_finalizacion_grupo DATE,
    @fecha_inicio_inscripcion DATETIME,
    @fecha_finalizacion_inscripcion DATETIME,
	@descripcion NVARCHAR(3000) = '',
	@horario NVARCHAR(512) = '',
	@lugar NVARCHAR(512) = '',
	@es_visible BIT = 1,
	@nombre_archivo NVARCHAR(256) = '',
	@adjunto VARBINARY(MAX) = NULL,
    @enlace NVARCHAR(64) = '',
    @clave_inscripcion NVARCHAR(64) = ''
AS
BEGIN
    SET NOCOUNT ON;

    -- Insertar el grupo en la tabla grupo
    INSERT INTO grupo
    (
		id_categoria_FK,
		id_asesor_FK,
        nombre,
        horario,
		fecha_inicio_grupo,
        fecha_finalizacion_grupo,
        fecha_inicio_inscripcion,
        fecha_finalizacion_inscripcion,
        cantidad_horas,
		modalidad,
        cupo,
		numero_grupo,
        descripcion,
        lugar,
        es_visible,
		nombre_archivo,
		adjunto,
        enlace,
        clave_inscripcion
    )
    VALUES
    (
		@idCategoria,
		@idAsesor,
		@nombre,
		@horario,
		@fecha_inicio_grupo,
		@fecha_finalizacion_grupo,
		@fecha_inicio_inscripcion,
		@fecha_finalizacion_inscripcion,
		@cantidad_horas,
		@modalidad,
		@cupo,
		@numeroGrupo,
		@descripcion,
		@lugar,
		@es_visible,
		@nombre_archivo,
		@adjunto,
        @enlace,
        @clave_inscripcion
    );

    -- Obtener el ID del grupo recién insertado
    DECLARE @idGrupo INT = SCOPE_IDENTITY();

    -- Retornar el ID del grupo
    SELECT @idGrupo AS idGrupo;
END;

GO


-- Creación de procedimiento para insertar grupo-tema
CREATE OR ALTER PROCEDURE InsertGrupoTema
    @idGrupo INT,
    @idTema INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Insertar la relación en la tabla grupo_tema
    INSERT INTO grupo_tema (id_grupo_FK, id_tema_FK)
    VALUES (@idGrupo, @idTema);
END;

GO


-- Creación de procedimiento para editar un grupo
CREATE OR ALTER PROCEDURE UpdateGrupo
    @idGrupo INT,
	@idCategoria INT,
	@idAsesor NVARCHAR(64) = NULL,
	@nombre NVARCHAR(256),
	@modalidad NVARCHAR(64),
	@cantidad_horas INT,
	@cupo INT,
	@numeroGrupo INT,
    @fecha_inicio_grupo DATE,
    @fecha_finalizacion_grupo DATE,
    @fecha_inicio_inscripcion DATETIME,
    @fecha_finalizacion_inscripcion DATETIME,
	@descripcion NVARCHAR(3000) = '',
	@horario NVARCHAR(512) = '',
	@lugar NVARCHAR(512) = '',
	@es_visible BIT = 1,
	@nombre_archivo NVARCHAR(256) = '',
	@adjunto VARBINARY(MAX) = NULL,
    @enlace NVARCHAR(64) = '',
    @clave_inscripcion NVARCHAR(64) = ''
AS
BEGIN
    SET NOCOUNT ON;
	IF (LEN(@adjunto) > 0)
		BEGIN
			UPDATE grupo 
			SET nombre_archivo = @nombre_archivo, adjunto = @adjunto 
			WHERE id_grupo_PK = @idGrupo;
		END

    UPDATE grupo
    SET
		id_categoria_FK = @idCategoria,
		id_asesor_FK = @idAsesor,
		nombre = @nombre,
        horario = @horario,
        fecha_inicio_grupo = @fecha_inicio_grupo,
        fecha_finalizacion_grupo = @fecha_finalizacion_grupo,
        fecha_inicio_inscripcion = @fecha_inicio_inscripcion,
        fecha_finalizacion_inscripcion = @fecha_finalizacion_inscripcion,
        cantidad_horas = @cantidad_horas,
        modalidad = @modalidad,
        cupo = @cupo,
		numero_grupo = @numeroGrupo,
        descripcion = @descripcion,
        lugar = @lugar,
        es_visible = @es_visible,
        enlace = @enlace,
        clave_inscripcion = @clave_inscripcion
    WHERE
        id_grupo_PK = @idGrupo;
END

GO
-- Creación de procedimiento para seleccionar todos los grupos disponibles
CREATE OR ALTER PROCEDURE SelectAllGrupos
AS
BEGIN
    SELECT 
		G.id_grupo_PK, 
		G.id_categoria_FK,
		G.id_asesor_FK,
		G.modalidad, 
		G.cupo, 
		G.numero_grupo,
		G.descripcion, 
		G.es_visible, 
		G.lugar,
		G.nombre,
		G.horario, 
		G.fecha_inicio_grupo, 
		G.fecha_finalizacion_grupo, 
		G.fecha_inicio_inscripcion, 
		G.fecha_finalizacion_inscripcion, 
		G.cantidad_horas, 
		G.nombre_archivo,
		C.nombre as nombre_categoria,
		A.nombre + ' ' + A.apellido_1 + ' ' + A.apellido_2 as nombre_asesor,
        G.enlace,
        G.clave_inscripcion
	FROM grupo G
		JOIN categoria C ON C.id_categoria_PK = G.id_categoria_FK
		LEFT JOIN asesor A ON A.id_asesor_PK = G.id_asesor_FK;
END

GO
-- Creación de procedimiento para seleccionar todos los grupos de un asesor
CREATE OR ALTER PROCEDURE SelectGruposAsesor
	@idAsesor NVARCHAR(64) = NULL
AS
BEGIN
    SELECT 
		G.id_grupo_PK, 
		G.id_categoria_FK,
		G.id_asesor_FK,
		G.modalidad, 
		G.cupo,
		G.numero_grupo,
		G.descripcion, 
		G.es_visible, 
		G.lugar,
		G.nombre,
		G.horario, 
		G.fecha_inicio_grupo, 
		G.fecha_finalizacion_grupo, 
		G.fecha_inicio_inscripcion, 
		G.fecha_finalizacion_inscripcion, 
		G.cantidad_horas, 
		G.nombre_archivo, 
		G.adjunto,
		C.nombre as nombre_categoria,
		A.nombre + ' ' + A.apellido_1 + ' ' + A.apellido_2 as nombre_asesor,
        G.enlace,
        G.clave_inscripcion
	FROM grupo G
		JOIN categoria C ON C.id_categoria_PK = G.id_categoria_FK
		JOIN asesor A ON A.id_asesor_PK = G.id_asesor_FK 
	WHERE G.id_asesor_FK = @idAsesor;
END

GO
-- Creación de procedimiento para seleccionar un grupo según su id
CREATE OR ALTER PROCEDURE SelectGrupo
    @idGrupo INT
AS
BEGIN
    SELECT 
		G.id_grupo_PK, 
		G.id_categoria_FK,
		G.id_asesor_FK,
		G.modalidad, 
		G.cupo,
		G.numero_grupo,
		G.descripcion, 
		G.es_visible, 
		G.lugar,
		G.nombre,
		G.horario, 
		G.fecha_inicio_grupo, 
		G.fecha_finalizacion_grupo, 
		G.fecha_inicio_inscripcion, 
		G.fecha_finalizacion_inscripcion, 
		G.cantidad_horas, 
		G.nombre_archivo, 
		G.adjunto,
		C.nombre as nombre_categoria,
		A.nombre + ' ' + A.apellido_1 + ' ' + A.apellido_2 as nombre_asesor,
        G.enlace,
        G.clave_inscripcion
	FROM grupo G
		JOIN categoria C ON C.id_categoria_PK = G.id_categoria_FK
		LEFT JOIN asesor A ON A.id_asesor_PK = G.id_asesor_FK 
    WHERE G.id_grupo_PK = @idGrupo;
END


/*
	SCRIPT SET DE DATOS INICIAL
*/

GO
--Crear roles
INSERT INTO rol
(rol_PK, nombre_rol)
VALUES
(0, N'Usuario'),
(1, N'Administrador'),
(2, N'Asesor')

--Crear correo de notificación
INSERT INTO correo_notificacion
VALUES (N'soporte.metics@ucr.ac.cr')

--Crear usuarios
	-- admin
EXEC InsertUsuario 
	@id = N'admin.admin@ucr.ac.cr',
	@rol = 1,
	@contrasena = N'#Q+3n?OWk3i0:qG'

EXEC InsertUsuario 
	@id = N'admin.docencia.metics@ucr.ac.cr',
	@rol = 1,
	@contrasena = N'12345'

--Crear asesores
EXEC InsertUsuario
	@id = N'MARIA.ENRIQUEZ@ucr.ac.cr',
	@rol = 2,
	@contrasena = N'4fG7hJ2kL9';

EXEC InsertAsesor 
    @idUsuario = N'MARIA.ENRIQUEZ@ucr.ac.cr', 
    @idAsesor = N'MARIA.ENRIQUEZ@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', 
    @numeroIdentificacion = N'1-1070-0400', 
    @correo = N'MARIA.ENRIQUEZ@ucr.ac.cr', 
    @nombre = N'María Ileana', 
    @apellido1 = N'Enriquez', 
    @apellido2 = N'Barrantes',
	@unidadAcademica = N'METICS',
	@sede = N'Sede Rodrigo Facio',
	@descripcion = N'Asesora docente en METICS.'

EXEC InsertUsuario
	@id = N'jose.elizondosalas@ucr.ac.cr',
	@rol = 2,
	@contrasena = N'9dH3kL8pQz';

EXEC InsertAsesor 
    @idUsuario = N'jose.elizondosalas@ucr.ac.cr', 
    @idAsesor = N'jose.elizondosalas@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', 
    @numeroIdentificacion = N'1-1520-0692', 
    @correo = N'jose.elizondosalas@ucr.ac.cr', 
    @nombre = N'Jose Antonio', 
    @apellido1 = N'Elizondo', 
    @apellido2 = N'Salas',
	@unidadAcademica = N'METICS',
	@sede = N'Sede Rodrigo Facio',
	@descripcion = N'Productor audiovisual y asesor docente en METICS.'

EXEC InsertUsuario
	@id = N'ORLANDO.GOMEZ@ucr.ac.cr',
	@rol = 2,
	@contrasena = N'2fJ8rK5tXq';

EXEC InsertAsesor 
    @idUsuario = N'ORLANDO.GOMEZ@ucr.ac.cr', 
    @idAsesor = N'ORLANDO.GOMEZ@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', 
    @numeroIdentificacion = N'4-0218-0838', 
    @correo = N'ORLANDO.GOMEZ@ucr.ac.cr', 
    @nombre = N'Orlando Daniel', 
    @apellido1 = N'Gómez', 
    @apellido2 = N'Arias',
	@unidadAcademica = N'METICS',
	@sede = N'Sede Rodrigo Facio',
	@descripcion = N'Gestor de Tecnologías de Información en METICS.'

EXEC InsertUsuario
	@id = N'AARON.MENAARAYA@ucr.ac.cr',
	@rol = 2,
	@contrasena = N'6hP2jN9vWz';

EXEC InsertAsesor 
    @idUsuario = N'AARON.MENAARAYA@ucr.ac.cr', 
    @idAsesor = N'AARON.MENAARAYA@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', 
    @numeroIdentificacion = N'6-0329-0803', 
    @correo = N'AARON.MENAARAYA@ucr.ac.cr', 
    @nombre = N'Aarón Elí', 
    @apellido1 = N'Mena', 
    @apellido2 = N'Araya',
	@unidadAcademica = N'METICS',
	@sede = N'Sede Rodrigo Facio',
	@descripcion = N'Director de METICS, Profesor Catedrático de la Escuela de Ciencias de la Comunicación Colectiva.'

EXEC InsertUsuario
	@id = N'brenda.alfaro@ucr.ac.cr',
	@rol = 2,
	@contrasena = N'3tX5kL8dVj';

EXEC InsertAsesor 
    @idUsuario = N'brenda.alfaro@ucr.ac.cr', 
    @idAsesor = N'brenda.alfaro@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', 
    @numeroIdentificacion = N'1-0603-0810', 
    @correo = N'brenda.alfaro@ucr.ac.cr', 
    @nombre = N'Brenda Lidis', 
    @apellido1 = N'Alfaro', 
    @apellido2 = N'González',
	@unidadAcademica = N'METICS',
	@sede = N'Sede Rodrigo Facio',
	@descripcion = N'Productora audiovisual y asesora docente en METICS.'


--Crear participantes


EXEC InsertUsuario @id = N'docencia.metics2@ucr.ac.cr', @rol = 0, @contrasena = N'12345';
EXEC InsertParticipante @idUsuario = N'docencia.metics2@ucr.ac.cr', @idParticipante = N'docencia.metics2@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', @numeroIdentificacion = N'222222222', @correo = N'docencia.metics2@ucr.ac.cr',
    @nombre = N'Nombre.metics2', @apellido1 = N'Apellido.metics2', @apellido2 = NULL, 
    @area = N'Área de Artes y Letras', @departamento = N'Facultad de Artes', 
    @unidadAcademica = N'Escuela de Artes Dramáticas', @telefono = N'200200200',
    @tipoParticipante = N'Interino', @condicion = N'Activo', @horasMatriculadas = 0, @horasAprobadas = 0;

EXEC InsertUsuario @id = N'docencia.metics3@ucr.ac.cr', @rol = 0, @contrasena = N'12345';
EXEC InsertParticipante @idUsuario = N'docencia.metics3@ucr.ac.cr', @idParticipante = N'docencia.metics3@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', @numeroIdentificacion = N'333333333', @correo = N'docencia.metics3@ucr.ac.cr',
    @nombre = N'Nombre.metics3', @apellido1 = N'Apellido.metics3', @apellido2 = NULL, 
    @area = N'Área de Artes y Letras', @departamento = N'Facultad de Artes', 
    @unidadAcademica = N'Escuela de Artes Dramáticas', @telefono = N'300300300',
    @tipoParticipante = N'Interino', @condicion = N'Activo', @horasMatriculadas = 0, @horasAprobadas = 0;

EXEC InsertUsuario @id = N'docencia.metics4@ucr.ac.cr', @rol = 0, @contrasena = N'12345';
EXEC InsertParticipante @idUsuario = N'docencia.metics4@ucr.ac.cr', @idParticipante = N'docencia.metics4@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', @numeroIdentificacion = N'444444444', @correo = N'docencia.metics4@ucr.ac.cr',
    @nombre = N'Nombre.metics4', @apellido1 = N'Apellido.metics4', @apellido2 = NULL, 
    @area = N'Área de Artes y Letras', @departamento = N'Facultad de Artes', 
    @unidadAcademica = N'Escuela de Artes Dramáticas', @telefono = N'400400400',
    @tipoParticipante = N'Interino', @condicion = N'Activo', @horasMatriculadas = 0, @horasAprobadas = 0;

EXEC InsertUsuario @id = N'docencia.metics5@ucr.ac.cr', @rol = 0, @contrasena = N'12345';
EXEC InsertParticipante @idUsuario = N'docencia.metics5@ucr.ac.cr', @idParticipante = N'docencia.metics5@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', @numeroIdentificacion = N'555555555', @correo = N'docencia.metics5@ucr.ac.cr',
    @nombre = N'Nombre.metics5', @apellido1 = N'Apellido.metics5', @apellido2 = NULL, 
    @area = N'Área de Artes y Letras', @departamento = N'Facultad de Artes', 
    @unidadAcademica = N'Escuela de Artes Dramáticas', @telefono = N'500500500',
    @tipoParticipante = N'Interino', @condicion = N'Activo', @horasMatriculadas = 0, @horasAprobadas = 0;

EXEC InsertUsuario @id = N'docencia.metics6@ucr.ac.cr', @rol = 0, @contrasena = N'12345';
EXEC InsertParticipante @idUsuario = N'docencia.metics6@ucr.ac.cr', @idParticipante = N'docencia.metics6@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', @numeroIdentificacion = N'666666666', @correo = N'docencia.metics6@ucr.ac.cr',
    @nombre = N'Nombre.metics6', @apellido1 = N'Apellido.metics6', @apellido2 = NULL, 
    @area = N'Área de Artes y Letras', @departamento = N'Facultad de Artes', 
    @unidadAcademica = N'Escuela de Artes Dramáticas', @telefono = N'600600600',
    @tipoParticipante = N'Interino', @condicion = N'Activo', @horasMatriculadas = 0, @horasAprobadas = 0;


--Crear tipos de actividades
/* INSERT INTO tipos_actividad
(nombre, descripcion)
VALUES
(N'Curso', N'Los cursos cuentan con una duración de cinco a seis horas semanales, en la que se realizarán tareas, exámenes y quices para evaluar los conceptos aprendidos por los estudiantes'),
(N'Taller', N'Un taller es una metodología de trabajo que se caracteriza por la investigación, el aprendizaje por descubrimiento y el trabajo en equipo'),
(N'Taller corto', N'Un taller de menos duración de dos a tres horas semanales donde se trabajará con un tema en específico. No hay exámenes, el objetivo es que aprendan el concepto tratado'),
(N'Charla', N'Se realizarán charlas con profesionales invitados') */

--Crear categorias
INSERT INTO categoria(nombre, descripcion)
VALUES
	(N'Nivel 1', N'Aborda las etapas de Integrador (B1) y Experto (B2)'),
	(N'Nivel 2', N'Incluye elementos de las etapas de Líder(C1) y Pionero (C2)')

--Crear temas
INSERT INTO tema(nombre)
VALUES
	(N'Compromiso profesional'),
	(N'Contenidos digitales'),
	(N'Enseñanza y aprendizaje'),
	(N'Evaluación y retroalimentación'),
	(N'Empoderamiento de estudiantes'),
	(N'Competencias de estudiantes')

--Declarar archivo que se va a adjuntar (RUTA DEL ARCHIVO, Ejm: C:\Users\UserName\Documents\Folder\FileName.pdf) 
/*DECLARE @adjunto varbinary(max)
SELECT @adjunto = CAST(bulkcolumn AS varbinary(max))
FROM OPENROWSET(BULK '\\wsl.localhost\Ubuntu-20.04\home\yasty\src\METICS-CORE\FileName.pdf', SINGLE_BLOB) AS x*/

--Crear grupos

INSERT INTO dbo.grupo(
    id_categoria_FK,
    id_asesor_FK,
    modalidad,
    cupo,
	numero_grupo,
    cantidad_horas,
    descripcion, 
    es_visible, 
    lugar,
    nombre, 
    horario,
    fecha_inicio_grupo, 
    fecha_finalizacion_grupo,
    fecha_inicio_inscripcion, 
    fecha_finalizacion_inscripcion,
    nombre_archivo,
    enlace,
    clave_inscripcion)
    VALUES
    ( 
    1, 
    N'AARON.MENAARAYA@ucr.ac.cr', 
    N'Virtual', 
    15, 
    1,
	10,
    N'Incluye actividades de aprendizaje que involucran el uso de entornos virtuales, herramientas de visualización de pensamiento y otros recursos digitales para mediar actividades de aprendizaje diseñadas para apoyar el desarrollo del pensamiento crítico.'
    + CHAR(10) + CHAR(10) + 'Para participar en este módulo, las personas docentes deben tener acceso a una computadora durante las sesiones sincrónicas y contar con al menos con un entorno virtual en la plataforma Mediación Virtual (Pasado, En Progreso o Futuro).',
    1, 
    N'Universidad de Costa Rica, Rodrigo Facio',
    N'Diseño Didáctico para el Desarrollo del Pensamiento Crítico', 
    N'Viernes de 09:30-12:00',
    '2024-09-13 00:00:00', 
    '2024-09-27 00:00:00', 
    '2024-01-01 00:00:00', 
    '2024-12-09 00:00:00', 
    N'ArchivoPrueba.pdf',
    'https://encuestas.ucr.ac.cr/index.php/256355?lang=es-MX',
    'DDDPC');

-- Insertar valores iniciales grupo_tema
INSERT INTO grupo_tema (id_grupo_FK, id_tema_FK)
VALUES (1, 1);