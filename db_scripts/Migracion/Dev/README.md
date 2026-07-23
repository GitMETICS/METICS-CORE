# Prueba de scripts de migracion

Estos scripts se deben probar sobre una copia de la base de datos, no sobre la base real.

## Orden recomendado

1. Crear un respaldo de la base actual con `01-respaldar_base.sql`.
2. Restaurar ese respaldo en un segundo servidor o contenedor SQL Server.
3. Ejecutar `02-revertir_a_base_inicial.sql` sobre la copia para simular el estado anterior a la migracion.
4. Ejecutar `03-aplicar_migracion.sql` sobre la copia.
5. Ejecutar `04-verificar_migracion.sql` para validar que la migracion quedo aplicada correctamente.

## Paginacion de inscripciones

Para probar la paginacion backend sobre una base que ya tiene las migraciones anteriores:

1. Ejecutar `05-aplicar_paginacion_inscripciones.sql`.
2. Ejecutar `06-verificar_paginacion_inscripciones.sql`.

Estos dos scripts son idempotentes y no eliminan ni modifican registros existentes.

## Notas

- Ajustar el nombre de la base y la ruta del respaldo segun el servidor SQL disponible.
- El script `02-revertir_a_base_inicial.sql` elimina datos de campos nuevos, por eso solo debe usarse en una copia de prueba.
- La verificacion debe mostrar los campos, tabla intermedia, restriccion y parametros esperados como correctos.

## Paginacion de participantes

Para probar el listado administrativo de participantes:

1. Ejecutar `07-aplicar_paginacion_participantes.sql`.
2. Ejecutar `08-verificar_paginacion_participantes.sql`.

La migracion crea el procedimiento paginado y reutiliza el indice de participantes si ya existe.
No elimina ni modifica participantes u otros registros.
