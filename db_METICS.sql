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

--Creación de la tabla usuario
CREATE TABLE usuario (
	id_usuario_PK NVARCHAR(64) PRIMARY KEY NOT NULL,
	rol_FK INT FOREIGN KEY REFERENCES rol(rol_PK) ON DELETE NO ACTION DEFAULT 0,
	hash_contrasena BINARY(64) NOT NULL,
	salt UNIQUEIDENTIFIER
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
    horas_matriculadas INT DEFAULT 0,
    horas_aprobadas INT DEFAULT 0,

    FOREIGN KEY (id_usuario_FK) REFERENCES usuario(id_usuario_PK)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

--Creación de la tabla tipo_actividad
CREATE TABLE tipos_actividad (
	id_tipos_actividad_PK INT IDENTITY(1,1) PRIMARY KEY,
	nombre NVARCHAR(64) NOT NULL UNIQUE,
	descripcion NVARCHAR(256),
);

--Creación de la tabla categoria
CREATE TABLE categoria (
	id_categoria_PK INT IDENTITY(1,1) PRIMARY KEY,
	nombre NVARCHAR(64) NOT NULL UNIQUE,
	descripcion NVARCHAR(256),
);

--Creación de la tabla tema
CREATE TABLE tema (
    id_tema_PK INT IDENTITY(1, 1) PRIMARY KEY,
    nombre NVARCHAR(64) NOT NULL,
    id_categoria_FK INT NOT NULL,
    id_tipos_actividad_FK INT NOT NULL,

    FOREIGN KEY (id_categoria_FK) REFERENCES categoria(id_categoria_PK)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    FOREIGN KEY (id_tipos_actividad_FK) REFERENCES tipos_actividad(id_tipos_actividad_PK)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

--Creación de la tabla asesor da tema
CREATE TABLE asesor_da_tema (
    id_tema_FK INT NOT NULL,
    id_asesor_FK NVARCHAR(64) NOT NULL,
    asesores_asistentes NVARCHAR(MAX),

    CONSTRAINT asesor_da_tema_PK PRIMARY KEY (id_tema_FK, id_asesor_FK),
    CONSTRAINT id_tema_FK FOREIGN KEY (id_tema_FK) REFERENCES tema(id_tema_PK)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT id_asesor_FK FOREIGN KEY (id_asesor_FK) REFERENCES asesor(id_asesor_PK)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

--Creación de la tabla grupo
CREATE TABLE grupo (
    id_grupo_PK INT IDENTITY(1, 1) PRIMARY KEY,
    id_tema_FK INT NOT NULL,
	asesor NVARCHAR(64) NULL,
	nombre NVARCHAR(64) NOT NULL,
    horario NVARCHAR(128) NOT NULL,
    fecha_inicio_grupo DATE NOT NULL,
    fecha_finalizacion_grupo DATE NOT NULL,
    fecha_inicio_inscripcion DATETIME NOT NULL,
    fecha_finalizacion_inscripcion DATETIME NOT NULL,
    cantidad_horas TINYINT NOT NULL,
    modalidad NVARCHAR(16) NOT NULL,
    cupo INT NOT NULL,
    descripcion NVARCHAR(256),
    lugar NVARCHAR(512),
    es_visible BIT,
    nombre_archivo NVARCHAR(255),
	adjunto VARBINARY(MAX),

    FOREIGN KEY (id_tema_FK) REFERENCES tema(id_tema_PK)
        ON DELETE NO ACTION
);

--Creacion de la tabla inscripcion
CREATE TABLE inscripcion (
    id_inscripcion_PK INT IDENTITY(1, 1) PRIMARY KEY,
    estado NVARCHAR(16) NOT NULL,
    observaciones NVARCHAR(512),
    id_grupo_FK INT NOT NULL,
    id_participante_FK NVARCHAR(64) NOT NULL,

    FOREIGN KEY (id_participante_FK) REFERENCES participante(id_participante_PK)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    FOREIGN KEY (id_grupo_FK) REFERENCES grupo(id_grupo_PK)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

--Creación de la tabla calificaciones
CREATE TABLE calificaciones (
    id_grupo_FK INT NOT NULL,
    id_participante_FK NVARCHAR(64) NOT NULL,
    calificacion FLOAT DEFAULT 0.0,

    CONSTRAINT id_participante_calificacion_FK FOREIGN KEY (id_participante_FK) REFERENCES participante(id_participante_PK)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT id_grupo_calificacion_FK FOREIGN KEY (id_grupo_FK) REFERENCES grupo(id_grupo_PK)
        ON DELETE CASCADE
        ON UPDATE CASCADE
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
--Creación de trigger al insertar inscripciones
CREATE OR ALTER TRIGGER TR_InsertarCalificacion
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
CREATE OR ALTER TRIGGER TR_EliminarCalificacion
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

    SET @salt = (SELECT TOP 1 salt FROM usuario WHERE id_usuario_PK=@id)

    IF EXISTS (SELECT TOP 1 id_usuario_PK FROM usuario WHERE id_usuario_PK=@id)
    BEGIN
        SET @userID = (SELECT id_usuario_PK FROM usuario WHERE id_usuario_PK=@id AND hash_contrasena=HASHBYTES('SHA2_512', @contrasena + CAST(@salt AS NVARCHAR(36))))

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
    @descripcion NVARCHAR(64) = '',
    @telefono NVARCHAR(64) = ''
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
    @descripcion NVARCHAR(64) = '',
    @telefono NVARCHAR(64) = ''
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
		horas_matriculadas,
		horas_aprobadas
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
        horas_matriculadas = @horasMatriculadas,
        horas_aprobadas = @horasAprobadas
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
    @idTema INT,
	@asesor NVARCHAR(64) = '',
	@nombre NVARCHAR(64),
	@modalidad NVARCHAR(16),
	@cantidad_horas TINYINT,
	@cupo INT,
    @fecha_inicio_grupo DATE,
    @fecha_finalizacion_grupo DATE,
    @fecha_inicio_inscripcion DATETIME,
    @fecha_finalizacion_inscripcion DATETIME,
	@descripcion NVARCHAR(256) = '',
	@horario NVARCHAR(128) = '',
	@lugar NVARCHAR(512) = '',
	@es_visible BIT = 1,
	@nombre_archivo NVARCHAR(255) = '',
	@adjunto VARBINARY(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO grupo
    (
        id_tema_FK,
		asesor,
        nombre,
        horario,
		fecha_inicio_grupo,
        fecha_finalizacion_grupo,
        fecha_inicio_inscripcion,
        fecha_finalizacion_inscripcion,
        cantidad_horas,
		modalidad,
        cupo,
        descripcion,
        lugar,
        es_visible,
		nombre_archivo,
		adjunto
    )
    VALUES
    (
        @idTema,
		@asesor,
		@nombre,
		@horario,
		@fecha_inicio_grupo,
		@fecha_finalizacion_grupo,
		@fecha_inicio_inscripcion,
		@fecha_finalizacion_inscripcion,
		@cantidad_horas,
		@modalidad,
		@cupo,
		@descripcion,
		@lugar,
		@es_visible,
		@nombre_archivo,
		@adjunto
    );
END

GO
-- Creación de procedimiento para editar un grupo
CREATE OR ALTER PROCEDURE UpdateGrupo
    @idGrupo INT,
    @idTema INT,
	@asesor NVARCHAR(64) = '',
	@nombre NVARCHAR(64),
	@modalidad NVARCHAR(16),
	@cantidad_horas TINYINT,
	@cupo INT,
    @fecha_inicio_grupo DATE,
    @fecha_finalizacion_grupo DATE,
    @fecha_inicio_inscripcion DATETIME,
    @fecha_finalizacion_inscripcion DATETIME,
	@descripcion NVARCHAR(256) = '',
	@horario NVARCHAR(128) = '',
	@lugar NVARCHAR(512) = '',
	@es_visible BIT = 1,
	@nombre_archivo NVARCHAR(255) = '',
	@adjunto VARBINARY(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE grupo
    SET
        id_tema_FK = @idTema,
        asesor = @asesor,
		nombre = @nombre,
        horario = @horario,
        fecha_inicio_grupo = @fecha_inicio_grupo,
        fecha_finalizacion_grupo = @fecha_finalizacion_grupo,
        fecha_inicio_inscripcion = @fecha_inicio_inscripcion,
        fecha_finalizacion_inscripcion = @fecha_finalizacion_inscripcion,
        cantidad_horas = @cantidad_horas,
        modalidad = @modalidad,
        cupo = @cupo,
        descripcion = @descripcion,
        lugar = @lugar,
        es_visible = @es_visible,
        nombre_archivo = @nombre_archivo,
        adjunto = @adjunto
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
		G.id_tema_FK, 
		G.modalidad, 
		G.cupo, 
		G.descripcion, 
		G.es_visible, 
		G.lugar,
		G.asesor,
		G.nombre,		
		G.horario, 
		G.fecha_inicio_grupo, 
		G.fecha_finalizacion_grupo, 
		G.fecha_inicio_inscripcion, 
		G.fecha_finalizacion_inscripcion, 
		G.cantidad_horas, 
		G.nombre_archivo, 
		T.nombre AS tema_asociado, 
		A.nombre + ' ' + A.apellido_1 AS asesor, 
		TA.nombre AS tipo_actividad
	FROM grupo G
		JOIN tema T ON T.id_tema_PK = G.id_tema_FK
		LEFT JOIN asesor_da_tema ADT ON T.id_tema_PK = ADT.id_tema_FK
		LEFT JOIN asesor A ON ADT.id_asesor_FK = A.id_asesor_PK
		JOIN tipos_actividad TA ON T.id_tipos_actividad_FK = TA.id_tipos_actividad_PK;
END

GO
-- Creación de procedimiento para seleccionar un grupo según su id
CREATE OR ALTER PROCEDURE SelectGrupo
    @idGrupo INT
AS
BEGIN
    SELECT 
		G.id_grupo_PK, 
		G.id_tema_FK, 
		G.modalidad, 
		G.cupo, 
		G.descripcion, 
		G.es_visible, 
		G.lugar,
		G.asesor,
		G.nombre,
		G.horario, 
		G.fecha_inicio_grupo, 
		G.fecha_finalizacion_grupo, 
		G.fecha_inicio_inscripcion, 
		G.fecha_finalizacion_inscripcion, 
		G.cantidad_horas, 
		G.nombre_archivo, 
		G.adjunto,
		T.nombre AS tema_asociado, 
		A.nombre + ' ' + A.apellido_1 AS asesor, 
		TA.nombre AS tipo_actividad
	FROM grupo G
		JOIN tema T ON T.id_tema_PK = G.id_tema_FK
		LEFT JOIN asesor_da_tema ADT ON T.id_tema_PK = ADT.id_tema_FK
		LEFT JOIN asesor A ON ADT.id_asesor_FK = A.id_asesor_PK
		JOIN tipos_actividad TA ON T.id_tipos_actividad_FK = TA.id_tipos_actividad_PK
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

--Crear usuarios
	-- admin
EXEC InsertUsuario 
	@id = N'admin.admin@ucr.ac.cr',
	@rol = 1,
	@contrasena = N'#Q+3n?OWk3i0:qG'

--Crear asesores
EXEC InsertUsuario
	@id = N'MARIA.ENRIQUEZ@ucr.ac.cr',
	@rol = 2,
	@contrasena = N'4fG7hJ2kL9';

EXEC InsertAsesor 
    @idUsuario = N'MARIA.ENRIQUEZ@ucr.ac.cr', 
    @idAsesor = N'MARIA.ENRIQUEZ@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', 
    @numeroIdentificacion = N'', 
    @correo = N'MARIA.ENRIQUEZ@ucr.ac.cr', 
    @nombre = N'María Ileana', 
    @apellido1 = N'Enriquez', 
    @apellido2 = N'Barrantes';

EXEC InsertUsuario
	@id = N'jose.elizondosalas@ucr.ac.cr',
	@rol = 2,
	@contrasena = N'9dH3kL8pQz';

EXEC InsertAsesor 
    @idUsuario = N'jose.elizondosalas@ucr.ac.cr', 
    @idAsesor = N'jose.elizondosalas@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', 
    @numeroIdentificacion = N'', 
    @correo = N'jose.elizondosalas@ucr.ac.cr', 
    @nombre = N'Jose Antonio', 
    @apellido1 = N'Elizondo', 
    @apellido2 = N'Salas';

EXEC InsertUsuario
	@id = N'ORLANDO.GOMEZ@ucr.ac.cr',
	@rol = 2,
	@contrasena = N'2fJ8rK5tXq';

EXEC InsertAsesor 
    @idUsuario = N'ORLANDO.GOMEZ@ucr.ac.cr', 
    @idAsesor = N'ORLANDO.GOMEZ@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', 
    @numeroIdentificacion = N'', 
    @correo = N'ORLANDO.GOMEZ@ucr.ac.cr', 
    @nombre = N'Orlando Daniel', 
    @apellido1 = N'Gómez', 
    @apellido2 = N'Arias';

EXEC InsertUsuario
	@id = N'AARON.MENAARAYA@ucr.ac.cr',
	@rol = 2,
	@contrasena = N'6hP2jN9vWz';

EXEC InsertAsesor 
    @idUsuario = N'AARON.MENAARAYA@ucr.ac.cr', 
    @idAsesor = N'AARON.MENAARAYA@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', 
    @numeroIdentificacion = N'', 
    @correo = N'AARON.MENAARAYA@ucr.ac.cr', 
    @nombre = N'Aarón Elí', 
    @apellido1 = N'Mena', 
    @apellido2 = N'Araya';

EXEC InsertUsuario
	@id = N'brenda.alfaro@ucr.ac.cr',
	@rol = 2,
	@contrasena = N'3tX5kL8dVj';

EXEC InsertAsesor 
    @idUsuario = N'brenda.alfaro@ucr.ac.cr', 
    @idAsesor = N'brenda.alfaro@ucr.ac.cr', 
    @tipoIdentificacion = N'Cédula', 
    @numeroIdentificacion = N'', 
    @correo = N'brenda.alfaro@ucr.ac.cr', 
    @nombre = N'Brenda Lidis', 
    @apellido1 = N'Alfaro', 
    @apellido2 = N'González';


--Crear participantes
EXEC InsertUsuario
	@id = N'jhondoo@ucr.ac.cr',
	@rol = 0,
	@contrasena = N'1234';

EXEC InsertParticipante
    @idUsuario = N'jhondoo@ucr.ac.cr',
    @idParticipante = N'jhondoo@ucr.ac.cr',
    @tipoIdentificacion = N'Cédula',
    @numeroIdentificacion = N'123456789',
    @correo = N'jhondoo@ucr.ac.cr',
    @nombre = N'Jhon',
    @apellido1 = N'Doo',
    @apellido2 = NULL,
    @area = N'Área de Artes y Letras',
    @departamento = N'Facultad de Artes',
    @unidadAcademica = N'Escuela de Artes Dramáticas',
    @telefono = N'800800800',
    @tipoParticipante = N'Interino',
    @condicion = N'Activo',
    @horasMatriculadas = 0,
    @horasAprobadas = 0

EXEC InsertUsuario
	@id = N'armandotorres_rojas@ucr.ac.cr',
	@rol = 0,
	@contrasena = N'1234';

EXEC InsertParticipante
    @idUsuario = N'armandotorres_rojas@ucr.ac.cr',
    @idParticipante = N'armandotorres_rojas@ucr.ac.cr',
    @tipoIdentificacion = N'Cédula',
    @numeroIdentificacion = N'987654321',
    @correo = N'armandotorres_rojas@ucr.ac.cr',
    @nombre = N'Armando',
    @apellido1 = N'Torres',
    @apellido2 = N'Rojas',
    @area = N'Área de Artes y Letras',
    @departamento = N'Facultad de Letras',
    @unidadAcademica = N'Escuela de Filología, Lingüística y Literatura',
    @telefono = N'900900900',
    @tipoParticipante = N'Docente',
    @condicion = N'Interino',
    @horasMatriculadas = 0,
    @horasAprobadas = 0


--Crear tipos de actividades
INSERT INTO tipos_actividad
(nombre, descripcion)
VALUES
(N'Curso', N'Los cursos cuentan con una duración de cinco a seis horas semanales, en la que se realizarán tareas, exámenes y quices para evaluar los conceptos aprendidos por los estudiantes'),
(N'Taller', N'Un taller es una metodología de trabajo que se caracteriza por la investigación, el aprendizaje por descubrimiento y el trabajo en equipo'),
(N'Taller corto', N'Un taller de menos duración de dos a tres horas semanales donde se trabajará con un tema en específico. No hay exámenes, el objetivo es que aprendan el concepto tratado'),
(N'Charla', N'Se realizarán charlas con profesionales invitados')

--Crear categorias
INSERT INTO categoria
(nombre, descripcion)
VALUES
(N'Primeros pasos en la plataforma', N'Aprenderá de manera básica las funciones del sitio de Mediación Virtual'),
(N'Evaluaciones y calificaciones', N'Aprenderá a realizar el proceso de evaluacion y calificacion de los trabajos dentro de la plataforma'),
(N'Material audio y visual', N'Utilice las herramientas audiovisuales que cuenta el sistema, suba y comparta material audiovisual a los estudiantes')

--Crear temas
INSERT INTO tema
(nombre, id_categoria_FK, id_tipos_actividad_FK)
VALUES(N'¿Cómo utilizar la plataforma?', 1, 1),
(N'Evaluaciones en la plataforma', 2, 2),
(N'Subir material audiovisual a la plataforma', 3, 3),
(N'Tema Adicional', 1, 2)

--Declarar archivo que se va a adjuntar (RUTA DEL ARCHIVO, Ejm: C:\Users\UserName\Documents\Folder\FileName.pdf) 
/*DECLARE @adjunto varbinary(max)
SELECT @adjunto = CAST(bulkcolumn AS varbinary(max))
FROM OPENROWSET(BULK '\\wsl.localhost\Ubuntu-20.04\home\yasty\src\METICS-CORE\FileName.pdf', SINGLE_BLOB) AS x*/

--Crear grupos
INSERT INTO dbo.grupo(
	id_tema_FK, modalidad, cupo, cantidad_horas,
	descripcion, es_visible, lugar,
	nombre, horario,
	fecha_inicio_grupo, fecha_finalizacion_grupo,
	fecha_inicio_inscripcion, fecha_finalizacion_inscripcion,
	nombre_archivo)
	VALUES
	(3, N'Presencial', 15, 3,
	N'Taller de videos interactivos', 1, N'Universidad de Costa Rica, Rodrigo Facio',
	N'Capacitación-Plataforma', N'V-S de 4pm a 7pm',
	'2023-06-01 00:00:00', '2026-12-02 00:00:00',
	'2023-01-01 00:00:00', '2026-01-01 00:00:00', N'ArchivoPrueba.pdf'),
	(2, N'Virtual', 10, 3,
	N'Aprenda a realizar evaluaciones en la plataforma', 1, N'Mediación Virtual',
	N'Capacitación-Profesores', N'L-V de 4pm a 7pm',
	'2023-06-01 00:00:00', '2026-12-02 00:00:00',
	'2023-01-01 00:00:00', '2026-01-01 00:00:00', N'ArchivoPrueba.pdf'),
	(3, N'Virtual', 10, 3,
	N'Aprenda a realizar videos llamativos e interesantes del temario del curso', 1, N'Mediación Virtual',
	N'Capacitaciones-Audiovisuales', N'L-M de 4pm a 7pm',
	'2023-06-01 00:00:00', '2026-12-02 00:00:00',
	'2023-01-01 00:00:00', '2026-01-01 00:00:00', N'ArchivoPrueba.pdf');
