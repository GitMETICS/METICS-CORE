# METICS: mapa de rutas y navegación para breadcrumbs

Revisión del código local realizada el 26 de octubre de 2026. Alcance: `Program.cs`, los 12 controladores, todas las vistas Razor y los enlaces/formularios/llamadas JavaScript encontrados en las vistas y en `site.js`. No se modificó la aplicación ni se ejecutaron operaciones contra la base de datos.

## 1. Cómo se resuelven las URLs

La ruta convencional configurada en `webMetics/Program.cs` es `{controller=Home}/{action=Index}/{id?}`. No se encontraron rutas personalizadas mediante `[Route]`. Aunque se registra `MapRazorPages`, no existe un directorio `webMetics/Pages` en este checkout.

- `/`, `/Home` y `/Home/Index` llevan a `Home.Index`, que redirige a `/Grupo/ListaGruposDisponibles`. **Ese catálogo es el Inicio funcional**, no `Views/Home/Index.cshtml`.
- Las URLs internas deben generarse con `Url.Action` o los tag helpers existentes, sin fijar dominio ni puerto.
- El segmento opcional se llama `id`. Muchos métodos esperan **`idGrupo`, `idParticipante`, `idAsesor`, `idTema` o `idCategoria`**. `/Grupo/EditarGrupo/10` no es un sustituto válido de `/Grupo/EditarGrupo?idGrupo=10` con la configuración actual.
- En el inventario, **ANY** significa que el método no declara un atributo HTTP restrictivo; no significa que sea una página segura para enlazar. Hay acciones que modifican datos y aceptan solicitudes GET.

Se identificaron **136 métodos públicos de instancia candidatos a acciones MVC**, distribuidos en **117 paths distintos**, contando las sobrecargas HTTP por separado. De ellos, **50 métodos no restringidos a POST contienen retornos de vista completa**. Esta última cifra incluye búsquedas que reutilizan vistas, resultados de operaciones y tres rutas con vistas ausentes; no equivale a 50 páginas independientes y navegables desde el menú.

El inventario es estático: no certifica que cada ruta responda correctamente con datos reales. Los roles indicados a continuación describen la navegación actual y sus condiciones visibles, no una certificación de autorización del servidor.

## 2. Árbol funcional de navegación

```text
Inicio → /Grupo/ListaGruposDisponibles
├─ Gestión académica [agrupador, sin endpoint propio]
│  ├─ Niveles → /Categoria/ListaCategorias
│  │  ├─ Crear nivel → /Categoria/CrearCategoria
│  │  └─ Editar nivel → /Categoria/EditarCategoria?idCategoria=…
│  └─ Áreas de competencia → /Tema/ListaTemas
│     ├─ Crear área → /Tema/CrearTema
│     └─ Editar área → /Tema/EditarTema?idTema=…
├─ Personas [agrupador, sin endpoint propio]
│  ├─ Facilitadores → /Asesor/ListaAsesores
│  │  ├─ Crear facilitador → /Asesor/CrearAsesor
│  │  └─ Editar facilitador → /Asesor/EditarAsesor?idAsesor=…
│  └─ Participantes → /Participante/VerParticipantes
│     ├─ Agregar participante → /Participante/FormularioParticipante
│     ├─ Detalle → /Participante/VerDatosParticipante?idParticipante=…
│     ├─ Editar → /Participante/EditarParticipante?idParticipante=…
│     ├─ Credenciales → /Usuario/CambiarCredencialesUsuario?idUsuario=…&nombreCompleto=…
│     └─ Contraseña administrada → /Usuario/CambiarContrasenaAdmin?idUsuario=…&nombreCompleto=…
├─ Inscripciones → /Inscripcion/VerInscripciones
│  └─ Detalle de participante → misma acción VerDatosParticipante
├─ Contenido [agrupador, sin endpoint propio]
│  ├─ Crear módulo → /Grupo/CrearGrupo
│  ├─ Crear área → /Tema/CrearTema [mismo destino que desde Gestión académica]
│  └─ Medallas → /Medallas/ListaMedallas
│     └─ Editar medalla → /Medallas/EditarMedalla?nombreMedalla=…
├─ Contexto de un módulo [no existe /Grupo/Detalle]
│  ├─ Editar módulo → /Grupo/EditarGrupo?idGrupo=…
│  ├─ Inscribir participante → /Inscripcion/FormularioInscripcionManual?idGrupo=…
│  ├─ Participantes del módulo → /Participante/ListaParticipantes?idGrupo=…
│  │  ├─ Detalle → /Participante/VerDatosParticipante?idParticipante=…&idGrupo=…
│  │  └─ Calificaciones → /Calificaciones/VerCalificaciones?idGrupo=… [pantalla anterior aún existente]
│  │     └─ Editar calificación → /Calificaciones/EditarCalificacion?idGrupo=…&idParticipante=…
│  └─ Programa → /Grupo/VerAdjunto?idGrupo=… y /Grupo/EditarAdjunto?idGrupo=… [rutas existentes; accesos ocultos o residuales]
├─ Mis módulos → /Asesor/MisModulos [entrada de facilitador]
│  └─ Mismos contextos de módulo, conservando este origen
└─ Mi cuenta [agrupador, sin endpoint propio]
   ├─ Información personal → /Usuario/InformacionPersonal
   │  └─ Editar participante o facilitador propio → acciones de edición compartidas
   ├─ Cambiar contraseña → /Usuario/CambiarContrasena
   └─ Configuración [solo administrador; agrupador]
      ├─ Correo → /EmailSettings/Index
      └─ Registro de accesos → /Usuario/VerBitacoraAccesos
```

Los niveles del árbol son una propuesta de jerarquía para breadcrumbs basada en los enlaces actuales. Los agrupadores no deben convertirse en enlaces a rutas inventadas.

