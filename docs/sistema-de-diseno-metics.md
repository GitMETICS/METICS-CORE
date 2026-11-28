# Sistema de diseño de METICS

Este documento es la referencia visual de METICS. Use este documento cuando agregue una pantalla o una función nueva.

## 1. Alcance

### 1.1 Qué contiene este documento

Este documento contiene:

- Los tokens de diseño: colores, tipografía, espaciado, radios, sombras, puntos de quiebre y alturas de controles.
- Las reglas de redacción de la interfaz.
- Los componentes de la interfaz.
- Las reglas de accesibilidad.
- Los problemas conocidos que debe corregir.
- El procedimiento para agregar una función nueva.

### 1.2 Origen de los datos

- **Rama:** `refactor-ui` (PR 58), commit `5420613`.
- **Archivo de estilos:** `webMetics/wwwroot/css/site.css`.
- **Método:** se leyeron los estilos calculados de la aplicación en ejecución, con anchos de pantalla de 1366 px y 375 px.
- **Valores:** son exactos. No se redondearon.

### 1.3 Versión visual

El archivo [sistema-de-diseno-metics.html](sistema-de-diseno-metics.html) muestra los colores, la tipografía y una vista previa de cada componente. Abra el archivo en un navegador.

### 1.4 Términos

| Término | Significado |
| --- | --- |
| Token | Un nombre fijo para un valor de diseño, por ejemplo `action-blue` = `#245e98`. |
| Tarjeta | El contenedor blanco que agrupa una lista, un formulario o una herramienta. |
| Módulo | Un curso, taller o charla. Use "módulo". No use "curso". |
| Modal | Una ventana de diálogo sobre la página. |
| Notificación | El mensaje temporal que aparece después de una acción (*toast*). |
| Contraste | La relación de luminancia entre el texto y su fondo, por ejemplo 4,5:1. |

## 2. Tecnología base

| Elemento | Versión | Uso |
| --- | --- | --- |
| Bootstrap | 5.3.2 | Base de estilos y componentes JavaScript. Se carga desde jsDelivr. |
| Bootstrap Icons | 1.10.3 | Todos los íconos. No hay íconos propios. |
| Inter | Google Fonts, pesos 100 a 900 | Única familia tipográfica. |
| DataTables | — | Tablas de administración. |
| `site.css` | ~6450 líneas | Hoja de estilos global. |

`site.css` cambia el color de `.btn-primary`, `.btn-outline-primary` y `.link-primary` a los azules de METICS.

Solo 13 colores son variables CSS (`--metics-*`). Los demás colores están escritos directamente en el código. Cuando modifique un área, cambie esos colores por variables.

## 3. Redacción de la interfaz

### 3.1 Reglas generales

1. Escriba en español de Costa Rica.
2. Trate al usuario de **usted**. Ejemplo: "Ingrese con su cuenta institucional para continuar."
3. Use las formas inclusivas del sistema. Ejemplos: "Facilitador(a)", "El/la facilitador(a)", "Profesor(a) Interino(a)".
4. Use los términos del dominio:
   - módulo
   - nivel
   - área de competencia
   - facilitador(a)
   - participante
   - inscripción
   - calificación
   - medalla
   - cupo
5. Use mayúscula solo en la primera palabra de los títulos. Ejemplo correcto: "Lista de inscripciones". Ejemplo incorrecto: "Lista de Inscripciones".
6. Muestre nombres legibles, no nombres de archivo. Ejemplo incorrecto: "1.Medalla_ Fundamentos Ciberseguridad.png". Ejemplo correcto: "Fundamentos de Ciberseguridad".

### 3.2 Patrones de mensajes

| Tipo | Patrón | Ejemplo |
| --- | --- | --- |
| Validación | "Es necesario ingresar…" | "Es necesario ingresar un nombre." |
| Éxito | Acción en pasado | "El/la facilitador(a) se agregó con éxito." |
| Error | Resultado, dos puntos, causa | "No se asignó ninguna medalla: falta la imagen de la medalla." |
| Botón | Verbo + objeto | "Crear módulo", "Agregar participante" |
| Ayuda | Instrucción con usted | "Deje la contraseña en blanco." |

### 3.3 Títulos que debe corregir

Estas páginas usan mayúsculas en cada palabra. Corríjalas cuando modifique la página.

- "Módulos Disponibles para Inscripción"
- "Crear Módulo"
- "Lista de Medallas"
- "Registro de Accesos"
- "Editar Facilitador(a)"

