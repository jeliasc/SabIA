# Matriz de pruebas de entrega final

Fecha: 16 de octubre de 2026. La matriz registra únicamente ejecuciones realizadas durante esta auditoría o cobertura presente en la suite.

| Comando o suite | Resultado | Cobertura relevante |
| --- | --- | --- |
| `dotnet build --no-restore -v:minimal` | Correcto; 0 advertencias, 0 errores | Compilación de aplicación, Razor y referencias. |
| `dotnet vstest Tests\Proyecto_Final.Tests\bin\Debug\net10.0\Proyecto_Final.Tests.dll /TestCaseFilter:"FullyQualifiedName!~Postgres" /Logger:"console;verbosity=minimal"` | 198 aprobadas de 198 | Servicios académicos, tareas, entregas, cuestionarios, calificaciones, notificaciones, auditoría, IA simulada, presentación y validación. |
| `dotnet vstest Tests\Proyecto_Final.Tests\bin\Debug\net10.0\Proyecto_Final.Tests.dll /TestCaseFilter:"FullyQualifiedName~Postgres" /Logger:"console;verbosity=minimal"` con `SABIA_PRUEBAS_PG_CONFIRMAR=BASE_DESCARTABLE` | 9 aprobadas de 9; 0 fallidas, 0 omitidas | PostgreSQL real temporal, cuestionarios, fechas de tareas, migración y reversión de versionado de asignaciones. |
| `node --test Tests\js\*.test.js` | 4 aprobadas de 4 | Tablas DataTables, acciones de cuestionario y restauración de archivo/borrador. |
| `git diff --check` (antes de la documentación) | Sin salida; correcto | Espacios finales y errores de diff. |

## Casos cubiertos destacados

| Área | Clases de prueba existentes incluidas en la ejecución .NET |
| --- | --- |
| Login, validación y enum | `PresentacionYValidacionTests`: campos obligatorios, credenciales genéricas, bloqueo, redirección válida, persistencia de valores de enum y nombres Display. |
| Asignaciones y continuidad | `AsignacionServicioTests`, `VersionarAsignacionesPostgresTests` (4/4: conservación de filas y referencias, unicidad y reversión segura), `AccesoAcademicoServicioTests`. |
| Tareas y entregas | `TareaServicioTests`, `TareasControllerTests`, `EntregaServicioTests`, `FechasTareasPostgresTests` (2/2: conversiones Guatemala/UTC y persistencia de fechas). |
| Cuestionarios e IA | `CuestionarioServicioTests`, `ImportarCuestionarioIaTests`, `LectorCuestionarioIaTests`, `IaServicioTests`, `CuestionarioPostgresTests` (3/3: flujos, intento único y finalización de vencidos). |
| Calificaciones | `CalculoCalificacionesTests`, `FlujoCalificacionesTests`, `ModeloCalificacionesTests`, `SeguridadAcademicaCalificacionesTests`. |
| Operación transversal | `AuditoriaServicioTests`, `NotificacionServicioTests`, `InscripcionServicioTests`, `MaterialServicioTests`, `UnidadServicioTests`, `PeriodoServicioTests`, `CicloServicioTests`. |

## Validación funcional: perfil Coordinación

La aplicación no define un rol fijo llamado «Coordinación». El perfil se determina por claims: `Planificaciones.Revisar` identifica el rol coordinador y el seeder añade `Dashboard.Coordinacion.Ver`, `Calificaciones.AprobarCorreccion`, `Ciclos.Cerrar` y `Ciclos.Reabrir`. La prueba HTTP crea un principal efímero con el conjunto explícito de permisos del escenario; no crea ni conserva usuarios, roles o datos fuera de PostgreSQL temporal.