### Visibilidad actual por rol

| Rol | Entradas del navbar y contexto |
|---|---|
| Administrador `1` | Inicio, Gestión académica, Personas, Inscripciones, Contenido; cuenta con Configuración. Crear módulo en la zona de acciones. |
| Facilitador `2` | Inicio y menú Facilitador con Mis módulos y Crear módulo; cuenta propia. No mostrar la rama administrativa de Configuración. |
| Participante `0` | Inicio y cuenta propia. Módulos inscritos es una sección dentro del catálogo, no una ruta separada. |

## 3. Páginas que necesitan un breadcrumb contextual

### Participante: tres entradas reales al mismo detalle

`Participante/VerDatosParticipante` conserva `idGrupo` en `ViewBag.IdGrupo`. La vista selecciona actualmente su retorno así:

1. Con `idGrupo`: `/Participante/ListaParticipantes?idGrupo=…`.
2. Sin `idGrupo`, si Referer contiene `/Participante/VerParticipantes`: listado general de participantes.
3. En otros casos: `/Inscripcion/VerInscripciones`.

Por tanto, los caminos esperados son distintos:

- `Inicio > Personas > Participantes > {persona}`.
- `Inicio > Inscripciones > {persona}`.
- Administrador: `Inicio > {módulo} > Participantes > {persona}`.
- Facilitador: `Inicio > Mis módulos > {módulo} > Participantes > {persona}`.

No depender exclusivamente de Referer para el futuro breadcrumb: falta al abrir una URL directa o una pestaña nueva. Conservar un origen explícito y permitido cuando se implemente, con un fallback determinista por rol. Esto es una recomendación futura, no un parámetro existente en estas rutas.

### Edición de la propia información frente a edición administrativa

`Participante/EditarParticipante` y `Asesor/EditarAsesor` se abren desde los listados administrativos y desde `Usuario/InformacionPersonal`. Las URLs por sí solas no expresan ese origen. Para edición propia: `Inicio > Mi cuenta > Información personal > Editar`; para edición administrativa: rama Personas y su listado correspondiente.

### Módulos y calificaciones

Conservar `idGrupo` en cada enlace de contexto. Usar el nombre del módulo y, cuando proceda, el número de grupo para identificarlo. No crear un enlace intermedio ficticio `/Grupo/Detalle` ni enlazar a CopiarGrupo o CambiarEstadoVisible. Un nombre de módulo puede ser texto contextual no clicable.

La página actual `Participante/ListaParticipantes` contiene edición de calificaciones por AJAX. La pantalla `Calificaciones/VerCalificaciones` aún existe, pero no se encontró una entrada actual hacia ella desde esa lista; sí recibe enlaces de retorno desde EditarCalificacion. Tratar ese flujo como existente pero de alcance limitado/posiblemente heredado, no como nueva entrada del navbar.

### Búsquedas y vistas alternativas

| Ruta de búsqueda/filtro | Página canónica para breadcrumb |
|---|---|
| `/Grupo/BuscarGrupos?searchTerm=…&userRole=…&userId=…` | `/Grupo/ListaGruposDisponibles` |
| `/Asesor/BuscarGrupos?searchTerm=…` | `/Asesor/MisModulos` |
| `/Participante/BuscarParticipantes?searchTerm=…` | Redirige a `/Participante/VerParticipantes` |
| `/Participante/BuscarParticipantesDelGrupo?idGrupo=…&searchTerm=…` | `/Participante/ListaParticipantes?idGrupo=…` |
| `/Inscripcion/BuscarInscripciones?searchTerm=…` | Redirige a `/Inscripcion/VerInscripciones` |
| `/Usuario/VerBitacoraAccesoUsuario`, `VerBitacoraAccesoPorFecha`, `VerBitacoraUltimoAccesoUsuario` | `/Usuario/VerBitacoraAccesos` |

No añadir un nivel por búsqueda, Todos/Activos/Inactivos, paginación, selección de filas o modo lista/cuadrícula. Son estado de una misma pantalla. Conservar los filtros pertinentes en el enlace de regreso si esa es la experiencia elegida. `userRole` y `userId` en una búsqueda existente no son una fuente para decidir permisos del breadcrumb.

## 4. Inicio de sesión, registro y pasos obligatorios

- `/Usuario/IniciarSesion` procesa también POST. Al completar el login, el código verifica si faltan correo alternativo, grado académico o carrera/áreas.
- Secuencia condicional: `/Usuario/CompletarCorreoAlternativo` → `/Usuario/CompletarGradoAcademico` → `/Usuario/CompletarCarreraYAreas` → catálogo. Puede saltar pasos que ya estén completos.
- `/Usuario/FormularioRegistro` devuelve `ParticipanteRegistrado` cuando termina correctamente. **No existe una acción GET `/Usuario/ParticipanteRegistrado`**: es una vista de resultado de ese POST.
- Recuperación: `/Participante/FormularioRecuperarContrasena` → POST `/Participante/RecuperarContrasena`, que vuelve a la misma vista.
- `/Usuario/CerrarSesion` ejecuta el cierre y redirige al login; es una acción, no un ancestro del breadcrumb.
- Propuesta: omitir el breadcrumb administrativo en login, recuperación, registro y completación obligatoria. Si se desea una guía de pasos, tratarla como flujo de onboarding, no como enlaces que permitan saltarse pasos.

## 5. Rutas que no deben ser ancestros clicables

No usar acciones de inscripción/desinscripción, eliminación, copia, cambios de visibilidad, asignación de medallas, cambios de estado, importación, envío de correo o guardado como destino de un breadcrumb. Varias aceptan GET actualmente: es imprescindible una lista explícita de páginas seguras, no basta filtrar por verbo HTTP.

