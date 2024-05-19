/*
	Script para crear las tablas de la base de datos
*/

--Creacion de la tabla rol
CREATE TABLE rol(
	-- null = usuario sin registrar
	-- 0 = Rol de participante (Usuario general)
	-- 1 = Rol de Administrador
	-- 2 = Rol de Asesor
	rol_PK INT PRIMARY KEY NOT NULL,
	nombre_rol VARCHAR(16) NOT NULL,
);

--Creación de la tabla usuario
CREATE TABLE usuario(
	id_usuario_PK VARCHAR(64) PRIMARY KEY NOT NULL,
	rol_FK INT FOREIGN KEY REFERENCES rol(rol_PK) ON DELETE NO ACTION DEFAULT 0,
	hash_contrasena BINARY(64) NOT NULL,
	salt UNIQUEIDENTIFIER
);

--Creación de la tabla asesor
CREATE TABLE asesor(
	id_usuario_FK VARCHAR(64) NOT NULL FOREIGN KEY REFERENCES usuario(id_usuario_PK) ON DELETE CASCADE ON UPDATE CASCADE,
	id_asesor_PK VARCHAR(64) PRIMARY KEY NOT NULL,
	nombre NVARCHAR(64) NOT NULL,
	apellido_1 NVARCHAR(64) NOT NULL,
	apellido_2 NVARCHAR(64),
	telefonos NVARCHAR(15),
	descripcion NVARCHAR(256)
);

--Creación de la tabla participante
CREATE TABLE participante(
	id_usuario_FK VARCHAR(64) NOT NULL FOREIGN KEY REFERENCES usuario(id_usuario_PK) ON DELETE CASCADE ON UPDATE CASCADE,
	id_participante_PK VARCHAR(64) PRIMARY KEY NOT NULL,
	tipo_identificacion VARCHAR(16) NOT NULL,
	correo NVARCHAR(64) NOT NULL,
	nombre NVARCHAR(64) NOT NULL,
	apellido_1 NVARCHAR(64) NOT NULL,
	apellido_2 NVARCHAR(64),
	unidad_academica NVARCHAR(64),
	area NVARCHAR(64) NOT NULL,
	departamento NVARCHAR(64) NOT NULL,
	telefonos NVARCHAR(64),
	seccion NVARCHAR(64),
	tipo_participante NVARCHAR(64),
	condicion NVARCHAR(32),
	horas_matriculadas INT DEFAULT 0,
	horas_aprobadas INT DEFAULT 0
);

--Creación de la tabla tipo_actividad
CREATE TABLE tipos_actividad(
	id_tipos_actividad_PK INT IDENTITY(1,1) PRIMARY KEY,
	nombre NVARCHAR(64) NOT NULL UNIQUE,
	descripcion NVARCHAR(256),
);

--Creación de la tabla categoria
CREATE TABLE categoria(
	id_categoria_PK INT IDENTITY(1,1) PRIMARY KEY,
	nombre NVARCHAR(64) NOT NULL UNIQUE,
	descripcion NVARCHAR(256),
);

--Creación de la tabla tema
CREATE TABLE tema(
	id_tema_PK INT IDENTITY(1,1) PRIMARY KEY,
	nombre NVARCHAR(64) NOT NULL,
	id_categoria_FK INT FOREIGN KEY REFERENCES categoria(id_categoria_PK) ON DELETE CASCADE ON UPDATE CASCADE,
	id_tipos_actividad_FK INT FOREIGN KEY REFERENCES tipos_actividad(id_tipos_actividad_PK) ON DELETE CASCADE ON UPDATE CASCADE,
);

--Creación de la tabla asesor da tema
CREATE TABLE asesor_da_tema(
	id_tema_FK INT NOT NULL,
	id_asesor_FK VARCHAR(64) NOT NULL,
	asesores_asistentes NVARCHAR(MAX),
	CONSTRAINT asesor_da_tema_PK PRIMARY KEY (id_tema_FK, id_asesor_FK),
	CONSTRAINT id_tema_FK FOREIGN KEY (id_tema_FK) REFERENCES tema(id_tema_PK)
	ON DELETE CASCADE
	ON UPDATE CASCADE,
	CONSTRAINT id_asesor_FK FOREIGN KEY (id_asesor_FK) REFERENCES asesor(id_asesor_PK)
	ON DELETE CASCADE
	ON UPDATE CASCADE,
);

