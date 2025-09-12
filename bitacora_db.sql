ALTER TABLE dbo.bitacora_accesos DROP CONSTRAINT IF EXISTS CK_bitacora_estado_acceso;
DROP TABLE IF EXISTS dbo.bitacora_accesos;

-- Antes de crear la nueva tabla, verificamos si existe
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'bitacora_accesos')
BEGIN
	    -- Creación de la tabla bitacora_accesos
    CREATE TABLE dbo.bitacora_accesos (
        id_acceso_PK BIGINT IDENTITY(1,1) NOT NULL,
        id_usuario NVARCHAR(64) NOT NULL,
        fecha_hora_acceso DATETIME2(3) NOT NULL,
        estado_acceso NVARCHAR(20) NOT NULL,
        
        -- Clave primaria
        CONSTRAINT PK_bitacora_accesos PRIMARY KEY CLUSTERED (id_acceso_PK),
        
        -- Constraints para validación
        CONSTRAINT CK_bitacora_estado_acceso 
            CHECK (estado_acceso IN ('EXITO', 'FRACASO'))

        );

    -- Creacion de incides.
    CREATE NONCLUSTERED INDEX IX_bitacora_usuario_fecha
    ON dbo.bitacora_accesos (id_usuario, fecha_hora_acceso DESC)
    INCLUDE (estado_acceso);


    CREATE NONCLUSTERED INDEX IX_bitacora_fecha
    ON dbo.bitacora_accesos (fecha_hora_acceso DESC)
    INCLUDE (id_usuario, estado_acceso);

    CREATE NONCLUSTERED INDEX IX_bitacora_estado
    ON dbo.bitacora_accesos (estado_acceso, fecha_hora_acceso DESC)
    WHERE estado_acceso <> 'EXITO'; -- Índice filtrado para el estado 'FRACASO'
END

-- =====================================================
-- Procedimientos almacenados para bitacora_accesos
-- =====================================================

GO
-- Procedimiento para insertar un registro de acceso
CREATE OR ALTER PROCEDURE InsertBitacoraAcceso
    @id_usuario NVARCHAR(64),
    @estado_acceso NVARCHAR(20) = 'EXITO'
AS
BEGIN
    SET NOCOUNT ON
    
    -- Insertar el registro de acceso
    INSERT INTO dbo.bitacora_accesos (id_usuario, fecha_hora_acceso, estado_acceso)
    VALUES (@id_usuario, GETDATE(), @estado_acceso);
    
    -- Retornar el ID del registro insertado
    SELECT SCOPE_IDENTITY() AS id_acceso_insertado, @id_usuario AS usuario, GETDATE() AS fecha_registro;
END

GO
-- Procedimiento para obtener accesos de un usuario N dias atras
CREATE OR ALTER PROCEDURE SelectBitacoraAccesoUsuario
    @id_usuario NVARCHAR(64),
    @dias_atras INT = 30
AS
BEGIN
    SET NOCOUNT ON
    
    DECLARE @fecha_desde DATETIME2 = DATEADD(DAY, -@dias_atras, GETDATE());
    
    SELECT 
        ba.id_acceso_PK,
        ba.id_usuario,
        ba.fecha_hora_acceso,
        ba.estado_acceso
    FROM dbo.bitacora_accesos ba
    WHERE 
        ba.id_usuario = @id_usuario
        AND ba.fecha_hora_acceso >= @fecha_desde
    ORDER BY ba.fecha_hora_acceso DESC;
END

GO
-- Procedimiento para obtener todos los accesos en un rango de fechas
CREATE OR ALTER PROCEDURE SelectBitacoraAccesosPorFecha
    @fecha_desde DATETIME2 = NULL,
    @fecha_hasta DATETIME2 = NULL,
    @estado_filtro NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON
    
    -- Si no se proporciona fecha_desde, usar los últimos 7 días
    IF @fecha_desde IS NULL
        SET @fecha_desde = DATEADD(DAY, -7, GETDATE());
    
    -- Si no se proporciona fecha_hasta, usar la fecha actual
    IF @fecha_hasta IS NULL
        SET @fecha_hasta = GETDATE();
    
    SELECT 
        ba.id_acceso_PK,
        ba.id_usuario,
        ba.fecha_hora_acceso,
        ba.estado_acceso
    FROM dbo.bitacora_accesos ba
    WHERE 
        ba.fecha_hora_acceso BETWEEN @fecha_desde AND @fecha_hasta
        AND (@estado_filtro IS NULL OR ba.estado_acceso = @estado_filtro)
    ORDER BY ba.fecha_hora_acceso DESC;
END

GO
-- Procedimiento para obtener el último acceso de un usuario
CREATE OR ALTER PROCEDURE SelectUltimoAccesoUsuario
    @id_usuario NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON
    
    SELECT TOP 1
        ba.id_acceso_PK,
        ba.id_usuario,
        ba.fecha_hora_acceso,
        ba.estado_acceso
    FROM dbo.bitacora_accesos ba
    WHERE ba.id_usuario = @id_usuario
    ORDER BY ba.fecha_hora_acceso DESC;
END
