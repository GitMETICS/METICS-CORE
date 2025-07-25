-- Antes de crear la nueva tabla, verificamos si existe
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'bitacora_accesos')
BEGIN

	    -- Creación de la tabla bitacora_accesos
    CREATE TABLE dbo.bitacora_accesos (
        id_acceso_PK BIGINT IDENTITY(1,1) NOT NULL,
        id_usuario_FK NVARCHAR(64) NOT NULL,
        fecha_hora_acceso DATETIME2(3) NOT NULL,
        estado_acceso NVARCHAR(20) NOT NULL,
        
        -- Clave primaria
        CONSTRAINT PK_bitacora_accesos PRIMARY KEY CLUSTERED (id_acceso_PK),
        
        -- Clave foránea hacia la tabla usuario
        CONSTRAINT FK_bitacora_accesos_usuario 
            FOREIGN KEY (id_usuario_FK) 
            REFERENCES dbo.usuario(id_usuario_PK) 
            ON DELETE CASCADE,
            
        -- Constraints para validación
        CONSTRAINT CK_bitacora_estado_acceso 
            CHECK (estado_acceso IN ('SUCCESS', 'FAILED', 'LOCKED', 'BLOCKED', 'EXPIRED'))

        );
END