| Módulo | Operación evaluada | Perfil y permisos | Resultado | Evidencia | Estado |
| --- | --- | --- | --- | --- | --- |
| Dashboard | Abrir el contexto `coordinacion` | `Dashboard.Coordinacion.Ver` y permisos de lectura disponibles | Devuelve 200 y renderiza el dashboard | `CoordinacionHttpTests.Coordinacion_PuedeAbrirDashboardYAccionesAutorizadas` | Aprobado |
| Estructura académica | Abrir creación de ciclos | `Ciclos.Ver`, `Ciclos.Crear` | Devuelve 200 | Misma prueba HTTP | Aprobado |
| Planificaciones | Solicitar revisión de un identificador inexistente | `Planificaciones.Ver`, `Planificaciones.Revisar` | Pasa autorización y devuelve 404, no 403 | Misma prueba HTTP | Aprobado |
| Calificaciones | Intentar configurar evaluación sin permiso | No se concede `Calificaciones.Configurar` | Devuelve 403 | `CoordinacionHttpTests.Coordinacion_NoObtienePermisosNoAsignados_YLosPostExigenAntiforgery` | Aprobado |
| Usuarios | Intentar abrir administración de usuarios | No se concede `Usuarios.Ver` | Devuelve 403 | Misma prueba HTTP | Aprobado |
| Antiforgery | POST de activación de ciclo sin token | `Ciclos.Activar`, sin token antiforgery | Devuelve 400 antes de modificar datos | Misma prueba HTTP | Aprobado |
| Ciclos, períodos y cierre | Activación, cierre, reapertura, vigencia y rechazo de permisos | Claims de Coordinación y combinaciones negativas | 14 escenarios de servicio, auditoría y restricciones | `CicloServicioTests` | Aprobado |
| Asignaciones y relevo | Ordinario, excepcional, obligaciones, delegación e integridad | Operador institucional y pruebas negativas | Se conserva historial y se rechazan incompatibilidades | `AsignacionServicioTests` | Aprobado |
| Inscripciones y acceso académico | Vigencia, traslado, ámbito contextual y protección por identificador | Perfiles académicos e institucional | Sin acceso por defecto ni exposición contextual indebida | `InscripcionServicioTests`, `AccesoAcademicoServicioTests`, `AccesoContextualAlumnosTests` | Aprobado |
| Calificaciones | Cierre, correcciones y límites de Coordinación | Claims de Coordinación y negativos | Coordinación no cierra período solo por un permiso técnico | `FlujoCalificacionesTests`, `SeguridadAcademicaCalificacionesTests` | Aprobado |
| Auditoría | Registro de operaciones y rechazos | Escenarios institucionales | Auditoría verificada por suite existente | `AuditoriaServicioTests` | Aprobado |

Comandos ejecutados durante esta fase:

- `dotnet vstest ... /TestCaseFilter:"FullyQualifiedName~CoordinacionHttpTests"`: 2 aprobadas de 2, contra PostgreSQL temporal.
- `dotnet vstest ... /TestCaseFilter:"...CicloServicioTests|...AsignacionServicioTests|...InscripcionServicioTests|...AccesoAcademicoServicioTests|...AccesoContextualAlumnosTests|...FlujoCalificacionesTests|...SeguridadAcademicaCalificacionesTests|...AuditoriaServicioTests"`: 65 aprobadas de 65.

Las pruebas HTTP usan un host de prueba, un principal de Coordinación efímero y claves de Data Protection temporales. No sustituyen un recorrido de navegador con usuario humano. Permanecen pendientes la verificación visual en escritorio y móvil, el alta completa de docentes/alumnos/encargados desde formularios y el ciclo de aprobación/rechazo real de una planificación; no se presentan como ejecutados.

## Validación funcional: perfil Docente

La prueba HTTP crea usuarios ficticios mediante `UserManager`, asigna el rol real `Docente` y obtiene los claims del rol con `RoleManager`. Los datos académicos (ciclo, secciones, asignaciones, inscripciones, unidad, planificación, tareas y entregas) existen únicamente en una base PostgreSQL temporal, aleatoria y eliminada al terminar cada caso.

| Módulo | Operación evaluada | Perfil y permisos | Resultado | Evidencia | Estado |
| --- | --- | --- | --- | --- | --- |
| Rol efectivo | Cargar claims del rol `Docente` sembrado | `Tareas.Crear`, `Calificaciones.Configurar` y permisos académicos; sin revisión/aprobación institucional | Se confirman permisos concedidos y ausentes | `DocenteHttpTests` durante la preparación del escenario | Aprobado |
| Dashboard y perfil | Abrir contexto `docente`, perfil, alumnos y notificaciones | Usuario Docente activo con asignación vigente y `Alumnos.Ver`, `Notificaciones.Ver` | 200; muestra solo curso, sección y alumnos propios | `Docente_ConRolSembrado_VisualizaSoloSuContextoAcademico` | Aprobado |
| Unidades, planificaciones y tareas | Abrir formularios de creación | `Unidades.Crear`, `Planificaciones.Crear`, `Tareas.Crear` | 200 en cada formulario; opciones preparadas con asignación propia | Misma prueba HTTP; `UnidadServicioTests`, `TareaServicioTests` | Aprobado |
| Entregas | Abrir calificación de entrega propia | `Entregas.Calificar`, asignación vigente propia | 200 | Misma prueba HTTP; `EntregaServicioTests` | Aprobado |
| Cuestionarios e IA | Abrir importación con tarea propia y validar importación/lectura | `Cuestionarios.Editar`; dobles HTTP de IA | 200;  pruebas de lectura/importación sin proveedor externo | Misma prueba HTTP; `CuestionarioServicioTests`, `ImportarCuestionarioIaTests`, `LectorCuestionarioIaTests`, `IaServicioTests` | Aprobado |
| Seguridad institucional | Intentar revisar planificación y abrir administración de docentes | Sin `Planificaciones.Revisar` ni `Docentes.Ver` | 403 | `Docente_NoAccedeARutasNiRecursosAjenos_YLosPostExigenAntiforgery` | Aprobado |
| Seguridad contextual | Cambiar identificadores para alumno, entrega y configuración ajenos | Permisos de lectura/gestión, sin asignación ajena | 404; no expone ni permite operar recurso ajeno | Misma prueba HTTP; `AccesoAcademicoServicioTests`, `AccesoContextualAlumnosTests`, `EntregaServicioTests`, `SeguridadAcademicaCalificacionesTests` | Aprobado |
| Antiforgery | POST de envío de planificación sin token | `Planificaciones.EnviarRevision`, sin token | 400 antes de la operación | `Docente_NoAccedeARutasNiRecursosAjenos_YLosPostExigenAntiforgery` | Aprobado |
| Persistencia PostgreSQL | Fechas de tareas, cuestionarios, intentos y vencimiento | Datos temporales aislados | 5 de 5 | `FechasTareasPostgresTests`, `CuestionarioPostgresTests` | Aprobado |

