# Catálogo de medallas: migración y despliegue

## Qué cambia

Antes, `dbo.medallas` mezclaba el catálogo con los reconocimientos otorgados. La lista
disponible dependía de `SELECT DISTINCT` sobre las asignaciones. Subir una imagen
también insertaba una asignación al administrador.

Ahora:

- `dbo.catalogo_medallas`: nombre de archivo único y estado `activa`.
- `dbo.medallas`: conserva las asignaciones existentes a participantes.
- Los selectores y el catálogo consultan medallas activas cuya imagen exista.
- Las vistas resuelven las imágenes desde `WebRootPath`, sin depender del directorio
  de trabajo desde el que IIS, el servicio o `dotnet` inicien la aplicación.
- El arranque sincroniza las imágenes nuevas de `wwwroot/images/medallas`.
- Subir una imagen la registra sin otorgarla a nadie.
- Editar con otro nombre conserva las asignaciones y desactiva el nombre anterior.
- Eliminar del catálogo desactiva la medalla y retira sus asignaciones, como la
  eliminación anterior. La imagen se conserva; el seed no reactiva el registro.
- Desasignar a un participante no elimina la medalla del catálogo.

No cambia el esquema de `dbo.medallas`, las rutas ni los formularios públicos.
No se ejecuta `dotnet ef database update`: es una migración SQL explícita.

## Orden obligatorio del deploy

1. Detener/drenar todas las instancias de la versión anterior. Evitar versiones
   mezcladas: la aplicación anterior todavía obtiene el catálogo de las asignaciones.
2. Respaldar la base de datos **y** la carpeta de imágenes. Conservar los archivos
   subidos en producción; no sustituir la carpeta por una copia que los borre.
3. En la base correcta, ejecutar **`scripts/medallas/001_catalogo_medallas.sql`**
   con SSMS o con el mecanismo SQL del deploy. Ejemplo con autenticación integrada:

   ```powershell
   sqlcmd -S SERVIDOR -d BASE_METICS -E -b -i scripts/medallas/001_catalogo_medallas.sql
   ```

   Usar el mecanismo de credenciales del entorno si no emplea autenticación
   integrada. No incorporar contraseñas al repositorio ni a este documento.
4. Publicar la nueva versión y verificar que incluya las imágenes PNG/JPG/JPEG en
   `wwwroot/images/medallas`. El SDK Web publica los archivos de `wwwroot`.
5. Iniciar la aplicación. Antes de aceptar solicitudes, se ejecuta el seed de
   imágenes. Buscar el log: **`Catálogo de medallas sincronizado: N imágenes nuevas registradas.`**
6. Verificar el catálogo y ambos selectores de asignación (individual y masivo).
   Asignar/desasignar una medalla a un participante de prueba y comprobar su perfil.

**La migración debe ejecutarse antes del primer arranque de esta versión.** Si
falta la tabla, la aplicación detiene el inicio con la ruta del script; no intenta
crear tablas usando las credenciales normales de ejecución.

El script usa una transacción y es reejecutable. Copia los nombres históricos al
catálogo, sin modificar sus asignaciones ni reactivar registros ya inactivos.
Rechaza nombres históricos de más de 450 caracteres para revisarlos antes de migrar.
No puede distinguir asignaciones legítimas de las que antiguamente se generaron al
subir imágenes: se conservan todas; cualquier limpieza requiere revisión manual.

## Seed, permisos y archivos

- Habilitado por defecto en cada arranque. Se puede desactivar con
  `Medallas__SincronizarAlIniciar=false` (o `Medallas:SincronizarAlIniciar` en
  configuración). Esto **no** sustituye la migración.
- Solo se registran archivos directos de la carpeta, con extensión `.png`, `.jpg`
  o `.jpeg`, sin distinguir mayúsculas en la extensión. No se recorren subcarpetas.
- No duplica nombres ni crea asignaciones. Los nombres inactivos permanecen
  inactivos, incluso si la imagen vuelve a incluirse en otro despliegue.
