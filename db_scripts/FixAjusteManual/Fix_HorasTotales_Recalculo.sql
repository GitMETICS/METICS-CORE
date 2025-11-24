-- =============================================
-- Script para recalcular las horas totales de participantes
-- =============================================
-- Este script recalcula y actualiza los campos total_horas_matriculadas 
-- y total_horas_aprobadas en la tabla participante para los participantes 
-- afectados por el script de ajuste manual
--
-- LÓGICA DE CÁLCULO (basada en ParticipanteHandler.cs):
-- - total_horas_matriculadas: Suma de horas_matriculadas donde estado = 'Inscrito'
-- - total_horas_aprobadas: Suma de horas_aprobadas donde estado = 'Aprobado'
-- =============================================

SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @now VARCHAR(19) = CONVERT(VARCHAR(19), SYSDATETIME(), 120);

-- Lista de participantes afectados por el script anterior
DECLARE @participantes_afectados TABLE (
    id_participante NVARCHAR(255)
);

INSERT INTO @participantes_afectados (id_participante) VALUES
(N'DORIAN.MENDEZ@ucr.ac.cr'),
(N'fabian.fallasmoya@ucr.ac.cr'),
(N'michael.fallasvillegas@ucr.ac.cr'),
(N'juan.gomezsolano@ucr.ac.cr');

-- Mostrar valores actuales (antes del UPDATE)
SELECT 
    p.id_participante_PK,
    p.total_horas_matriculadas AS horas_matriculadas_actual,
    p.total_horas_aprobadas AS horas_aprobadas_actual,
    ISNULL(SUM(CASE WHEN i.estado = 'Inscrito' THEN i.horas_matriculadas ELSE 0 END), 0) AS horas_matriculadas_calculadas,
    ISNULL(SUM(CASE WHEN i.estado = 'Aprobado' THEN i.horas_aprobadas ELSE 0 END), 0) AS horas_aprobadas_calculadas
FROM dbo.participante AS p
LEFT JOIN dbo.inscripcion AS i ON i.id_participante_FK = p.id_participante_PK
WHERE p.id_participante_PK IN (SELECT id_participante FROM @participantes_afectados)
GROUP BY p.id_participante_PK, p.total_horas_matriculadas, p.total_horas_aprobadas
ORDER BY p.id_participante_PK;

-- Actualizar total_horas_matriculadas (suma de inscripciones con estado 'Inscrito')
UPDATE p
SET p.total_horas_matriculadas = ISNULL(
    (SELECT SUM(i.horas_matriculadas)
     FROM dbo.inscripcion AS i
     WHERE i.id_participante_FK = p.id_participante_PK
     AND i.estado = 'Inscrito'), 0)
FROM dbo.participante AS p
WHERE p.id_participante_PK IN (SELECT id_participante FROM @participantes_afectados);

PRINT 'Horas matriculadas actualizadas: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' participantes';

-- Actualizar total_horas_aprobadas (suma de inscripciones con estado 'Aprobado')
UPDATE p
SET p.total_horas_aprobadas = ISNULL(
    (SELECT SUM(i.horas_aprobadas)
     FROM dbo.inscripcion AS i
     WHERE i.id_participante_FK = p.id_participante_PK
     AND i.estado = 'Aprobado'), 0)
FROM dbo.participante AS p
WHERE p.id_participante_PK IN (SELECT id_participante FROM @participantes_afectados);

PRINT 'Horas aprobadas actualizadas: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' participantes';

-- Mostrar valores finales (después del UPDATE)
SELECT 
    p.id_participante_PK,
    p.total_horas_matriculadas AS horas_matriculadas_final,
    p.total_horas_aprobadas AS horas_aprobadas_final,
    ISNULL(SUM(CASE WHEN i.estado = 'Inscrito' THEN i.horas_matriculadas ELSE 0 END), 0) AS horas_matriculadas_verificacion,
    ISNULL(SUM(CASE WHEN i.estado = 'Aprobado' THEN i.horas_aprobadas ELSE 0 END), 0) AS horas_aprobadas_verificacion
FROM dbo.participante AS p
LEFT JOIN dbo.inscripcion AS i ON i.id_participante_FK = p.id_participante_PK
WHERE p.id_participante_PK IN (SELECT id_participante FROM @participantes_afectados)
GROUP BY p.id_participante_PK, p.total_horas_matriculadas, p.total_horas_aprobadas
ORDER BY p.id_participante_PK;

COMMIT TRAN;
PRINT 'Transacción completada exitosamente en: ' + @now;