Caso especial: `/Inscripcion/Inscribir?idGrupo=…&idParticipante=…` ejecuta la inscripción y después devuelve una vista de resultado. El breadcrumb puede mostrarse en esa respuesta, pero su último elemento debe ser texto; no enlazar otra vez a Inscribir ni tratarla como una página de consulta.

Descargas PDF/Word/Excel/plantillas/adjuntos, endpoints JSON de DataTables y formularios dependientes, y parciales de modales pertenecen a sus páginas de origen. No generan un nivel propio.

## 6. Hallazgos de rutas que conviene resolver o excluir antes de implementar

Son hallazgos estáticos de la implementación actual. No se corrigieron aquí y no implican que todos los caminos se ejecuten actualmente desde la interfaz.

| Hallazgo | Evidencia y efecto para el mapa |
|---|---|
| Formulario apunta a `Inscripcion/EliminarInscripcionMasivo`, pero el método es `EliminarInscripcionesMasivo` | `Views/Participante/ListaParticipantes.cshtml:283`. La misma vista también contiene un BeginForm con el nombre correcto en su modal. Referencia inconsistente; validar qué formulario se envía efectivamente. |
| Dos vistas generan `Admin/CerrarSesion`, sin AdminController | `Views/Usuario/CambiarContrasenaAdmin.cshtml:130` y `CambiarCredencialesUsuario.cshtml:136`. Aparece en la rama de redirección cuando falta sesión. El cierre existente está en Usuario. |
| JavaScript genera `Calificaciones/EnviarCalificaciones`, sin acción coincidente | `Views/Calificaciones/VerCalificaciones.cshtml:154`. Existe `EnviarCalificacionesAlCorreo`; no asumir equivalencia automática porque también cambian los parámetros. |
| `Grupo/EditarTemas` retorna una vista ausente | `Controllers/GrupoController.cs:870` y retorno alrededor de 902. No existe `Views/Grupo/EditarTemas.cshtml`. No agregar esta ruta al árbol navegable aprobado. |
| `Inscripcion/ListaDeParticipantesInscritos` retorna una vista ausente | `Controllers/InscripcionController.cs:64`. La pantalla usada actualmente para un módulo es `Participante/ListaParticipantes`. |
| `Inscripcion/InscribirManualmente` retorna por convención una vista ausente | `Controllers/InscripcionController.cs:468`. No confundir con FormularioInscripcionManual; además ejecuta operaciones. |
| Vista `Participante/ObtenerParticipante` sin acción homónima | Su formulario apunta a la misma acción inexistente. No crear breadcrumb por la existencia del archivo Razor. |
| Vistas sin retorno directo encontrado | `Home/Index`, `Calificaciones/ExportarCalificaciones`, `Inscripcion/ExportarParticipantes`, `Participante/ObtenerParticipante`. Podrían ser remanentes; no asumir una URL navegable. |
| Programa del módulo conserva rutas, aunque sus enlaces del catálogo están comentados | `Grupo/VerAdjunto`, `EditarAdjunto`, `DescargarArchivo`; revisar por separado si se van a mantener como páginas navegables. Los breadcrumbs no deben reactivar opciones ocultadas. |
| Métodos públicos auxiliares | `Asesor/PuedeEliminarAsesor`, `Inscripcion/ConstructorDelMensajeNotificacionInscripcion` y los métodos de Cookies no son páginas. Con la convención MVC son candidatos públicos y por eso aparecen en el inventario, pero deben excluirse del breadcrumb. `Participante.GetSedes` es estático: no es una acción MVC. |
| Ruta de prueba | `/Home/ThrowTest` lanza una excepción deliberadamente; excluir del mapa de navegación. `/Home/Error` es una pantalla de error, no un ancestro. |

## 7. Enfoque recomendado para implementar después

1. Un registro central por `(controller, action)` con etiqueta, padre canónico, parámetros requeridos y aliases. No deducir el árbol separando la URL por `/` ni usando solo ViewBag.Title: los títulos se repiten y los controladores mezclan responsabilidades.
2. Resolver únicamente páginas completas. Normalizar búsquedas y filtros a su pantalla canónica.
3. Resolver contexto de módulo/persona y origen permitido por rol; conservar `idGrupo` y el identificador correspondiente. Definir fallback para acceso directo, refresco y nuevas pestañas.
4. Tratar nombres de agrupadores como Personas, Contenido, Mi cuenta y Configuración como texto cuando no tienen página propia.
5. Mapear retornos de error de POST a la página del formulario. Un POST a `Asesor/AgregarAsesor`, por ejemplo, puede devolver CrearAsesor; el nombre de la acción de guardado no debe aparecer en el breadcrumb.
6. Renderizar el resultado una sola vez en un parcial compartido del layout, sin tocar controladores de negocio ni endpoints para la primera versión. Los datos que falten para un contexto robusto se deben definir explícitamente al implementar.
7. Verificar administrador, facilitador y participante; rutas directas; búsquedas; errores de validación; personas abiertas desde distintos listados; módulo → participantes → detalle; acceso móvil. Los enlaces no deben ejecutar operaciones y la última miga debe ser texto con `aria-current="page"`.

Lo pendiente antes de considerar cerrado el mapa funcional es decidir el fallback de las páginas compartidas y comprobar en ejecución los caminos heredados/inconsistentes. El inventario que sigue cubre los candidatos encontrados en el código, incluidos los que no deben enlazarse.

## 8. Inventario completo por controlador

Generado mediante inspección estática de declaraciones públicas, retornos de vista y referencias de navegación. Las operaciones pueden devolver también JSON u otros resultados según la rama; la columna de tipo resume lo encontrado, no garantiza una respuesta única. La lista de parámetros reproduce la firma C#; en GET los parámetros simples se pasan normalmente por query string, y en POST los modelos/archivos se envían mediante formulario o cuerpo.

<!-- INVENTARIO_GENERADO -->

