-- ============================================================
-- Migracion: normalizacion del formato de carrera (Prod)
--
-- Deja participante.carrera en un solo formato -- mayusculas, sin tildes, sin
-- puntuacion -- igual que el catalogo de wwwroot/data/dataAreas.json y que
-- CarreraResolver en C#.
--
-- Pasos:
--   1. crea dbo.fn_NormalizarCarrera
--   2. respalda en participante_carrera_respaldo las filas que van a cambiar
--   3. normaliza participante.carrera
--
-- ANTES DE EJECUTAR EN PRODUCCION: tomar un respaldo completo de la base.
-- El UPDATE de este script pierde informacion -- tildes, mayusculas y
-- minusculas, puntuacion. La tabla participante_carrera_respaldo que se crea
-- aqui es la unica forma de deshacerlo, con
-- 03-revertir_normalizacion_carreras.sql.
--
-- Idempotente: re-ejecutarlo no toca las filas que ya esten en el formato
-- correcto, y como el respaldo cubre exactamente lo que el UPDATE va a
-- cambiar, tampoco duplica respaldos. Si mas adelante entra un valor sin
-- normalizar, la siguiente corrida SI lo respalda: el respaldo es un
-- historico, no una foto de la primera corrida.
-- ============================================================

SET XACT_ABORT ON;
GO

-- ------------------------------------------------------------
-- 0. Guard: esta migracion asume que la columna carrera ya existe. Si se
--    ejecuta contra una base en el esquema viejo, aborta aqui en vez de
--    fallar de forma confusa mas adelante.
--
--    El RAISERROR va ANTES del SET NOEXEC ON: NOEXEC surte efecto de
--    inmediato, asi que al reves el mensaje nunca llegaria a ejecutarse.
-- ------------------------------------------------------------
IF COL_LENGTH('dbo.participante', 'carrera') IS NULL
BEGIN
    RAISERROR(N'ABORTADO: la columna participante.carrera no existe. Esta base esta en un esquema anterior; hay que crear la columna antes de normalizarla.', 16, 1);
    SET NOEXEC ON;
END
GO

