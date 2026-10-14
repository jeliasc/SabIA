# Validación técnica de `VersionarAsignaciones`

Fecha de ejecución: 13 de octubre de 2026  
Migración: `20261014031647_VersionarAsignaciones`  
Migración anterior: `20261013034418_ConfigurarResolucionCuestionarios`

## Dictamen

La migración superó las comprobaciones técnicas adicionales de conservación de
datos históricos y seguridad de reversión. Este resultado **no constituye una
autorización para migrar desarrollo, Render ni producción**.

Se ejecutaron cuatro pruebas nuevas y las cuatro finalizaron correctamente:

- migración hacia adelante con datos académicos preexistentes;
- reversión sin historial incompatible y posterior reaplicación;
- bloqueo de reversión por existencia de un relevo;
- bloqueo de reversión por versiones históricas sin relevos.

Resultado del ejecutor: `Total: 4, Errors: 0, Failed: 0, Skipped: 0`, en
40.736 segundos. La compilación previa terminó con 0 errores. Los avisos
`NU1900` provinieron de la imposibilidad de consultar el índice de
vulnerabilidades de NuGet y no afectaron la compilación ni las pruebas.

## Aislamiento

Se creó un clúster PostgreSQL 18.6 nuevo y descartable en el directorio temporal
del usuario, con autenticación local `trust`, escucha exclusiva en
`127.0.0.1:55432` y sin volumen compartido. La conexión administrativa usada
fue `Database=postgres;Username=postgres;Pooling=false`, acompañada de
`SABIA_PRUEBAS_PG_CONFIRMAR=BASE_DESCARTABLE`.

Cada caso creó una base aleatoria con prefijo `sabia_prueba_versionar_` y la
eliminó con `DROP DATABASE ... WITH (FORCE)` al finalizar. Después de la suite se
confirmó que no quedaban bases con prefijo `sabia_prueba_`; luego se detuvo el
servidor y se eliminó el clúster temporal. No se usaron conexiones de la
aplicación ni se accedió a desarrollo, Render o producción.

## Datos preexistentes y conteos

La base se llevó primero al estado inmediatamente anterior a
`VersionarAsignaciones`. La siembra utilizó las relaciones y restricciones
reales de ese esquema.

| Tabla | Antes | Después |
|---|---:|---:|
| `AspNetUsers` | 2 | 2 |
| `Docentes` | 1 | 1 |
| `Alumnos` | 1 | 1 |
| `CiclosEscolares` | 1 | 1 |
| `Periodos` | 1 | 1 |
| `Grados` | 1 | 1 |
| `Secciones` | 1 | 1 |
| `Cursos` | 1 | 1 |
| `Asignaciones` | 1 | 1 |
| `Inscripciones` | 1 | 1 |
| `Unidades` | 1 | 1 |
| `UnidadesAsignaciones` | 1 | 1 |
| `Tareas` | 1 | 1 |
| `Entregas` | 1 | 1 |
| `ConfiguracionesEvaluacion` | 1 | 1 |
| `CategoriasEvaluacion` | 1 | 1 |
| `ActividadesEvaluables` | 1 | 1 |
| `CalificacionesManuales` | 1 | 1 |

En el modelo real, la evaluación sembrada se representa mediante
`ConfiguracionesEvaluacion`, `CategoriasEvaluacion` y
`ActividadesEvaluables`; la calificación se comprobó tanto en
`CalificacionesManuales.Nota = 18.00` como en
`Entregas.Calificacion = 18.00`.

Identificadores principales registrados:

- docente `1101`, alumno `1201`, ciclo `1301`, periodo `1401`;
- grado `1501`, sección `1601`, curso `1701`, asignación `1801`;
- inscripción `1901`, unidad `2001`, unidad-asignación `2101`;
- tarea `2201`, entrega `2301`, configuración `2401`;
- categoría `2501`, actividad evaluable `2601`, calificación manual `2701`.

