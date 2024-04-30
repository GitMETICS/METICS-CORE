USE METICS
GO
/****** Object:  Table [dbo].[AspNetRoleClaims]    Script Date: 6/4/2018 10:18:03 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoleClaims]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[AspNetRoleClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
	[RoleId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[AspNetRoles]    Script Date: 6/4/2018 10:18:03 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoles]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[AspNetRoles](
	[Id] [nvarchar](450) NOT NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
	[Name] [nvarchar](256) NULL,
	[NormalizedName] [nvarchar](256) NULL,
 CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[AspNetUserClaims]    Script Date: 6/4/2018 10:18:03 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserClaims]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[AspNetUserClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
	[UserId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[AspNetUserLogins]    Script Date: 6/4/2018 10:18:03 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserLogins]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[AspNetUserLogins](
	[LoginProvider] [nvarchar](450) NOT NULL,
	[ProviderKey] [nvarchar](450) NOT NULL,
	[ProviderDisplayName] [nvarchar](max) NULL,
	[UserId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED 
(
	[LoginProvider] ASC,
	[ProviderKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[AspNetUserRoles]    Script Date: 6/4/2018 10:18:03 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[AspNetUserRoles](
	[UserId] [nvarchar](450) NOT NULL,
	[RoleId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[AspNetUsers]    Script Date: 6/4/2018 10:18:03 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[AspNetUsers](
	[Id] [nvarchar](450) NOT NULL,
	[AccessFailedCount] [int] NOT NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
	[Email] [nvarchar](256) NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[LockoutEnabled] [bit] NOT NULL,
	[LockoutEnd] [datetimeoffset](7) NULL,
	[NormalizedEmail] [nvarchar](256) NULL,
	[NormalizedUserName] [nvarchar](256) NULL,
	[PasswordHash] [nvarchar](max) NULL,
	[PhoneNumber] [nvarchar](max) NULL,
	[PhoneNumberConfirmed] [bit] NOT NULL,
	[SecurityStamp] [nvarchar](max) NULL,
	[TwoFactorEnabled] [bit] NOT NULL,
	[UserName] [nvarchar](256) NULL,
 CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[AspNetUserTokens]    Script Date: 6/4/2018 10:18:03 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserTokens]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[AspNetUserTokens](
	[UserId] [nvarchar](450) NOT NULL,
	[LoginProvider] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
	[Value] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[LoginProvider] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetRoleClaims_AspNetRoles_RoleId]') AND parent_object_id = OBJECT_ID(N'[dbo].[AspNetRoleClaims]'))
ALTER TABLE [dbo].[AspNetRoleClaims]  WITH CHECK ADD  CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[AspNetRoles] ([Id])
ON DELETE CASCADE
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetRoleClaims_AspNetRoles_RoleId]') AND parent_object_id = OBJECT_ID(N'[dbo].[AspNetRoleClaims]'))
ALTER TABLE [dbo].[AspNetRoleClaims] CHECK CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetUserClaims_AspNetUsers_UserId]') AND parent_object_id = OBJECT_ID(N'[dbo].[AspNetUserClaims]'))
ALTER TABLE [dbo].[AspNetUserClaims]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetUserClaims_AspNetUsers_UserId]') AND parent_object_id = OBJECT_ID(N'[dbo].[AspNetUserClaims]'))
ALTER TABLE [dbo].[AspNetUserClaims] CHECK CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetUserLogins_AspNetUsers_UserId]') AND parent_object_id = OBJECT_ID(N'[dbo].[AspNetUserLogins]'))
ALTER TABLE [dbo].[AspNetUserLogins]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetUserLogins_AspNetUsers_UserId]') AND parent_object_id = OBJECT_ID(N'[dbo].[AspNetUserLogins]'))
ALTER TABLE [dbo].[AspNetUserLogins] CHECK CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetUserRoles_AspNetRoles_RoleId]') AND parent_object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]'))
ALTER TABLE [dbo].[AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[AspNetRoles] ([Id])
ON DELETE CASCADE
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetUserRoles_AspNetRoles_RoleId]') AND parent_object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]'))
ALTER TABLE [dbo].[AspNetUserRoles] CHECK CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetUserRoles_AspNetUsers_UserId]') AND parent_object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]'))
ALTER TABLE [dbo].[AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetUserRoles_AspNetUsers_UserId]') AND parent_object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]'))
ALTER TABLE [dbo].[AspNetUserRoles] CHECK CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetUserTokens_AspNetUsers_UserId]') AND parent_object_id = OBJECT_ID(N'[dbo].[AspNetUserTokens]'))
ALTER TABLE [dbo].[AspNetUserTokens]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AspNetUserTokens_AspNetUsers_UserId]') AND parent_object_id = OBJECT_ID(N'[dbo].[AspNetUserTokens]'))
ALTER TABLE [dbo].[AspNetUserTokens] CHECK CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId]
GO

/******************************************************************************************/

CREATE TABLE asesor(
	id_usuario_FK NVARCHAR(450) NOT NULL FOREIGN KEY REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE ON UPDATE CASCADE,
	id_asesor_PK VARCHAR(64) PRIMARY KEY NOT NULL,
	nombre NVARCHAR(64) NOT NULL,
	apellido_1 NVARCHAR(64) NOT NULL,
	apellido_2 NVARCHAR(64),
	telefonos NVARCHAR(15),
	descripcion NVARCHAR(256)
);

--Creación de la tabla participante
CREATE TABLE participante(
	id_usuario_FK NVARCHAR(450) NOT NULL FOREIGN KEY REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE ON UPDATE CASCADE,
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
ON dbo.AspNetUsers
AFTER UPDATE
AS
BEGIN
	DECLARE @id NVARCHAR(450) = (SELECT Id FROM inserted)

	UPDATE participante SET id_participante_PK =  @id WHERE id_usuario_FK = @id
END
GO

--Creación de trigger para cambiar rol de usuario cuando se agrega un asesor
CREATE TRIGGER TR_ActualizarRolAsesor
ON asesor
AFTER INSERT
AS
BEGIN
	DECLARE @id NVARCHAR(450) = (SELECT id_usuario_FK FROM inserted)

	UPDATE dbo.AspNetUserRoles SET RoleId = 2 WHERE UserId = @id
END
GO

--Creación de trigger cuando se cambia la identificación de un usuario en tabla de asesores
CREATE TRIGGER TR_ActualizarIdAsesor
ON dbo.AspNetUsers
AFTER UPDATE
AS
BEGIN
	DECLARE @id VARCHAR(64) = (SELECT Id FROM inserted)

	UPDATE asesor SET id_asesor_PK = @id WHERE id_usuario_FK = @id
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
GO

/*
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



--Creación de procedimiento para obtener los datos de un usuario
CREATE PROCEDURE ObtenerUsuario
    @id VARCHAR(64)
AS
BEGIN
	SELECT id_usuario_PK, rol_FK FROM usuario WHERE id_usuario_PK=@id
END

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

*/