### Asesor (9 métodos)

| Método | Path | Parámetros | Respuesta/vista | Fuente |
|---|---|---|---|---|
| ANY | `/Asesor/MisModulos` | — | Página: MisModulos | `webMetics/Controllers/AsesorController.cs:53` |
| ANY | `/Asesor/BuscarGrupos` | `string searchTerm` | Página: MisModulos | `webMetics/Controllers/AsesorController.cs:127` |
| ANY | `/Asesor/ListaAsesores` | — | Página: ListaAsesores | `webMetics/Controllers/AsesorController.cs:167` |
| ANY | `/Asesor/CrearAsesor` | — | Página: CrearAsesor | `webMetics/Controllers/AsesorController.cs:198` |
| POST | `/Asesor/AgregarAsesor` | `AsesorModel asesor` | Página: CrearAsesor | `webMetics/Controllers/AsesorController.cs:232` |
| ANY | `/Asesor/EditarAsesor` | `string idAsesor` | Página: EditarAsesor | `webMetics/Controllers/AsesorController.cs:301` |
| POST | `/Asesor/ActualizarAsesor` | `AsesorModel asesor` | Página: EditarAsesor | `webMetics/Controllers/AsesorController.cs:335` |
| ANY | `/Asesor/EliminarAsesor` | `string idAsesor` | Operación/redirección | `webMetics/Controllers/AsesorController.cs:501` |
| ANY | `/Asesor/PuedeEliminarAsesor` | `string idAsesor` | Auxiliar público; no es una pantalla | `webMetics/Controllers/AsesorController.cs:533` |

### Calificaciones (11 métodos)

| Método | Path | Parámetros | Respuesta/vista | Fuente |
|---|---|---|---|---|
| ANY | `/Calificaciones/VerCalificaciones` | `int idGrupo` | Página: VerCalificaciones | `webMetics/Controllers/CalificacionesController.cs:65` |
| ANY | `/Calificaciones/EditarCalificacion` | `int idGrupo, string idParticipante` | Página: EditarCalificacion | `webMetics/Controllers/CalificacionesController.cs:101` |
| POST | `/Calificaciones/SubirHorasAprobadasYCalificacion` | `int idGrupo, string idParticipante, int horasAprobadas = 0, int calificacion = 0` | Operación/redirección | `webMetics/Controllers/CalificacionesController.cs:133` |
| POST | `/Calificaciones/SubirHorasAprobadasYCalificacionAjax` | `int idGrupo, string idParticipante, int horasAprobadas = -1, int calificacion = -1` | JSON | `webMetics/Controllers/CalificacionesController.cs:192` |
| POST | `/Calificaciones/SubirCalificacion` | `int idGrupo, string idParticipante, int calificacion` | Operación/redirección | `webMetics/Controllers/CalificacionesController.cs:310` |
| POST | `/Calificaciones/SubirExcelCalificaciones` | `IFormFile file, int idGrupo` | Operación/redirección | `webMetics/Controllers/CalificacionesController.cs:351` |
| ANY | `/Calificaciones/DescargarPlantillaSubirCalificaciones` | — | Archivo | `webMetics/Controllers/CalificacionesController.cs:537` |
| ANY | `/Calificaciones/ExportarCalificacionesPDF` | `int idGrupo, string? searchTerm` | Archivo | `webMetics/Controllers/CalificacionesController.cs:619` |
| ANY | `/Calificaciones/ExportarCalificacionesWord` | `int idGrupo, string? searchTerm` | Archivo | `webMetics/Controllers/CalificacionesController.cs:683` |
| ANY | `/Calificaciones/ExportarCalificacionesExcel` | `int idGrupo, string? searchTerm` | Archivo | `webMetics/Controllers/CalificacionesController.cs:745` |
| ANY | `/Calificaciones/EnviarCalificacionesAlCorreo` | `int idGrupo` | Operación/redirección | `webMetics/Controllers/CalificacionesController.cs:883` |

### Categoria (6 métodos)

| Método | Path | Parámetros | Respuesta/vista | Fuente |
|---|---|---|---|---|
| ANY | `/Categoria/ListaCategorias` | — | Página: ListaCategorias | `webMetics/Controllers/CategoriaController.cs:57` |
| ANY | `/Categoria/CrearCategoria` | — | Página: CrearCategoria | `webMetics/Controllers/CategoriaController.cs:75` |
| POST | `/Categoria/CrearCategoria` | `CategoriaModel categoria` | Página: CrearCategoria | `webMetics/Controllers/CategoriaController.cs:93` |
| ANY | `/Categoria/EditarCategoria` | `int idCategoria` | Página: EditarCategoria | `webMetics/Controllers/CategoriaController.cs:125` |
| POST | `/Categoria/EditarCategoria` | `CategoriaModel categoria` | Página: EditarCategoria | `webMetics/Controllers/CategoriaController.cs:145` |
| ANY | `/Categoria/EliminarCategoria` | `int idCategoria` | Operación/redirección | `webMetics/Controllers/CategoriaController.cs:178` |

### Cookies (4 métodos)

| Método | Path | Parámetros | Respuesta/vista | Fuente |
|---|---|---|---|---|
| ANY | `/Cookies/CreateCookie` | `string cookieName, string cookieValue, DateTimeOffset expirationTime` | Operación/redirección | `webMetics/Controllers/CookiesController.cs:26` |
| ANY | `/Cookies/UpdateCookie` | `string cookieName, string value` | Operación/redirección | `webMetics/Controllers/CookiesController.cs:40` |
| ANY | `/Cookies/DeleteCookie` | `string cookieName` | Operación/redirección | `webMetics/Controllers/CookiesController.cs:60` |
| ANY | `/Cookies/FetchCookieValue` | `string cookieName` | Auxiliar público; no es una pantalla | `webMetics/Controllers/CookiesController.cs:70` |