Se comparó antes y después una huella con todas las claves anteriores y sus
referencias. La huella fue idéntica y la consulta explícita de huérfanos devolvió
0. La asignación preexistente conservó `Id = 1801`; las tres columnas nuevas
quedaron en `NULL`, como corresponde a un registro sin historia previa. Las
tablas nuevas `RelevosAsignaciones` y `DelegacionesAcademicas` quedaron vacías.

## Restricciones comprobadas

Después de `Up()` se verificó en el catálogo que estaban presentes y validadas:

- `FK_Asignaciones_Asignaciones_AsignacionAnteriorId`;
- `CK_Asignaciones_ActivaSinFin`;
- `CK_Asignaciones_SinAutorreferencia`;
- `CK_Asignaciones_VersionConInicio`;
- `CK_Asignaciones_Vigencia`.

También se comprobó que
`IX_Asignaciones_SeccionId_CursoId_Activa` era un índice único parcial con
predicado `Estado = 1`. Una inserción conflictiva de una segunda asignación
activa para la misma sección y curso fue rechazada por PostgreSQL con SQLSTATE
`23505` (`unique_violation`).

Las relaciones preexistentes se recorrieron mediante `JOIN` sobre las claves
foráneas reales:

- asignación → sección, curso, grado y docente;
- unidad-asignación → unidad, asignación y periodo;
- tarea → asignación y unidad-asignación;
- entrega → tarea, alumno e inscripción compuesta;
- configuración → asignación/sección, ciclo y periodo;
- actividad → configuración/asignación, categoría y tarea;
- calificación manual → actividad, alumno e inscripción.

## Seguridad de `Down()`

### Sin historial incompatible

La reversión hasta `ConfigurarResolucionCuestionarios` terminó correctamente.
Se verificó que:

- la migración dejó de figurar en `__EFMigrationsHistory`;
- se eliminaron las dos tablas nuevas y las tres columnas nuevas;
- se restauró `IX_Asignaciones_SeccionId_CursoId`;
- la migración pudo aplicarse nuevamente sin errores.

### Con relevo

Se creó un relevo `2801` entre dos asignaciones de pares sección/curso
distintos. Antes de revertir se confirmó que no existía ningún par con más de
una asignación, aislando así la condición `EXISTS (RelevosAsignaciones)`.
`Down()` fue rechazado con SQLSTATE `P0001` y el mensaje de reversión insegura.

Después del rechazo seguían presentes la entrada de la migración, las dos
tablas, las tres columnas y el relevo `2801`; por tanto, el fallo fue
transaccional y no dejó una reversión parcial.

### Con versiones históricas y sin relevo

Se cerró la asignación `1801` y se creó la versión activa `1802`, enlazada por
`AsignacionAnteriorId = 1801`, para el mismo par sección/curso. Se confirmó que
`RelevosAsignaciones` estaba vacía, aislando la condición de múltiples versiones.
`Down()` volvió a ser rechazado con SQLSTATE `P0001`.

Después del rechazo permanecían las dos versiones, el enlace histórico, la
entrada de la migración y todo el esquema nuevo.

## Limitaciones y alcance

- La muestra es sintética y representativa de las relaciones del modelo; no es
  una copia de datos reales ni mide volúmenes de producción.
- La ejecución se realizó con PostgreSQL 18.6. Debe repetirse en la misma versión
  mayor del destino si el entorno autorizado usa otra versión.
- El host de `dotnet test` presentó un fallo de handshake dentro del sandbox al
  exportar la conexión temporal. La misma DLL compilada se ejecutó con el runner
  xUnit v3 in-process generado por el proyecto, en modo no paralelo; las cuatro
  pruebas fueron descubiertas y finalizaron correctamente.
- Estas comprobaciones no sustituyen el respaldo lógico, la restauración de
  ensayo, la ventana de mantenimiento, la revisión de capacidad ni la aprobación
  operativa previa a producción.
- No se realizaron commits, merges, push ni despliegues.

La prueba automatizada reproducible está en
`Tests/Proyecto_Final.Tests/VersionarAsignacionesPostgresTests.cs` y conserva la
doble protección `SABIA_PRUEBAS_PG` +
`SABIA_PRUEBAS_PG_CONFIRMAR=BASE_DESCARTABLE`.
