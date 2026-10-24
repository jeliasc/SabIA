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

## Paso 6: matriz de demostración en navegador

No hubo controlador de navegador disponible. Estos casos no se consideran aprobados por pruebas HTTP o de servicio; deben ejecutarse manualmente en Kestrel con una base aislada y datos ficticios.

| Orden | Escenario manual | Resultado esperado | Estado |
| --- | --- | --- | --- |
| 1 | Coordinación inicia sesión y abre dashboard | Login en español y navegación de Coordinación visible | Pendiente de comprobación manual |
| 2 | Configura ciclo, períodos, grado, curso y sección | Formularios, selects y confirmaciones funcionan | Pendiente de comprobación manual |
| 3 | Registra docente, alumno y encargado ficticios | Validaciones y resultado de guardado visibles | Pendiente de comprobación manual |
| 4 | Crea asignación e inscripción | Contexto académico esperado | Pendiente de comprobación manual |
| 5 | Docente inicia sesión | Ve únicamente cursos y alumnos propios | Pendiente de comprobación manual |
| 6 | Docente crea unidad y planificación | Borrador, validaciones y envío a revisión visibles | Pendiente de comprobación manual |
| 7 | Coordinación revisa planificación | Puede aprobar/devolver; Docente no accede a revisión | Pendiente de comprobación manual |
| 8 | Docente publica material y tarea | Alumno solo ve lo disponible en su contexto | Pendiente de comprobación manual |
| 9 | Docente crea/importa cuestionario IA simulado | Vista previa editable; sin proveedor ni clave real | Pendiente de comprobación manual |
| 10 | Alumno inicia sesión y revisa actividades | Dashboard y tarjetas muestran solo su inscripción | Pendiente de comprobación manual |
| 11 | Alumno entrega o responde cuestionario | Estados, fechas y mensajes se muestran correctamente | Pendiente de comprobación manual |
| 12 | Docente consulta y califica | Solo califica contexto propio | Pendiente de comprobación manual |
| 13 | Alumno consulta resultados | Solo aparecen resultados propios autorizados | Pendiente de comprobación manual |
| 14 | Coordinación revisa estadísticas, notificaciones y auditoría | Datos ficticios consistentes, sin secretos | Pendiente de comprobación manual |

Controles transversales pendientes: repetir Login, formularios, sidebar, DataTables, selects con búsqueda, SweetAlert2, PDF, enlaces protegidos y errores JavaScript en escritorio y móvil. Como negativos, probar URL ajena, acción docente con Alumno, acción institucional con Docente y POST sin antiforgery; deben dar 404/403/400 según corresponda y no modificar datos.

### Evidencia E2E ejecutada en Paso 6.1

| Paso | Ejecutado realmente en navegador | Resultado | Evidencia | Observación |
| --- | --- | --- | --- | --- |
| 1. Login de Coordinación | Sí | Aprobado | `Tests/E2E/sabia.e2e.spec.mjs`, Playwright 2/2 | Escritorio: requerido, credencial inválida, sesión, cambio inicial y ausencia de «Recordarme». Móvil 390×844: campos y validación visibles. |
| 2–14. Recorrido académico | No | Pendiente | No se declara evidencia E2E | Requiere siembra completa de datos ficticios e interacciones de navegador por perfil; las pruebas HTTP previas no sustituyen esta fila. |

### Evidencia E2E ejecutada en Paso 6.2

| Escenario | Ejecutado realmente en Chrome | Resultado | Evidencia comprobada | Observación |
| --- | --- | --- | --- | --- |
| Docente crea unidad, planificación y la envía a revisión | Sí | Aprobado | Formularios y SweetAlert2; filas `Unidades`, `Planificaciones`, `PlanificacionesAsignaciones`, `PlanificacionesDetalles` y auditoría temporal | Acciones hechas desde el navegador; no por el seeder. |
| Coordinación aprueba la planificación | Sí | Aprobado | Botón `Aprobar` en `/Planificaciones/Revisar/1`; estado, fechas, aprobador y dos registros `Planificaciones/Revisar` persistidos | El primer test excedió el timeout global después de iniciar la operación; la operación finalizó y se comprobó en PostgreSQL temporal. |
| Docente crea y publica tarea | Sí | Aprobado | Formulario `/Tareas/Crear`, SweetAlert2 «Publicar tarea», estado publicado, fechas y auditoría `Tareas/Crear` y `Tareas/Publicar` | Prueba final Playwright: 1/1 aprobada. |
| Alumno visualiza tarea disponible en móvil | Sí | Aprobado | Chrome 390×844, `/Tareas`, texto `Tarea visible E2E`, `Curso E2E` y `E2E A` | La disponibilidad se configuró vigente en hora de Guatemala. |
| Alumno no ve tareas de otra sección | Sí | Aprobado | Chrome móvil no mostró `Tarea ajena E2E`; PostgreSQL temporal confirmó que estaba publicada en `Curso ajeno E2E`, sección B | El Alumno probado tiene inscripción solo en A; B cuenta con un Alumno ficticio diferente para que la publicación sea válida. |