Comandos ejecutados durante esta fase:

- `dotnet vstest ... /TestCaseFilter:"FullyQualifiedName~DocenteHttpTests"`: 2 aprobadas de 2, PostgreSQL temporal.
- `dotnet vstest ... /TestCaseFilter:"...AccesoAcademicoServicioTests|...AccesoContextualAlumnosTests|...UnidadServicioTests|...MaterialServicioTests|...TareaServicioTests|...TareasControllerTests|...EntregaServicioTests|...CuestionarioServicioTests|...ImportarCuestionarioIaTests|...LectorCuestionarioIaTests|...IaServicioTests|...CalculoCalificacionesTests|...ModeloCalificacionesTests|...FlujoCalificacionesTests|...SeguridadAcademicaCalificacionesTests|...NotificacionServicioTests|...PresentacionYValidacionTests"`: 162 aprobadas de 162.
- `dotnet vstest ... /TestCaseFilter:"FullyQualifiedName~CuestionarioPostgresTests|FullyQualifiedName~FechasTareasPostgresTests"`: 5 aprobadas de 5, PostgreSQL temporal.

No se ejecutó una prueba E2E de navegador; queda pendiente antes de demostración la revisión manual responsive de dashboard, formularios, selects con búsqueda, mensajes de validación, SweetAlert2 y visibilidad de acciones según permisos.

## Validación funcional: perfil Alumno

`AlumnoHttpTests` reutiliza el patrón de host de prueba y PostgreSQL temporal de los perfiles anteriores. El manejador de autenticación de pruebas obtiene roles y claims desde Identity; no codifica permisos manuales ni afecta configuración de producción. Los usuarios, datos académicos, claves de Data Protection y bases generadas son descartables.

| Módulo | Operación evaluada | Perfil y permisos | Resultado | Evidencia | Estado |
| --- | --- | --- | --- | --- | --- |
| Rol efectivo | Cargar claims del rol `Alumno` sembrado | `Entregas.Entregar`, `Cuestionarios.Resolver`; sin creación docente ni configuración de calificaciones | Se confirman permisos concedidos y ausentes | `AlumnoHttpTests` durante la preparación del escenario | Aprobado |
| Dashboard y perfil | Abrir contexto `alumno`, perfil e inscripción vigente | Alumno activo con inscripción y `Alumnos.Ver` | 200; muestra únicamente el curso propio | `Alumno_ConRolSembrado_VisualizaSoloSuActividadVigente` | Aprobado |
| Actividad académica | Consultar unidad, tareas, entregas, entrega pendiente, detalle, calificaciones y notificaciones | Permisos de consulta del rol Alumno | 200; recursos vinculados a la inscripción vigente | Misma prueba HTTP; `EntregaServicioTests`, `TareasControllerTests`, `NotificacionServicioTests` | Aprobado |
| Cuestionarios | Abrir cuestionario propio; intentos, respuestas, vencimiento y duplicados | `Cuestionarios.Ver`, `Cuestionarios.Resolver` | 200 en HTTP; flujos y restricciones en servicios | Misma prueba HTTP; `CuestionarioServicioTests`, `CuestionarioPostgresTests` | Aprobado |
| Seguridad contextual | Cambiar ID de alumno, entrega y cuestionario por recursos ajenos | Rol Alumno sin pertenencia académica | 404; no expone recurso ajeno | `Alumno_NoAccedeARecursosAjenosNiOperacionesDocentes_YLosPostExigenAntiforgery`; `AccesoAcademicoServicioTests`, `AccesoContextualAlumnosTests` | Aprobado |
| Acciones docentes | Abrir creación de tarea/unidad/planificación y configuración de calificaciones | Sin `Tareas.Crear`, `Unidades.Crear`, `Planificaciones.Ver`, `Calificaciones.Configurar` | 403 | Misma prueba HTTP | Aprobado |
| Antiforgery | POST para iniciar cuestionario sin token | `Cuestionarios.Resolver`, sin token | 400 antes de iniciar el intento | Misma prueba HTTP | Aprobado |
| Persistencia PostgreSQL | Fechas, cuestionarios, intentos únicos y vencimiento | Datos temporales aislados | 5 de 5 | `FechasTareasPostgresTests`, `CuestionarioPostgresTests` | Aprobado |

