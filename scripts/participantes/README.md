# Ordenamiento por estado de notificación: migración y despliegue

## Qué cambia

La tabla de participantes puede ordenar por el estado de la campana:

- Ascendente: notificaciones pendientes, enviadas y filas sin campana (menos de
  30 horas aprobadas).
- Descendente: el orden inverso.

La aplicación usa `dbo.SelectParticipantesPaginados`, por lo que las bases ya
existentes necesitan actualizar ese procedimiento antes de desplegar el código.
La migración no modifica los datos de participantes.

## Orden requerido para cada ambiente

1. Seleccionar la base correcta (`METICS`) y ejecutar
   **`scripts/participantes/001_ordenamiento_estado_notificacion.sql`** antes de
   desplegar la aplicación.
2. Desplegar la aplicación.
3. Abrir `Participante/VerParticipantes` y comprobar el orden ascendente y
   descendente de la campana.

Ejemplo con autenticación integrada:

```powershell
sqlcmd -S SERVIDOR -d METICS -E -b -i scripts/participantes/001_ordenamiento_estado_notificacion.sql
```

Usar el mecanismo de credenciales del ambiente si no emplea autenticación
integrada. No guardar contraseñas en el repositorio ni en este documento.

**Recordatorio de deploy:** ejecutar esta migración en cada base destino antes
de publicar el código que envía `estadoNotificacion`, tanto en desarrollo como
en producción. Si se despliega primero la aplicación, la tabla seguirá usando
el orden predeterminado del procedimiento anterior.

## Verificación

Ejecutar en `METICS`; el resultado debe ser `OK`:

```sql
SELECT CASE
    WHEN OBJECT_DEFINITION(OBJECT_ID(N'dbo.SelectParticipantesPaginados'))
         LIKE N'%estadoNotificacion%'
    THEN N'OK'
    ELSE N'PENDIENTE'
END AS EstadoMigracion;
```

El script usa `CREATE OR ALTER PROCEDURE`, es reejecutable y requiere que la
base ya tenga la tabla `dbo.participante` y las columnas usadas por el listado.
En instalaciones nuevas, el procedimiento del bloque de participantes de
`db_scripts/db_METICS.sql` ya incluye este ordenamiento. No ejecutar ese archivo
completo sobre una base existente.

## Reversión

La migración es compatible con versiones anteriores de la aplicación. Si se
revierte el código, el procedimiento puede permanecer instalado: la versión
anterior no envía `estadoNotificacion` y conserva su orden habitual.
