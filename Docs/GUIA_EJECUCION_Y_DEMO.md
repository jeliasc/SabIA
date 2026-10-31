# Guía de ejecución y demostración

## Preparación segura

1. Trabajar desde la rama local `dev` y mantener las credenciales fuera del repositorio (`.env*`, `appsettings.Local.json` o secretos de usuario según el entorno).
2. Usar una base de datos de demostración aislada. No conectar a desarrollo compartido, Render ni producción.
3. Confirmar que la cultura de interfaz sea `es-GT` y que el entorno no exponga detalles de excepción.
4. Compilar antes de iniciar: `dotnet build --no-restore -v:minimal`.
5. Las pruebas PostgreSQL ya fueron validadas con PostgreSQL 18.6 en un clúster temporal. Para repetirlas, crear otro clúster desechable, comprobar su directorio y puerto, y usar las variables `SABIA_PRUEBAS_PG` y `SABIA_PRUEBAS_PG_CONFIRMAR=BASE_DESCARTABLE`; no reutilizar la configuración de la aplicación.

### Migraciones al inicio

`Migraciones:AplicarAlInicio` controla si la aplicación ejecuta `Database.MigrateAsync()` durante el arranque. Su valor predeterminado es `true`, incluso si la clave no está configurada, para conservar la compatibilidad con desarrollo, pruebas y despliegues existentes. Para una futura operación con migrador independiente, establézcase `Migraciones__AplicarAlInicio=false` únicamente después de que la base haya sido migrada mediante el procedimiento institucional aprobado. Con la opción deshabilitada no se crea ni altera el esquema desde el arranque; los seeders continúan ejecutándose y reportarán cualquier esquema incompatible.

## Recorrido sugerido de demostración

### Perfil Coordinación

Antes de la demostración, confirme los claims efectivos del rol institucional: `Planificaciones.Revisar` habilita el contexto de Coordinación y los permisos complementarios de dashboard, cierre/reapertura de ciclo y aprobación de correcciones. Agregue además los permisos concretos de cada módulo administrativo que se vaya a demostrar; no basta con el nombre del rol.

1. Inicie sesión con una cuenta ficticia de Coordinación y abra `Inicio > Dashboard de coordinación`.
2. Compruebe ciclos, períodos, grados, secciones y cursos con registros de demostración; pruebe un dato obligatorio y una referencia incompatible.
3. Consulte docentes, alumnos y encargados; confirme las búsquedas y que las cuentas académicas se vinculan al usuario correspondiente.
4. Muestre asignaciones, una inscripción y, si hay datos preparados, el relevo docente. No use información real ni confirme un relevo no planificado.
5. Abra una planificación en revisión y registre una aprobación o devolución de prueba; compruebe responsable, fecha y auditoría.
6. Verifique que módulos no concedidos al rol devuelven acceso denegado y que cada POST exige el token antiforgery.

1. **Login:** mostrar mensajes de usuario y contraseña obligatorios; probar una credencial incorrecta y verificar el texto genérico. No usar credenciales reales en capturas ni registros.
2. **Datos académicos:** crear o editar un dato no sensible y verificar que selectores y badges muestran los nombres de presentación en español, conservando sus valores internos.
3. **Planificación y materiales:** crear un borrador, revisar una publicación y comprobar que un alumno solo puede visualizar/descargar según disponibilidad y permiso.
4. **Tarea y entrega:** crear una actividad, hacer una entrega de prueba y comprobar límites, estados y control de archivos.
5. **Cuestionario y calificaciones:** crear un cuestionario de prueba, resolverlo con un alumno de demostración, revisar resultados y el flujo de calificación/cierre.
6. **Relevo docente:** abrir `Asignaciones > Reemplazar docente`; comprobar resumen de obligaciones, validaciones y confirmación. No confirmar un relevo sobre información real durante la demostración.
7. **IA y notificaciones:** usar solo entradas de prueba. Confirmar que la importación deja el cuestionario en borrador para revisión docente y revisar notificaciones/auditoría sin datos sensibles.

### Perfil Docente

Prepare un docente ficticio activo con una asignación vigente, alumnos inscritos, una unidad, una planificación en borrador, una tarea publicada y una entrega de demostración. El rol debe conservar los permisos académicos sembrados; no asigne `Planificaciones.Revisar` ni `Calificaciones.AprobarCorreccion` para este recorrido.

