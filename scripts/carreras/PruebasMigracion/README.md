# Pruebas de regresión de la migración de carreras

Ejecutor de integración .NET 8 contra SQL Server. Dev y Prod se ejecutan en bases
nuevas con nombres `MeticsCarrerasTest_<GUID>`. El ejecutor elimina únicamente las
bases que acaba de crear, también si una prueba falla, y termina con código de
salida distinto de cero ante un fallo.

## Preparación y ejecución

Usar un servidor SQL **desechable**, sin montar volúmenes de bases originales.
Se requieren permisos para crear/eliminar bases y consultar
`sys.dm_exec_requests` (las pruebas verifican el bloqueo concurrente real).

No se lee `appsettings.json`. Ambas variables son obligatorias: la conexión
explícita y el valor de `SELECT @@SERVERNAME` del servidor de pruebas. Si el nombre
no coincide, el ejecutor termina antes de crear una base.

Desde la raíz del repositorio, después de definir las variables en la sesión:

```powershell
# METICS_TEST_SQL_CONNECTION: conexión al servidor desechable.
# METICS_TEST_SQL_SERVER: su @@SERVERNAME exacto.
dotnet run --project scripts/carreras/NormalizacionSql -- --check
dotnet run --project scripts/carreras/PruebasMigracion
```

Mantener las credenciales en el entorno local, sin agregarlas al repositorio.
SQL Server 2022 fue la versión usada para validar esta corrección.

## Cobertura

- Guardia ante esquema sin columna carrera.
- Tildes precompuestas/descompuestas, puntuación, caracteres extendidos, emojis,
  NULL, cadena vacía, espacios finales y longitud máxima de 512 caracteres.
- Paridad con el normalizador C# real: caracteres BMP válidos entre letras ASCII,
  500 cadenas mixtas reproducibles y todas las carreras del catálogo en formas
  normal y descompuesta. Se usa una base CI_AI_SC para detectar dependencias de
  colación y problemas al recorrer UTF-16.
- Respaldo del valor original y del valor escrito, e idempotencia.
- Ejecución del verificador SQL: ningún `FAIL` ni `MISSING` después de migrar.
- Reversión exacta, incluidas diferencias de espacios y NULL; no modifica
  ediciones posteriores distintas y conserva el historial.
- Un trigger de prueba que falla después del UPDATE: comprueba que tanto el dato
  como el respaldo se revierten.
- Un escritor concurrente que mantiene un bloqueo hasta que la migración queda
  esperando: verifica que se respalda el último valor realmente reemplazado.
- Dos ejecuciones de la migración solapadas, sin duplicación de respaldos.
- Borrado de un participante sin perder su respaldo.
- Actualización de los dos esquemas anteriores de respaldo, conservando el
  resultado del normalizador antiguo; tratamiento conservador si falta la
  función antigua.

No son pruebas de carga ni una validación del esquema/datos de producción.
