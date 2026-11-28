-- Ejecutar en la base de METICS antes de desplegar la nueva aplicación.
-- Reejecutable: no otorga medallas ni modifica asignaciones existentes.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lockResult int;
    EXEC @lockResult = sys.sp_getapplock @Resource = N'METICS.CatalogoMedallas',
        @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000;
    IF @lockResult < 0 THROW 51000, 'No se pudo bloquear el catálogo de medallas.', 1;
    IF OBJECT_ID(N'dbo.medallas', N'U') IS NULL
        THROW 51001, 'Debe existir la tabla de asignaciones dbo.medallas.', 1;
    IF EXISTS (SELECT 1 FROM dbo.medallas WHERE DATALENGTH(nombre_medalla) > 900)
        THROW 51002, 'Hay nombres históricos de más de 450 caracteres; revisarlos antes de migrar.', 1;

    IF OBJECT_ID(N'dbo.catalogo_medallas', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.catalogo_medallas (
            nombre_medalla nvarchar(450) NOT NULL CONSTRAINT PK_catalogo_medallas PRIMARY KEY,
            activa bit NOT NULL CONSTRAINT DF_catalogo_medallas_activa DEFAULT (1)
        );
    END;

    -- Las filas históricas continúan siendo asignaciones, nunca se infiere su propietario.
    INSERT INTO dbo.catalogo_medallas (nombre_medalla, activa)
    SELECT DISTINCT m.nombre_medalla, 1
    FROM dbo.medallas AS m
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.catalogo_medallas AS c WHERE c.nombre_medalla = m.nombre_medalla
    );
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