### EmailSettings (3 métodos)

| Método | Path | Parámetros | Respuesta/vista | Fuente |
|---|---|---|---|---|
| ANY | `/EmailSettings/Index` | — | Página: Index | `webMetics/Controllers/EmailSettingsController.cs:65` |
| POST | `/EmailSettings/ToggleEmailSending` | `bool isEnabled` | Operación/redirección | `webMetics/Controllers/EmailSettingsController.cs:90` |
| POST | `/EmailSettings/UpdateNotificationEmail` | `string correoLimiteHoras` | Operación/redirección | `webMetics/Controllers/EmailSettingsController.cs:140` |

### Grupo (15 métodos)

| Método | Path | Parámetros | Respuesta/vista | Fuente |
|---|---|---|---|---|
| ANY | `/Grupo/ListaGruposDisponibles` | — | Página: ListaGruposDisponibles | `webMetics/Controllers/GrupoController.cs:87` |
| ANY | `/Grupo/BuscarGrupos` | `string searchTerm, int userRole, string userId` | Página: ListaGruposDisponibles | `webMetics/Controllers/GrupoController.cs:205` |
| ANY | `/Grupo/DescargarArchivo` | `int idGrupo` | Archivo | `webMetics/Controllers/GrupoController.cs:318` |
| ANY | `/Grupo/CrearGrupo` | — | Página: CrearGrupo | `webMetics/Controllers/GrupoController.cs:341` |
| POST | `/Grupo/CrearGrupo` | `GrupoModel grupo, int[] temasSeleccionados` | Página: CrearGrupo | `webMetics/Controllers/GrupoController.cs:382` |
| ANY | `/Grupo/EliminarGrupo` | `int? idGrupo` | Operación/redirección | `webMetics/Controllers/GrupoController.cs:485` |
| ANY | `/Grupo/CambiarEstadoVisible` | `int idGrupo` | Operación/redirección | `webMetics/Controllers/GrupoController.cs:522` |
| ANY | `/Grupo/EditarGrupo` | `int idGrupo` | Página: EditarGrupo | `webMetics/Controllers/GrupoController.cs:564` |
| POST | `/Grupo/EditarGrupo` | `GrupoModel grupo, int[] temasSeleccionados` | Página: EditarGrupo | `webMetics/Controllers/GrupoController.cs:623` |
| ANY | `/Grupo/VerAdjunto` | `int idGrupo` | Página: VerAdjunto | `webMetics/Controllers/GrupoController.cs:736` |
| ANY | `/Grupo/EditarAdjunto` | `int idGrupo` | Página: EditarAdjunto | `webMetics/Controllers/GrupoController.cs:779` |
| POST | `/Grupo/EditarAdjunto` | `GrupoModel grupo` | Operación/redirección | `webMetics/Controllers/GrupoController.cs:824` |
| ANY | `/Grupo/EditarTemas` | `int idGrupo` | Página: EditarTemas **VISTA AUSENTE** | `webMetics/Controllers/GrupoController.cs:870` |
| POST | `/Grupo/GuardarTemasAsociados` | `int idGrupo, int[] TemasSeleccionados` | Operación/redirección | `webMetics/Controllers/GrupoController.cs:922` |
| ANY | `/Grupo/CopiarGrupo` | `int idGrupo` | Operación/redirección | `webMetics/Controllers/GrupoController.cs:959` |

### Home (3 métodos)

| Método | Path | Parámetros | Respuesta/vista | Fuente |
|---|---|---|---|---|
| ANY | `/Home/Index` | — | Operación/redirección | `webMetics/Controllers/HomeController.cs:27` |
| GET | `/Home/ThrowTest` | — | Operación/redirección | `webMetics/Controllers/HomeController.cs:37` |
| ANY | `/Home/Error` | — | Página: Error | `webMetics/Controllers/HomeController.cs:43` |

### Inscripcion (23 métodos)

