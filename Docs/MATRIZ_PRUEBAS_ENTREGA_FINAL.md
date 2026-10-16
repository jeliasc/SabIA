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

## Pruebas no ejecutadas y motivo

- No se hicieron llamadas reales a IA ni a correo. Las pruebas de IA usan dobles de `HttpMessageHandler` y datos de prueba.
- No se ejecutó una prueba E2E de navegador. Debe efectuarse manualmente con datos no sensibles antes de la demostración.

## Criterio de repetición

Tras cualquier cambio de código, repetir compilación, la suite .NET no PostgreSQL, las pruebas JavaScript y `git diff --check`. Para PostgreSQL, crear un clúster nuevo y aislado, verificar su directorio y puerto antes de usarlo, establecer `SABIA_PRUEBAS_PG` contra su base administrativa y confirmar `SABIA_PRUEBAS_PG_CONFIRMAR=BASE_DESCARTABLE`. Nunca usar configuraciones de desarrollo, Render o producción.
