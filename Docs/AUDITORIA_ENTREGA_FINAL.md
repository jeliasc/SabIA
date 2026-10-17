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

## Paso 6: validación final de navegador y demostración

El equipo dispone de Edge y Chrome locales, pero el repositorio no tiene Playwright, Selenium ni otro controlador de navegador instalado y no existe una herramienta de navegador conectada a esta sesión. Por tanto, no se inició Kestrel ni se declaró como ejecutada una prueba visual o E2E: abrir un navegador sin automatización ni operador no produciría una evidencia reproducible.

Se creó exclusivamente el clúster PostgreSQL 18 temporal `.tmp-pg-entrega-20261016` en `127.0.0.1:55440`, con usuario local de prueba `SabiaEntrega` y autenticación `trust`. Las pruebas siguieron usando bases aleatorias `sabia_prueba_*`; dos que quedaron tras el cierre anómalo del runner se eliminaron desde ese mismo clúster. No se consultó ninguna base de desarrollo, Render ni producción.

La infraestructura de VSTest/xUnit devolvió la consola antes de terminar algunos procesos hijos y el adaptador ignoró el filtro de clase. Los diagnósticos de las ejecuciones completadas registraron 29/29, 33/33 y 33/33 pruebas aprobadas, sin fallos. Se cerraron los procesos huérfanos y esta ejecución no se cuenta como recorrido E2E ni sustituye las suites ya aprobadas.

Los catorce pasos funcionales, Login, visualización responsive, DataTables, selects, SweetAlert2, PDF y controles negativos quedan **pendientes de comprobación manual**; la guía y matriz se actualizaron. No se cambiaron archivos de producción, seguridad, Identity, permisos, cookies, sesiones, migraciones ni reglas de calificación.

## Paso 6.1: habilitación y ejecución E2E en Chrome

Se instaló `@playwright/test` 1.58.2 de forma aislada en `Tests/E2E`, sin añadir dependencias al proyecto ASP.NET Core. Playwright controló el Chrome local instalado y ejecutó Kestrel en `127.0.0.1:5099` contra un clúster PostgreSQL 18 descartable en el puerto 55441, con el superusuario ficticio `demo.coordinacion` configurado solo por variables de entorno.

La suite `Tests/E2E/sabia.e2e.spec.mjs` aprobó 2 de 2 pruebas reales en navegador. Verificó en escritorio el Login vacío, credenciales incorrectas, la ausencia de «Recordarme», el mensaje genérico de credenciales, el cambio obligatorio de contraseña inicial, el acceso de Coordinación y el formulario protegido de creación de Docente con sus validaciones en español. También verificó Login y validación obligatoria a 390×844. No hubo llamadas a servicios externos ni datos reales.

Esta evidencia aprueba exclusivamente el paso de Login/entrada de Coordinación y la comprobación visual asociada. El recorrido académico restante no se considera ejecutado hasta crear el conjunto completo de datos ficticios y automatizarlo; se mantiene como pendiente en la matriz. La primera ejecución detectó un selector de prueba desactualizado («Ingresar» frente a «Iniciar sesión»), que se corrigió en la suite; no era un defecto de SabIA.

## Paso 6.2: aprobación y publicación académica en Chrome

El 16/10/2026 se ejecutó Chrome mediante Playwright contra Kestrel local (`127.0.0.1:5102`) y PostgreSQL 18 temporal aislado (`55444`). El seeder creó solo cuentas y contexto académico ficticios; la unidad, planificación y tareas del escenario se crearon mediante formularios web.

- Docente creó `Unidad creada E2E`, creó `Planificación creada E2E` y confirmó «Enviar a revisión» en SweetAlert2.
- Coordinación abrió `Planificaciones/Revisar/1` y aprobó la planificación desde la interfaz. La persistencia dejó estado `3`, fecha de envío, fecha de aprobación y usuario aprobador. La auditoría registró `Planificaciones/Revisar` (crear revisión y editar planificación).
- Docente creó y publicó `Tarea visible E2E` mediante el formulario y la confirmación «Publicar tarea». Persistió como estado `2`, con fecha de publicación y fechas de disponibilidad/límite; la auditoría registró `Tareas/Crear` y `Tareas/Publicar`.
- Alumno inició sesión y, en viewport móvil 390×844, visualizó `Tarea visible E2E`, `Curso E2E` y `E2E A` en `/Tareas`.

