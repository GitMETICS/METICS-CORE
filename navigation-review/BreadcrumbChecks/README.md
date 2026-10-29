# Comprobaciones de breadcrumbs

Ejecutar desde la raíz del repositorio:

```powershell
dotnet run --project navigation-review/BreadcrumbChecks/BreadcrumbChecks.csproj
```

El ejecutable usa el resolver real `BreadcrumbBuilder.cs`, `PageHeadingBuilder.cs`, `PageHeadingTagHelper.cs` y el framework ASP.NET Core disponible con .NET 8. No agrega paquetes de pruebas a la aplicación, no instancia controladores y no utiliza la base de datos.

Comprueba roles, padres permitidos, aliases de búsquedas y POST, identificadores de módulo desde query/ViewData/modelo, páginas excluidas, origen explícito y fallback de Referer, acceso directo y despliegue bajo PathBase. El generador de URLs de la prueba simula las URLs MVC convencionales; no sustituye una revisión visual ni una prueba de navegación en el servidor real.

También comprueba la configuración del encabezado compartido, la conservación de títulos y subtítulos, la supresión del encabezado original cuando se presenta en la franja gris y su conservación como fallback cuando no se activa el encabezado compartido. El atributo `metics-page-heading` se aplica únicamente a los encabezados y separadores registrados de cada vista.

Los orígenes admitidos en `breadcrumbOrigin` son `participants`, `inscriptions` y `account`. No se aceptan URLs arbitrarias. Los enlaces de los listados y la edición desde Mi cuenta incluyen ese contexto; los formularios de edición lo conservan en su URL al devolver errores de validación.

Si un flujo existente redirige descartando parámetros de navegación, el resolver aplica el fallback seguro de la pantalla. Para el detalle del participante sin contexto de módulo ni origen de Personas se mantiene Inscripciones como fallback administrativo, igual que el retorno existente de la vista. Los breadcrumbs no cambian las redirecciones de las acciones de negocio.