Comandos ejecutados durante esta fase:

- `dotnet vstest ... /TestCaseFilter:"FullyQualifiedName~AlumnoHttpTests"`: 2 aprobadas de 2, PostgreSQL temporal.
- `dotnet vstest ... /TestCaseFilter:"...AccesoAcademicoServicioTests|...AccesoContextualAlumnosTests|...EntregaServicioTests|...CuestionarioServicioTests|...TareasControllerTests|...NotificacionServicioTests|...SeguridadAcademicaCalificacionesTests|...PresentacionYValidacionTests"`: 69 aprobadas de 69.
- `dotnet vstest ... /TestCaseFilter:"FullyQualifiedName~CuestionarioPostgresTests|...FechasTareasPostgresTests"`: 5 aprobadas de 5, PostgreSQL temporal.

No se ejecutó una prueba E2E de navegador. Antes de demostración queda pendiente validar manualmente el dashboard, perfil, tablas/tarjetas responsive, selects, mensajes de validación, botones condicionales y confirmaciones en escritorio y móvil.

## Validación integral: Inteligencia Artificial

| Área | Escenario | Resultado | Evidencia | Estado |
| --- | --- | --- | --- | --- |
| Configuración externa | Clave versionada vacía y URL privada histórica | No se consultaron secretos locales ni se llamó al proveedor | `appsettings.json`, `ConfiguracionIa` | Bloqueado de forma segura |
| Generación directa | Prompt, tokens, encabezado, HTTP, timeout, cancelación y JSON inválido | Contrato y errores controlados con proveedor simulado | `IaServicioTests` | Aprobado simulado |
| Generación desde PDF | Multipart, tipos, instrucciones, archivo local, tamaño, MIME, extensión y firma | Solo PDF válido llega al cliente HTTP; archivo local no se persiste | `IaServicioTests` | Aprobado simulado |
| Trabajos asíncronos | `job_id`, estados pendiente/listo/error, propiedad, expiración lógica y deduplicación | Solo el propietario consulta; respuesta malformada produce error seguro | `IaServicioTests` | Aprobado simulado |
| Seguridad contextual | Permiso de IA y planificación ajena | Rechazo antes de enviar documento ajeno | `IaServicioTests.GenerarDesdeDocumento_PlanificacionAjena_NoEnviaDocumentoAlProveedor` | Aprobado simulado |
| Importación académica | Lectura de texto/JSON, vista previa, preguntas inválidas y guardado autorizado | No publica ni guarda automáticamente sin revisión | `LectorCuestionarioIaTests`, `ImportarCuestionarioIaTests`, `CuestionarioServicioTests` | Aprobado simulado |

Comando ejecutado: `dotnet vstest ... /TestCaseFilter:"FullyQualifiedName~IaServicioTests|...LectorCuestionarioIaTests|...ImportarCuestionarioIaTests|...CuestionarioServicioTests"`: 67 aprobadas de 67, 0 fallidas y 0 omitidas.

No hubo pruebas reales contra proveedor ni medición de tiempos externos. Para completarlas se requiere una clave suministrada por configuración segura, red autorizada y una solicitud ficticia mínima; no deben declararse cubiertas antes de esa ejecución.

## Pruebas no ejecutadas y motivo

- No se hicieron llamadas reales a IA ni a correo. Las pruebas de IA usan dobles de `HttpMessageHandler` y datos de prueba.
- No se ejecutó una prueba E2E de navegador. Debe efectuarse manualmente con datos no sensibles antes de la demostración.

## Criterio de repetición

Tras cualquier cambio de código, repetir compilación, la suite .NET no PostgreSQL, las pruebas JavaScript y `git diff --check`. Para PostgreSQL, crear un clúster nuevo y aislado, verificar su directorio y puerto antes de usarlo, establecer `SABIA_PRUEBAS_PG` contra su base administrativa y confirmar `SABIA_PRUEBAS_PG_CONFIRMAR=BASE_DESCARTABLE`. Nunca usar configuraciones de desarrollo, Render o producción.
