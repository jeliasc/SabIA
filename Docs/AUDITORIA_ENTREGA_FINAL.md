# Auditoría técnica de entrega final

Fecha de revisión: 16 de octubre de 2026.
Base revisada: `dev`, confirmación `7ed5735` (`chore: completar ajustes de enums y vistas academicas`).

## Alcance y evidencia

La revisión fue estática y automatizada sobre el código local. No se accedió a bases de datos de desarrollo, Render ni producción; no se crearon migraciones, no se modificaron permisos, políticas, sesiones, Identity ni servicios de correo.

Las pruebas de integración usaron un clúster PostgreSQL 18.6 creado para esta auditoría en `.tmp-pg-entrega-20261016`, con puerto local 55436, usuario exclusivo `SabiaAuditoria` y autenticación local `trust`. Se verificó su directorio de datos antes de ejecutar. Las dos bases manuales y las bases dinámicas de las pruebas tuvieron nombres descartables; los recursos se eliminan al cierre de la auditoría.

La compilación principal fue satisfactoria (`dotnet build --no-restore -v:minimal`): 0 advertencias y 0 errores. La batería .NET no PostgreSQL aprobó 198 de 198 pruebas, la batería PostgreSQL aprobó 9 de 9 y las pruebas JavaScript aprobaron 4 de 4. Tras el cierre autorizado de Login, `PresentacionYValidacionTests` aprobó 8 de 8 pruebas, incluidos los flujos de cuenta activa, cuenta inactiva y credenciales incorrectas. Los comandos y la cobertura se detallan en [MATRIZ_PRUEBAS_ENTREGA_FINAL.md](MATRIZ_PRUEBAS_ENTREGA_FINAL.md).

## Resultado por área

| Área | Resultado verificable |
| --- | --- |
| Presentación y localización | `Program.cs` configura `es-GT` y `UseRequestLocalization`. Los mensajes de model binding están definidos en español y `_ValidationScriptsPartial` carga `wwwroot/js/validacion-es.js`. |
| Enumeraciones | `Extensions/EnumDisplayExtensions.cs` obtiene `DisplayAttribute`; las vistas revisadas usan `GetDisplayName()` o `Html.GetEnumSelectList<T>()`. Las pruebas comprueban, entre otros, `PorUnidad = 4`, `HojaCalculo = 3`, «Por unidad», «Hoja de cálculo» y «Tío». |
| Login | Los campos obligatorios muestran «Ingrese su nombre de usuario.» y «Ingrese su contraseña.». Usuario inexistente, contraseña incorrecta y cuenta inactiva muestran «El usuario o la contraseña son incorrectos.». La vista tiene antiforgery, `asp-validation-summary` y validación por campo. |
| Autorización | Existe una `FallbackPolicy` que requiere autenticación; los únicos `AllowAnonymous` revisados están en `LoginController`. Las descargas de documentos, materiales y entregas pasan por política o comprobaciones de acceso académico en servicio. |
| Asignaciones y relevo | El POST de relevo exige antiforgery y la política `RelevarAsignacion`. El servicio valida permisos, ciclo activo, sustituto, solapamientos, fechas, obligaciones y transacción serializable antes de persistir. |
| Tareas, entregas, cuestionarios y calificaciones | Las pruebas existentes cubren entrega, tareas, cuestionarios, flujo de calificaciones, seguridad de calificaciones y acceso académico. La importación de IA permanece en borrador y es cubierta por pruebas de importación y lectura. |
| IA | Las pruebas emplean un `HttpMessageHandler` de prueba y claves ficticias; no llaman a un proveedor externo. La clave se toma de configuración y no se encontró una credencial rastreada en Git. |
| Auditoría, notificaciones y dashboard | Hay pruebas de auditoría y notificaciones. La auditoría de Login registra descripciones de resultado, no la contraseña ni el valor de credenciales. |
| PostgreSQL y migraciones | Las 21 migraciones se aplicaron desde una base vacía. El catálogo resultante contiene 59 tablas, 223 índices, 122 claves foráneas, 59 claves primarias, 15 restricciones únicas y 45 comprobaciones. |
| `VisualizacionesActividades` | Se creó con clave primaria compuesta, dos claves foráneas restrictivas e índice por `TareaId`. El usuario temporal es propietario y tiene SELECT/INSERT/UPDATE/DELETE efectivos. |

## Revisión especial: `Views/Asignaciones/ReemplazarDocente.cshtml`

El cambio respecto a la confirmación anterior es principalmente de reindentación y división de líneas; no cambia rutas, valores enviados, permisos ni lógica. Se comprobó lo siguiente:

- El archivo es UTF-8; las tildes se leen correctamente al interpretarlo con esa codificación.
- Tiene 19 aperturas y 19 cierres de `div`; `@section Scripts` está después del cierre del contenedor principal.
- Conserva `@Html.AntiForgeryToken()`, el identificador oculto, resumen de validación y validaciones de `DocenteSustitutoId`, `FechaEfectiva`, `Motivo` y confirmación.
- Conserva los valores numéricos de delegación y los valores del enum de relevo. La autorización y las reglas permanecen en controlador/servicio, fuera de la vista.