| Método | Path | Parámetros | Respuesta/vista | Fuente |
|---|---|---|---|---|
| ANY | `/Inscripcion/ListaDeParticipantesInscritos` | `int idGrupo` | Página: ListaDeParticipantesInscritos **VISTA AUSENTE** | `webMetics/Controllers/InscripcionController.cs:64` |
| ANY | `/Inscripcion/VerInscripciones` | `bool reload = false, string? searchTerm = null` | Página: VerInscripciones | `webMetics/Controllers/InscripcionController.cs:90` |
| GET | `/Inscripcion/ObtenerInscripcionesPaginadas` | — | JSON | `webMetics/Controllers/InscripcionController.cs:113` |
| ANY | `/Inscripcion/BuscarInscripciones` | `string searchTerm` | Operación/redirección | `webMetics/Controllers/InscripcionController.cs:171` |
| ANY | `/Inscripcion/FormularioInscripcion` | — | Página: FormularioInscripcion | `webMetics/Controllers/InscripcionController.cs:187` |
| POST | `/Inscripcion/FormularioInscripcion` | `InscripcionModel inscripcion` | Página: FormularioInscripcion | `webMetics/Controllers/InscripcionController.cs:214` |
| ANY | `/Inscripcion/FormularioInscripcionManual` | `int idGrupo` | Página: FormularioInscripcionManual | `webMetics/Controllers/InscripcionController.cs:255` |
| POST | `/Inscripcion/FormularioInscripcionManual` | `InscripcionModel inscripcion` | Página: FormularioInscripcionManual | `webMetics/Controllers/InscripcionController.cs:284` |
| ANY | `/Inscripcion/Inscribir` | `int idGrupo, string idParticipante` | Página: Inscribir | `webMetics/Controllers/InscripcionController.cs:337` |
| ANY | `/Inscripcion/InscribirManualmente` | `int idGrupo, string idParticipante` | Página: InscribirManualmente **VISTA AUSENTE** | `webMetics/Controllers/InscripcionController.cs:468` |
| POST | `/Inscripcion/EliminarInscripcion` | `string nombreGrupo, int numeroGrupo, string idParticipante` | Operación/redirección | `webMetics/Controllers/InscripcionController.cs:585` |
| POST | `/Inscripcion/EliminarInscripcionesMasivo` | `List<string> participantesSeleccionados, string nombreGrupo, int numeroGrupo` | Operación/redirección | `webMetics/Controllers/InscripcionController.cs:640` |
| ANY | `/Inscripcion/DesinscribirParticipante` | `string idParticipante, int idGrupo` | Operación/redirección | `webMetics/Controllers/InscripcionController.cs:719` |
| ANY | `/Inscripcion/ConstructorDelMensajeNotificacionInscripcion` | `GrupoModel grupo, ParticipanteModel participante` | Auxiliar público; no es una pantalla | `webMetics/Controllers/InscripcionController.cs:778` |
| ANY | `/Inscripcion/DescargarPlantillaSubirInscripciones` | — | Archivo | `webMetics/Controllers/InscripcionController.cs:823` |
| POST | `/Inscripcion/SubirArchivoExcelInscripciones` | `IFormFile file` | Operación/redirección | `webMetics/Controllers/InscripcionController.cs:864` |
| ANY | `/Inscripcion/ExportarParticipantesPDF` | `int idGrupo, string? searchTerm` | Archivo | `webMetics/Controllers/InscripcionController.cs:1001` |
| ANY | `/Inscripcion/ExportarParticipantesWord` | `int idGrupo, string? searchTerm` | Archivo | `webMetics/Controllers/InscripcionController.cs:1129` |
| ANY | `/Inscripcion/ExportarParticipantesExcel` | `int idGrupo, string? searchTerm` | Archivo | `webMetics/Controllers/InscripcionController.cs:1250` |
| ANY | `/Inscripcion/ExportarTodosParticipantesPDF` | `string? searchTerm` | Archivo | `webMetics/Controllers/InscripcionController.cs:1418` |
| ANY | `/Inscripcion/ExportarTodosParticipantesWord` | `string? searchTerm` | Archivo | `webMetics/Controllers/InscripcionController.cs:1512` |
| ANY | `/Inscripcion/ExportarTodosParticipantesExcel` | `string? searchTerm` | Archivo | `webMetics/Controllers/InscripcionController.cs:1606` |
| POST | `/Inscripcion/CambiarEstadoInscritoAIncompleto` | `List<string> participantesSeleccionados, int idGrupo` | Operación/redirección | `webMetics/Controllers/InscripcionController.cs:1832` |

### Medallas (6 métodos)

| Método | Path | Parámetros | Respuesta/vista | Fuente |
|---|---|---|---|---|
| ANY | `/Medallas/ListaMedallas` | — | Página: ListaMedallas | `webMetics/Controllers/MedallasController.cs:65` |
| POST | `/Medallas/SubirMedalla` | `IFormFile imageFile` | Operación/redirección | `webMetics/Controllers/MedallasController.cs:90` |
| ANY | `/Medallas/EditarMedalla` | `string nombreMedalla` | Página: EditarMedalla | `webMetics/Controllers/MedallasController.cs:141` |
| POST | `/Medallas/EditarMedalla` | `string nombreMedalla, IFormFile imageFile` | Operación/redirección | `webMetics/Controllers/MedallasController.cs:162` |
| ANY | `/Medallas/EliminarMedalla` | `string nombreMedalla` | Operación/redirección | `webMetics/Controllers/MedallasController.cs:196` |
| POST | `/Medallas/EliminarMedallaParticipante` | `string idParticipante, string nombreMedalla` | Operación/redirección | `webMetics/Controllers/MedallasController.cs:230` |

### Participante (28 métodos)