--Creación de la tabla grupo
CREATE TABLE grupo(
	id_grupo_PK INT IDENTITY(1,1) PRIMARY KEY,
	id_tema_FK INT FOREIGN KEY REFERENCES tema(id_tema_PK) ON DELETE NO ACTION,
	modalidad NVARCHAR(16) NOT NULL,
	cupo INT NOT NULL,
	descripcion NVARCHAR(256),
	adjunto VARBINARY(MAX),
	es_visible BIT,
	lugar NVARCHAR(512),
	nombre NVARCHAR(64) NOT NULL,
	horario NVARCHAR(128) NOT NULL,
	fecha_inicio_grupo DATE NOT NULL,
	fecha_finalizacion_grupo DATE NOT NULL,
	fecha_inicio_inscripcion DATETIME NOT NULL,
	fecha_finalizacion_inscripcion DATETIME NOT NULL,
	cantidad_horas TINYINT NOT NULL,
	nombre_archivo NVARCHAR(255),
);

--Creacion de la tabla inscripcion
CREATE TABLE inscripcion(
	id_inscripcion_PK INT IDENTITY(1,1) PRIMARY KEY,
	estado VARCHAR(16) NOT NULL,
	observaciones NVARCHAR(512),
	id_grupo_FK INT NOT NULL,
	id_participante_FK VARCHAR(64) NOT NULL,
	CONSTRAINT id_participante_FK FOREIGN KEY (id_participante_FK) REFERENCES participante(id_participante_PK)
	ON DELETE CASCADE
	ON UPDATE CASCADE,
	CONSTRAINT id_grupo_FK FOREIGN KEY (id_grupo_FK) REFERENCES grupo(id_grupo_PK)
	ON DELETE CASCADE
	ON UPDATE CASCADE,
);

--Creación de la tabla calificaciones
CREATE TABLE calificaciones(
	id_grupo_FK INT NOT NULL,
	id_participante_FK VARCHAR(64) NOT NULL,
	calificacion FLOAT DEFAULT 0.0,
	CONSTRAINT id_participante_calificacion_FK FOREIGN KEY (id_participante_FK) REFERENCES participante(id_participante_PK)
	ON DELETE CASCADE
	ON UPDATE CASCADE,
	CONSTRAINT id_grupo_calificacion_FK FOREIGN KEY (id_grupo_FK) REFERENCES grupo(id_grupo_PK)
	ON DELETE CASCADE
	ON UPDATE CASCADE,
);

/*
	Script para crear los triggers de la base de datos
*/

GO
--Creación de trigger cuando se cambia la identificación de un usuario en tabla de participantes
CREATE TRIGGER TR_ActualizarIdParticipante
ON usuario
AFTER UPDATE
AS
BEGIN
	DECLARE @id VARCHAR(64) = (SELECT id_usuario_PK FROM inserted)

	UPDATE participante SET id_participante_PK =  @id WHERE id_usuario_FK = @id
END

GO
--Creación de trigger para cambiar rol de usuario cuando se agrega un asesor
CREATE TRIGGER TR_ActualizarRolAsesor
ON asesor
AFTER INSERT
AS
BEGIN
	DECLARE @id VARCHAR(64) = (SELECT id_asesor_PK FROM inserted)

	UPDATE usuario SET rol_FK =  2 WHERE id_usuario_PK = @id
END

GO
--Creación de trigger cuando se cambia la identificación de un usuario en tabla de asesores
CREATE TRIGGER TR_ActualizarIdAsesor
ON usuario
AFTER UPDATE
AS
BEGIN
	DECLARE @id VARCHAR(64) = (SELECT id_usuario_PK FROM inserted)

	UPDATE asesor SET id_asesor_PK =  @id WHERE id_usuario_FK = @id