1. Inicie sesión con la cuenta ficticia y abra `Inicio > Dashboard docente`. Compruebe cursos, alumnos, tareas, entregas y planificación; no debe aparecer información de otro docente.
2. Abra `Mi perfil`, el listado contextual de alumnos y las notificaciones. Verifique que los datos corresponden a sus secciones vigentes.
3. Cree una unidad y una planificación de prueba. Valide un campo obligatorio, guarde el borrador y envíelo a revisión. El docente no debe tener acceso a la pantalla de revisión institucional.
4. Cree/publice una tarea de prueba, compruebe fechas de disponibilidad/vencimiento y consulte una entrega del alumno asignado. Califique y, si procede, pruebe la reapertura individual con datos desechables.
5. Configure un cuestionario manual y una pregunta de cada tipo. Para importación IA, utilice solo texto ficticio o la simulación local: revise y edite la vista previa antes de confirmar; no exponga ni use una clave externa durante la demostración.
6. Como control negativo, intente abrir por URL un alumno, entrega o configuración de calificaciones de otra asignación. Debe recibir acceso no encontrado/denegado y los POST deben exigir antiforgery.

### Perfil Alumno

Prepare un alumno ficticio activo, con inscripción vigente, un curso asignado, una unidad publicada, una tarea disponible, una entrega pendiente y un cuestionario de prueba. No asigne permisos docentes o administrativos a su rol.

1. Inicie sesión con la cuenta ficticia y abra `Inicio > Mi espacio académico`. Compruebe curso, sección, unidades disponibles, tareas, entregas y resultados que correspondan solo a su inscripción vigente.
2. Abra `Mi perfil`, `Mis compañeros`, tareas, entregas y notificaciones. No utilice ni exponga información de otros alumnos en la demostración.
3. Abra una tarea publicada dentro de su período de disponibilidad, realice una entrega desechable y consulte su detalle e historial. Pruebe una tarea vencida y una reapertura individual únicamente con datos de prueba.
4. Resuelva un cuestionario ficticio con preguntas de selección única, verdadero/falso y respuesta directa. Revise límite de intentos, tiempo y resultado según su configuración; no invoque IA externa.
5. Como control negativo, cambie en la URL los identificadores de alumno, entrega o cuestionario por los de otra sección. Compruebe que no se muestre información ajena. Intente crear una tarea o configurar calificaciones: debe recibir acceso denegado.

### Inteligencia Artificial

La demostración de IA requiere una cuenta Docente ficticia con `ContenidoIA.Generar`, una planificación propia con PDF ficticio o un PDF local no sensible. No copie claves API en formularios, capturas, documentación ni consola.

1. Seleccione uno o más tipos de contenido y use instrucciones ficticias. Confirme que el formulario rechaza origen ambiguo, ausencia de tipos y PDF inválido.
2. Para un PDF local, pruebe únicamente archivos no sensibles. El archivo debe ser `.pdf`, MIME `application/pdf`, firma PDF válida y tamaño dentro del límite configurado; no debe persistirse como archivo académico.
3. Consulte el trabajo sólo desde la misma cuenta que inició la generación. Ante resultado de cuestionario, abra la importación, revise y corrija la vista previa antes de guardar.
4. Si no hay clave configurada de forma segura o no existe autorización de red/costo, muestre solamente el flujo simulado o el mensaje controlado de configuración. No intente acceder a la dirección privada histórica ni afirme que el proveedor fue validado.

## Verificaciones de interfaz

- Repetir los pasos principales en ancho móvil y de escritorio.
- Confirmar que `asp-validation-summary` y `asp-validation-for` son legibles, no quedan ocultos y coinciden con los mensajes del servidor.
- Probar fechas, números, correo, archivos y selectores con búsqueda.
- Validar que enlaces de retorno después de iniciar sesión siguen siendo locales.

## Límites conocidos antes de la entrega

- La repetición de pruebas PostgreSQL requiere una instancia temporal desechable; no usar una base existente aunque su nombre parezca de pruebas.
- Los hallazgos de seguridad y persistencia de «Recordarme» se encuentran documentados en [AUDITORIA_ENTREGA_FINAL.md](AUDITORIA_ENTREGA_FINAL.md) y no deben modificarse sin autorización para el módulo protegido.
- No realizar migraciones, cambios de permisos, commits, pushes ni despliegues como parte de estas verificaciones.

## Paso 6: recorrido final manual obligatorio

La sesión de validación no cuenta con Playwright, Selenium ni un navegador automatizado. No se debe marcar una fila como aprobada hasta ejecutarla en Edge o Chrome, en escritorio y móvil, sobre Kestrel y una base PostgreSQL descartable ya migrada. Cree solo cuentas ficticias `demo.coordinacion`, `demo.docente` y `demo.alumno`; no guarde sus contraseñas en este documento ni en capturas.

Actualización Paso 6.1: Playwright ya está disponible de forma aislada en `Tests/E2E` y controló Chrome local con éxito. El Login de Coordinación y su variante móvil fueron ejecutados realmente; los demás pasos de esta guía permanecen pendientes hasta preparar sus datos ficticios completos. Para repetir la prueba aprobada, con Kestrel temporal en `http://127.0.0.1:5099`, ejecute desde `Tests/E2E`: `npm.cmd run test:e2e -- --reporter=line`.