### Evidencia E2E ejecutada en Paso 6.3

| Escenario | Ejecutado realmente en Chrome | Resultado | Evidencia comprobada | Observación |
| --- | --- | --- | --- | --- |
| Alumno entrega tarea de su sección | Sí | Aprobado | Formulario de `/Entregas/Entregar`, archivo ficticio, mensaje de éxito y fila temporal de `Entregas` para `E2E-ALU` | La tarea y entrega fueron creadas desde la interfaz; el seeder no creó ninguna entrega. |
| Docente consulta y califica | Sí | Aprobado | Formulario de calificación, nota `8.00`, retroalimentación, persistencia y auditoría `Entregas/Entregar` y `Entregas/Calificar` | El Docente es el asignado al curso A. |
| Alumno consulta su resultado | Sí | Aprobado | Chrome móvil, historial de intentos con estado calificado, nota y retroalimentación | Verificación Playwright de solo lectura: 1/1 aprobada en 55.3 s. |
| Alumno ajeno no consulta la entrega | Sí | Aprobado | Solicitud autenticada desde el contexto Chrome del Alumno de B a la entrega de A devolvió 404 | Sin exposición ni modificación de datos. |
| Docente sin asignación no califica | Sí | Aprobado | Solicitud autenticada desde el contexto Chrome del Docente ajeno a `Calificar` devolvió 404 | Sin cambio de calificación. |

## Pruebas no ejecutadas y motivo

- No se hicieron llamadas reales a IA ni a correo. Las pruebas de IA usan dobles de `HttpMessageHandler` y datos de prueba.
- No se ejecutó una prueba E2E de navegador. Debe efectuarse manualmente con datos no sensibles antes de la demostración.

## Criterio de repetición

Tras cualquier cambio de código, repetir compilación, la suite .NET no PostgreSQL, las pruebas JavaScript y `git diff --check`. Para PostgreSQL, crear un clúster nuevo y aislado, verificar su directorio y puerto antes de usarlo, establecer `SABIA_PRUEBAS_PG` contra su base administrativa y confirmar `SABIA_PRUEBAS_PG_CONFIRMAR=BASE_DESCARTABLE`. Nunca usar configuraciones de desarrollo, Render o producción.

### Evidencia E2E ejecutada en Paso 6.4

| Escenario | Ejecutado realmente en Chrome | Resultado | Evidencia comprobada | Observación |
| --- | --- | --- | --- | --- |
| Docente configura y publica cuestionario | Sí | Aprobado | Formulario de tarea y configuración; tres preguntas (múltiple, V/F y directa); SweetAlert2 de publicación; filas temporales de tarea, cuestionario y preguntas | El seeder no creó el cuestionario. |
| Alumno inicia, navega, responde y finaliza | Sí | Aprobado | Chrome móvil, formulario de intento, tres respuestas, resultado visible 100 % | Persistencia: intento finalizado, calificación `100.00` y tres respuestas. |
| Docente consulta resultados | Sí | Aprobado | Pantalla de resultados y detalle del intento del Alumno A | Auditoría temporal de configuración, respuestas y finalización. |
| Alumno ajeno no accede a cuestionario/intento | Sí | Aprobado | Solicitudes autenticadas desde contexto Chrome de Alumno B devolvieron 404 | Sin exposición ni cambio de datos. |
| Docente sin asignación no consulta resultados | Sí | Aprobado | Solicitud autenticada desde contexto Chrome del Docente ajeno devolvió 404 | Sin exposición ni cambio de datos. |

### Evidencia E2E ejecutada en Paso 6.5

| Escenario | Ejecutado realmente en Chrome | Resultado | Evidencia comprobada | Observación |
| --- | --- | --- | --- | --- |
| Generación desde PDF con IA simulada | Sí | Aprobado | Carga multipart del PDF ficticio, trabajo pendiente y sondeo posterior completado | Proveedor HTTP local; no hubo conexión externa ni clave real. |
| Vista previa editable e importación | Sí | Aprobado | Edición del enunciado, redirección a Configurar y persistencia de dos preguntas | Tarea permanece en borrador; no se publicó automáticamente. |
| Persistencia y auditoría | Sí | Aprobado | PostgreSQL: origen IA, dos preguntas, texto corregido; auditoría `ConfirmarImportacionIa` | Datos eliminados con la base temporal. |
| Validaciones y antiforgery | Sí | Aprobado con observación | Archivo requerido/MIME inválido muestran resumen español; POST sin token devuelve 400 | Falta mensaje por campo más específico para el archivo inválido. |
| Restricciones de IA | Sí | Aprobado | Alumno recibe denegación/redirección sin acceso a la función; Docente ajeno no ve planificación de A | Catálogo temporal confirma permiso solo para Docente/Superusuario. |

