-- ============================================================
-- METICS migration verification
-- Run after db_migrate_to_current.sql to confirm every change
-- landed correctly.  All checks print OK or MISSING/FAIL.
-- ============================================================

-- 1. New columns on participante
SELECT
    CASE WHEN COL_LENGTH('dbo.participante', 'carrera')            IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS [carrera],
    CASE WHEN COL_LENGTH('dbo.participante', 'correo_alternativo') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS [correo_alternativo],
    CASE WHEN COL_LENGTH('dbo.participante', 'grado_academico')    IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS [grado_academico];

-- 2. Junction table
SELECT
    CASE WHEN OBJECT_ID('dbo.participante_area_extra', 'U') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS [participante_area_extra];

-- 3. CHECK constraint on grado_academico
SELECT
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE name = 'CK_participante_grado_academico'
          AND parent_object_id = OBJECT_ID('dbo.participante')
    ) THEN 'OK' ELSE 'MISSING' END AS [CK_participante_grado_academico];

-- 4. SP parameters — all three SPs should expose @correoAlternativo, @gradoAcademico, @carrera
SELECT SPECIFIC_NAME AS sp_name, PARAMETER_NAME AS param
FROM INFORMATION_SCHEMA.PARAMETERS
WHERE SPECIFIC_NAME IN ('InsertParticipante', 'UpdateParticipante')
  AND PARAMETER_NAME IN ('@correoAlternativo', '@gradoAcademico', '@carrera')
ORDER BY SPECIFIC_NAME, ORDINAL_POSITION;
-- Expected: 6 rows (2 SPs × 3 params each)

-- 5. CHECK constraint actually rejects invalid values
BEGIN TRY
    INSERT INTO dbo.participante
        (id_usuario_FK, id_participante_PK, nombre, apellido_1, correo,
         area, departamento, unidad_academica, grado_academico)
    VALUES
        ('admin.admin@ucr.ac.cr', '__verify_test__', 'T', 'T', 'test@ucr.ac.cr',
         'A', 'D', 'U', 'VALOR_INVALIDO');

    -- If we reach here the constraint did not fire — clean up and report failure
    DELETE FROM dbo.participante WHERE id_participante_PK = '__verify_test__';
    PRINT 'CHECK 5: FAIL — constraint did not reject invalid grado_academico';
END TRY
BEGIN CATCH
    PRINT 'CHECK 5: OK — constraint correctly rejected invalid grado_academico value';
END CATCH;

-- 6. Valid grado_academico value is accepted
BEGIN TRY
    INSERT INTO dbo.participante
        (id_usuario_FK, id_participante_PK, nombre, apellido_1, correo,
         area, departamento, unidad_academica, grado_academico)
    VALUES
        ('admin.admin@ucr.ac.cr', '__verify_test__', 'T', 'T', 'test@ucr.ac.cr',
         'A', 'D', 'U', N'Licenciatura - Lic');

    DELETE FROM dbo.participante WHERE id_participante_PK = '__verify_test__';
    PRINT 'CHECK 6: OK — valid grado_academico value accepted';
END TRY
BEGIN CATCH
    PRINT 'CHECK 6: FAIL — valid grado_academico value was unexpectedly rejected: ' + ERROR_MESSAGE();
END CATCH;

-- 7. Existing participant count is unchanged (sanity check)
SELECT COUNT(*) AS total_participantes FROM dbo.participante;
