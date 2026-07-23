# Paginacion backend de inscripciones

## Objetivo

Agregar paginacion, busqueda y ordenamiento en servidor para el listado administrativo de inscripciones.
La tabla inicia con 50 registros por pagina.

La migracion es no destructiva: no elimina ni modifica inscripciones, participantes u otros datos existentes.

## Orden de despliegue

La migracion de base de datos debe ejecutarse antes de desplegar la aplicacion. Si se despliega primero la
aplicacion, la tabla mostrara un error AJAX porque el procedimiento almacenado aun no existira.

### Desarrollo o pruebas

Ejecutar sobre la base `METICS`, en este orden:

1. `Dev/05-aplicar_paginacion_inscripciones.sql`
2. `Dev/06-verificar_paginacion_inscripciones.sql`

### Produccion

1. Crear y comprobar un respaldo de la base de datos.
2. Ejecutar `Prod/04-aplicar_paginacion_inscripciones.sql` sobre la base `METICS`.
3. Ejecutar `Prod/05-verificar_paginacion_inscripciones.sql`.
4. Confirmar que el procedimiento y los dos indices aparezcan como `OK`.
5. Desplegar la aplicacion.
6. Iniciar sesion nuevamente y realizar las pruebas de humo.

Los scripts de aplicacion son idempotentes y se pueden volver a ejecutar si fuera necesario.

## Importante

- No ejecutar `db_scripts/db_METICS.sql` sobre una base existente.
- El bloque agregado a `db_METICS.sql` esta comentado y solo sirve para preparar futuras instalaciones limpias.
- No incluir el `appsettings.json` local en el despliegue.
- Este cambio de seguridad no requiere columnas, tablas ni migraciones adicionales.
- Las claves de ASP.NET Data Protection deben persistir entre reinicios y compartirse entre instancias.
- Las sesiones creadas antes del despliegue deberan iniciar sesion nuevamente para obtener `METICS.AUTH`.

## Pruebas de humo

1. Iniciar sesion como administrador y abrir `Inscripcion/VerInscripciones`.
2. Confirmar que la tabla cargue 50 registros por defecto.
3. Probar tamanos de pagina 5, 10, 25 y 50.
4. Probar busqueda por participante, correo, unidad academica, modulo, estado y horas.
5. Probar ordenamiento en ambas direcciones para todas las columnas habilitadas.
6. Verificar que PDF, Word y Excel respeten el filtro visible.
7. Verificar seleccion de participantes al cambiar de pagina.
8. Usar datos de prueba para validar asignacion de medallas, importacion y eliminacion.
9. Sin iniciar sesion, confirmar que el endpoint paginado responda `401`.
10. Con un usuario no administrador, confirmar que el endpoint y los POST administrativos respondan `403`.

## Reversion

Si la aplicacion debe revertirse, desplegar la version anterior. El procedimiento y los indices pueden permanecer
en la base porque no cambian datos ni interfieren con la version anterior. No eliminar objetos durante una
reversion de emergencia.
