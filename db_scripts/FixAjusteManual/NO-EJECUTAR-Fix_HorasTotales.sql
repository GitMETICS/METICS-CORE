-- =============================================================================
-- SCRIPT ORIGINAL PARA AJUSTE MANUAL DE HORAS TOTALES
-- NO EJECUTAR
-- =============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @now VARCHAR(19) = CONVERT(VARCHAR(19), SYSDATETIME(), 120);

DECLARE @updates TABLE (
    id_participante_FK NVARCHAR(255),
    nombre_grupo       NVARCHAR(255),
    numero_grupo       INT,
    horas_matriculadas INT,
    horas_aprobadas    INT
);

INSERT INTO @updates (id_participante_FK, nombre_grupo, numero_grupo, horas_matriculadas, horas_aprobadas) VALUES
(N'DORIAN.MENDEZ@ucr.ac.cr',         N'AUTOGESTIONADO Diseño de Ambientes Virtuales Accesibles para el Aprendizaje', 12, 0, 5),
(N'fabian.fallasmoya@ucr.ac.cr',     N'AUTOGESTIONADO TM-8000 Principios y Procedimientos para el Uso de Entornos Virtuales en la UCR', 22, 0, 5),
(N'michael.fallasvillegas@ucr.ac.cr',N'AUTOGESTIONADO TM-8000 Principios y Procedimientos para el Uso de Entornos Virtuales en la UCR', 22, 0, 5),
(N'juan.gomezsolano@ucr.ac.cr',      N'AUTOGESTIONADO TM-8000 Principios y Procedimientos para el Uso de Entornos Virtuales en la UCR', 22, 0, 5);

UPDATE i
   SET i.horas_matriculadas = u.horas_matriculadas,
       i.horas_aprobadas    = u.horas_aprobadas,
       i.estado             = CASE 
                                  WHEN u.horas_aprobadas >= u.horas_matriculadas AND u.horas_aprobadas > 0 THEN 'Aprobado'
                                  WHEN u.horas_aprobadas < u.horas_matriculadas AND u.horas_aprobadas > 0 THEN 'Incompleto'
                                  ELSE 'Inscrito'
                              END,
       i.observaciones      = CONCAT(
                                 ISNULL(i.observaciones, ''),
                                 CASE WHEN ISNULL(i.observaciones, '') = '' THEN '' ELSE ' | ' END,
                                 'Ajuste manual ', @now
                              )
FROM METICS.dbo.inscripcion AS i
JOIN @updates AS u
  ON u.id_participante_FK = i.id_participante_FK
 AND i.numero_grupo       = u.numero_grupo
 AND i.nombre_grupo LIKE u.nombre_grupo + N'%';

SELECT @@ROWCOUNT AS filas_afectadas;

COMMIT TRAN;

