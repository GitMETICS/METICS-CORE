# Guardado de horas y calificación en el detalle del participante

## Comportamiento

El formulario POST `Calificaciones/SubirHorasAprobadasYCalificacion` distingue
campos vacíos (conservar valor anterior) de un cero explícito (guardar cero).
Solo acepta enteros: horas entre 0 y la duración del módulo; notas entre 0 y 100.
Valida ambos campos antes de escribir. Un dato incorrecto impide guardar el otro.

`CalificacionParticipanteService` guarda en una transacción la inscripción,
su estado, la nota y, si cambian las horas, los acumulados del participante.
Conserva la regla existente: las inscripciones aprobadas suman horas aprobadas,
las inscritas suman horas matriculadas y las incompletas no suman esos acumulados.
El guardado se serializa por participante para mantener los totales coherentes
si se editan distintos módulos de la misma persona simultáneamente.

No hay migración ni seed adicional. Se requiere desplegar la aplicación actualizada
y reiniciar el proceso para cargar el controlador y el servicio nuevos.
Este cambio corresponde al formulario del detalle; no modifica los endpoints AJAX
utilizados por otras pantallas ni las importaciones masivas.

## Notificaciones

El componente compartido `_MeticsToast` admite:

| Tipo | Color | Casos |
| --- | --- | --- |
| `SuccessMessage` | Verde | Cambios realmente guardados; medallas nuevas asignadas. |
| `ErrorMessage` | Rojo | Datos fuera de rango/no numéricos, falta de permisos, inscripción inexistente o fallo SQL. |
| `WarningMessage` | Amarillo | Formulario vacío, valores iguales, ninguna medalla seleccionada o medallas ya asignadas. |

Los POST afectados limpian notificaciones anteriores antes de establecer la nueva.
La vista de detalle recupera también `TempData["warningMessage"]` para mostrarla.

## Pruebas

El proyecto `tests/CalificacionesChecks.csproj` ejecuta 30 comprobaciones contra
una base SQL temporal que crea y elimina automáticamente. Solo acepta un servidor
local (`localhost` o `127.0.0.1`); no modifica los datos de la base METICS.
Se requiere permiso para crear y eliminar esa base temporal.

Desde la raíz del repositorio, usando una conexión proporcionada por el entorno:

```powershell
$env:METICS_TEST_CONNECTION = 'CONEXION_SQL_LOCAL_DEL_ENTORNO'
dotnet build scripts/calificaciones/tests/CalificacionesChecks.csproj -o webMetics/bin/guardar-tests-review -p:UseAppHost=false
dotnet webMetics/bin/guardar-tests-review/CalificacionesChecks.dll
Remove-Item Env:METICS_TEST_CONNECTION
```

La salida separada permite ejecutar las pruebas sin sobrescribir el ejecutable
abierto por Visual Studio. No guardar contraseñas ni cadenas reales en este archivo.

Cobertura: ceros, campos vacíos, valores iguales, edición de un único campo,
límites/negativos, registros inexistentes, acumulados de varios módulos, concurrencia,
rollback por fallo SQL y clasificación de mensajes en el controlador.