-- ------------------------------------------------------------
-- 1. Funcion de normalizacion. Va primero porque el respaldo del paso 3 la
--    usa para decidir que filas van a cambiar.
-- ------------------------------------------------------------
-- ------------------------------------------------------------
-- dbo.fn_NormalizarCarrera
--
-- Replica exactamente webMetics/Services/CarreraResolver.Normalizar:
--   1. quita diacriticos
--   2. pasa a mayusculas
--   3. conserva solo A-Z y 0-9; cualquier otro caracter actua como
--      SEPARADOR, no se borra sin mas: DISENO/MODAS queda DISENO MODAS
--   4. colapsa separadores repetidos en un espacio simple y recorta extremos
--   5. trunca a 512 caracteres
--
-- El mapa de diacriticos cubre el bloque Latin-1 Supplement de Unicode. Los
-- caracteres que no descomponen a una letra ASCII -- D con trazo, O con barra,
-- eszett, ligadura AE, thorn -- quedan como separador, igual que en C#.
--
-- Todas las comparaciones usan Latin1_General_BIN2 para no depender de la colacion de la
-- base: con una colacion CI_AI, REPLACE consideraria que la A y la A con tilde
-- son el mismo caracter.
-- ------------------------------------------------------------
CREATE OR ALTER FUNCTION dbo.fn_NormalizarCarrera (@entrada NVARCHAR(512))
RETURNS NVARCHAR(512)
AS
BEGIN
    IF @entrada IS NULL
        RETURN N'';

    DECLARE @texto NVARCHAR(512) = @entrada;

    -- 1. Diacriticos
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(192), N'A');  -- À -> A
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(193), N'A');  -- Á -> A
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(194), N'A');  -- Â -> A
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(195), N'A');  -- Ã -> A
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(196), N'A');  -- Ä -> A
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(197), N'A');  -- Å -> A
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(199), N'C');  -- Ç -> C
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(200), N'E');  -- È -> E
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(201), N'E');  -- É -> E
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(202), N'E');  -- Ê -> E
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(203), N'E');  -- Ë -> E
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(204), N'I');  -- Ì -> I
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(205), N'I');  -- Í -> I
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(206), N'I');  -- Î -> I
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(207), N'I');  -- Ï -> I
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(209), N'N');  -- Ñ -> N
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(210), N'O');  -- Ò -> O
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(211), N'O');  -- Ó -> O
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(212), N'O');  -- Ô -> O
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(213), N'O');  -- Õ -> O
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(214), N'O');  -- Ö -> O
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(217), N'U');  -- Ù -> U
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(218), N'U');  -- Ú -> U
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(219), N'U');  -- Û -> U
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(220), N'U');  -- Ü -> U
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(221), N'Y');  -- Ý -> Y
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(224), N'a');  -- à -> a
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(225), N'a');  -- á -> a
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(226), N'a');  -- â -> a
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(227), N'a');  -- ã -> a
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(228), N'a');  -- ä -> a
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(229), N'a');  -- å -> a
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(231), N'c');  -- ç -> c
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(232), N'e');  -- è -> e
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(233), N'e');  -- é -> e
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(234), N'e');  -- ê -> e
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(235), N'e');  -- ë -> e
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(236), N'i');  -- ì -> i
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(237), N'i');  -- í -> i
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(238), N'i');  -- î -> i
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(239), N'i');  -- ï -> i
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(241), N'n');  -- ñ -> n
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(242), N'o');  -- ò -> o
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(243), N'o');  -- ó -> o
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(244), N'o');  -- ô -> o
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(245), N'o');  -- õ -> o
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(246), N'o');  -- ö -> o
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(249), N'u');  -- ù -> u
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(250), N'u');  -- ú -> u
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(251), N'u');  -- û -> u
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(252), N'u');  -- ü -> u
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(253), N'y');  -- ý -> y
    SET @texto = REPLACE(@texto COLLATE Latin1_General_BIN2, NCHAR(255), N'y');  -- ÿ -> y

    -- 2. Mayusculas
    SET @texto = UPPER(@texto);

    -- 3. Todo lo que no sea A-Z o 0-9 pasa a ser un espacio. Con Latin1_General_BIN2 la
    --    clase [^A-Z0-9 ] se evalua por punto de codigo, asi que tambien
    --    atrapa minusculas que UPPER no haya cubierto.
    DECLARE @posicion INT = PATINDEX(N'%[^A-Z0-9 ]%', @texto COLLATE Latin1_General_BIN2);
    WHILE @posicion > 0
    BEGIN
        SET @texto = STUFF(@texto, @posicion, 1, N' ');
        SET @posicion = PATINDEX(N'%[^A-Z0-9 ]%', @texto COLLATE Latin1_General_BIN2);
    END

    -- 4. Colapsar espacios repetidos. Los marcadores < y > son seguros porque
    --    en este punto la cadena solo contiene A-Z, 0-9 y espacios.
    SET @texto = REPLACE(REPLACE(REPLACE(@texto, N' ', N'<>'), N'><', N''), N'<>', N' ');
    SET @texto = LTRIM(RTRIM(@texto));

    -- 5. Tope de la columna
    RETURN LEFT(@texto, 512);
END

GO

-- ------------------------------------------------------------
-- 2. Tabla de respaldo. Es un historico: una fila por cada vez que la
--    migracion esta por reescribir la carrera de un participante. Con una
--    sola fila por participante, un valor sin normalizar que entrara despues
--    de migrar se perderia sin respaldo, y la reversion restauraria un valor
--    viejo pisando ediciones legitimas.
--
--    Sin llave foranea a participante a proposito: si un participante se
--    elimina, su respaldo sobrevive y no bloquea el borrado.
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.participante_carrera_respaldo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.participante_carrera_respaldo (
        id_respaldo        INT IDENTITY(1,1) NOT NULL,
        id_participante_FK NVARCHAR(64)  NOT NULL,
        carrera_original   NVARCHAR(512) NULL,
        fecha_respaldo     DATETIME2(3)  NOT NULL
            CONSTRAINT DF_participante_carrera_respaldo_fecha DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_participante_carrera_respaldo PRIMARY KEY (id_respaldo)
    );

    CREATE INDEX IX_participante_carrera_respaldo_participante
        ON dbo.participante_carrera_respaldo (id_participante_FK, fecha_respaldo DESC);

    PRINT N'Tabla participante_carrera_respaldo creada.';
END
GO