1. Con `demo.coordinacion`, pruebe Login vacío, contraseña vacía y credencial errónea; compruebe mensajes en español. Cree ciclo, períodos, grado, curso, sección, docente, alumno y encargado ficticios.
2. Cree asignación docente e inscripción. Compruebe selects, DataTables/tarjetas y una confirmación SweetAlert2, sin datos sensibles.
3. Con `demo.docente`, confirme que solo ve su asignación y alumnos. Cree unidad y planificación, guarde borrador, envíe a revisión e intente la ruta de revisión institucional: debe denegarse.
4. Como Coordinación, apruebe o devuelva la planificación. Intente modificar una calificación sin permiso específico: debe denegarse y no cambiar datos.
5. Como Docente, publique material y tarea. Cree cuestionario manual o importe solo una respuesta IA simulada; revise la vista previa y no configure clave externa. Pruebe un PDF ficticio válido e inválido.
6. Como Alumno, confirme que solo aparecen unidad, material, tarea y cuestionario propios. Realice entrega o cuestionario y compruebe validaciones, fechas y mensajes de éxito/error.
7. Como Docente, consulte y califique el resultado permitido; como Alumno, compruebe que solo ve su resultado.
8. Como Coordinación, revise estadísticas, notificaciones y auditoría. Confirme que no se muestran contraseñas, tokens ni credenciales.
9. Como negativos, cambie un ID de alumno, entrega y cuestionario por recursos de otra sección; pruebe URL docente siendo Alumno y POST sin antiforgery. Deben resultar 404, 403 o 400 y no cambiar datos.
10. Repita 1, 3, 5 y 6 en móvil. Compruebe sidebar, botones, encabezados numéricos, validación por campo/resumen y ausencia de errores en consola.

Al terminar, anote cada caso como Aprobado, Fallido, Omitido o Pendiente en la matriz. Si aparece un defecto, capture ruta, rol, pasos mínimos y resultado sin copiar sesiones, tokens, contraseñas ni datos reales.

## Evidencia ejecutada: continuación Paso 6.2

El recorrido parcial siguiente fue ejecutado realmente en Chrome con cuentas y datos ficticios sobre Kestrel y PostgreSQL temporales: Docente creó una unidad y planificación, la envió a revisión; Coordinación la aprobó; Docente creó/publicó una tarea; Alumno la visualizó en móvil. Las confirmaciones de enviar a revisión y publicar se resolvieron mediante SweetAlert2.

Para repetirlo, use el seeder `Tests/E2E/Seed/Sabia.E2E.Seed.csproj` contra una base PostgreSQL descartable ya migrada y ejecute desde `Tests/E2E`:

`npx.cmd playwright test flujo-academico.spec.mjs --grep "Continuación: Docente publica" --project=chrome-local --reporter=line`

La tarea debe usar una disponibilidad ya vigente expresada como hora de Guatemala; el servidor la conserva en UTC. No interprete una tarea futura como una falla de visibilidad del Alumno. El aislamiento entre secciones fue comprobado con una segunda asignación y un segundo Alumno ficticios: la tarea publicada de B no apareció en la sesión móvil del Alumno inscrito en A.

## Evidencia ejecutada: continuación Paso 6.3

Con el mismo escenario temporal, el Alumno de A adjuntó y envió una respuesta ficticia para `Tarea visible E2E`; el Docente asignado abrió el historial y registró una calificación de demostración. El Alumno consultó después su resultado en viewport móvil. La persistencia temporal confirmó la entrega, nota y retroalimentación, y la auditoría registró las acciones de entregar y calificar.

La comprobación final `Paso 6.3: verifica resultado y aislamiento de la entrega calificada` aprobó 1/1 en Chrome. Además del resultado del Alumno y la consulta autorizada del Docente, validó solicitudes autenticadas de un Alumno de la sección B y de un Docente sin asignación: ambas recibieron 404 para detalle/calificación de la entrega de A. Para repetir solo esta verificación, con la base temporal que ya contenga la entrega calificada, ejecute:

`npx.cmd playwright test flujo-academico.spec.mjs --grep "verifica resultado y aislamiento" --project=chrome-local --reporter=line`

La vista `Entregas/Entregar` ahora incluye la salida de validación para `Archivo`, de modo que los errores de propiedad enviados por el servidor se muestran junto al control de carga. Esto no modifica el requisito de archivo ni las reglas de calificación.

## Evidencia ejecutada: Paso 6.4 — cuestionarios

La prueba real `Tests/E2E/cuestionarios.spec.mjs` aprobó 1/1 en Chrome, Kestrel local y PostgreSQL temporal. Con datos ficticios, Docente creó desde la interfaz `Cuestionario completo E2E A`, configuró un intento y agregó preguntas de selección múltiple, verdadero/falso y respuesta directa; luego lo publicó mediante SweetAlert2.