## Paso 7: matriz consolidada vigente

Esta tabla reemplaza para cierre las menciones cronológicas que indiquen que no se ejecutó E2E. Las filas de detalle anteriores se mantienen como evidencia, pero no deben sumarse entre sí ni con las suites HTTP, .NET o JavaScript.

| Recorrido o control | Clasificación | Estado | Evidencia |
| --- | --- | --- | --- |
| Compilación y regresión automatizada | Prueba automatizada | Aprobado | 198 .NET sin PostgreSQL, 9 PostgreSQL temporal y 4 JavaScript ya documentadas. |
| Login y entrada de Coordinación | Navegador | Aprobado | Chrome: `sabia.e2e.spec.mjs`, 2/2; escritorio y móvil. |
| Docente consulta contexto académico | Navegador | Aprobado | Navegación de cursos y alumnos con cuenta ficticia autorizada. |
| Crear unidad y planificación; enviar a revisión | Navegador | Aprobado | Formularios reales, confirmación SweetAlert2 y persistencia temporal. |
| Coordinación aprueba planificación | Navegador | Aprobado | Interfaz, estado, fechas, aprobador y auditoría temporal. |
| Crear y publicar tarea; Alumno la visualiza | Navegador | Aprobado | Interfaz de Docente y vista móvil del Alumno; aislamiento entre secciones. |
| Entrega, calificación y consulta de resultado | Navegador | Aprobado | Formularios reales, persistencia, auditoría y denegaciones 404 cruzadas. |
| Cuestionario: configurar, resolver y consultar | Navegador | Aprobado | `cuestionarios.spec.mjs`, 1/1; tres tipos de pregunta y aislamiento contextual. |
| IA: generar, sondear, editar e importar | Simulación en navegador | Aprobado con alcance limitado | `ia-flujo.spec.mjs`, 1/1; proveedor HTTP local ficticio, sin proveedor externo. |
| Crear estructura académica, usuarios, encargados, asignaciones e inscripciones | Manual pendiente | Pendiente | El seeder prepara contexto, pero no es evidencia de formularios reales. |
| Publicar material; estadísticas, notificaciones y auditoría desde dashboard | Manual pendiente | Pendiente | Se comprobó auditoría de acciones concretas, no el recorrido completo de esos paneles. |
| IA con proveedor externo | Proveedor externo | Pendiente | Requiere configuración y autorización independiente; no se usaron claves reales. |

### Integridad de artefactos de prueba

`Tests/E2E` conserva sólo fuente reproducible, fixtures ficticios, `package.json` y `package-lock.json`. No forman parte del conjunto propuesto `node_modules`, `test-results`, reportes, trazas, archivos de salida ni clústeres PostgreSQL temporales; ninguno está presente entre los cambios actuales. Las cuentas y contraseñas que aparecen en el seeder son ficticias y exclusivas de la base descartable; secretos, conexiones reales y datos personales no se versionan.

### Paso 8: administración académica ejecutada en navegador

| Recorrido | Clasificación | Estado | Evidencia |
| --- | --- | --- | --- |
| Crear y activar ciclo; crear período, grado, curso y secciones | Navegador | Aprobado | `administracion-academica.spec.mjs`, Chrome; formularios y validación de fecha requerida. |
| Crear docente y comprobar duplicidad de carnet | Navegador | Aprobado | Alta con confirmación SweetAlert2, cuenta temporal y error visible de duplicidad. |
| Crear encargado, alumno y asociación inmediata | Navegador | Aprobado | Alta de encargado independiente y alta de alumno con encargado en línea; perfil móvil del Alumno. |
| Asignación e inscripción inicial; duplicidad activa | Navegador | Aprobado | Asignación creada; inscripción inicial creada por el formulario de alumno y segunda inscripción activa rechazada. |
| Seguridad transversal | Navegador/HTTP desde contexto Chrome | Aprobado | Docente denegado en creación de ciclo; POST sin antiforgery devuelve 400. |
| Persistencia temporal | PostgreSQL temporal | Aprobado | Conteos de ciclo, período, grado, secciones, cursos, personas, asignaciones e inscripciones. |
| Traslado, retiro y reinscripción | Navegador | Pendiente | No se declara ejecutado en esta corrida. |
| Estadísticas, notificaciones y auditoría desde paneles | Navegador | Pendiente | Se comprobó la portada; no el recorrido completo de esos paneles. |

La corrida usó el superusuario inicial ficticio para las acciones administrativas. Antes de una demostración con el perfil limitado de Coordinación, se debe comprobar separadamente que sus permisos efectivos cubran sólo las operaciones autorizadas.