- Para reactivar deliberadamente una medalla, subirla desde el formulario. No
  borrar su registro de baja: hacerlo permitiría que el próximo seed la restaure.
- Una imagen ausente no elimina el catálogo ni sus premios, pero deja de ofrecerse
  para nuevas asignaciones. Restaurar el archivo con su nombre exacto.
- En Linux, respetar mayúsculas/minúsculas de los nombres. El seed rechaza archivos
  que solo difieran en ellas; un reemplazo no permite cambiar únicamente el casing.
- La identidad de la aplicación requiere SELECT/INSERT/UPDATE en el catálogo y
  los permisos de asignación ya existentes. La migración requiere permiso CREATE
  TABLE. Se utiliza `sys.sp_getapplock` para coordinar seeds y operaciones de catálogo
  concurrentes entre instancias; no requiere que la aplicación sea propietaria de la BD.
- Para subir/reemplazar imágenes, la carpeta debe ser persistente y escribible. En
  varias réplicas, compartir el mismo almacenamiento de imágenes y publicar los
  archivos antes de iniciar las réplicas. El bloqueo SQL no distribuye archivos.
- La edición conserva el archivo anterior para recuperación. La baja del catálogo
  tampoco borra archivos físicos. La limpieza de archivos es una operación manual
  posterior al respaldo, no parte del arranque.
- Si falla SQL al registrar una subida/edición, se restaura el archivo reemplazado
  o se retira el archivo nuevo. Un corte abrupto del proceso entre disco y commit
  requiere revisar ambas fuentes: SQL y filesystem no comparten una transacción.

## Comprobaciones SQL

Guardar el conteo de asignaciones antes de la migración y compararlo después de
migrar y del primer arranque: debe ser idéntico si no hay actividad concurrente.

```sql
SELECT COUNT(*) AS asignaciones FROM dbo.medallas;
SELECT activa, COUNT(*) AS medallas FROM dbo.catalogo_medallas GROUP BY activa;
SELECT nombre_medalla FROM dbo.catalogo_medallas WHERE activa = 1 ORDER BY nombre_medalla;
SELECT m.nombre_medalla
FROM dbo.medallas AS m
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.catalogo_medallas AS c WHERE c.nombre_medalla = m.nombre_medalla
);
```

La última consulta debe quedar vacía después de migrar. Repetir el arranque debe
informar cero imágenes nuevas si la carpeta no cambió.

## Pruebas de integración

`tests/MedallasChecks.csproj` no agrega paquetes nuevos. Requiere SQL Server local
y una cuenta con permiso para crear/eliminar una **base temporal de pruebas**.
El ejecutable rechaza servidores que no sean localhost/127.0.0.1. No usa la base
indicada en la conexión como destino: crea `METICS_Medallas_Test_<guid>` y la elimina
al finalizar, junto con sus imágenes temporales.

```powershell
$env:METICS_TEST_CONNECTION = 'CONEXION_SQL_LOCAL_PROPORCIONADA_POR_EL_ENTORNO'
dotnet run --project scripts/medallas/tests/MedallasChecks.csproj
Remove-Item Env:METICS_TEST_CONNECTION
```

Ejecutar desde la raíz del repositorio. Cubre migración repetida, seed concurrente,
ausencia de autoasignaciones, duplicados, reemplazo preservando premios, colisiones,
bajas persistentes, reactivación explícita, imágenes ausentes y nombres inválidos.

## Reversión

Detener la nueva versión y restaurar la versión anterior si fuera necesario. Dejar
la tabla nueva intacta no afecta el SQL anterior; no eliminarla como paso automático.
La versión anterior volverá a ofrecer solo nombres presentes en las asignaciones.
Si hubo modificaciones de medallas tras el deploy, volver al respaldo de BD/archivos
implica perder esos cambios: coordinar esa decisión antes de restaurar.
