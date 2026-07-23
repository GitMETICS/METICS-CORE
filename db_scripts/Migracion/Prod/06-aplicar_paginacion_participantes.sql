-- ============================================================
-- Migracion: paginacion backend del listado de participantes
--
-- Cambios no destructivos:
--   1. indice para el ordenamiento principal del listado
--   2. procedimiento SelectParticipantesPaginados
--
-- No elimina ni modifica datos existentes. Es idempotente.
-- ============================================================

SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_participante_listado_inscripciones'
          AND object_id = OBJECT_ID('dbo.participante')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_participante_listado_inscripciones
            ON dbo.participante (nombre, apellido_1, apellido_2, id_participante_PK)
            INCLUDE (correo, unidad_academica);
    END;

    COMMIT TRANSACTION;
    PRINT 'Indice para paginacion de participantes creado o ya existente.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

CREATE OR ALTER PROCEDURE dbo.SelectParticipantesPaginados
    @Offset        INT = 0,
    @PageSize      INT = 50,
    @SearchTerm    NVARCHAR(512) = NULL,
    @SortColumn    NVARCHAR(32) = N'nombre',
    @SortDirection NVARCHAR(4) = N'asc'
AS
BEGIN
    SET NOCOUNT ON;

    SET @Offset = CASE WHEN @Offset < 0 THEN 0 ELSE @Offset END;
    SET @PageSize = CASE WHEN @PageSize IN (5, 10, 25, 50) THEN @PageSize ELSE 50 END;
    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
    SET @SortColumn = CASE
        WHEN @SortColumn IN (
            N'unidadAcademica', N'nombre', N'correo',
            N'horasMatriculadas', N'horasAprobadas'
        ) THEN @SortColumn
        ELSE N'nombre'
    END;
    SET @SortDirection = CASE WHEN LOWER(@SortDirection) = N'desc' THEN N'desc' ELSE N'asc' END;

    DECLARE @SearchPattern NVARCHAR(1030) = NULL;

    IF @SearchTerm IS NOT NULL
    BEGIN
        SET @SearchPattern = N'%' +
            REPLACE(
                REPLACE(
                    REPLACE(
                        REPLACE(@SearchTerm, N'~', N'~~'),
                    N'%', N'~%'),
                N'_', N'~_'),
            N'[', N'~[') +
            N'%';
    END;

    SELECT COUNT(*) AS TotalRecords
    FROM dbo.participante;

    SELECT COUNT(*) AS FilteredRecords
    FROM dbo.participante AS p
    WHERE @SearchPattern IS NULL
       OR p.unidad_academica LIKE @SearchPattern ESCAPE N'~'
       OR p.nombre LIKE @SearchPattern ESCAPE N'~'
       OR p.apellido_1 LIKE @SearchPattern ESCAPE N'~'
       OR p.apellido_2 LIKE @SearchPattern ESCAPE N'~'
       OR p.correo LIKE @SearchPattern ESCAPE N'~'
       OR CONVERT(NVARCHAR(20), ISNULL(p.total_horas_matriculadas, 0)) LIKE @SearchPattern ESCAPE N'~'
       OR CONVERT(NVARCHAR(20), ISNULL(p.total_horas_aprobadas, 0)) LIKE @SearchPattern ESCAPE N'~';

    SELECT
        p.id_participante_PK AS IdParticipante,
        p.nombre AS Nombre,
        p.apellido_1 AS PrimerApellido,
        p.apellido_2 AS SegundoApellido,
        p.correo AS Correo,
        p.unidad_academica AS UnidadAcademica,
        ISNULL(p.total_horas_matriculadas, 0) AS HorasMatriculadas,
        ISNULL(p.total_horas_aprobadas, 0) AS HorasAprobadas,
        ISNULL(p.correo_notificacion_enviado, 0) AS CorreoNotificacionEnviado
    FROM dbo.participante AS p
    WHERE @SearchPattern IS NULL
       OR p.unidad_academica LIKE @SearchPattern ESCAPE N'~'
       OR p.nombre LIKE @SearchPattern ESCAPE N'~'
       OR p.apellido_1 LIKE @SearchPattern ESCAPE N'~'
       OR p.apellido_2 LIKE @SearchPattern ESCAPE N'~'
       OR p.correo LIKE @SearchPattern ESCAPE N'~'
       OR CONVERT(NVARCHAR(20), ISNULL(p.total_horas_matriculadas, 0)) LIKE @SearchPattern ESCAPE N'~'
       OR CONVERT(NVARCHAR(20), ISNULL(p.total_horas_aprobadas, 0)) LIKE @SearchPattern ESCAPE N'~'
    ORDER BY
        CASE WHEN @SortColumn = N'unidadAcademica' AND @SortDirection = N'asc' THEN p.unidad_academica END ASC,
        CASE WHEN @SortColumn = N'unidadAcademica' AND @SortDirection = N'desc' THEN p.unidad_academica END DESC,
        CASE WHEN @SortColumn = N'nombre' AND @SortDirection = N'asc' THEN p.nombre END ASC,
        CASE WHEN @SortColumn = N'nombre' AND @SortDirection = N'desc' THEN p.nombre END DESC,
        CASE WHEN @SortColumn = N'nombre' AND @SortDirection = N'asc' THEN p.apellido_1 END ASC,
        CASE WHEN @SortColumn = N'nombre' AND @SortDirection = N'desc' THEN p.apellido_1 END DESC,
        CASE WHEN @SortColumn = N'nombre' AND @SortDirection = N'asc' THEN p.apellido_2 END ASC,
        CASE WHEN @SortColumn = N'nombre' AND @SortDirection = N'desc' THEN p.apellido_2 END DESC,
        CASE WHEN @SortColumn = N'correo' AND @SortDirection = N'asc' THEN p.correo END ASC,
        CASE WHEN @SortColumn = N'correo' AND @SortDirection = N'desc' THEN p.correo END DESC,
        CASE WHEN @SortColumn = N'horasMatriculadas' AND @SortDirection = N'asc' THEN ISNULL(p.total_horas_matriculadas, 0) END ASC,
        CASE WHEN @SortColumn = N'horasMatriculadas' AND @SortDirection = N'desc' THEN ISNULL(p.total_horas_matriculadas, 0) END DESC,
        CASE WHEN @SortColumn = N'horasAprobadas' AND @SortDirection = N'asc' THEN ISNULL(p.total_horas_aprobadas, 0) END ASC,
        CASE WHEN @SortColumn = N'horasAprobadas' AND @SortDirection = N'desc' THEN ISNULL(p.total_horas_aprobadas, 0) END DESC,
        p.id_participante_PK ASC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
