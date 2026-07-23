-- ============================================================
-- Verificacion: paginacion backend del listado de participantes
-- Solo realiza lecturas; no modifica datos.
-- ============================================================

SELECT
    CASE WHEN OBJECT_ID('dbo.SelectParticipantesPaginados', 'P') IS NOT NULL
         THEN 'OK' ELSE 'MISSING' END AS [SelectParticipantesPaginados];

SELECT
    i.name AS indice,
    CASE WHEN i.name IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS estado
FROM (VALUES
    ('IX_participante_listado_inscripciones', OBJECT_ID('dbo.participante'))
) AS esperado(nombre, object_id)
LEFT JOIN sys.indexes AS i
    ON i.name = esperado.nombre
   AND i.object_id = esperado.object_id;

SELECT
    p.name AS parametro,
    TYPE_NAME(p.user_type_id) AS tipo
FROM sys.parameters AS p
WHERE p.object_id = OBJECT_ID('dbo.SelectParticipantesPaginados')
ORDER BY p.parameter_id;
-- Esperado: @Offset, @PageSize, @SearchTerm, @SortColumn, @SortDirection

BEGIN TRY
    EXEC dbo.SelectParticipantesPaginados
        @Offset = 0,
        @PageSize = 50,
        @SearchTerm = NULL,
        @SortColumn = N'nombre',
        @SortDirection = N'asc';

    PRINT 'CHECK: OK - el procedimiento paginado se ejecuto correctamente.';
END TRY
BEGIN CATCH
    PRINT 'CHECK: FAIL - ' + ERROR_MESSAGE();
END CATCH;