No se hizo un cambio a esta vista porque no se demostró un defecto de marcado o comportamiento y modificarla solo produciría ruido en el diff.

## Validación funcional de Coordinación

Se ejecutaron dos pruebas HTTP de integración contra PostgreSQL temporal y 65 pruebas existentes de servicios y seguridad relacionados. El host de prueba usa un principal efímero con permisos de Coordinación y un almacén de claves de Data Protection temporal; no modifica Identity, los roles, las políticas ni la configuración de la aplicación.

- El dashboard de Coordinación, la creación de ciclos y la ruta de revisión de planificaciones se autorizaron correctamente.
- Sin `Calificaciones.Configurar` ni `Usuarios.Ver`, las rutas respectivas devolvieron 403.
- Un POST de activación de ciclo sin token antiforgery devolvió 400 y no alcanzó la operación.
- Los flujos existentes cubren cierre/reapertura de ciclo, inscripciones, acceso contextual, relevo docente, auditoría y límites de calificaciones.

«Coordinación» no es un rol fijo en el seeder: es el perfil de un rol activo que posee `Planificaciones.Revisar`; el seeder agrega sus claims coordinadores complementarios. Para la demostración debe verificarse que el rol institucional asignado al usuario cuente con los permisos administrativos específicos que se pretendan mostrar.

No hubo pruebas visuales de navegador. También quedan pendientes el alta completa de docentes, alumnos y encargados desde la interfaz y el ciclo real de aprobar/devolver una planificación; se documentan como pendientes y no como resultados ejecutados.

## Hallazgos que requieren autorización

1. **Cuenta inactiva (corregido con alcance autorizado).** `AutenticacionServicio` conserva la clasificación interna `CuentaInactiva` y registra «Cuenta inactiva.» en auditoría, pero entrega al formulario «El usuario o la contraseña son incorrectos.», igual que para usuario inexistente y contraseña incorrecta. La cuenta inactiva no llega a `PasswordSignInAsync`, por lo que continúa sin autenticarse.
2. **Cierre al cerrar una pestaña (comportamiento pendiente de decisión).** La casilla «Recordarme» se retiró de Login y `PasswordSignInAsync` conserva `isPersistent: false`. El cierre actual requiere el POST explícito de «Cerrar sesión»; no hay evento de página que cierre la sesión al cerrar una pestaña. Una cookie no persistente se elimina al cerrar el navegador, no de manera fiable al cerrar solo una pestaña. Cualquier cambio a este comportamiento debe evaluarse como una modificación de autenticación protegida.
3. **Prueba visual manual pendiente.** Compilación y pruebas verifican Razor, validación y comportamiento unitario; no sustituyen una pasada manual en navegador de escritorio y móvil con datos de demostración.

## Higiene y límites

- `git diff --check` no produjo errores antes de añadir esta documentación.
- No había archivos sin seguimiento no ignorados ni cambios locales al comenzar la auditoría.
- No se hallaron archivos de secretos, certificados, llaves, logs, temporales ni configuraciones locales rastreadas. `appsettings.Local.json` y `.env*` están excluidos por `.gitignore`; no se revelaron ni leyeron sus valores.
- La infraestructura de pruebas PostgreSQL se reforzó en `CuestionarioPostgresTests.BasePrueba`: ahora crea explícitamente cada base aleatoria antes de ejecutar `MigrateAsync`. Corrige una interrupción de transporte reproducible en Windows/Npgsql al migrar una base aún inexistente; no modifica producción ni migraciones.
- La búsqueda de marcadores en fuentes propias no encontró `TODO`, `FIXME` ni `NotImplementedException` pendientes; las coincidencias de «TODO» eran comentarios en español y no tareas abiertas.

## Dictamen

La base compila y todas las pruebas disponibles, incluidas las 9 de PostgreSQL aislado, pasan. El código académico y la españolización pueden avanzar a una revisión de entrega, con la salvedad del cierre por pestaña pendiente de decisión y la prueba visual manual pendiente. Esta auditoría no cambió valores persistidos, reglas de negocio, esquema ni contratos externos.

## Validación funcional: perfil Docente

Se añadió `DocenteHttpTests` para validar el perfil con un usuario ficticio creado por `UserManager`, el rol `Docente` y sus claims realmente sembrados. Cada caso crea una base PostgreSQL aleatoria y descartable, aplica las migraciones de la aplicación, inserta un ciclo activo, grado, secciones, dos docentes, dos alumnos inscritos, asignaciones, unidad, planificación, tareas y entregas. El manejador de autenticación de prueba obtiene los roles y claims desde Identity; no codifica permisos ni modifica la configuración de producción.