La prueba integrada inicial superó su límite de 45 segundos mientras el POST de aprobación terminaba, pero sus efectos se verificaron en interfaz, persistencia y auditoría. Se elevó únicamente el tiempo de la prueba y se ejecutó después una continuación aislada: 1 prueba Playwright aprobada de 1. Dos intentos previos de tarea quedaron con disponibilidad futura por datos E2E (uno con 2099 y otro por conversión Guatemala→UTC); no fueron visibles para el Alumno. El tercer intento usó una hora de Guatemala vigente y aprobó. Todo ese contenido existió solo en la base temporal eliminada al finalizar.

También se ejecutó un fixture cruzado: el seeder preparó la sección B, una asignación adicional del mismo Docente y un Alumno distinto inscrito allí. Desde Chrome el Docente creó y publicó `Tarea ajena E2E` para B. La consulta temporal confirmó ambas tareas publicadas y una entrega inicial por cada inscripción; la sesión móvil de `demo.alumno`, inscrito solo en A, mostró `Tarea visible E2E` y no mostró `Tarea ajena E2E`. El aislamiento entre secciones queda aprobado.

No se modificaron archivos de producción, Identity, permisos, cookies, sesiones, migraciones ni reglas académicas.

## Paso 6.3: entrega, calificación y consulta de resultado en Chrome

El 16/10/2026 se completó el ciclo de entrega con Chrome controlado por Playwright, Kestrel local y PostgreSQL temporal aislado. La tarea `Tarea visible E2E` y el archivo de respuesta fueron creados desde la interfaz; el seeder solamente aportó cuentas ficticias, inscripciones y contexto académico.

- Alumno de la sección A abrió la entrega en vista móvil, adjuntó el archivo ficticio y recibió la confirmación de entrega. La fila persistida corresponde al alumno `E2E-ALU`, tarea `Tarea visible E2E`, con fecha de entrega y estado calificado.
- Docente asignado abrió la entrega y registró desde el formulario la calificación `8.00` y la retroalimentación de prueba. PostgreSQL temporal confirmó ambos valores y la auditoría registró `Entregas/Entregar` y `Entregas/Calificar`.
- Alumno consultó su historial y resultado en móvil. La comprobación final Playwright de solo lectura aprobó 1/1 en 55.3 s y verificó calificación y retroalimentación visibles; Docente pudo consultar el mismo historial autorizado.
- Alumno de otra sección y Docente sin asignación iniciaron sesión en Chrome. Sus solicitudes autenticadas a detalle y calificación, respectivamente, devolvieron 404; no se modificaron datos.

La primera corrida de mutación alcanzó la entrega y calificación, pero el aserto posterior tomaba el enlace global `Ver historial` en lugar de `Ver historial de intentos`. Fue un selector de Playwright, no un defecto de SabIA; se hizo exacto y la verificación posterior aprobó. Durante la prueba también se detectó que la vista de entrega no renderizaba el error de propiedad `Archivo`; se agregó exclusivamente `asp-validation-for="Archivo"` en `Views/Entregas/Entregar.cshtml`. No cambia la regla que exige el archivo ni la lógica de entrega.

No se modificaron Identity, autenticación, roles, permisos, cookies, sesiones, migraciones ni reglas de calificación.

## Paso 6.4: cuestionarios en Chrome

El 16/10/2026 se ejecutó en Chrome controlado por Playwright el ciclo de cuestionario con Kestrel local y PostgreSQL temporal aislado. El seeder aportó exclusivamente usuarios, secciones, asignaciones e inscripciones ficticias; el Docente creó y publicó el cuestionario desde la interfaz.

