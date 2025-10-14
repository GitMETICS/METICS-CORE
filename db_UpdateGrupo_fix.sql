-- Script para arreglar el procedimiento almacenado UpdateGrupo
-- 13/10/2025

USE Local

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
    @enlace NVARCHAR(1000) = '',
    @clave_inscripcion NVARCHAR(1000) = ''
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RowsAffected INT = 0;
    
	IF (@adjunto IS NOT NULL AND DATALENGTH(@adjunto) > 0)
		BEGIN
			UPDATE grupo 
			SET nombre_archivo = @nombre_archivo, adjunto = @adjunto 
			WHERE id_grupo_PK = @idGrupo;
			SET @RowsAffected = @RowsAffected + @@ROWCOUNT;
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
    
    SET @RowsAffected = @RowsAffected + @@ROWCOUNT;
    
    -- Retornar el número de filas afectadas
    SELECT @RowsAffected AS FilasAfectadas;
END
