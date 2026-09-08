-- Reversion de la normalizacion. Usa el ultimo respaldo por participante.
-- Solo restaura si la carrera coincide exactamente con el valor escrito;
-- incluye espacios finales y distingue NULL de una cadena vacia.
SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.participante_carrera_respaldo', 'U') IS NULL
BEGIN
    RAISERROR(N'ABORTADO: no existe participante_carrera_respaldo.', 16, 1);
    SET NOEXEC ON;
END
GO
IF COL_LENGTH('dbo.participante_carrera_respaldo', 'id_respaldo') IS NULL
   OR COL_LENGTH('dbo.participante_carrera_respaldo', 'carrera_normalizada') IS NULL
BEGIN
    RAISERROR(N'ABORTADO: actualizar primero el esquema del respaldo con 01-aplicar_normalizacion_carreras.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

-- SQL dinamico para respetar NOEXEC incluso si aun no existe el esquema.
EXEC sp_executesql N'
WITH ultimo AS (
    SELECT r.id_participante_FK, r.carrera_original, r.carrera_normalizada,
           ROW_NUMBER() OVER (PARTITION BY r.id_participante_FK
                              ORDER BY r.fecha_respaldo DESC, r.id_respaldo DESC) AS orden
    FROM dbo.participante_carrera_respaldo AS r
)
UPDATE p
SET carrera = u.carrera_original
FROM dbo.participante AS p
INNER JOIN ultimo AS u ON u.id_participante_FK = p.id_participante_PK AND u.orden = 1
WHERE CONVERT(VARBINARY(MAX), p.carrera) = CONVERT(VARBINARY(MAX), u.carrera_normalizada)
  AND EXISTS (SELECT CONVERT(VARBINARY(MAX), p.carrera)
              EXCEPT SELECT CONVERT(VARBINARY(MAX), u.carrera_original));
PRINT N''Filas restauradas: '' + CAST(@@ROWCOUNT AS NVARCHAR(16));';
GO

-- No restaurados: ediciones posteriores o respaldos antiguos sin valor escrito.
-- No se adivina el valor escrito usando una version nueva de la funcion.
EXEC sp_executesql N'
WITH ultimo AS (
    SELECT r.id_participante_FK, r.carrera_original, r.carrera_normalizada,
           ROW_NUMBER() OVER (PARTITION BY r.id_participante_FK
                              ORDER BY r.fecha_respaldo DESC, r.id_respaldo DESC) AS orden
    FROM dbo.participante_carrera_respaldo AS r
)
SELECT p.id_participante_PK AS id_participante,
       u.carrera_original AS respaldo, p.carrera AS actual,
       CASE WHEN u.carrera_normalizada IS NULL THEN N''Sin valor escrito conocido''
            ELSE N''Edicion posterior'' END AS motivo
FROM dbo.participante AS p
INNER JOIN ultimo AS u ON u.id_participante_FK = p.id_participante_PK AND u.orden = 1
WHERE EXISTS (SELECT CONVERT(VARBINARY(MAX), p.carrera)
              EXCEPT SELECT CONVERT(VARBINARY(MAX), u.carrera_original))
ORDER BY p.id_participante_PK;';
GO
SET NOEXEC OFF;
GO