- Docente configuró `Cuestionario completo E2E A` para su curso, con un intento máximo y tres preguntas: selección múltiple, verdadero/falso y respuesta directa. La publicación se confirmó con SweetAlert2.
- Alumno A, en viewport móvil, inició el intento, navegó entre preguntas, respondió las tres y lo finalizó. El resultado visible fue 100 %; la base temporal confirmó tres respuestas e intento finalizado con calificación `100.00`.
- Docente consultó desde su interfaz los resultados y el detalle del intento. La auditoría temporal registró configuración, preguntas, publicación, inicio, guardado de respuestas y finalización.
- Alumno de la sección B y Docente sin asignación recibieron 404 en solicitudes autenticadas originadas desde contextos de Chrome para resolver/consultar el cuestionario e intento de A. No hubo cambios no autorizados.

`Tests/E2E/cuestionarios.spec.mjs` aprobó 1 de 1 prueba. Un primer intento reveló solamente una aserción de Playwright sobre un mensaje temporal que esa vista no renderiza; la pregunta sí se había persistido. Se corrigió el selector de la prueba, no el código de producción. No se inició la demostración de IA ni se modificaron Identity, autenticación, permisos, migraciones o reglas de calificación.

## Paso 6.5: IA simulada en Chrome

El 16/10/2026 se ejecutó en Chrome con Playwright el recorrido completo Docente → proveedor IA simulado local → vista previa editable → importación. Kestrel recibió una URL y clave ficticias solo por variables de entorno y envió el PDF ficticio multipart al simulador local; no se consultó el proveedor externo ni se usaron claves reales.

- Docente validó origen de archivo requerido e inválido, creó desde la interfaz la tarea en borrador `Cuestionario IA importado E2E`, cargó un PDF ficticio y solicitó exclusivamente cuestionario.
- El simulador devolvió un trabajo `pendiente`; Chrome consultó `/ContenidoIA/Estado` y recibió después `completado`, mostró el JSON y habilitó la importación.
- Docente abrió la vista previa, corrigió el primer enunciado y confirmó la importación. PostgreSQL temporal confirmó la tarea aún en borrador, origen `InteligenciaArtificial`, dos preguntas y el texto corregido. La auditoría registró `Cuestionarios/ConfirmarImportacionIa`.
- En móvil se comprobó la navegación a la configuración del cuestionario importado. Un POST sin antiforgery fue rechazado con 400; Alumno fue redirigido/denegado antes de la función de IA y Docente ajeno no vio la planificación propia en su selector.

`Tests/E2E/ia-flujo.spec.mjs` aprobó 1 de 1 prueba. Para archivo sin seleccionar o MIME inválido, la interfaz mostró el resumen español genérico "Revise la información ingresada"; la comprobación de detalle específico por archivo queda como mejora de presentación pendiente, sin debilitar la validación. No hubo cambios de producción, seguridad, permisos, autenticación, migraciones o reglas académicas.

## Paso 7: cierre técnico consolidado

Este apartado es el estado vigente de entrega y prevalece sobre las notas cronológicas de planificación o pendientes anteriores. La evidencia no se suma como si fueran pruebas únicas: las suites HTTP, .NET, JavaScript y E2E cubren capas y escenarios parcialmente superpuestos.

| Alcance | Estado consolidado | Evidencia real |
| --- | --- | --- |
| Compilación y pruebas no E2E | Aprobado | Compilación previa, 198 pruebas .NET sin PostgreSQL, 9 pruebas PostgreSQL en clúster aislado y 4 pruebas JavaScript aprobadas. |
| Login y validaciones | Aprobado en navegador | `sabia.e2e.spec.mjs`: 2/2 en Chrome; escritorio y móvil, campos requeridos, credencial genérica, cambio inicial y ausencia de «Recordarme». |
| Unidad, planificación, revisión y tarea | Aprobado en navegador | Docente creó unidad, planificación y tarea; Coordinación aprobó; SweetAlert2, persistencia y auditoría temporal comprobadas. |
| Actividades, entrega, calificación y resultado | Aprobado en navegador | Alumno entregó y consultó resultado; Docente calificó; vista móvil, persistencia, auditoría y restricciones cruzadas comprobadas. |
| Cuestionarios | Aprobado en navegador | `cuestionarios.spec.mjs`: 1/1; creación, publicación, resolución móvil, resultado y aislamiento contextual. |
| IA | Comprobado con simulación en navegador | `ia-flujo.spec.mjs`: 1/1; proveedor local ficticio, sondeo pendiente/completado, edición e importación persistida. No prueba un proveedor externo. |
| Estructura académica, altas e inscripciones desde UI | Pendiente de ejecución manual | El seeder creó estas precondiciones en la base descartable; no sustituyó sus formularios ni permite declararlos aprobados en navegador. |
| Materiales y panel final de Coordinación | Pendiente de ejecución manual | No hay evidencia E2E completa de carga de material ni de estadísticas, notificaciones y auditoría desde el dashboard. |