## 4. Color

### 4.1 Reglas de color

1. Use el degradado de marca solo en el encabezado y en el pie de página.
2. Use `action-blue` solo para el relleno de los botones primarios.
3. Use `interactive-blue` para enlaces, íconos, bordes de botones secundarios y texto informativo.
4. Use fondos blancos. No use fondos de página con color.
5. Separe las áreas con bordes de 1 px y sombras suaves.
6. Use los colores de estado (rojo, verde, ámbar) solo para estados. No use estos colores como decoración.
7. No use un gris más claro que `ink-muted` para texto.

### 4.2 Marca

| Token | Valor | Uso |
| --- | --- | --- |
| `brand-deep` | `#08284b` | Degradado del encabezado y del pie, color inicial (112°). |
| `brand-mid` | `#073766` | Degradado, color central (52 %). |
| `brand-end` | `#0b4b87` | Degradado, color final. |

El degradado completo es `linear-gradient(112deg, #08284b 0%, #073766 52%, #0b4b87 100%)`. El texto blanco sobre el degradado tiene un contraste de 8,9:1 a 14,8:1.

### 4.3 Acción e interacción

| Token | Valor | Variable CSS | Uso |
| --- | --- | --- | --- |
| `action-blue` | `#245e98` | `--metics-action-blue` | Relleno del botón primario. Texto blanco: 6,7:1. |
| `action-hover` | `#1e4f80` | `--metics-action-hover` | Botón primario al pasar el cursor o al hacer clic. |
| `interactive-blue` | `#2a5f95` | `--metics-interactive-blue` | Enlaces, íconos, botón secundario, texto de insignias. 6,6:1 sobre blanco. |
| `soft-blue` | `#dcebfa` | `--metics-soft-blue` | Fondo de insignias informativas, contadores y filtro activo. |
| `icon-surface` | `#eef5fc` | `--metics-icon-surface` | Fondo de los íconos y del botón Editar. |
| `navy` | `#204c6f` | `--metics-navy` | Color antiguo de títulos. No lo use en trabajo nuevo. |

### 4.4 Texto

| Token | Valor | Uso | Contraste sobre blanco |
| --- | --- | --- | --- |
| `ink-title` | `#14243a` | Título de página, título de tarjeta de módulo. | 15,6:1 |
| `ink-strong` | `#172b49` | Títulos y etiquetas de modales. | 14,2:1 |
| `ink-heading` | `#1b2c43` | Títulos de tarjetas y secciones. | 14,1:1 |
| `ink-label` | `#263b57` | Etiquetas de campos en formularios de página completa. | — |
| `ink-control` | `#27384e` | Texto dentro de campos. | 11,9:1 |
| `ink-table-head` | `#23344b` | Encabezado de tabla (sobre `#f1f6fc`). | 11,6:1 |
| `ink-body` | `#42536a` | Celdas de tabla y texto largo. | 7,8:1 |
| `ink-muted` | `#526b8b` | Subtítulos y texto de ayuda. Gris mínimo para texto. | 5,5:1 |
| `ink-muted-alt` | `#59708f` | Subtítulo de modales. Duplicado de `ink-muted`. | 5,1:1 |
| `ink-breadcrumb` | `#58718c` | Enlaces de la ruta de navegación. | 5,1:1 |
| `ink-faint` | `#718096` | **No use este color para texto.** No cumple el mínimo. | 4,0:1 |
| `ink-on-dark` | `#ffffff` | Texto sobre el degradado y sobre `action-blue`. | — |

### 4.5 Superficies y bordes

| Token | Valor | Uso |
| --- | --- | --- |
| `surface` | `#ffffff` | Fondo de página, tarjetas y modales. |
| `surface-table-head` | `#f1f6fc` | Fondo del encabezado de tabla. |
| `surface-toggle` | `#f2f5fa` | Fondo del botón "Modo lista / Modo cuadrícula". |
| `border-card` | `#e1e8f0` | Borde de tarjetas de sección. |
| `border-module-card` | `#d9e2ed` | Borde de tarjetas de módulo. |
| `divider` | `#e5ebf3` | Línea bajo el encabezado de una tarjeta. |
| `divider-row` | `#e8eef5` | Línea entre filas de tabla. |
| `border-control` | `#cbd8e8` | Borde de campos en modales. |
| `border-select` | `#d3deeb` | Borde de la lista desplegable personalizada. |
| `border-search` | `#d9e2ee` | Borde del campo de búsqueda de tablas. |
| `border-filter` | `#cbd5e1` | Borde del filtro segmentado y del botón de vista. |
| `border-modal` | `#dce6f2` | Borde de modales. |