Alumno A lo resolvió desde viewport móvil: inició intento, navegó entre las tres preguntas, respondió y finalizó. El resultado visible fue 100 %. Docente consultó el resultado y el detalle autorizado. PostgreSQL temporal confirmó cuestionario publicado, tres preguntas, tres respuestas e intento finalizado con calificación `100.00`.

Como control negativo, Alumno B y Docente sin asignación recibieron 404 en solicitudes autenticadas desde sus propios contextos Chrome para recursos de A. No se ejecutó IA en este paso. Para repetirlo, use una base temporal ya migrada y el seeder, inicie Kestrel y ejecute desde `Tests/E2E`: `npx.cmd playwright test cuestionarios.spec.mjs --project=chrome-local --reporter=line`.

## Evidencia ejecutada: Paso 6.5 — IA simulada

`Tests/E2E/ia-flujo.spec.mjs` aprobó 1/1 en Chrome contra Kestrel, PostgreSQL y un proveedor HTTP simulados en localhost. Docente cargó un PDF ficticio, solicitó cuestionario, observó el estado `pendiente` y el resultado `completado`, abrió la vista previa, corrigió un enunciado e importó dos preguntas a `Cuestionario IA importado E2E` desde la interfaz.

La importación conservó la tarea en borrador. PostgreSQL temporal confirmó origen IA, dos preguntas y el texto corregido; la auditoría registró `ConfirmarImportacionIa`. Se comprobó también la configuración en viewport móvil, el rechazo 400 de un POST sin antiforgery, la denegación/redirección al Alumno y la ausencia de la planificación A para Docente ajeno.

La prueba no representa una conexión real al proveedor externo. Los casos de archivo requerido y MIME inválido mostraron el resumen español genérico de validación; queda pendiente mejorar el detalle por campo si se autoriza una corrección de presentación. Para repetirla, configure solo la URL y clave ficticias del proveedor local y ejecute desde `Tests/E2E`: `npx.cmd playwright test ia-flujo.spec.mjs --project=chrome-local --reporter=line`.

## Paso 7: demostración reproducible y límites declarados

La demostración aprobada usa exclusivamente Kestrel local, PostgreSQL temporal aislado, usuarios ficticios y la fuente de `Tests/E2E`. Desde esa carpeta, restaure las dependencias declaradas con `npm ci`; prepare una base descartable migrada, ejecute el seeder con una cadena de conexión temporal e inicie Kestrel con esa misma conexión. Para el flujo de IA, ejecute además el proveedor simulado local y use únicamente sus valores ficticios por variables de entorno.

Los recorridos que pueden demostrarse como ejecutados en Chrome son Login, unidad, planificación, aprobación, tarea, entrega, calificación, resultado, cuestionarios e IA simulada. La evidencia de pantalla móvil corresponde a 390×844. No presente como ejecutados en navegador los formularios de estructura académica, altas de usuarios/encargados, asignación e inscripción, carga de material ni el recorrido completo de estadísticas, notificaciones y auditoría de Coordinación: requieren comprobación manual previa.

Antes de la demostración, valide manualmente que la cuenta ficticia de Coordinación tenga los permisos efectivos esperados y realice esas operaciones pendientes. Si se desea conectar IA real, debe obtenerse autorización independiente para URL, credenciales, costos y conectividad; la evidencia actual sólo valida el contrato con un proveedor local simulado. No se deben reutilizar bases reales, credenciales reales, ni archivos de `node_modules`, reportes, trazas o clústeres temporales.

## Paso 8: demostración de administración académica

La prueba reproducible `Tests/E2E/administracion-academica.spec.mjs` ya ejecutó en Chrome la creación de estructura académica y de personas desde la interfaz. Para repetirla, inicie Kestrel contra una base PostgreSQL temporal vacía y ejecute desde `Tests/E2E`:

`npx.cmd playwright test administracion-academica.spec.mjs --project=chrome-local --reporter=line`

La demostración debe mostrar: validación de ciclo sin fechas, activación de ciclo, período, grado, curso y dos secciones; alta de docente con confirmación y comprobación de carnet duplicado; alta de encargado; alta de alumno con encargado asociado e inscripción inicial; asignación docente; y rechazo de una inscripción activa duplicada. Luego inicie sesión con las cuentas temporales que muestra cada resultado —sin registrarlas en capturas— para comprobar perfil de Docente y perfil móvil del Alumno. El flujo también valida un POST sin antiforgery (400) y que Docente no pueda crear ciclos.

No se debe presentar como ejecutado el traslado/retiro/reinscripción ni el recorrido completo de estadísticas, notificaciones y auditoría: siguen pendientes. Para una demostración de Coordinación limitada, no use el resultado del superusuario ficticio como sustituto de la verificación de permisos efectivos del rol.
