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

## Pruebas no ejecutadas y motivo

- No se hicieron llamadas reales a IA ni a correo. Las pruebas de IA usan dobles de `HttpMessageHandler` y datos de prueba.
- No se ejecutó una prueba E2E de navegador. Debe efectuarse manualmente con datos no sensibles antes de la demostración.

## Criterio de repetición

Tras cualquier cambio de código, repetir compilación, la suite .NET no PostgreSQL, las pruebas JavaScript y `git diff --check`. Para PostgreSQL, crear un clúster nuevo y aislado, verificar su directorio y puerto antes de usarlo, establecer `SABIA_PRUEBAS_PG` contra su base administrativa y confirmar `SABIA_PRUEBAS_PG_CONFIRMAR=BASE_DESCARTABLE`. Nunca usar configuraciones de desarrollo, Render o producción.