END

GO
--Creación de trigger al insertar inscripciones
CREATE TRIGGER TR_InsertarCalificacion
ON inscripcion
AFTER INSERT
AS
BEGIN
	INSERT INTO calificaciones(id_grupo_FK, id_participante_FK)
		SELECT id_grupo_FK, id_participante_FK
		FROM inserted
END

GO
--Creación de trigger al eliminar inscripciones
CREATE TRIGGER TR_EliminarCalificacion
ON inscripcion
AFTER DELETE
AS
BEGIN
	DELETE FROM calificaciones
	WHERE id_grupo_FK IN (SELECT id_grupo_FK FROM deleted)
	AND id_participante_FK IN (SELECT id_participante_FK FROM deleted)
END


/*
	Script para crear los procedimientos almacenados de la base de datos
*/

GO
--Creación de procedimiento para insertar un usuario
CREATE PROCEDURE InsertarUsuario
    @id VARCHAR(64),
    @contrasena NVARCHAR(64),
	@exito INT=0 OUTPUT
AS
BEGIN
    SET NOCOUNT ON

	DECLARE @salt UNIQUEIDENTIFIER=NEWID()
    BEGIN TRY
        INSERT INTO usuario (id_usuario_PK, hash_contrasena, salt)
        VALUES(@id, HASHBYTES('SHA2_512', @contrasena + CAST(@salt AS NVARCHAR(36))), @salt)

		SET @exito=1
    END TRY
    BEGIN CATCH
		SET @exito=0
    END CATCH

	RETURN
END

GO
--Creación de procedimiento para editar un usuario
CREATE PROCEDURE EditarUsuario
    @id VARCHAR(64),
    @contrasena NVARCHAR(64),
    @exito INT=0 OUTPUT
AS
BEGIN
    SET NOCOUNT ON

	DECLARE @salt UNIQUEIDENTIFIER=NEWID()
    BEGIN TRY

        UPDATE usuario SET id_usuario_PK = @id, hash_contrasena = HASHBYTES('SHA2_512', @contrasena + CAST(@salt AS NVARCHAR(36))), salt = @salt
        WHERE id_usuario_PK = @id

        SET @exito=1

    END TRY
    BEGIN CATCH
        SET @exito=0
    END CATCH

END

GO
--Creación de procedimiento para obtener los datos de un usuario
CREATE PROCEDURE ObtenerUsuario
    @id VARCHAR(64)
AS
BEGIN
	SELECT id_usuario_PK, rol_FK FROM usuario WHERE id_usuario_PK=@id
END

GO
--Creación de procedimiento para verificar si existe un usuario
CREATE PROCEDURE ExisteUsuario
    @id VARCHAR(64),
    @existe INT=0 OUTPUT
AS
BEGIN
    IF EXISTS (SELECT TOP 1 id_usuario_PK FROM usuario WHERE id_usuario_PK=@id)
		SET @existe=1
	ELSE
		SET @existe=0

	RETURN
END

GO
--Creación de procedimiento para verificar datos de inicio de sesión
CREATE PROCEDURE ValidarUsuario
    @id VARCHAR(64),
    @contrasena NVARCHAR(64),
    @exito INT=0 OUTPUT
AS
BEGIN

    SET NOCOUNT ON

    DECLARE @userID INT

	DECLARE @salt UNIQUEIDENTIFIER = (SELECT TOP 1 salt FROM usuario WHERE id_usuario_PK=@id)

    IF EXISTS (SELECT TOP 1 id_usuario_PK FROM usuario WHERE id_usuario_PK=@id)
    BEGIN
        SET @userID=(SELECT id_usuario_PK FROM usuario WHERE id_usuario_PK=@id AND hash_contrasena=HASHBYTES('SHA2_512', @contrasena + CAST(@salt AS NVARCHAR(36))))

       IF(@userID IS NULL)
           SET @exito=0
       ELSE 
           SET @exito=1
    END
    ELSE
       SET @exito=0

END