| Método | Path | Parámetros | Respuesta/vista | Fuente |
|---|---|---|---|---|
| ANY | `/Participante/ListaParticipantes` | `int idGrupo` | Página: ListaParticipantes | `webMetics/Controllers/ParticipanteController.cs:73` |
| ANY | `/Participante/VerParticipantes` | `string? searchTerm = null` | Página: VerParticipantes | `webMetics/Controllers/ParticipanteController.cs:121` |
| GET | `/Participante/ObtenerParticipantesPaginados` | — | JSON | `webMetics/Controllers/ParticipanteController.cs:140` |
| ANY | `/Participante/BuscarParticipantes` | `string searchTerm` | Operación/redirección | `webMetics/Controllers/ParticipanteController.cs:195` |
| ANY | `/Participante/BuscarParticipantesDelGrupo` | `int idGrupo, string searchTerm` | Página: ListaParticipantes | `webMetics/Controllers/ParticipanteController.cs:217` |
| POST | `/Participante/AsignarMedallas` | `string idParticipante, List<string> selectedMedallas` | Operación/redirección | `webMetics/Controllers/ParticipanteController.cs:281` |
| POST | `/Participante/AsignarMedallaMasiva` | `string nombreMedalla, List<string> participantesSeleccionados` | Operación/redirección | `webMetics/Controllers/ParticipanteController.cs:334` |
| POST | `/Participante/SubirArchivoExcelParticipantes` | `IFormFile file` | Operación/redirección | `webMetics/Controllers/ParticipanteController.cs:383` |
| POST | `/Participante/NotificarLimiteHoras` | `string idParticipante, string? searchTerm = null` | Operación/redirección | `webMetics/Controllers/ParticipanteController.cs:462` |
| ANY | `/Participante/ExportarParticipantesPDF` | `string? searchTerm` | Archivo | `webMetics/Controllers/ParticipanteController.cs:537` |
| ANY | `/Participante/ExportarParticipantesWord` | `string? searchTerm` | Archivo | `webMetics/Controllers/ParticipanteController.cs:638` |
| ANY | `/Participante/ExportarParticipantesExcel` | `string? searchTerm` | Archivo | `webMetics/Controllers/ParticipanteController.cs:758` |
| ANY | `/Participante/ExportarParticipantesExcel2` | `string? searchTerm` | Archivo | `webMetics/Controllers/ParticipanteController.cs:892` |
| ANY | `/Participante/DescargarPlantillaSubirParticipantes` | — | Archivo | `webMetics/Controllers/ParticipanteController.cs:993` |
| ANY | `/Participante/VerDatosParticipante` | `string idParticipante, int? idGrupo` | Página: VerDatosParticipante | `webMetics/Controllers/ParticipanteController.cs:1042` |
| ANY | `/Participante/FormularioParticipante` | — | Página: FormularioParticipante | `webMetics/Controllers/ParticipanteController.cs:1095` |
| POST | `/Participante/FormularioParticipante` | `ParticipanteModel participante` | Página: FormularioParticipante | `webMetics/Controllers/ParticipanteController.cs:1127` |
| ANY | `/Participante/EditarParticipante` | `string idParticipante` | Página: EditarParticipante | `webMetics/Controllers/ParticipanteController.cs:1313` |
| POST | `/Participante/ActualizarParticipante` | `ParticipanteModel participante` | Página: EditarParticipante | `webMetics/Controllers/ParticipanteController.cs:1352` |
| POST | `/Participante/EliminarParticipante` | `string idParticipante` | Operación/redirección | `webMetics/Controllers/ParticipanteController.cs:1499` |
| POST | `/Participante/DisplayModal` | `string idParticipante` | Parcial: _Modal | `webMetics/Controllers/ParticipanteController.cs:1529` |
| GET | `/Participante/GetTiposParticipante` | `string tipoParticipante` | JSON | `webMetics/Controllers/ParticipanteController.cs:1542` |
| GET | `/Participante/GetDepartamentosByArea` | `string areaName` | JSON | `webMetics/Controllers/ParticipanteController.cs:1552` |
| GET | `/Participante/GetSeccionesByDepartamento` | `string areaName, string departamentoName` | JSON | `webMetics/Controllers/ParticipanteController.cs:1567` |
| GET | `/Participante/GetAllAreasData` | — | JSON | `webMetics/Controllers/ParticipanteController.cs:1586` |
| GET | `/Participante/GetCarrerasBySeccionAndSede` | `string areaName, string departamentoName, string unidadAcademica, string sede` | JSON | `webMetics/Controllers/ParticipanteController.cs:1624` |
| ANY | `/Participante/FormularioRecuperarContrasena` | — | Página: FormularioRecuperarContrasena | `webMetics/Controllers/ParticipanteController.cs:1755` |
| POST | `/Participante/RecuperarContrasena` | `ParticipanteModel participante` | Página: FormularioRecuperarContrasena | `webMetics/Controllers/ParticipanteController.cs:1773` |

### Tema (6 métodos)

| Método | Path | Parámetros | Respuesta/vista | Fuente |
|---|---|---|---|---|
| ANY | `/Tema/ListaTemas` | — | Página: ListaTemas | `webMetics/Controllers/TemaController.cs:59` |
| ANY | `/Tema/CrearTema` | — | Página: CrearTema | `webMetics/Controllers/TemaController.cs:79` |
| POST | `/Tema/CrearTema` | `TemaModel tema` | Página: CrearTema | `webMetics/Controllers/TemaController.cs:98` |
| ANY | `/Tema/EditarTema` | `int idTema` | Página: EditarTema | `webMetics/Controllers/TemaController.cs:133` |
| POST | `/Tema/EditarTema` | `TemaModel tema` | Página: EditarTema | `webMetics/Controllers/TemaController.cs:154` |
| ANY | `/Tema/EliminarTema` | `int idTema` | Operación/redirección | `webMetics/Controllers/TemaController.cs:188` |

### Usuario (22 métodos)