No se detectaron secretos reales, trazas, resultados de Playwright, dependencias generadas ni bases temporales dentro del árbol de cambios. Las únicas credenciales localizables son ficticias y están limitadas al seeder E2E; las cadenas de conexión y los valores del proveedor simulado se inyectan por variables de entorno y no se almacenan en archivos.

Observaciones abiertas, sin cambios de código en este cierre:

- Prioridad media: para PDF ausente o MIME inválido, IA conserva la validación pero muestra un resumen genérico; falta decidir si se autoriza un mensaje específico por campo.
- Prioridad media: antes de la demostración, ejecutar manualmente altas de estructura, usuarios, encargados, asignaciones e inscripciones con una Coordinación de demostración y permisos efectivos verificados.
- Prioridad media: ejecutar manualmente materiales, estadísticas y notificaciones del dashboard de Coordinación.
- Prioridad alta para cualquier demostración de proveedor real: configurar y autorizar separadamente conectividad, credenciales y costos. Esta entrega sólo aprobó el contrato mediante simulador local.
- PostgreSQL productivo, Render y cualquier base no descartable no fueron accedidos ni validados por estas pruebas.

El único cambio de producción pendiente de incorporar es la presentación de `asp-validation-for="Archivo"` en `Views/Entregas/Entregar.cshtml`; conserva la regla de archivo y no modifica autenticación, permisos, calificación, migraciones ni reglas de negocio.

## Paso 8: administración académica en Chrome

El 16/10/2026 se ejecutó `Tests/E2E/administracion-academica.spec.mjs` en Chrome controlado por Playwright contra Kestrel local y PostgreSQL temporal aislado. La corrida final aprobó 1/1 en 3.0 minutos. Todas las entidades del escenario fueron creadas desde formularios reales: ciclo 2099, período, grado, curso, secciones A y B, docente con cuenta temporal, encargado independiente, alumno con encargado registrado en línea, asignación docente e inscripción inicial.

- Se comprobaron en pantalla la validación de fechas requeridas del ciclo y la de nombre requerido del alumno, la confirmación SweetAlert2 de las altas de Docente y Alumno, y el rechazo visible de un carnet docente duplicado.
- La creación de alumno con sección registró la inscripción inicial; un intento posterior de inscripción activa duplicada fue rechazado desde el formulario. La asignación fue creada para el curso y sección correspondientes.
- Los conteos de la base temporal confirmaron persistencia de 1 ciclo, 1 período, 1 grado, 2 secciones y las filas creadas de cursos, docentes, alumnos, encargados, asignaciones e inscripciones. Un POST de creación de encargado sin antiforgery devolvió 400.
- Docente inició sesión y vio su curso; al abrir la creación de ciclos recibió denegación. Alumno inició sesión en viewport 390×844 y vio únicamente su sección y encargado asociado. La portada de Coordinación cargó correctamente.

No se modificó código de producción. Los únicos ajustes fueron selectores y datos ficticios de la nueva prueba E2E. La cuenta de coordinación de esta corrida es el superusuario inicial ficticio, por lo que valida las pantallas administrativas pero no sustituye una comprobación futura del conjunto exacto de permisos de un rol de Coordinación limitado. Permanecen pendientes el traslado/reinscripción desde UI y el recorrido completo de estadísticas, notificaciones y auditoría desde sus paneles.
