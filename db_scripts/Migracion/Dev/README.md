# Normalización del formato de carrera

Los scripts de Dev y Prod normalizan `participante.carrera` con las reglas de
`webMetics/Services/CarreraResolver.cs`: mayúsculas, eliminación de diacríticos,
caracteres ASCII alfanuméricos y espacios simples entre palabras.

## Ejecución

1. Probar primero sobre una copia. En producción, tomar un respaldo completo.
2. Ejecutar `01-aplicar_normalizacion_carreras.sql` en la base elegida.
3. Ejecutar `02-verificar_normalizacion_carreras.sql`. Los casos deben dar `OK`
   y ambos contadores de formato deben dar cero.
4. Si se necesita deshacer, ejecutar `03-revertir_normalizacion_carreras.sql`.

Los archivos son UTF-8. Con sqlcmd usar `-f 65001 -I -b`, indicar explícitamente
el servidor y la base, y detenerse ante errores. `01` aborta si falta la columna
`participante.carrera`; no reemplaza las migraciones anteriores del esquema.
No ejecutar simultáneamente instalaciones o actualizaciones del esquema.

## Respaldo atómico e historial

`UPDATE ... OUTPUT deleted ... INTO` guarda en la misma sentencia el valor
original y el valor escrito (`carrera_normalizada`). Si falla la actualización
o el respaldo, la sentencia se revierte completa. No hay una lectura de respaldo
separada que pueda quedar desactualizada por una escritura concurrente.

Solo se modifican y respaldan filas que cambian. La comparación usa VARBINARY
para incluir espacios finales, que incluso una comparación textual BIN2 ignora.
Una segunda ejecución sobre datos normalizados no agrega respaldos. Si después
entra otro valor sin normalizar, una nueva ejecución guarda un nuevo respaldo.
Las escrituras posteriores a la sentencia quedan para la próxima ejecución;
no se instala un trigger que normalice futuras escrituras externas.

El respaldo no tiene FK a participante, por lo que sobrevive a sus borrados.
Conservar esta tabla: la normalización elimina información y no se puede invertir
sin los valores originales. El respaldo completo sigue siendo necesario.

## Reversión y versiones anteriores

`03` toma el último respaldo de cada participante y restaura solo si el valor
actual coincide exactamente con el valor que escribió esa migración. Compara
espacios finales y distingue NULL de una cadena vacía. Las filas con un valor
diferente se conservan y se muestran al final para revisión. Una edición que
escriba exactamente el mismo valor es indistinguible de la escritura original.
La reversión no depende de la versión actual de `fn_NormalizarCarrera`.

`01` actualiza tanto el esquema de una fila por participante como el historial
anterior. Antes de reemplazar la función SQL antigua, calcula con ella el valor
escrito de los respaldos antiguos. Este paso conserva la posibilidad de revertir
los resultados de aquella versión aunque su normalización fuera diferente.
Si falta la función antigua, no adivina: deja el valor escrito en NULL, avisa y
`03` omite esos respaldos. Revisarlos con el respaldo completo. No vuelve a llenar
estos NULL usando una función nueva en ejecuciones posteriores.

Esta actualización no reconstruye automáticamente nombres dañados por una
migración anterior: un valor como `DISEN O` ya cumple el formato ASCII. Para
recuperar su significado se necesita consultar el respaldo original.

## Unicode y mantenimiento

El bloque marcado como generado en `01` se obtiene ejecutando el mismo
`CarreraResolver` que usa la aplicación. Incluye letras precompuestas fuera de
Latin-1 y marcas combinantes, y no depende del UPPER ni de la colación de SQL.
La función recorre unidades UTF-16 sobre la columna NVARCHAR(512).

Desde la raíz del repositorio:

```powershell
dotnet run --project scripts/carreras/NormalizacionSql
dotnet run --project scripts/carreras/NormalizacionSql -- --check
```

Regenerar y revisar el diff si cambia `CarreraResolver` o la versión de .NET y sus
reglas Unicode. No editar el bloque generado a mano. El resto de los scripts
sigue siendo SQL autónomo: no requiere instalar .NET en el servidor de base de
datos ni habilitar SQL CLR.

Las pruebas en [PruebasMigracion](../../../scripts/carreras/PruebasMigracion/README.md)
comparan SQL con C#, verifican ambos ambientes y prueban concurrencia, fallos,
idempotencia, reversión y actualización de respaldos anteriores.
