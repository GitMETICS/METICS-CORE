-- =============================================
-- Script para corregir los estados de las inscripciones
-- =============================================
-- Este script actualiza el estado de las inscripciones afectadas por el 
-- script Fix_HorasTotales.sql que no actualizó correctamente los estados.
--
-- LÓGICA DE ESTADOS (basada en InscripcionHandler.cs):
-- - "Aprobado": horas_aprobadas >= horas_matriculadas AND horas_aprobadas > 0
-- - "Incompleto": horas_aprobadas < horas_matriculadas AND horas_aprobadas > 0
-- - "Inscrito": cualquier otro caso (horas_aprobadas = 0)
-- =============================================

SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @now VARCHAR(19) = CONVERT(VARCHAR(19), SYSDATETIME(), 120);

-- Lista de inscripciones afectadas
DECLARE @inscripciones_afectadas TABLE (
    id_participante_FK NVARCHAR(255),
    nombre_grupo       NVARCHAR(255),
    numero_grupo       INT
);

INSERT INTO @inscripciones_afectadas (id_participante_FK, nombre_grupo, numero_grupo) VALUES
(N'DORIAN.MENDEZ@ucr.ac.cr',         N'AUTOGESTIONADO Diseño de Ambientes Virtuales Accesibles para el Aprendizaje', 12),
(N'fabian.fallasmoya@ucr.ac.cr',     N'AUTOGESTIONADO TM-8000 Principios y Procedimientos para el Uso de Entornos Virtuales en la UCR', 22),
(N'michael.fallasvillegas@ucr.ac.cr',N'AUTOGESTIONADO TM-8000 Principios y Procedimientos para el Uso de Entornos Virtuales en la UCR', 22),
(N'juan.gomezsolano@ucr.ac.cr',      N'AUTOGESTIONADO TM-8000 Principios y Procedimientos para el Uso de Entornos Virtuales en la UCR', 22);

-- Mostrar estado actual (antes del UPDATE)
SELECT 
    i.id_participante_FK,
    i.nombre_grupo,
    i.numero_grupo,
    i.horas_matriculadas,
    i.horas_aprobadas,
    i.estado AS estado_actual,
    CASE 
        WHEN i.horas_aprobadas >= i.horas_matriculadas AND i.horas_aprobadas > 0 THEN 'Aprobado'
        WHEN i.horas_aprobadas < i.horas_matriculadas AND i.horas_aprobadas > 0 THEN 'Incompleto'
        ELSE 'Inscrito'
    END AS estado_correcto
FROM dbo.inscripcion AS i
WHERE EXISTS (
    SELECT 1 
    FROM @inscripciones_afectadas AS ia
    WHERE ia.id_participante_FK = i.id_participante_FK
    AND ia.numero_grupo = i.numero_grupo
    AND i.nombre_grupo LIKE ia.nombre_grupo + N'%'
)
ORDER BY i.id_participante_FK;

-- Actualizar el estado según la lógica de negocio
UPDATE i
SET i.estado = CASE 
                   WHEN i.horas_aprobadas >= i.horas_matriculadas AND i.horas_aprobadas > 0 THEN 'Aprobado'
                   WHEN i.horas_aprobadas < i.horas_matriculadas AND i.horas_aprobadas > 0 THEN 'Incompleto'
                   ELSE 'Inscrito'
               END,
    i.observaciones = CONCAT(
                          ISNULL(i.observaciones, ''),
                          CASE WHEN ISNULL(i.observaciones, '') = '' THEN '' ELSE ' | ' END,
                          'Corrección de estado ', @now
                      )
FROM dbo.inscripcion AS i
WHERE EXISTS (
    SELECT 1 
    FROM @inscripciones_afectadas AS ia
    WHERE ia.id_participante_FK = i.id_participante_FK
    AND ia.numero_grupo = i.numero_grupo
    AND i.nombre_grupo LIKE ia.nombre_grupo + N'%'
);

PRINT 'Estados actualizados: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' inscripciones';

-- Mostrar estado final (después del UPDATE)
SELECT 
    i.id_participante_FK,
    i.nombre_grupo,
    i.numero_grupo,
    i.horas_matriculadas,
    i.horas_aprobadas,
    i.estado AS estado_final,
    CASE 
        WHEN i.horas_aprobadas >= i.horas_matriculadas AND i.horas_aprobadas > 0 THEN 'Aprobado'
        WHEN i.horas_aprobadas < i.horas_matriculadas AND i.horas_aprobadas > 0 THEN 'Incompleto'
        ELSE 'Inscrito'
    END AS estado_esperado
FROM dbo.inscripcion AS i
WHERE EXISTS (
    SELECT 1 
    FROM @inscripciones_afectadas AS ia
    WHERE ia.id_participante_FK = i.id_participante_FK
    AND ia.numero_grupo = i.numero_grupo
    AND i.nombre_grupo LIKE ia.nombre_grupo + N'%'
)
ORDER BY i.id_participante_FK;

COMMIT TRAN;
PRINT 'Transacción completada exitosamente en: ' + @now;
