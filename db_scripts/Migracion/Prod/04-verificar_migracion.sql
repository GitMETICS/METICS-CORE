-- ============================================================
-- Verificación de la migración de METICS
-- Ejecutar después de 03-aplicar_migracion.sql para confirmar que
-- cada cambio se aplicó correctamente. Todas las verificaciones
-- imprimen OK o MISSING/FAIL.
-- ============================================================

-- 1. Columnas nuevas en participante
SELECT
    CASE WHEN COL_LENGTH('dbo.participante', 'carrera')            IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS [carrera],
    CASE WHEN COL_LENGTH('dbo.participante', 'correo_alternativo') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS [correo_alternativo],
    CASE WHEN COL_LENGTH('dbo.participante', 'grado_academico')    IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS [grado_academico];

-- 2. Tabla de unión
SELECT
    CASE WHEN OBJECT_ID('dbo.participante_area_extra', 'U') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS [participante_area_extra];

-- 3. Restricción CHECK sobre grado_academico
SELECT
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE name = 'CK_participante_grado_academico'
          AND parent_object_id = OBJECT_ID('dbo.participante')
    ) THEN 'OK' ELSE 'MISSING' END AS [CK_participante_grado_academico];

-- 4. Parámetros de los SP — los tres SP deben exponer @correoAlternativo, @gradoAcademico, @carrera
SELECT SPECIFIC_NAME AS sp_name, PARAMETER_NAME AS param
FROM INFORMATION_SCHEMA.PARAMETERS
WHERE SPECIFIC_NAME IN ('InsertParticipante', 'UpdateParticipante')
  AND PARAMETER_NAME IN ('@correoAlternativo', '@gradoAcademico', '@carrera')
ORDER BY SPECIFIC_NAME, ORDINAL_POSITION;
-- Esperado: 6 filas (2 SP × 3 parámetros cada uno)

-- 5. La restricción CHECK efectivamente rechaza valores inválidos
BEGIN TRY
    INSERT INTO dbo.participante
        (id_usuario_FK, id_participante_PK, nombre, apellido_1, correo,
         area, departamento, unidad_academica, grado_academico)
    VALUES
        ('admin.admin@ucr.ac.cr', '__verify_test__', 'T', 'T', 'test@ucr.ac.cr',
         'A', 'D', 'U', 'VALOR_INVALIDO');

    -- Si llegamos aquí la restricción no se activó — limpiar y reportar la falla
    DELETE FROM dbo.participante WHERE id_participante_PK = '__verify_test__';
    PRINT 'CHECK 5: FAIL — la restricción no rechazó un grado_academico inválido';
END TRY
BEGIN CATCH
    PRINT 'CHECK 5: OK — la restricción rechazó correctamente un valor de grado_academico inválido';
END CATCH;

-- 6. Se acepta un valor válido de grado_academico
BEGIN TRY
    INSERT INTO dbo.participante
        (id_usuario_FK, id_participante_PK, nombre, apellido_1, correo,
         area, departamento, unidad_academica, grado_academico)
    VALUES
        ('admin.admin@ucr.ac.cr', '__verify_test__', 'T', 'T', 'test@ucr.ac.cr',
         'A', 'D', 'U', N'Licenciatura - Lic');

    DELETE FROM dbo.participante WHERE id_participante_PK = '__verify_test__';
    PRINT 'CHECK 6: OK — se aceptó un valor válido de grado_academico';
END TRY
BEGIN CATCH
    PRINT 'CHECK 6: FAIL — un valor válido de grado_academico fue rechazado inesperadamente: ' + ERROR_MESSAGE();
END CATCH;

-- 7. El conteo de participantes existentes no cambió (verificación de cordura)
SELECT COUNT(*) AS total_participantes FROM dbo.participante;
