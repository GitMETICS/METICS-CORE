-- Ambiente: Prod
-- Normalizacion del formato de carrera. Ejecutar primero en una copia.
-- En produccion, tomar tambien un respaldo completo antes de ejecutar.
-- Cada UPDATE guarda atomicamente el valor anterior y el valor escrito.
-- Una segunda corrida solo respalda filas que vuelvan a necesitar cambios.
SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.participante', 'carrera') IS NULL
BEGIN
    RAISERROR(N'ABORTADO: la columna participante.carrera no existe. Aplicar primero las migraciones del esquema.', 16, 1);
    SET NOEXEC ON;
END
GO

-- Historico sin FK a participante: el respaldo sobrevive a sus borrados.
IF OBJECT_ID('dbo.participante_carrera_respaldo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.participante_carrera_respaldo (
        id_respaldo        INT IDENTITY(1,1) NOT NULL,
        id_participante_FK NVARCHAR(64)  NOT NULL,
        carrera_original   NVARCHAR(512) NULL,
        carrera_normalizada NVARCHAR(512) NULL,
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
    BEGIN TRY
        BEGIN TRANSACTION;
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
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO

-- Agregar el valor escrito para que la reversion no dependa de futuras
-- versiones del normalizador. Para respaldos anteriores se usa la funcion
-- anterior ANTES de reemplazarla. Sin ella se conserva NULL (no inferir).
IF COL_LENGTH('dbo.participante_carrera_respaldo', 'carrera_normalizada') IS NULL
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;
        EXEC sp_executesql N'ALTER TABLE dbo.participante_carrera_respaldo ADD carrera_normalizada NVARCHAR(512) NULL';
        IF OBJECT_ID('dbo.fn_NormalizarCarrera', 'FN') IS NOT NULL
            EXEC sp_executesql N'UPDATE dbo.participante_carrera_respaldo
                SET carrera_normalizada = dbo.fn_NormalizarCarrera(carrera_original)';
        ELSE
            PRINT N'AVISO: respaldos anteriores sin funcion original. No se revertiran automaticamente; revisar contra el respaldo completo.';
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO

-- BEGIN GENERATED NORMALIZAR CARRERA
-- Generado por scripts/carreras/NormalizacionSql; no editar este bloque a mano.
-- Mapeo Unicode derivado de CarreraResolver, incluidas marcas combinantes.
-- BIN2 y UNICODE evitan depender de UPPER, rangos o colacion de la base.
CREATE OR ALTER FUNCTION dbo.fn_NormalizarCarrera (@entrada NVARCHAR(512))
RETURNS NVARCHAR(512)
AS
BEGIN
    DECLARE @resultado NVARCHAR(512) = N'', @pos INT = 1,
            @longitud INT = DATALENGTH(@entrada) / 2, @codigo INT,
            @letra NVARCHAR(1), @separador BIT = 0;
    WHILE @pos <= @longitud
    BEGIN
        SET @codigo = UNICODE(SUBSTRING(@entrada COLLATE Latin1_General_BIN2, @pos, 1));
        SET @letra = CASE
            WHEN @codigo BETWEEN 768 AND 879
              OR @codigo BETWEEN 1155 AND 1159
              OR @codigo BETWEEN 1425 AND 1469
              OR @codigo = 1471
              OR @codigo BETWEEN 1473 AND 1474
              OR @codigo BETWEEN 1476 AND 1477
              OR @codigo = 1479
              OR @codigo BETWEEN 1552 AND 1562
              OR @codigo BETWEEN 1611 AND 1631
              OR @codigo = 1648
              OR @codigo BETWEEN 1750 AND 1756
              OR @codigo BETWEEN 1759 AND 1764
              OR @codigo BETWEEN 1767 AND 1768
              OR @codigo BETWEEN 1770 AND 1773
              OR @codigo = 1809
              OR @codigo BETWEEN 1840 AND 1866
              OR @codigo BETWEEN 1958 AND 1968
              OR @codigo BETWEEN 2027 AND 2035
              OR @codigo = 2045
              OR @codigo BETWEEN 2070 AND 2073
              OR @codigo BETWEEN 2075 AND 2083
              OR @codigo BETWEEN 2085 AND 2087
              OR @codigo BETWEEN 2089 AND 2093
              OR @codigo BETWEEN 2137 AND 2139
              OR @codigo BETWEEN 2200 AND 2207
              OR @codigo BETWEEN 2250 AND 2273
              OR @codigo BETWEEN 2275 AND 2306
              OR @codigo = 2362
              OR @codigo = 2364
              OR @codigo BETWEEN 2369 AND 2376
              OR @codigo = 2381
              OR @codigo BETWEEN 2385 AND 2391
              OR @codigo BETWEEN 2402 AND 2403
              OR @codigo = 2433
              OR @codigo = 2492
              OR @codigo BETWEEN 2497 AND 2500
              OR @codigo = 2509
              OR @codigo BETWEEN 2530 AND 2531
              OR @codigo = 2558
              OR @codigo BETWEEN 2561 AND 2562
              OR @codigo = 2620
              OR @codigo BETWEEN 2625 AND 2626
              OR @codigo BETWEEN 2631 AND 2632
              OR @codigo BETWEEN 2635 AND 2637
              OR @codigo = 2641
              OR @codigo BETWEEN 2672 AND 2673
              OR @codigo = 2677
              OR @codigo BETWEEN 2689 AND 2690
              OR @codigo = 2748
              OR @codigo BETWEEN 2753 AND 2757
              OR @codigo BETWEEN 2759 AND 2760
              OR @codigo = 2765
              OR @codigo BETWEEN 2786 AND 2787
              OR @codigo BETWEEN 2810 AND 2815
              OR @codigo = 2817
              OR @codigo = 2876
              OR @codigo = 2879
              OR @codigo BETWEEN 2881 AND 2884
              OR @codigo = 2893
              OR @codigo BETWEEN 2901 AND 2902
              OR @codigo BETWEEN 2914 AND 2915
              OR @codigo = 2946
              OR @codigo = 3008
              OR @codigo = 3021
              OR @codigo = 3072
              OR @codigo = 3076
              OR @codigo = 3132
              OR @codigo BETWEEN 3134 AND 3136
              OR @codigo BETWEEN 3142 AND 3144
              OR @codigo BETWEEN 3146 AND 3149
              OR @codigo BETWEEN 3157 AND 3158
              OR @codigo BETWEEN 3170 AND 3171
              OR @codigo = 3201
              OR @codigo = 3260
              OR @codigo = 3263
              OR @codigo = 3270
              OR @codigo BETWEEN 3276 AND 3277
              OR @codigo BETWEEN 3298 AND 3299
              OR @codigo BETWEEN 3328 AND 3329
              OR @codigo BETWEEN 3387 AND 3388
              OR @codigo BETWEEN 3393 AND 3396
              OR @codigo = 3405
              OR @codigo BETWEEN 3426 AND 3427
              OR @codigo = 3457
              OR @codigo = 3530
              OR @codigo BETWEEN 3538 AND 3540
              OR @codigo = 3542
              OR @codigo = 3633
              OR @codigo BETWEEN 3636 AND 3642
              OR @codigo BETWEEN 3655 AND 3662
              OR @codigo = 3761
              OR @codigo BETWEEN 3764 AND 3772
              OR @codigo BETWEEN 3784 AND 3790
              OR @codigo BETWEEN 3864 AND 3865
              OR @codigo = 3893
              OR @codigo = 3895
              OR @codigo = 3897
              OR @codigo BETWEEN 3953 AND 3966
              OR @codigo BETWEEN 3968 AND 3972
              OR @codigo BETWEEN 3974 AND 3975
              OR @codigo BETWEEN 3981 AND 3991
              OR @codigo BETWEEN 3993 AND 4028
              OR @codigo = 4038
              OR @codigo BETWEEN 4141 AND 4144
              OR @codigo BETWEEN 4146 AND 4151
              OR @codigo BETWEEN 4153 AND 4154
              OR @codigo BETWEEN 4157 AND 4158
              OR @codigo BETWEEN 4184 AND 4185
              OR @codigo BETWEEN 4190 AND 4192
              OR @codigo BETWEEN 4209 AND 4212
              OR @codigo = 4226
              OR @codigo BETWEEN 4229 AND 4230
              OR @codigo = 4237
              OR @codigo = 4253
              OR @codigo BETWEEN 4957 AND 4959
              OR @codigo BETWEEN 5906 AND 5908
              OR @codigo BETWEEN 5938 AND 5939
              OR @codigo BETWEEN 5970 AND 5971
              OR @codigo BETWEEN 6002 AND 6003
              OR @codigo BETWEEN 6068 AND 6069
              OR @codigo BETWEEN 6071 AND 6077
              OR @codigo = 6086
              OR @codigo BETWEEN 6089 AND 6099
              OR @codigo = 6109
              OR @codigo BETWEEN 6155 AND 6157
              OR @codigo = 6159
              OR @codigo BETWEEN 6277 AND 6278
              OR @codigo = 6313
              OR @codigo BETWEEN 6432 AND 6434
              OR @codigo BETWEEN 6439 AND 6440
              OR @codigo = 6450
              OR @codigo BETWEEN 6457 AND 6459
              OR @codigo BETWEEN 6679 AND 6680
              OR @codigo = 6683
              OR @codigo = 6742
              OR @codigo BETWEEN 6744 AND 6750
              OR @codigo = 6752
              OR @codigo = 6754
              OR @codigo BETWEEN 6757 AND 6764
              OR @codigo BETWEEN 6771 AND 6780
              OR @codigo = 6783
              OR @codigo BETWEEN 6832 AND 6845
              OR @codigo BETWEEN 6847 AND 6862
              OR @codigo BETWEEN 6912 AND 6915
              OR @codigo = 6964
              OR @codigo BETWEEN 6966 AND 6970
              OR @codigo = 6972
              OR @codigo = 6978
              OR @codigo BETWEEN 7019 AND 7027
              OR @codigo BETWEEN 7040 AND 7041
              OR @codigo BETWEEN 7074 AND 7077
              OR @codigo BETWEEN 7080 AND 7081
              OR @codigo BETWEEN 7083 AND 7085
              OR @codigo = 7142
              OR @codigo BETWEEN 7144 AND 7145
              OR @codigo = 7149
              OR @codigo BETWEEN 7151 AND 7153
              OR @codigo BETWEEN 7212 AND 7219
              OR @codigo BETWEEN 7222 AND 7223
              OR @codigo BETWEEN 7376 AND 7378
              OR @codigo BETWEEN 7380 AND 7392
              OR @codigo BETWEEN 7394 AND 7400
              OR @codigo = 7405
              OR @codigo = 7412
              OR @codigo BETWEEN 7416 AND 7417
              OR @codigo BETWEEN 7616 AND 7679
              OR @codigo BETWEEN 8400 AND 8412
              OR @codigo = 8417
              OR @codigo BETWEEN 8421 AND 8432
              OR @codigo BETWEEN 11503 AND 11505
              OR @codigo = 11647
              OR @codigo BETWEEN 11744 AND 11775
              OR @codigo BETWEEN 12330 AND 12333
              OR @codigo BETWEEN 12441 AND 12442
              OR @codigo = 42607
              OR @codigo BETWEEN 42612 AND 42621
              OR @codigo BETWEEN 42654 AND 42655
              OR @codigo BETWEEN 42736 AND 42737
              OR @codigo = 43010
              OR @codigo = 43014
              OR @codigo = 43019
              OR @codigo BETWEEN 43045 AND 43046
              OR @codigo = 43052
              OR @codigo BETWEEN 43204 AND 43205
              OR @codigo BETWEEN 43232 AND 43249
              OR @codigo = 43263
              OR @codigo BETWEEN 43302 AND 43309
              OR @codigo BETWEEN 43335 AND 43345
              OR @codigo BETWEEN 43392 AND 43394
              OR @codigo = 43443
              OR @codigo BETWEEN 43446 AND 43449
              OR @codigo BETWEEN 43452 AND 43453
              OR @codigo = 43493
              OR @codigo BETWEEN 43561 AND 43566
              OR @codigo BETWEEN 43569 AND 43570
              OR @codigo BETWEEN 43573 AND 43574
              OR @codigo = 43587
              OR @codigo = 43596
              OR @codigo = 43644
              OR @codigo = 43696
              OR @codigo BETWEEN 43698 AND 43700
              OR @codigo BETWEEN 43703 AND 43704
              OR @codigo BETWEEN 43710 AND 43711
              OR @codigo = 43713
              OR @codigo BETWEEN 43756 AND 43757
              OR @codigo = 43766
              OR @codigo = 44005
              OR @codigo = 44008
              OR @codigo = 44013
              OR @codigo = 64286
              OR @codigo BETWEEN 65024 AND 65039
              OR @codigo BETWEEN 65056 AND 65071 THEN N''
            WHEN @codigo = 48 THEN N'0'
            WHEN @codigo = 49 THEN N'1'
            WHEN @codigo = 50 THEN N'2'
            WHEN @codigo = 51 THEN N'3'
            WHEN @codigo = 52 THEN N'4'
            WHEN @codigo = 53 THEN N'5'
            WHEN @codigo = 54 THEN N'6'
            WHEN @codigo = 55 THEN N'7'
            WHEN @codigo = 56 THEN N'8'
            WHEN @codigo = 57 THEN N'9'
            WHEN @codigo = 65
              OR @codigo = 97
              OR @codigo BETWEEN 192 AND 197
              OR @codigo BETWEEN 224 AND 229
              OR @codigo BETWEEN 256 AND 261
              OR @codigo BETWEEN 461 AND 462
              OR @codigo BETWEEN 478 AND 481
              OR @codigo BETWEEN 506 AND 507
              OR @codigo BETWEEN 512 AND 515
              OR @codigo BETWEEN 550 AND 551
              OR @codigo BETWEEN 7680 AND 7681
              OR @codigo BETWEEN 7840 AND 7863
              OR @codigo = 8491 THEN N'A'
            WHEN @codigo = 66
              OR @codigo = 98
              OR @codigo BETWEEN 7682 AND 7687 THEN N'B'
            WHEN @codigo = 67
              OR @codigo = 99
              OR @codigo = 199
              OR @codigo = 231
              OR @codigo BETWEEN 262 AND 269
              OR @codigo BETWEEN 7688 AND 7689 THEN N'C'
            WHEN @codigo = 68
              OR @codigo = 100
              OR @codigo BETWEEN 270 AND 271
              OR @codigo BETWEEN 7690 AND 7699 THEN N'D'
            WHEN @codigo = 69
              OR @codigo = 101
              OR @codigo BETWEEN 200 AND 203
              OR @codigo BETWEEN 232 AND 235
              OR @codigo BETWEEN 274 AND 283
              OR @codigo BETWEEN 516 AND 519
              OR @codigo BETWEEN 552 AND 553
              OR @codigo BETWEEN 7700 AND 7709
              OR @codigo BETWEEN 7864 AND 7879 THEN N'E'
            WHEN @codigo = 70
              OR @codigo = 102
              OR @codigo BETWEEN 7710 AND 7711 THEN N'F'
            WHEN @codigo = 71
              OR @codigo = 103
              OR @codigo BETWEEN 284 AND 291
              OR @codigo BETWEEN 486 AND 487
              OR @codigo BETWEEN 500 AND 501
              OR @codigo BETWEEN 7712 AND 7713 THEN N'G'
            WHEN @codigo = 72
              OR @codigo = 104
              OR @codigo BETWEEN 292 AND 293
              OR @codigo BETWEEN 542 AND 543
              OR @codigo BETWEEN 7714 AND 7723
              OR @codigo = 7830 THEN N'H'
            WHEN @codigo = 73
              OR @codigo = 105
              OR @codigo BETWEEN 204 AND 207
              OR @codigo BETWEEN 236 AND 239
              OR @codigo BETWEEN 296 AND 304
              OR @codigo BETWEEN 463 AND 464
              OR @codigo BETWEEN 520 AND 523
              OR @codigo BETWEEN 7724 AND 7727
              OR @codigo BETWEEN 7880 AND 7883 THEN N'I'
            WHEN @codigo = 74
              OR @codigo = 106
              OR @codigo BETWEEN 308 AND 309
              OR @codigo = 496 THEN N'J'
            WHEN @codigo = 75
              OR @codigo = 107
              OR @codigo BETWEEN 310 AND 311
              OR @codigo BETWEEN 488 AND 489
              OR @codigo BETWEEN 7728 AND 7733
              OR @codigo = 8490 THEN N'K'
            WHEN @codigo = 76
              OR @codigo = 108
              OR @codigo BETWEEN 313 AND 318
              OR @codigo BETWEEN 7734 AND 7741 THEN N'L'
            WHEN @codigo = 77
              OR @codigo = 109
              OR @codigo BETWEEN 7742 AND 7747 THEN N'M'
            WHEN @codigo = 78
              OR @codigo = 110
              OR @codigo = 209
              OR @codigo = 241
              OR @codigo BETWEEN 323 AND 328
              OR @codigo BETWEEN 504 AND 505
              OR @codigo BETWEEN 7748 AND 7755 THEN N'N'
            WHEN @codigo = 79
              OR @codigo = 111
              OR @codigo BETWEEN 210 AND 214
              OR @codigo BETWEEN 242 AND 246
              OR @codigo BETWEEN 332 AND 337
              OR @codigo BETWEEN 416 AND 417
              OR @codigo BETWEEN 465 AND 466
              OR @codigo BETWEEN 490 AND 493
              OR @codigo BETWEEN 524 AND 527
              OR @codigo BETWEEN 554 AND 561
              OR @codigo BETWEEN 7756 AND 7763
              OR @codigo BETWEEN 7884 AND 7907 THEN N'O'
            WHEN @codigo = 80
              OR @codigo = 112
              OR @codigo BETWEEN 7764 AND 7767 THEN N'P'
            WHEN @codigo = 81
              OR @codigo = 113 THEN N'Q'
            WHEN @codigo = 82
              OR @codigo = 114
              OR @codigo BETWEEN 340 AND 345
              OR @codigo BETWEEN 528 AND 531
              OR @codigo BETWEEN 7768 AND 7775 THEN N'R'
            WHEN @codigo = 83
              OR @codigo = 115
              OR @codigo BETWEEN 346 AND 353
              OR @codigo = 383
              OR @codigo BETWEEN 536 AND 537
              OR @codigo BETWEEN 7776 AND 7785
              OR @codigo = 7835 THEN N'S'
            WHEN @codigo = 84
              OR @codigo = 116
              OR @codigo BETWEEN 354 AND 357
              OR @codigo BETWEEN 538 AND 539
              OR @codigo BETWEEN 7786 AND 7793
              OR @codigo = 7831 THEN N'T'
            WHEN @codigo = 85
              OR @codigo = 117
              OR @codigo BETWEEN 217 AND 220
              OR @codigo BETWEEN 249 AND 252
              OR @codigo BETWEEN 360 AND 371
              OR @codigo BETWEEN 431 AND 432
              OR @codigo BETWEEN 467 AND 476
              OR @codigo BETWEEN 532 AND 535
              OR @codigo BETWEEN 7794 AND 7803
              OR @codigo BETWEEN 7908 AND 7921 THEN N'U'
            WHEN @codigo = 86
              OR @codigo = 118
              OR @codigo BETWEEN 7804 AND 7807 THEN N'V'
            WHEN @codigo = 87
              OR @codigo = 119
              OR @codigo BETWEEN 372 AND 373
              OR @codigo BETWEEN 7808 AND 7817
              OR @codigo = 7832 THEN N'W'
            WHEN @codigo = 88
              OR @codigo = 120
              OR @codigo BETWEEN 7818 AND 7821 THEN N'X'
            WHEN @codigo = 89
              OR @codigo = 121
              OR @codigo = 221
              OR @codigo = 253
              OR @codigo = 255
              OR @codigo BETWEEN 374 AND 376
              OR @codigo BETWEEN 562 AND 563
              OR @codigo BETWEEN 7822 AND 7823
              OR @codigo = 7833
              OR @codigo BETWEEN 7922 AND 7929 THEN N'Y'
            WHEN @codigo = 90
              OR @codigo = 122
              OR @codigo BETWEEN 377 AND 382
              OR @codigo BETWEEN 7824 AND 7829 THEN N'Z'
            ELSE N' ' END;
        IF DATALENGTH(@letra) = 0
            SET @pos = @pos + 1; -- Una marca se elimina; no separa palabras.
        ELSE
        BEGIN
            IF @letra = N' '
                SET @separador = 1;
            ELSE
            BEGIN
                IF @separador = 1 AND DATALENGTH(@resultado) > 0
                    SET @resultado = @resultado + N' ';
                SET @resultado = @resultado + @letra;
                SET @separador = 0;
            END;
            SET @pos = @pos + 1;
        END;
    END;
    RETURN @resultado;
END
-- END GENERATED NORMALIZAR CARRERA
GO

-- OUTPUT INTO participa en la misma sentencia y transaccion que UPDATE.
-- Si falla cualquiera de los dos, no queda ningun cambio ni respaldo parcial.
-- No hay una ventana entre leer el respaldo y escribir la carrera.
-- SQL dinamico evita enlazar p.carrera cuando el guard activa NOEXEC.
EXEC sp_executesql N'
UPDATE p
SET carrera = dbo.fn_NormalizarCarrera(p.carrera)
OUTPUT deleted.id_participante_PK, deleted.carrera, inserted.carrera
    INTO dbo.participante_carrera_respaldo
        (id_participante_FK, carrera_original, carrera_normalizada)
FROM dbo.participante AS p
WHERE p.carrera IS NOT NULL
  AND CONVERT(VARBINARY(MAX), p.carrera)
      <> CONVERT(VARBINARY(MAX), dbo.fn_NormalizarCarrera(p.carrera));
PRINT N''Filas normalizadas y respaldadas: '' + CAST(@@ROWCOUNT AS NVARCHAR(16));';
GO

SET NOEXEC OFF;
GO