### 4.6 Estados

| Token | Valor | Uso |
| --- | --- | --- |
| `danger` | `#c0392b` | Borde de campo inválido. Variable `--metics-red`. |
| `danger-text` | `#c02f3d` | Texto de validación (13 px). 5,7:1. |
| `danger-badge-text` | `#bd1738` | Texto de la insignia "Inscripción finalizada". |
| `danger-badge-fill` | `#ffe1e6` | Fondo de insignias de peligro. |
| `danger-icon` | `#d93030` | Ícono del botón Eliminar. |
| `danger-icon-fill` | `#fff0f0` | Fondo del botón Eliminar. |
| `required-mark` | `#e3344b` | Asterisco de campo obligatorio. |
| `focus-halo` | `rgba(42, 95, 149, 0.14)` | Anillo de foco de los campos. |

### 4.7 Notificaciones

| Tono | Acento | Ícono | Fondo del ícono | Texto | Fondo final | Borde |
| --- | --- | --- | --- | --- | --- | --- |
| Éxito | `#168653` | `#168653` | `#def2e3` | `#174d2e` | `#f5fbf7` | `#c8dfd0` |
| Error | `#c83c32` | `#c83c32` | `#fde6e4` | `#742721` | `#fff7f6` | `#efcfcc` |
| Advertencia | `#d3980c` | `#a97608` | `#fff0c2` | `#765710` | `#fffaf0` | `#ead39a` |

## 5. Tipografía

### 5.1 Familia

`Inter, "Segoe UI", Arial, sans-serif`

Use solo estos pesos: 400, 450, 600, 650 y 700. No agregue pesos nuevos.

### 5.2 Estilos

| Estilo | Tamaño | Interlineado | Peso | Uso |
| --- | --- | --- | --- | --- |
| `page-title` | `clamp(1.65rem, 2.3vw, 2.25rem)` | 1,2 | 700 | Título de página (h1). Espaciado de letras −0,025em. |
| `modal-title` | 25 px | 1,25 | 700 | Título de modal. |
| `module-card-title` | 20,8 px | 28 px | 600 | Título de tarjeta de módulo. |
| `card-title` | 20 px | 1,3 | 700 | Título de tarjeta de sección. |
| `section-title` | 19 px | 1,3 | 700 | Título de sección de formulario. |
| `body` | 16 px | 24 px | 400 | Texto general y campos. |
| `control` | 14 px | 1,5 | 400 | Celdas de tabla, búsqueda, subtítulos. |
| `label` | 14 px | 1,5 | 600 | Etiquetas de campos. En modales: 16 px, peso 650. |
| `nav` | 14 px | 1,3 | 450 | Navegación del encabezado. |
| `table-head` | 13 px | 1,5 | 700 | Encabezado de tabla. |
| `caption` | 13 px | 1,4 | 400 | Mensajes de validación y ayuda. |
| `breadcrumb` | 12 px | 1,6 | 400 | Ruta de navegación. La página actual usa peso 600. |
| `badge` | 12 px | 1 | 600 | Insignias y contadores. |
| `button` | 14,4 px | 1,5 | 600 | Botón estándar. |
| `button-modal` | 16 px | 1,5 | 650 | Botones de modal. |

El título de página mide 26,4 px en teléfonos, 31,4 px a 1366 px y 36 px como máximo.

## 6. Espaciado, radios, sombras y tamaños

### 6.1 Espaciado

| Token | Valor | Uso |
| --- | --- | --- |
| `space-1` | 4 px | Entre título y subtítulo. |
| `space-2` | 8 px | Dentro de insignias; entre ícono y texto. |
| `space-3` | 12 px | Entre botones de una fila de acciones. |
| `space-4` | 16 px | Relleno superior del cuerpo de una tarjeta. |
| `space-5` | 20 px | Relleno lateral de tarjetas; separación entre tarjetas. |
| `space-6` | 24 px | Relleno lateral del encabezado de tarjeta y del encabezado de página. |
| `space-8` | 34 px | Relleno lateral de modales. |

Estos valores son observados. `site.css` no los declara como variables.

### 6.2 Radios

| Token | Valor | Uso |
| --- | --- | --- |
| `radius-sm` | 7 px | Búsqueda, botones de ícono, contador, botón "Crear módulo". |
| `radius-md` | 9 px | Campos y botones de modal, lista desplegable personalizada. |
| `radius-lg` | 10 px | Tarjetas de sección, filtro segmentado, notificaciones. |
| `radius-card` | 12 px | Tarjetas de módulo e insignias. |
| `radius-modal` | 20 px | Modales. |
| `radius-pill` | 999 px | Círculos y chips redondos. |

### 6.3 Sombras

| Token | Valor | Uso |
| --- | --- | --- |
| `shadow-card` | `0 4px 18px rgba(25, 54, 86, 0.06)` | Tarjetas de sección. |
| `shadow-module-card` | `0 3px 12px rgba(24, 52, 84, 0.07)` | Tarjetas de módulo. |
| `shadow-menu` | `0 14px 30px rgba(25, 54, 86, 0.16)` | Menús desplegables. |
| `shadow-toast` | `0 12px 32px rgba(25, 54, 86, 0.16)` | Notificaciones. |
| `shadow-modal` | `0 18px 50px rgba(20, 42, 73, 0.2)` | Modales. |
| `shadow-focus` | `0 0 0 3px rgba(42, 95, 149, 0.12)` | Foco de controles personalizados. |
| `shadow-invalid` | `0 0 0 3px rgba(192, 57, 43, 0.12)` | Campo inválido. |

### 6.4 Puntos de quiebre

| Token | Valor | Efecto |
| --- | --- | --- |
| `bp-sm-max` | 575.98 px | Teléfonos. Algunas tablas cambian a tarjetas. Las notificaciones ocupan todo el ancho. |
| `bp-md-max` | 767.98 px | Tabletas pequeñas. |
| `bp-lg-max` | 991.98 px | El encabezado cambia a menú de hamburguesa. |
| `bp-header-max` | 1339.98 px | La firma de la UCR se reduce. |

### 6.5 Alturas de controles

Hoy hay siete alturas en uso: 36, 38, 40, 42, 44, 46 y 48 px. En trabajo nuevo, use solo estas cuatro:

| Token | Valor | Uso |
| --- | --- | --- |
| `control-sm` | 40 px | Búsqueda de tablas y selector "Mostrar". |
| `control-md` | 44 px | Botones de formularios de página completa. |
| `control-lg` | 46 px | Botones de modal y lista desplegable personalizada. |
| `control-xl` | 48 px | Acción del encabezado de página, filtro segmentado, botón de vista. |

## 7. Estructura de página

Cada página de contenido tiene esta estructura, en este orden:

1. Encabezado institucional.
2. Ruta de navegación.
3. Título de página y una acción primaria como máximo.
4. Tarjetas de sección, separadas por 20 px.
5. Pie de página.

Ponga primero la lista o el formulario que da nombre a la página. Ponga las herramientas de importación, exportación y asignación masiva después.

## 8. Componentes

### 8.1 Encabezado

- Fondo: degradado de marca.
- Altura: 98 px. Con desplazamiento: 78 px.
- Debajo de 992 px, el encabezado cambia a menú de hamburguesa.
- Marcas, en este orden: UCR, separador blanco al 50 %, VD, METICS. Cada marca abre el sitio de su institución.
- Menú de administrador: Gestión académica, Personas, Inscripciones, Contenido.
- Menú de facilitador(a) y de participante: Inicio, Mi perfil, Cerrar sesión.
- Archivos: `wwwroot/images/firma-ucr-c.svg`, `ucr-whitelogo.png`, `VD-01.png`, `METICS-01.png`.

### 8.2 Encabezado de página

- La ruta de navegación sale de `BreadcrumbBuilder` y `_Breadcrumbs.cshtml`.
- La página actual en la ruta usa peso 600, color `#24364b` y `aria-current="page"`.
- Use un solo `h1` por página.
- Use un sustantivo para listas ("Niveles"). Use un verbo para formularios ("Crear módulo").
- Ponga la acción primaria a la derecha. En teléfonos, la acción ocupa todo el ancho.

### 8.3 Botón

Hay dos variantes:

| Variante | Fondo | Borde | Texto | Al pasar el cursor |
| --- | --- | --- | --- | --- |
| Primario | `action-blue` | `action-blue` | blanco | `action-hover` |
| Secundario | blanco | `interactive-blue` | `interactive-blue` | fondo `icon-surface` |

Reglas:

1. Use un solo botón primario por vista.
2. Ponga el verbo primero: "Guardar", "Crear módulo".
3. En un pie de acciones, ponga "Cancelar" antes de la acción primaria.
4. Alinee los botones a la derecha, con 12 px de separación.
5. Un botón deshabilitado conserva sus colores con menor opacidad.

Hoy hay tres tamaños:

| Contexto | Altura | Radio | Texto |
| --- | --- | --- | --- |
| Formularios y tarjetas | 44 px | 8 px | 14,4 px / 600 |
| Acción del encabezado de página | 48 px | 7 px | 14 px / 600 |
| Pie de modal | 46 px | 9 px | 16 px / 650 |

No agregue un cuarto tamaño.

### 8.4 Botón de ícono

- Tamaño: 36 × 36 px, radio 7 px.
- Editar: fondo `icon-surface`, ícono `bi-pencil-square` en `interactive-blue`.
- Eliminar: fondo `danger-icon-fill`, ícono `bi-trash3` en `danger-icon`.
- Eliminar siempre abre un modal de confirmación.
- **Obligatorio:** agregue `aria-label` con la acción y el registro. Ejemplo: `aria-label="Editar Nivel 1"`.

### 8.5 Insignia y contador

| Tipo | Fondo | Texto | Ejemplo |
| --- | --- | --- | --- |
| Informativa | `soft-blue` | `interactive-blue` | "Cupo: 12", "Inscripciones: 9" |
| Peligro | `danger-badge-fill` | `danger-badge-text` | "Inscripción finalizada" |
| Contador | `soft-blue` | `interactive-blue` | "4 niveles encontrados" |

- Insignia: 12 px, peso 600, relleno 7 px × 10 px, radio 12 px.
- Contador: radio 7 px.
- Las insignias no son botones. No tienen estado al pasar el cursor.
- El contador debe mostrar el total filtrado.

### 8.6 Tarjeta de sección

- Fondo `surface`, borde 1 px `border-card`, radio 10 px, sombra `shadow-card`.
- Encabezado: relleno 20 px × 24 px, línea inferior `divider`.
- El encabezado contiene, en este orden:
  1. Un ícono opcional de 44 px.
  2. El título y el subtítulo.
  3. Las acciones o el contador, a la derecha.
- Cuerpo: relleno 16 px arriba, 20 px a los lados, 18 px abajo.
- No use el estilo antiguo de `.table-card` (borde azul de 2 px, encabezado azul claro).

### 8.7 Tabla de datos

- Use DataTables dentro de una tarjeta de sección.
- Barra superior: campo de búsqueda de 40 px y selector "Mostrar N registros".
- Encabezado: estilo `table-head`, fondo `surface-table-head`, relleno 15 px × 18 px.
- Filas: 14 px, color `ink-body`, relleno 13 px × 18 px, línea `divider-row`.
- Contenedor: borde 1 px, radio 10 px, desplazamiento horizontal dentro de la tarjeta.
- La página nunca se desplaza hacia los lados.

Reglas:

1. Cargue el archivo de idioma español de DataTables por HTTPS.
2. Muestre el estado vacío en español.
3. Actualice el contador cuando el usuario filtra.
4. Agregue un nombre accesible a cada casilla de selección.

### 8.8 Campo de formulario

- Orden: etiqueta, control, texto de ayuda o de validación. Separación: 7 px.
- Campo: texto 16 px, color `ink-control`, radio 9 px, altura mínima 42 px.
- Foco: borde `interactive-blue` y anillo `focus-halo`.
- Campo inválido: borde `danger` y sombra `shadow-invalid`.
- Mensaje de validación: 13 px, color `danger-text`, debajo del campo.
- Campo obligatorio: asterisco `required-mark` después de la etiqueta, con `aria-hidden="true"`.
- Lista desplegable en formularios de página completa: use el control personalizado `.create-group-select-trigger` (46 px, radio 9 px).

Reglas:

1. Cada control debe tener `<label for="…">`.
2. Cada `id` debe ser único.
3. No muestre un campo solo con su borde. El contraste del borde es menor que 3:1.

### 8.9 Filtro segmentado

- Ejemplo: "Todos / Activos / Inactivos".
- Grupo: borde 1 px `border-filter`, radio 10 px, altura 48 px.
- Segmento activo: fondo `soft-blue`, texto `interactive-blue`, peso 700.
- Segmento inactivo: fondo blanco, texto `#334155`, peso 400.
- Indique el estado con `aria-pressed`.
- Botón "Modo lista / Modo cuadrícula": fondo `surface-toggle`, texto `#334155` peso 600. Este botón no aparece en teléfonos.

### 8.10 Tarjeta de módulo

- Fondo `surface`, borde 1 px `border-module-card`, radio 12 px, sombra `shadow-module-card`.
- Orden del contenido:
  1. Insignias.
  2. Título.
  3. Fechas.
  4. Detalles.
  5. Acción.
- Detalles: Número de grupo, Cantidad de horas, Facilitador(a), Área(s) de competencia, Nivel, Modalidad.
- Cada detalle tiene un ícono de 36 px sobre `icon-surface`.
- Acción del administrador: "Inscribir participante" (secundario) y "Ver participantes" (primario).
- Acción del participante: "Inscribirse". Este botón está deshabilitado si la inscripción terminó.
- Cada tarjeta debe tener un `id` único en la vista de cuadrícula y en la vista de lista.

### 8.11 Modal

- Fondo `surface`, borde 1 px `border-modal`, radio 20 px, sombra `shadow-modal`, ancho máximo 680 px.
- Encabezado: ícono de 54 px, título `modal-title`, subtítulo en `ink-muted-alt`, márgenes laterales de 34 px.
- Cuerpo: relleno 26 px arriba, 34 px a los lados, 16 px abajo. Separación entre campos: 22 px.
- Pie: "Cancelar" (secundario) y la acción primaria, a la derecha.
- La tecla Escape y el botón de cerrar cierran el modal.
- Al cerrar, el foco regresa al control que abrió el modal.
- Un modal de eliminación debe indicar qué se elimina. También debe indicar que la acción no se puede deshacer.

### 8.12 Notificación

- Archivo: `Views/Shared/_MeticsToast.cshtml`.
- Origen: `TempData["successMessage"]`, `TempData["errorMessage"]` y el mensaje de advertencia.
- Posición: arriba a la derecha, a 88 px del borde superior, ancho máximo 440 px.
- Forma:
  - Radio 10 px, sombra `shadow-toast` y borde 1 px.
  - Acento de 5 px a la izquierda.
  - Ícono redondo de 48 px.
- Título: 17 px, negrita. Mensaje: 15 px, color `#52626a`.
- Duración: 6,75 s.
- Roles:
  - Éxito: `role="status"`.
  - Advertencia: `role="status"`.
  - Error: `role="alert"`.
- Si el usuario pide movimiento reducido, la animación no se muestra.

## 9. Íconos y marcas

### 9.1 Íconos

- Use solo Bootstrap Icons (`bi-*`).
- Color: `interactive-blue`, normalmente sobre un fondo `icon-surface`.
- Tamaño del fondo:
  - 36 px en detalles y en acciones de fila.
  - 44 px en encabezados de tarjeta.
  - 54 px en modales.
- Íconos comunes:

| Acción | Ícono |
| --- | --- |
| Crear | `bi-plus-lg` |
| Editar | `bi-pencil-square` |
| Eliminar | `bi-trash3` |
| Subir / bajar archivo | `bi-upload` / `bi-download` |
| Buscar | `bi-search` |
| Información | `bi-info-circle-fill` |
| Inicio de la ruta | `bi-house-door-fill` |

### 9.2 Marcas

| Archivo | Descripción | Uso |
| --- | --- | --- |
| `firma-ucr-c.svg` | Firma de la UCR (conjunto de símbolos SVG) | Encabezado en escritorio, con `<use>`. |
| `ucr-whitelogo.png` | Logo blanco de la UCR | Encabezado en teléfonos. |
| `VD-01.png` | Marca blanca de la Vicerrectoría de Docencia | Encabezado, 48 × 44 px. |
| `METICS-01.png` | Marca blanca de METICS | Encabezado, 88 × 48 px. |
| `ucr-logo_azul.png` | Logo azul de la UCR | Ícono del navegador. |

- No modifique las marcas.
- Use las marcas blancas solo sobre el degradado de marca.
- Deje al menos 16 px de espacio libre alrededor de cada marca.

## 10. Accesibilidad

### 10.1 Requisitos

1. El texto debe tener un contraste de 4,5:1 o más.
2. El texto de 24 px o más debe tener un contraste de 3:1 o más.
3. Los íconos, los bordes de controles y los anillos de foco deben tener un contraste de 3:1 o más.
4. Cada control debe tener un nombre accesible.
5. Cada `id` debe ser único en la página.

### 10.2 Problemas conocidos

| Problema | Lugar |
| --- | --- |
| Texto con contraste 4,0:1 (`ink-faint`) | Subtítulo `.table-card-desc` |
| Bordes de campos con contraste de 1,3:1 a 1,45:1 | Campos, búsqueda, listas desplegables |
| Campo de contraseña sin etiqueta | Inicio de sesión |
| Listas sin etiqueta: Área, Departamento, Unidad Académica, Carrera | Formularios de participante y de registro |
| Casillas sin nombre | Inscripciones, Calificaciones |
| Enlaces de ícono sin nombre | Calificaciones, Crear facilitador(a) |
| `id` duplicado `condicion` | Botones de opción del registro |
| `id` duplicados `module-N` y `noModulesMessageGrid` | Lista de módulos (cuadrícula y lista) |
| `id` duplicados `medallaSeleccionada` y `lugarText` | Inscripciones, Editar módulo |

## 11. Problemas de consistencia

Corrija estos problemas cuando modifique cada área.

1. **Tamaños de botón primario.** Hay tres tamaños en uso. Use la tabla de la sección 8.3.
2. **Alturas de controles.** Hay siete alturas en uso. Use la tabla de la sección 6.5.
3. **Campos de "Crear módulo".** Usan el estilo de Bootstrap por defecto (38 px, `#dee2e6`, radio 6 px). Cambie los campos al estilo de la sección 8.8.
4. **Página de calificaciones.** `Calificaciones/VerCalificaciones` usa el selector de archivo nativo del navegador. Use el control de carga de la lista de participantes del módulo.
5. **Colores casi iguales.** `site.css` tiene 190 colores distintos. Reemplace estos grupos por un solo token:
   - Grises de texto: `#526b8b`, `#59708f`, `#526b88`, `#58718c` → `ink-muted`.
   - Grises de borde: `#e1e8f0`, `#e5ebf3`, `#e8eef5`, `#d9e2ed`, `#d9e2ee`, `#d3deeb`, `#cbd8e8`, `#cbd5e1`, `#dce6f2`.
   - Opacidades del anillo de foco: 0,12, 0,14, 0,17 y 0,2.
6. **Radios.** Hay 13 radios en uso. Use la tabla de la sección 6.2.
7. **Estilo antiguo de `.table-card`.** Las reglas siguen en `site.css`. Elimine esas reglas cuando ninguna vista las use.
8. **Estados vacíos y filtrados:**
   - El contador no cambia cuando el usuario filtra.
   - La búsqueda de módulos sin resultados muestra un área vacía.
   - DataTables muestra texto en inglés si el archivo de idioma no carga.
9. **Posición de la notificación.** La notificación está a 88 px del borde superior. El encabezado mide 98 px. Por eso la notificación cubre 10 px del encabezado.

## 12. Procedimiento: agregar una función nueva

1. Busque una página existente con la misma forma:

   | Tipo de página | Ejemplo |
   | --- | --- |
   | Lista | Niveles |
   | Formulario largo | Crear módulo |
   | Formulario corto | Modal "Crear nivel" |
   | Detalle | Ver datos del participante |

2. Copie el marcado y las clases de la vista Razor de esa página.
3. Arme la página con los componentes de la sección 8.
4. Use los tokens de este documento para cada color, tamaño, radio y sombra.
5. Si necesita un valor nuevo, compare el valor con los tokens existentes.
6. Si ningún token sirve, agregue el token nuevo a este documento.
7. Escriba los textos según la sección 3.
8. Pruebe la página a 1366 px y a 375 px.
9. Pruebe la página con cada rol que la puede ver: administrador, facilitador(a) y participante.
10. Verifique las etiquetas, los `id` y el contraste según la sección 10.

## 13. Límites de este documento

- Este documento no incluye:
  - Los elementos de los menús desplegables del encabezado.
  - El menú lateral para teléfonos.
  - El carrusel y el catálogo de medallas.
  - El menú lateral del panel de administración.
  - La paginación.
  - El botón "Volver arriba".
- Los valores de espaciado y de radio son observados. `site.css` no los declara como variables.
- La fuente Inter se carga desde Google Fonts. El repositorio no contiene los archivos de la fuente.
