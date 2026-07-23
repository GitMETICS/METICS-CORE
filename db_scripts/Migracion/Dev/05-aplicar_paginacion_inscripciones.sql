-- ============================================================
-- Migracion: paginacion backend del listado de inscripciones
--
-- Cambios no destructivos:
--   1. indices para el JOIN y el ordenamiento del listado
--   2. procedimiento SelectInscripcionesPaginadas
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
        WHERE name = 'IX_inscripcion_id_participante'
          AND object_id = OBJECT_ID('dbo.inscripcion')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_inscripcion_id_participante
            ON dbo.inscripcion (id_participante_FK, id_inscripcion_PK)
            INCLUDE (nombre_grupo, numero_grupo, horas_matriculadas, horas_aprobadas);
    END;

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
    PRINT 'Indices para paginacion de inscripciones creados o ya existentes.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

CREATE OR ALTER PROCEDURE dbo.SelectInscripcionesPaginadas
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
            N'nombre', N'primerApellido', N'segundoApellido', N'correo',
            N'modulo', N'estado', N'horasMatriculadas', N'horasAprobadas'
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
    FROM dbo.inscripcion;

    SELECT COUNT(*) AS FilteredRecords
    FROM dbo.inscripcion AS i
    INNER JOIN dbo.participante AS p
        ON p.id_participante_PK = i.id_participante_FK
    CROSS APPLY (
        VALUES (
            CASE
                WHEN ISNULL(i.horas_matriculadas, 0) <= 0
                     AND ISNULL(i.horas_aprobadas, 0) > 0 THEN N'Aprobado'
                WHEN ISNULL(i.horas_aprobadas, 0) > 0 THEN N'Incompleto'
                ELSE N'Inscrito'
            END
        )
    ) AS estado(EstadoCalculado)
    WHERE @SearchPattern IS NULL
       OR p.unidad_academica LIKE @SearchPattern ESCAPE N'~'
       OR p.nombre LIKE @SearchPattern ESCAPE N'~'
       OR p.apellido_1 LIKE @SearchPattern ESCAPE N'~'
       OR p.apellido_2 LIKE @SearchPattern ESCAPE N'~'
       OR p.correo LIKE @SearchPattern ESCAPE N'~'
       OR i.nombre_grupo LIKE @SearchPattern ESCAPE N'~'
       OR CONVERT(NVARCHAR(20), ISNULL(i.horas_matriculadas, 0)) LIKE @SearchPattern ESCAPE N'~'
       OR CONVERT(NVARCHAR(20), ISNULL(i.horas_aprobadas, 0)) LIKE @SearchPattern ESCAPE N'~'
       OR estado.EstadoCalculado LIKE @SearchPattern ESCAPE N'~';

    SELECT
        i.id_inscripcion_PK AS IdInscripcion,
        i.id_participante_FK AS IdParticipante,
        p.nombre AS Nombre,
        p.apellido_1 AS PrimerApellido,
        p.apellido_2 AS SegundoApellido,
        p.correo AS Correo,
        p.unidad_academica AS UnidadAcademica,
        i.nombre_grupo AS NombreGrupo,
        ISNULL(i.numero_grupo, 0) AS NumeroGrupo,
        estado.EstadoCalculado AS Estado,
        ISNULL(i.horas_matriculadas, 0) AS HorasMatriculadas,
        ISNULL(i.horas_aprobadas, 0) AS HorasAprobadas
    FROM dbo.inscripcion AS i
    INNER JOIN dbo.participante AS p
        ON p.id_participante_PK = i.id_participante_FK
    CROSS APPLY (
        VALUES (
            CASE
                WHEN ISNULL(i.horas_matriculadas, 0) <= 0
                     AND ISNULL(i.horas_aprobadas, 0) > 0 THEN N'Aprobado'
                WHEN ISNULL(i.horas_aprobadas, 0) > 0 THEN N'Incompleto'
                ELSE N'Inscrito'
            END
        )
    ) AS estado(EstadoCalculado)
    WHERE @SearchPattern IS NULL
       OR p.unidad_academica LIKE @SearchPattern ESCAPE N'~'
       OR p.nombre LIKE @SearchPattern ESCAPE N'~'
       OR p.apellido_1 LIKE @SearchPattern ESCAPE N'~'
       OR p.apellido_2 LIKE @SearchPattern ESCAPE N'~'
       OR p.correo LIKE @SearchPattern ESCAPE N'~'
       OR i.nombre_grupo LIKE @SearchPattern ESCAPE N'~'
       OR CONVERT(NVARCHAR(20), ISNULL(i.horas_matriculadas, 0)) LIKE @SearchPattern ESCAPE N'~'
       OR CONVERT(NVARCHAR(20), ISNULL(i.horas_aprobadas, 0)) LIKE @SearchPattern ESCAPE N'~'
       OR estado.EstadoCalculado LIKE @SearchPattern ESCAPE N'~'
    ORDER BY
        CASE WHEN @SortColumn = N'nombre' AND @SortDirection = N'asc' THEN p.nombre END ASC,
        CASE WHEN @SortColumn = N'nombre' AND @SortDirection = N'desc' THEN p.nombre END DESC,
        CASE WHEN @SortColumn = N'primerApellido' AND @SortDirection = N'asc' THEN p.apellido_1 END ASC,
        CASE WHEN @SortColumn = N'primerApellido' AND @SortDirection = N'desc' THEN p.apellido_1 END DESC,
        CASE WHEN @SortColumn = N'segundoApellido' AND @SortDirection = N'asc' THEN p.apellido_2 END ASC,
        CASE WHEN @SortColumn = N'segundoApellido' AND @SortDirection = N'desc' THEN p.apellido_2 END DESC,
        CASE WHEN @SortColumn = N'correo' AND @SortDirection = N'asc' THEN p.correo END ASC,
        CASE WHEN @SortColumn = N'correo' AND @SortDirection = N'desc' THEN p.correo END DESC,
        CASE WHEN @SortColumn = N'modulo' AND @SortDirection = N'asc' THEN i.nombre_grupo END ASC,
        CASE WHEN @SortColumn = N'modulo' AND @SortDirection = N'desc' THEN i.nombre_grupo END DESC,
        CASE WHEN @SortColumn = N'modulo' AND @SortDirection = N'asc' THEN i.numero_grupo END ASC,
        CASE WHEN @SortColumn = N'modulo' AND @SortDirection = N'desc' THEN i.numero_grupo END DESC,
        CASE WHEN @SortColumn = N'estado' AND @SortDirection = N'asc' THEN estado.EstadoCalculado END ASC,
        CASE WHEN @SortColumn = N'estado' AND @SortDirection = N'desc' THEN estado.EstadoCalculado END DESC,
        CASE WHEN @SortColumn = N'horasMatriculadas' AND @SortDirection = N'asc' THEN ISNULL(i.horas_matriculadas, 0) END ASC,
        CASE WHEN @SortColumn = N'horasMatriculadas' AND @SortDirection = N'desc' THEN ISNULL(i.horas_matriculadas, 0) END DESC,
        CASE WHEN @SortColumn = N'horasAprobadas' AND @SortDirection = N'asc' THEN ISNULL(i.horas_aprobadas, 0) END ASC,
        CASE WHEN @SortColumn = N'horasAprobadas' AND @SortDirection = N'desc' THEN ISNULL(i.horas_aprobadas, 0) END DESC,
        i.id_inscripcion_PK ASC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

