-- Verificacion de solo lectura. Ejecutar despues de 01.
SELECT
    CASE WHEN OBJECT_ID('dbo.fn_NormalizarCarrera', 'FN') IS NOT NULL
         THEN 'OK' ELSE 'MISSING' END AS fn_NormalizarCarrera,
    CASE WHEN COL_LENGTH('dbo.participante_carrera_respaldo', 'carrera_normalizada') IS NOT NULL
         THEN 'OK' ELSE 'MISSING' END AS respaldo_con_valor_escrito;

-- Casos independientes del mapa generado, incluidos los fallos de la revision.
SELECT prueba, entrada, esperado, dbo.fn_NormalizarCarrera(entrada) AS obtenido,
       CASE WHEN CONVERT(VARBINARY(MAX), dbo.fn_NormalizarCarrera(entrada))
               = CONVERT(VARBINARY(MAX), esperado) THEN 'OK' ELSE 'FAIL' END AS estado
FROM (VALUES
    (1, N'Diseño Gráfico', N'DISENO GRAFICO'),
    (2, N'Bachillerato en Inglés', N'BACHILLERATO EN INGLES'),
    (3, N'   varios    espacios   ', N'VARIOS ESPACIOS'),
    (4, N'DISENO/MODAS', N'DISENO MODAS'),
    (5, N'Tecnico en Redes 5G!!', N'TECNICO EN REDES 5G'),
    (6, NULL, N''),
    (7, N'MEDICINA ', N'MEDICINA'),
    (8, N'Disen' + NCHAR(771) + N'o', N'DISENO'),
    (9, N'Ingeniería Łódź', N'INGENIERIA ODZ'),
    (10, N'   ', N'')
) AS casos(prueba, entrada, esperado);

-- VARBINARY compara tambien los espacios finales; BIN2 solo no lo hace.
SELECT COUNT(*) AS filas_sin_normalizar,
       CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'FAIL' END AS estado
FROM dbo.participante
WHERE carrera IS NOT NULL
  AND CONVERT(VARBINARY(MAX), carrera)
      <> CONVERT(VARBINARY(MAX), dbo.fn_NormalizarCarrera(carrera));

SELECT COUNT(*) AS filas_con_formato_invalido,
       CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'FAIL' END AS estado
FROM dbo.participante
WHERE carrera IS NOT NULL AND DATALENGTH(carrera) > 0
  AND (carrera COLLATE Latin1_General_BIN2 LIKE N'%[^A-Z0-9 ]%'
    OR carrera COLLATE Latin1_General_BIN2 LIKE N'%  %'
    OR carrera COLLATE Latin1_General_BIN2 LIKE N' %'
    OR carrera COLLATE Latin1_General_BIN2 LIKE N'% ');

WITH ultimo AS (
    SELECT r.*,
           ROW_NUMBER() OVER (PARTITION BY r.id_participante_FK
                              ORDER BY r.fecha_respaldo DESC, r.id_respaldo DESC) AS orden
    FROM dbo.participante_carrera_respaldo AS r
), estado AS (
    SELECT CASE
        WHEN NOT EXISTS (SELECT CONVERT(VARBINARY(MAX), p.carrera)
                         EXCEPT SELECT CONVERT(VARBINARY(MAX), u.carrera_original)) THEN 'Restaurado'
        WHEN u.carrera_normalizada IS NULL THEN 'Sin valor escrito conocido'
        WHEN CONVERT(VARBINARY(MAX), p.carrera) = CONVERT(VARBINARY(MAX), u.carrera_normalizada) THEN 'Revertible'
        ELSE 'Edicion posterior' END AS valor
    FROM ultimo AS u
    INNER JOIN dbo.participante AS p ON p.id_participante_PK = u.id_participante_FK
    WHERE u.orden = 1
)
SELECT valor AS estado_respaldo, COUNT(*) AS participantes
FROM estado GROUP BY valor;

WITH ultimo AS (
    SELECT r.*,
           ROW_NUMBER() OVER (PARTITION BY r.id_participante_FK
                              ORDER BY r.fecha_respaldo DESC, r.id_respaldo DESC) AS orden
    FROM dbo.participante_carrera_respaldo AS r
)
SELECT TOP (20) u.id_participante_FK, u.carrera_original,
       u.carrera_normalizada AS valor_escrito, p.carrera AS valor_actual
FROM ultimo AS u
INNER JOIN dbo.participante AS p ON p.id_participante_PK = u.id_participante_FK
WHERE u.orden = 1
ORDER BY u.id_participante_FK;
