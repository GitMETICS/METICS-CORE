-- ============================================================
-- Reversion: normalizacion del formato de carrera
--
-- Restaura participante.carrera desde participante_carrera_respaldo, es decir
-- al valor que tenia antes de la PRIMERA corrida de
-- 01-aplicar_normalizacion_carreras.sql.
--
-- La normalizacion pierde informacion, asi que esa tabla es la unica via de
-- regreso. Si el respaldo se borro, no hay reversion posible.
-- ============================================================

SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.participante_carrera_respaldo', 'U') IS NULL
BEGIN
    RAISERROR(N'ABORTADO: no existe participante_carrera_respaldo. No hay nada desde donde restaurar.', 16, 1);
    SET NOEXEC ON;
END
GO

-- Via sp_executesql por la misma razon que en 01: bajo SET NOEXEC ON el lote
-- igual se compila, y referirse a una tabla que no existe haria fallar el
-- script con un error de enlace en vez del mensaje del guard.
EXEC sp_executesql N'
UPDATE p
SET carrera = r.carrera_original
FROM dbo.participante AS p
INNER JOIN dbo.participante_carrera_respaldo AS r
        ON r.id_participante_FK = p.id_participante_PK
WHERE ISNULL(p.carrera, N'''')          COLLATE Latin1_General_BIN2
   <> ISNULL(r.carrera_original, N'''') COLLATE Latin1_General_BIN2;
PRINT N''Filas restauradas: '' + CAST(@@ROWCOUNT AS NVARCHAR(16));';
GO

-- Limpieza opcional. Descomentar solo si se decide abandonar la migracion por
-- completo; mientras el respaldo exista, la reversion se puede repetir.
-- DROP FUNCTION IF EXISTS dbo.fn_NormalizarCarrera;
-- DROP TABLE IF EXISTS dbo.participante_carrera_respaldo;

SET NOEXEC OFF;
GO