| Método | Path | Parámetros | Respuesta/vista | Fuente |
|---|---|---|---|---|
| ANY | `/Usuario/IniciarSesion` | — | Página: IniciarSesion | `webMetics/Controllers/UsuarioController.cs:50` |
| POST | `/Usuario/IniciarSesion` | `LoginModel usuario` | Página: IniciarSesion | `webMetics/Controllers/UsuarioController.cs:81` |
| ANY | `/Usuario/FormularioRegistro` | — | Página: FormularioRegistro | `webMetics/Controllers/UsuarioController.cs:114` |
| POST | `/Usuario/FormularioRegistro` | `UsuarioModel usuario` | Página: ParticipanteRegistrado, FormularioRegistro | `webMetics/Controllers/UsuarioController.cs:136` |
| ANY | `/Usuario/InformacionPersonal` | — | Página: InformacionPersonal | `webMetics/Controllers/UsuarioController.cs:377` |
| ANY | `/Usuario/CerrarSesion` | — | Operación/redirección | `webMetics/Controllers/UsuarioController.cs:471` |
| ANY | `/Usuario/CompletarCorreoAlternativo` | — | Página: CompletarCorreoAlternativo | `webMetics/Controllers/UsuarioController.cs:494` |
| POST | `/Usuario/CompletarCorreoAlternativo` | `UsuarioModel usuario` | Operación/redirección | `webMetics/Controllers/UsuarioController.cs:541` |
| ANY | `/Usuario/CompletarCarreraYAreas` | — | Página: CompletarCarreraYAreas | `webMetics/Controllers/UsuarioController.cs:622` |
| POST | `/Usuario/CompletarCarreraYAreas` | `[Bind("area,departamento,unidadAcademica,sede,carrera,areasExtra")] ParticipanteModel participante` | Página: CompletarCarreraYAreas | `webMetics/Controllers/UsuarioController.cs:657` |
| ANY | `/Usuario/CompletarGradoAcademico` | — | Página: CompletarGradoAcademico | `webMetics/Controllers/UsuarioController.cs:785` |
| POST | `/Usuario/CompletarGradoAcademico` | `UsuarioModel usuario` | Operación/redirección | `webMetics/Controllers/UsuarioController.cs:822` |
| ANY | `/Usuario/CambiarCredencialesUsuario` | `string idUsuario, string nombreCompleto` | Página: CambiarCredencialesUsuario | `webMetics/Controllers/UsuarioController.cs:878` |
| POST | `/Usuario/CambiarCredencialesUsuario` | `NewLoginModel usuario` | Operación/redirección | `webMetics/Controllers/UsuarioController.cs:938` |
| ANY | `/Usuario/CambiarContrasena` | — | Página: CambiarContrasena | `webMetics/Controllers/UsuarioController.cs:1063` |
| POST | `/Usuario/CambiarContrasena` | `NewLoginModel usuario` | Operación/redirección | `webMetics/Controllers/UsuarioController.cs:1111` |
| ANY | `/Usuario/CambiarContrasenaAdmin` | `string idUsuario, string nombreCompleto` | Página: CambiarContrasenaAdmin | `webMetics/Controllers/UsuarioController.cs:1150` |
| POST | `/Usuario/CambiarContrasenaAdmin` | `NewLoginModel usuario` | Operación/redirección | `webMetics/Controllers/UsuarioController.cs:1210` |
| ANY | `/Usuario/VerBitacoraAccesos` | — | Página: VerBitacoraAccesos | `webMetics/Controllers/UsuarioController.cs:1251` |
| GET | `/Usuario/VerBitacoraAccesoUsuario` | `string idUsuario, int diasAtras = 30` | Página: VerBitacoraAccesos | `webMetics/Controllers/UsuarioController.cs:1282` |
| GET | `/Usuario/VerBitacoraAccesoPorFecha` | `string fechaDesde, string fechaHasta, string estadoAcceso` | Página: VerBitacoraAccesos | `webMetics/Controllers/UsuarioController.cs:1317` |
| GET | `/Usuario/VerBitacoraUltimoAccesoUsuario` | `string idUsuario` | Página: VerBitacoraAccesos | `webMetics/Controllers/UsuarioController.cs:1348` |

## 9. Aliases de POST que devuelven formularios

Estos métodos pueden mostrar una vista completa al fallar una validación. El breadcrumb debe describir el formulario/resultante y nunca ofrecer un enlace GET a la acción de guardado.

| POST | Vista devuelta | Pantalla/criterio para breadcrumb |
|---|---|---|
| `/Asesor/AgregarAsesor` | `CrearAsesor` | `/Asesor/CrearAsesor` (conservar parámetros del modelo) |
| `/Asesor/ActualizarAsesor` | `EditarAsesor` | `/Asesor/EditarAsesor` (conservar parámetros del modelo) |
| `/Categoria/CrearCategoria` | `CrearCategoria` | `/Categoria/CrearCategoria` (conservar parámetros del modelo) |
| `/Categoria/EditarCategoria` | `EditarCategoria` | `/Categoria/EditarCategoria` (conservar parámetros del modelo) |
| `/Grupo/CrearGrupo` | `CrearGrupo` | `/Grupo/CrearGrupo` (conservar parámetros del modelo) |
| `/Grupo/EditarGrupo` | `EditarGrupo` | `/Grupo/EditarGrupo` (conservar parámetros del modelo) |
| `/Inscripcion/FormularioInscripcion` | `FormularioInscripcion` | `/Inscripcion/FormularioInscripcion` (conservar parámetros del modelo) |
| `/Inscripcion/FormularioInscripcionManual` | `FormularioInscripcionManual` | `/Inscripcion/FormularioInscripcionManual` (conservar parámetros del modelo) |
| `/Participante/FormularioParticipante` | `FormularioParticipante` | `/Participante/FormularioParticipante` (conservar parámetros del modelo) |
| `/Participante/ActualizarParticipante` | `EditarParticipante` | `/Participante/EditarParticipante` (conservar parámetros del modelo) |
| `/Participante/RecuperarContrasena` | `FormularioRecuperarContrasena` | `/Participante/FormularioRecuperarContrasena` (conservar parámetros del modelo) |
| `/Tema/CrearTema` | `CrearTema` | `/Tema/CrearTema` (conservar parámetros del modelo) |
| `/Tema/EditarTema` | `EditarTema` | `/Tema/EditarTema` (conservar parámetros del modelo) |
| `/Usuario/IniciarSesion` | `IniciarSesion` | `/Usuario/IniciarSesion` (conservar parámetros del modelo) |
| `/Usuario/FormularioRegistro` | `ParticipanteRegistrado` | Vista de resultado sin ruta GET canónica identificada; último elemento no clicable |
| `/Usuario/FormularioRegistro` | `FormularioRegistro` | `/Usuario/FormularioRegistro` (conservar parámetros del modelo) |
| `/Usuario/CompletarCarreraYAreas` | `CompletarCarreraYAreas` | `/Usuario/CompletarCarreraYAreas` (conservar parámetros del modelo) |

## 10. Referencias dinámicas que requieren resolver contexto

Se encontraron estas expresiones de destino variable; la revisión manual debe resolver sus valores y los parámetros antes de generar migas.

- `webMetics/Views/Participante/VerDatosParticipante.cshtml:212`: `Url.Action(volverAction, volverController, volverRouteValues)">`
- `webMetics/Views/Participante/VerDatosParticipante.cshtml:519`: `Url.Action(volverAction, volverController, volverRouteValues)" class="btn btn-secondary btn-block">@volverTexto</a>`
