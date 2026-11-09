# Validación de la pantalla de detalle del participante

Fecha: 2026-11-08. Entorno: aplicación local del usuario en `https://localhost:7201`,
sesión de administrador, navegador integrado. Revisión efectuada sobre los cambios
locales del catálogo de medallas y el refactor visual de `refactor-ui`.

## Alcance y aislamiento

Se inspeccionó la pantalla abierta por el usuario y se usó un participante temporal
`QA Interfaz Temporal Prueba` con una inscripción temporal en un módulo de 10 horas
para las operaciones de escritura. No se modificaron las medallas, horas,
calificaciones ni los datos del participante original.

Al finalizar se retiraron la inscripción, la medalla de prueba y el participante.
Se comprobó que no quedaran registros temporales en `usuario`, `participante`,
`inscripcion` ni `medallas`. Se verificó además que la eliminación no alterara el
registro original: el trigger `TR_EliminarParticipante` elimina al usuario propietario,
por lo que la limpieza aisló el registro QA de ese propietario antes de eliminarlo.
No se eliminaron ni desactivaron medallas del catálogo.

## Resultados observados en el navegador

| Prueba | Resultado |
| --- | --- |
| Catálogo de selección | 28 medallas y ninguna imagen rota. |
| Contraer/expandir «Asignar medallas» | Correcto. |
| Buscar por nombre | Correcto; «Ciberseguridad» muestra una coincidencia. |
| Búsqueda sin coincidencias | Oculta las tarjetas; no muestra un mensaje de lista vacía. Mejora de UX pendiente. |
| Mantener selección al cambiar el filtro | Correcto: conserva una medalla oculta y permite seleccionar otra visible. |
| Asignar dos medallas | Ambas aparecen en «Medallas del participante». |
| Confirmación de asignación | Toast visible. |
| Repetir una asignación | No duplica la medalla. |
| Contraer/expandir medallas otorgadas | Correcto. |
| Cancelar el modal de desasignación | Conserva ambas medallas. |
| Confirmar desasignación | Retira únicamente la medalla indicada y muestra toast. |
| Contraer/expandir módulo | Correcto. |
| Asociación de campos con formulario | Horas, calificación y botón pertenecen al formulario esperado en el DOM renderizado. |
| Guardar 5 horas y nota 85 | Persiste ambos valores y muestra estado «Incompleto». |
| Guardar 10 horas y nota 90 | Persiste ambos valores, muestra «Aprobado» y total de horas aprobadas 10. |
| Horas negativas | El navegador bloquea el envío por `min=0`. |
| Nota mayor de 100 | El navegador bloquea el envío por `max=100`. |
| Guardar ceros | Problema detectado inicialmente y corregido después: cero se guarda; vacío conserva el dato anterior. Ver pruebas de calificaciones. |
| Horas superiores al módulo | Corregido después de la revisión: se rechaza con error rojo sin guardar el otro campo. |
| Asignar sin selección | **Corregido tras la revisión:** muestra un aviso amarillo solicitando seleccionar al menos una medalla; no informa éxito ni crea asignaciones. |
| Carrusel mediante teclado | Se desplaza horizontalmente; no desborda el documento. |
| Escritorio | Sin desbordamiento horizontal global ni errores de consola observados. |
| Pantalla estrecha (390 px) | Se detectó y corrigió una regresión de ancho de celdas; se repitió la prueba satisfactoriamente. |

Los totales siguen la regla existente: se suman horas aprobadas de inscripciones
en estado `Aprobado` y horas matriculadas en estado `Inscrito`; las inscripciones
`Incompleto` no se incluyen en esos acumulados. No se alteró esa regla.

## Regresión corregida durante la revisión

En la tabla de datos personales, el estilo móvil convertía `tbody` y filas en bloques,
pero mantenía el layout de tabla y los anchos porcentuales de sus etiquetas. El
navegador reducía el cuerpo a un cuarto del ancho y algunas celdas a unos 25 px.

Se corrigió exclusivamente en el CSS móvil de `.participant-details-card`: la
tabla, cabecera y cuerpo se muestran como bloques, con cada campo al 100%.
Se comprobaron nuevamente las 10 filas y sus valores. En el viewport de 390 px,
tabla/cuerpo/celdas ocuparon los 291 px disponibles dentro de la tarjeta.
La tabla de módulos conserva scroll interno (1050 px de contenido en un contenedor
de 325 px), sin generar scroll horizontal global.

## Problemas anteriores detectados

1. `ParticipanteController.AsignarMedallas` establece el mensaje de éxito aunque
   `selectedMedallas` esté vacío. La misma condición existe en `HEAD` anterior a
   las modificaciones del catálogo. **Corregido:** ahora se retorna con
   `warningMessage` antes de consultar o asignar medallas y se elimina cualquier
   mensaje de éxito pendiente. La respuesta HTTP se comprobó con un envío vacío.
2. `CalificacionesController.SubirHorasAprobadasYCalificacion` solo procesa horas
   y notas cuando son mayores de cero. Por tanto, no permite corregir una nota
   a cero, aunque el input lo permite. Tampoco devuelve error al exceder las
   horas del módulo; termina con un mensaje de éxito sin modificar ese valor.
   El controlador no fue modificado por este refactor; su último cambio en el
   historial consultado antes de la corrección era `36e78f3`. **Corregido después:**
   se agregó guardado transaccional con campos opcionales, ceros válidos,
   validación completa y avisos sin cambios. Ver `scripts/calificaciones/README.md`.
3. El redirect de asignación/desasignación no conserva `breadcrumbOrigin`.
   Al volver al detalle, el breadcrumb puede pasar de «Personas / Participantes»
   a «Inscripciones». Las acciones no se pierden, pero el contexto de navegación
   cambia. El redirect de asignación ya existía así antes de esta revisión.

La selección vacía y los errores de guardado se corrigieron a petición del usuario
después de la prueba de regresión. El contexto del breadcrumb sigue pendiente.
La nueva clasificación de toasts usa verde para éxito, rojo para errores y amarillo
para avisos. Las 30 pruebas de guardado son adicionales a la revisión interactiva.

## Evidencia y límites

Se guardaron capturas locales en `webMetics/bin/qa-participante-movil.png` y
`webMetics/bin/qa-participante-escritorio.png` (archivos ignorados por Git).
El viewport temporal se restableció y se cerró la pestaña del registro QA.

Esta revisión comprueba los flujos indicados como administrador. No certifica
todos los roles, todos los módulos ni toda la aplicación. Los 25 checks de
integración del catálogo de medallas ejecutados en una base temporal se documentan
por separado en `README.md`; no deben confundirse con esta prueba interactiva.
