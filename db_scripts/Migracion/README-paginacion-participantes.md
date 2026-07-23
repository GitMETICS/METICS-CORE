# Paginacion backend de participantes

## Objetivo

Agregar paginacion, busqueda y ordenamiento en servidor para
`Participante/VerParticipantes`. La tabla inicia con 50 registros por pagina.

La migracion es no destructiva: no elimina ni modifica participantes,
usuarios, inscripciones u otros datos existentes.

## Orden de despliegue

La migracion debe ejecutarse antes de desplegar la aplicacion. Si la aplicacion
se despliega primero, DataTables mostrara un error AJAX porque el procedimiento
almacenado aun no existira.

### Desarrollo o pruebas

1. Ejecutar `Dev/07-aplicar_paginacion_participantes.sql` sobre la base `METICS`.
2. Ejecutar `Dev/08-verificar_paginacion_participantes.sql`.

### Produccion

1. Crear y comprobar un respaldo de la base de datos.
2. Ejecutar `Prod/06-aplicar_paginacion_participantes.sql` sobre `METICS`.
3. Ejecutar `Prod/07-verificar_paginacion_participantes.sql`.
4. Confirmar que el procedimiento y el indice aparezcan como `OK`.
5. Desplegar la aplicacion.
6. Iniciar sesion nuevamente y realizar las pruebas de humo.

Los scripts son idempotentes y se pueden volver a ejecutar.

## Importante

- No ejecutar `db_scripts/db_METICS.sql` sobre una base existente.
- El bloque de participantes agregado a `db_METICS.sql` esta comentado y solo
  sirve para futuras instalaciones limpias.
- La migracion reutiliza `IX_participante_listado_inscripciones`; si no existe,
  lo crea sin modificar datos.
- No incluir el `appsettings.json` local en el despliegue.

## Pruebas de humo

1. Iniciar sesion como administrador y abrir `Participante/VerParticipantes`.
2. Confirmar que la tabla cargue 50 registros por defecto.
3. Probar tamanos de pagina 5, 10, 25 y 50.
4. Buscar por unidad academica, nombre, apellidos, correo y horas.
5. Ordenar unidad academica, nombre, correo y ambas columnas de horas.
6. Confirmar que las campanas y el menu de acciones funcionen tras cambiar de pagina.
7. Confirmar que PDF, Word y ambos Excel respeten el filtro aplicado.
8. Probar agregar, importar y eliminar con datos de prueba.
9. Sin iniciar sesion, confirmar que el endpoint paginado responda `401`.
10. Con un usuario no administrador, confirmar que responda `403`.

## Reversion

Si la aplicacion debe revertirse, desplegar la version anterior. El procedimiento
y el indice pueden permanecer en la base porque no cambian datos ni interfieren
con la version anterior.
