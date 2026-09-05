-- ============================================================
-- Reversion: normalizacion del formato de carrera
--
-- Restaura participante.carrera desde participante_carrera_respaldo. De cada
-- participante toma el respaldo MAS RECIENTE: el valor que tenia justo antes
-- de la ultima corrida de 01-aplicar_normalizacion_carreras.sql que lo toco.
--
-- No pisa ediciones posteriores. Si la carrera actual ya no es la
-- normalizacion del valor respaldado, es que alguien la cambio despues de
-- migrar; esa fila se deja como esta y se lista al final. Restaurarla seria
-- descartar esa edicion en silencio.
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

IF OBJECT_ID('dbo.participante_carrera_respaldo', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.participante_carrera_respaldo', 'id_respaldo') IS NULL
BEGIN
    RAISERROR(N'ABORTADO: participante_carrera_respaldo tiene el esquema anterior, de una fila por participante. Correr 01-aplicar_normalizacion_carreras.sql, que lo migra a historico sin perder lo respaldado, y volver a intentar.', 16, 1);
    SET NOEXEC ON;
END
GO

-- fn_NormalizarCarrera es lo que distingue "esto lo escribio la migracion" de
-- "esto lo edito alguien despues". Sin ella la reversion no puede saber cual
-- de las dos cosas estaria pisando.
IF OBJECT_ID('dbo.fn_NormalizarCarrera', 'FN') IS NULL
BEGIN
    RAISERROR(N'ABORTADO: no existe dbo.fn_NormalizarCarrera. La reversion la necesita para no pisar ediciones hechas despues de migrar. Volver a crearla con el paso 1 de 01-aplicar_normalizacion_carreras.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

-- Via sp_executesql por la misma razon que en 01: bajo SET NOEXEC ON el lote
-- igual se compila, y referirse a una tabla que no existe haria fallar el
-- script con un error de enlace en vez del mensaje del guard.
EXEC sp_executesql N'
WITH ultimo AS (
    SELECT r.id_participante_FK,
           r.carrera_original,
           ROW_NUMBER() OVER (PARTITION BY r.id_participante_FK
                              ORDER BY r.fecha_respaldo DESC, r.id_respaldo DESC) AS orden
    FROM dbo.participante_carrera_respaldo AS r
)
UPDATE p
SET carrera = u.carrera_original
FROM dbo.participante AS p
INNER JOIN ultimo AS u
        ON u.id_participante_FK = p.id_participante_PK
       AND u.orden = 1
WHERE ISNULL(p.carrera, N'''')          COLLATE Latin1_General_BIN2
   <> ISNULL(u.carrera_original, N'''') COLLATE Latin1_General_BIN2
  AND ISNULL(p.carrera, N'''')          COLLATE Latin1_General_BIN2
    = ISNULL(dbo.fn_NormalizarCarrera(u.carrera_original), N'''') COLLATE Latin1_General_BIN2;
PRINT N''Filas restauradas: '' + CAST(@@ROWCOUNT AS NVARCHAR(16));';
GO

-- Filas que la reversion dejo intactas a proposito: su carrera actual no es la
-- normalizacion de lo respaldado, asi que se edito despues de migrar. Si la
-- lista sale vacia, la reversion fue completa.
EXEC sp_executesql N'
WITH ultimo AS (
    SELECT r.id_participante_FK,
           r.carrera_original,
           ROW_NUMBER() OVER (PARTITION BY r.id_participante_FK
                              ORDER BY r.fecha_respaldo DESC, r.id_respaldo DESC) AS orden
    FROM dbo.participante_carrera_respaldo AS r
)
SELECT p.id_participante_PK AS id_participante,
       u.carrera_original   AS respaldo,
       p.carrera            AS actual
FROM dbo.participante AS p
INNER JOIN ultimo AS u
        ON u.id_participante_FK = p.id_participante_PK
       AND u.orden = 1
WHERE ISNULL(p.carrera, N'''')          COLLATE Latin1_General_BIN2
   <> ISNULL(u.carrera_original, N'''') COLLATE Latin1_General_BIN2
ORDER BY p.id_participante_PK;';
GO

-- Limpieza opcional. Descomentar solo si se decide abandonar la migracion por
-- completo; mientras el respaldo exista, la reversion se puede repetir.
-- DROP FUNCTION IF EXISTS dbo.fn_NormalizarCarrera;
-- DROP TABLE IF EXISTS dbo.participante_carrera_respaldo;

SET NOEXEC OFF;
GO
