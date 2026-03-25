-- =============================================
-- Script para agregar el campo correo alternativo a usuario
-- =============================================
-- Este script agrega la columna correo_alternativo en la tabla
-- usuario dentro de la base de datos existente.
--
-- PROPÓSITO:
-- - Incorporar el nuevo atributo "correo alternativo"
--   en la entidad base del sistema
-- - Hacer que el cambio aplique tanto para participantes
--   como para asesores, ya que ambos dependen de usuario
--
-- LÓGICA DE IMPLEMENTACIÓN:
-- - Se valida si la columna ya existe antes de agregarla
-- - La columna se crea como NULL para no afectar registros
--   existentes en la base de datos
-- - La obligatoriedad del dato se controlará desde la aplicación
--   mientras se completa la transición
--
-- IMPORTANTE:
-- - Este script solo modifica la estructura de la tabla usuario
-- - No inserta datos, no actualiza registros existentes y no
--   convierte el campo en NOT NULL todavía
-- =============================================

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.usuario', 'correo_alternativo') IS NULL
    BEGIN
        ALTER TABLE dbo.usuario
        ADD correo_alternativo NVARCHAR(64) NULL;
    END;

    COMMIT TRANSACTION;
    PRINT 'Script ejecutado exitosamente: columna correo_alternativo agregada en dbo.usuario.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO