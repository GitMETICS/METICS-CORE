<h1>Sistema de matrícula-METICS</h1>
<h2>Descripción</h2>
  <p>
Se está desarrollando un sistema de matrícula de cursos, talleres y otras actividades para el personal docente y administrativo de la Universidad de Costa Rica como parte de los esfuerzos de la Vicerrectoría de Docencia
en fomentar, apoyar y capacitar en las tecnologías de información y comunicación y el adecuado uso de la plataforma METICS para los distintos procesos académicos de la universidad.

Los participantes, que serían los docentes y administrativos, podrán inscribirse a grupos que llevarán a cabo alguna actividad; ya sea un taller, una charla, un curso.
Cada grupo será impartido por un profesor guía y por profesores asesores. La información y detalles del grupo serán brindados en el momento de la inscripción del mismo.
 El sistema permitirá llevar un control de las horas que cada participante ha llevado hasta el momento que se desee cerrar las actas.

Los flujos de las funcionalidades implementadas serán explicadas más adelante. Así también como las funcionalidades pendientes de desarrollar.
  </p>

<h2>Prerrequisitos</h2>

- SQL Server
  - Instalar SQL Server
  - Microsoft SQL Server Management
    - Instalar Microsoft SSMS
    - Crear base de datos local
    - Crear las tablas en la base de datos local corriendo el archivo “db_METICS.sql” sobre la base de datos creada anteriormente.
- Instalar Microsoft Visual Studio (versión más actualizada)
  - Agregar e instalar en el instalador del Visual Studio los componentes de:
    - ASP.NET and web development
    - .Net desktop development
    - Desktop development with C++
    - Data storage and processing

<h2>Instalación</h2>

- Clonar el repositorio
- Abrir el archivo appsettings.json y configurar los datos de:
  - Correo electrónico
  - Connection String a la base de datos
- Instalar los paquetes que utiliza el proyecto (instalar dependencias del proyecto).

Para información más detallada revisar la [Documentación del Proyecto aquí](www.Todo.file).