- El rol conserva permisos de creación/consulta docente (`Tareas.Crear`, `Calificaciones.Configurar` y los módulos académicos correspondientes) y no recibe `Planificaciones.Revisar` ni `Calificaciones.AprobarCorreccion`.
- El dashboard Docente mostró exclusivamente `Curso propio HTTP`; no mostró el curso de la otra asignación. También se verificaron perfil, alumnos contextuales, creación de unidad, planificación y tarea, calificación de una entrega propia, importación de cuestionario IA sin `jobId` y notificaciones.
- Las rutas administrativas de docentes y revisión de planificación devolvieron 403. Identificadores de alumno, entrega y configuración de calificaciones ajenos devolvieron 404. El POST de envío a revisión sin antiforgery devolvió 400 antes de modificar datos.
- `DocenteHttpTests` aprobó 2 de 2 contra PostgreSQL temporal. La batería docente de servicios aprobó 162 de 162 y las pruebas PostgreSQL de tareas/cuestionarios aprobaron 5 de 5. La importación IA se comprobó con dobles de prueba; no hubo llamada a proveedor externo.

No se detectó un defecto comprobado que justificara cambios en módulos académicos. No se modificaron Usuarios, Roles, permisos, Identity, cookies, sesiones, políticas, migraciones ni reglas de calificaciones.

Quedan pendientes, por falta de automatización de navegador disponible, la comprobación visual manual en escritorio y móvil, incluyendo selects con búsqueda, SweetAlert2, tablas/tarjetas responsive, confirmaciones y la validación cliente de formularios. Las pruebas HTTP y de servicio no se presentan como sustituto de dicha revisión.

## Validación funcional: perfil Alumno

Se añadió `AlumnoHttpTests` siguiendo la infraestructura HTTP temporal de los perfiles anteriores. Cada caso usa un usuario ficticio creado mediante `UserManager`, el rol real `Alumno` y claims recuperados desde `RoleManager`; la base PostgreSQL aleatoria recibe únicamente datos sintéticos de ciclo, grado, secciones, asignaciones, inscripciones, unidad, tareas, entregas y cuestionarios.

- Se confirmó que el rol Alumno recibe `Entregas.Entregar` y `Cuestionarios.Resolver`, pero no `Tareas.Crear` ni `Calificaciones.Configurar`.
- El dashboard mostró únicamente `Curso alumno HTTP`. Se validaron perfil propio, unidades, tareas, entregas, formulario de entrega pendiente, detalle de entrega, cuestionario propio, calificaciones de consulta y notificaciones.
- El perfil ajeno, entrega ajena y cuestionario ajeno devolvieron 404. Las pantallas de crear tarea/unidad/planificación y configurar calificaciones devolvieron 403. Un POST para iniciar cuestionario sin antiforgery devolvió 400.
- `AlumnoHttpTests` aprobó 2 de 2 contra PostgreSQL temporal; la batería de servicios de acceso contextual, entregas, cuestionarios, notificaciones y regresión de Login aprobó 69 de 69; las pruebas PostgreSQL de tareas/cuestionarios aprobaron 5 de 5.

No se detectó un defecto probado ni se modificó lógica académica de producción. No se cambiaron Usuarios, Roles, permisos, autenticación, cookies, sesiones, políticas, migraciones ni reglas de calificaciones. La revisión visual real de navegador —escritorio, móvil, validaciones cliente y botones condicionales— permanece pendiente y no se sustituye con estas pruebas HTTP.

## Validación integral: Inteligencia Artificial

La configuración versionada define la URL histórica privada del proveedor y deja `InteligenciaArtificial:ApiKey` vacía. No se leyeron archivos de secretos, variables locales ni credenciales reales; por ello no se realizó ni se declara una conexión real al proveedor. Este resultado es un bloqueo seguro, no un fallo de la aplicación ni una confirmación de conectividad externa.

La validación simulada verificó el contrato de `IaServicio` mediante `HttpMessageHandler`: `POST /generar/` envía `prompt`, `max_tokens` y `X-API-Key`; `POST /generar-desde-documento/` envía multipart `documento` y `solicitudes`; `GET /estado/{job_id}/` procesa estados pendiente, completado y error. También se comprobaron respuestas HTTP y JSON inválidos, errores de comunicación, timeout, cancelación explícita, deduplicación de dos minutos y propiedad de cada trabajo durante 24 horas.

- El PDF local exige tamaño válido, extensión `.pdf`, MIME `application/pdf` y firma `%PDF-`; se procesa en memoria y no se almacena mediante `IArchivoFisicoServicio`.
- Una planificación ajena se rechaza antes de abrir/enviar su documento; las solicitudes requieren `ContenidoIA.Generar`, usuario autenticado y perfil Docente u operador institucional.
- El resultado de IA no publica ni guarda cuestionarios de forma automática: `LectorCuestionarioIa` e `ImportarCuestionarioIaTests` validan vista previa editable, preguntas incompletas, respuestas ambiguas y la importación mediante el servicio académico existente.
- `IaServicioTests`, `LectorCuestionarioIaTests`, `ImportarCuestionarioIaTests` y `CuestionarioServicioTests` aprobaron 67 de 67 casos. Se agregaron pruebas de planificación ajena y respuesta de estado malformada.

No se modificaron contratos externos, infraestructura, configuración productiva ni reglas académicas. Queda pendiente una prueba real mínima y no destructiva cuando exista una clave configurada de manera segura, conectividad autorizada y confirmación de costo/límites; también permanece pendiente el recorrido visual de las vistas de IA en navegador.