-- Una version anterior de este script creaba la tabla con la llave primaria
-- sobre id_participante_FK, o sea una sola fila por participante. Ese esquema
-- no puede guardar el segundo respaldo del mismo participante, asi que se
-- migra a historico conservando lo ya respaldado.
--
-- El DDL va por EXEC porque bajo SET NOEXEC ON el lote igual se compila, y un
-- ALTER TABLE sobre una tabla que no existe fallaria al enlazar en vez de
-- dejar hablar al guard.
IF OBJECT_ID('dbo.participante_carrera_respaldo', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.participante_carrera_respaldo', 'id_respaldo') IS NULL
BEGIN
    DECLARE @llaveVieja SYSNAME = (
        SELECT kc.name
        FROM sys.key_constraints AS kc
        WHERE kc.parent_object_id = OBJECT_ID('dbo.participante_carrera_respaldo')
          AND kc.type = 'PK');

    -- El nombre se arma aparte: EXEC() no admite llamadas a funcion dentro de
    -- la expresion que ejecuta.
    DECLARE @quitarLlave NVARCHAR(MAX) =
        N'ALTER TABLE dbo.participante_carrera_respaldo DROP CONSTRAINT ' + QUOTENAME(@llaveVieja);

    IF @llaveVieja IS NOT NULL
        EXEC sp_executesql @quitarLlave;

    EXEC sp_executesql N'ALTER TABLE dbo.participante_carrera_respaldo ADD id_respaldo INT IDENTITY(1,1) NOT NULL';
    EXEC sp_executesql N'ALTER TABLE dbo.participante_carrera_respaldo ADD CONSTRAINT PK_participante_carrera_respaldo PRIMARY KEY (id_respaldo)';
    EXEC sp_executesql N'CREATE INDEX IX_participante_carrera_respaldo_participante ON dbo.participante_carrera_respaldo (id_participante_FK, fecha_respaldo DESC)';

    PRINT N'Respaldo migrado a historico: se agrego id_respaldo y se quito la llave por participante.';
END
GO

-- ------------------------------------------------------------
-- 3. Respaldar exactamente las filas que el paso 4 va a reescribir. Ese
--    recorte es lo que hace idempotente al script sin necesidad de recordar
--    si ya se corrio: cuando no queda nada por normalizar, no hay nada que
--    respaldar.
--
--    Un corte de conexion entre este paso y el 4 deja respaldos sin UPDATE;
--    la siguiente corrida vuelve a respaldar esas mismas filas y quedan dos
--    respaldos identicos, que la reversion trata igual que uno solo.
-- ------------------------------------------------------------
-- Via sp_executesql a proposito: SET NOEXEC ON evita la ejecucion pero NO la
-- compilacion, asi que una referencia directa a p.carrera haria fallar el
-- script con 'Invalid column name' justo en el caso que el guard querria
-- reportar con claridad. El SQL dinamico se compila solo si llega a correr.
EXEC sp_executesql N'
INSERT INTO dbo.participante_carrera_respaldo (id_participante_FK, carrera_original)
SELECT p.id_participante_PK, p.carrera
FROM dbo.participante AS p
WHERE p.carrera IS NOT NULL
  AND p.carrera COLLATE Latin1_General_BIN2
      <> dbo.fn_NormalizarCarrera(p.carrera) COLLATE Latin1_General_BIN2;
PRINT N''Filas respaldadas en esta corrida: '' + CAST(@@ROWCOUNT AS NVARCHAR(16));';
GO

-- ------------------------------------------------------------
-- 4. Normalizar las filas existentes. La comparacion va en Latin1_General_BIN2 para que
--    una base con colacion CI_AI no considere que el valor sin normalizar ya
--    es igual al normalizado y se salte la actualizacion.
-- ------------------------------------------------------------
EXEC sp_executesql N'
UPDATE p
SET carrera = dbo.fn_NormalizarCarrera(p.carrera)
FROM dbo.participante AS p
WHERE p.carrera IS NOT NULL
  AND p.carrera COLLATE Latin1_General_BIN2
      <> dbo.fn_NormalizarCarrera(p.carrera) COLLATE Latin1_General_BIN2;
PRINT N''Filas normalizadas en esta corrida: '' + CAST(@@ROWCOUNT AS NVARCHAR(16));';
GO

SET NOEXEC OFF;
GO
