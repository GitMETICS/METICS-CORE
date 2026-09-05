-- ============================================================
-- Verificacion: normalizacion del formato de carrera
-- Solo lee; no modifica datos.
-- ============================================================

-- 1. Objetos creados por la migracion
SELECT
    CASE WHEN OBJECT_ID('dbo.fn_NormalizarCarrera', 'FN') IS NOT NULL
         THEN 'OK' ELSE 'MISSING' END AS [fn_NormalizarCarrera],
    CASE WHEN OBJECT_ID('dbo.participante_carrera_respaldo', 'U') IS NOT NULL
         THEN 'OK' ELSE 'MISSING' END AS [participante_carrera_respaldo];

-- 2. La funcion produce lo esperado sobre casos conocidos. El caso 4 confirma
--    que la puntuacion separa en vez de pegar palabras.
SELECT
    prueba,
    entrada,
    esperado,
    dbo.fn_NormalizarCarrera(entrada) AS obtenido,
    CASE WHEN dbo.fn_NormalizarCarrera(entrada) COLLATE Latin1_General_BIN2
              = esperado COLLATE Latin1_General_BIN2
         THEN 'OK' ELSE 'FAIL' END AS estado
FROM (VALUES
    (1, N'Diseño Gráfico',           N'DISENO GRAFICO'),
    (2, N'Bachillerato en Inglés',   N'BACHILLERATO EN INGLES'),
    (3, N'   varios    espacios   ', N'VARIOS ESPACIOS'),
    (4, N'DISENO/MODAS',             N'DISENO MODAS'),
    (5, N'Tecnico en Redes 5G!!',    N'TECNICO EN REDES 5G'),
    (6, NULL,                        N'')
) AS casos(prueba, entrada, esperado);

-- 3. Ninguna fila puede quedar fuera del formato. Este es el chequeo que
--    importa: debe dar 0.
SELECT COUNT(*) AS filas_sin_normalizar,
       CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'FAIL' END AS estado
FROM dbo.participante
WHERE carrera IS NOT NULL
  AND carrera COLLATE Latin1_General_BIN2
      <> dbo.fn_NormalizarCarrera(carrera) COLLATE Latin1_General_BIN2;

-- 4. Chequeo independiente de la funcion: caracteres fuera de A-Z0-9,
--    espacios dobles o espacios en los extremos.
SELECT COUNT(*) AS filas_con_formato_invalido,
       CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'FAIL' END AS estado
FROM dbo.participante
WHERE carrera IS NOT NULL
  AND carrera <> N''
  AND (
        carrera COLLATE Latin1_General_BIN2 LIKE N'%[^A-Z0-9 ]%'
     OR carrera COLLATE Latin1_General_BIN2 LIKE N'%  %'
     OR carrera COLLATE Latin1_General_BIN2 LIKE N' %'
     OR carrera COLLATE Latin1_General_BIN2 LIKE N'% '
  );

-- 5. Cobertura del respaldo: todo participante deberia tener su fila.
SELECT
    (SELECT COUNT(*) FROM dbo.participante)                  AS participantes,
    (SELECT COUNT(*) FROM dbo.participante_carrera_respaldo) AS respaldados,
    (SELECT COUNT(*) FROM dbo.participante AS p
      WHERE NOT EXISTS (SELECT 1 FROM dbo.participante_carrera_respaldo AS r
                        WHERE r.id_participante_FK = p.id_participante_PK)
    ) AS sin_respaldo;

-- 6. Muestra de lo que efectivamente cambio.
SELECT TOP (20)
    r.id_participante_FK,
    r.carrera_original,
    p.carrera AS carrera_normalizada
FROM dbo.participante_carrera_respaldo AS r
INNER JOIN dbo.participante AS p
        ON p.id_participante_PK = r.id_participante_FK
WHERE ISNULL(r.carrera_original, N'') COLLATE Latin1_General_BIN2
   <> ISNULL(p.carrera, N'')          COLLATE Latin1_General_BIN2
ORDER BY r.id_participante_FK;
