# Normalizacion del formato de carrera

Estos scripts dejan `participante.carrera` en un solo formato: mayusculas, sin
tildes y sin puntuacion. Es el mismo formato que ya tienen el catalogo de
`webMetics/wwwroot/data/dataAreas.json` y `CarreraResolver` en C#, de modo que
los dos escritores de la columna --- el formulario de registro y
`CompletarCarreraYAreas` --- guardan lo mismo.

## Orden

1. `01-aplicar_normalizacion_carreras.sql` --- respalda, crea la funcion y normaliza.
2. `02-verificar_normalizacion_carreras.sql` --- confirma que no quedo ninguna fila fuera de formato.
3. `03-revertir_normalizacion_carreras.sql` --- solo si hay que deshacer.

`01` es idempotente: re-ejecutarlo no vuelve a respaldar ni toca filas que ya
esten normalizadas.

## Que hay que saber antes de correrlo

- **La normalizacion pierde informacion.** `Bachillerato en Economia Agricola y
  Agronegocios (Desconcentrada)` queda `BACHILLERATO EN ECONOMIA AGRICOLA Y
  AGRONEGOCIOS DESCONCENTRADA`: se van los parentesis, las comas y los dos
  puntos. La tabla `participante_carrera_respaldo` es la unica vuelta atras.
- `01` aborta si `participante.carrera` no existe. Una base en un esquema
  anterior necesita primero la columna.
- La funcion `dbo.fn_NormalizarCarrera` queda instalada, porque `02` y
  cualquier corrida futura la necesitan.
- Las comparaciones usan `Latin1_General_BIN2` a proposito. Con una colacion
  acento-insensible la base creeria que el valor sin normalizar ya es igual al
  normalizado, y el `UPDATE` no haria nada.

## Equivalencia con el codigo

`dbo.fn_NormalizarCarrera` replica `CarreraResolver.Normalizar`. El mapa de
diacriticos se derivo del bloque Latin-1 Supplement de Unicode aplicando la
misma regla que usa C# --- NFD y descarte de las marcas sin espacio ---, no a
mano. Si esa funcion de C# cambia, hay que actualizar la de SQL.
