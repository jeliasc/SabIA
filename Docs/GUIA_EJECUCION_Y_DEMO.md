# Guía de ejecución y demostración

## Preparación segura

1. Trabajar desde la rama local `dev` y mantener las credenciales fuera del repositorio (`.env*`, `appsettings.Local.json` o secretos de usuario según el entorno).
2. Usar una base de datos de demostración aislada. No conectar a desarrollo compartido, Render ni producción.
3. Confirmar que la cultura de interfaz sea `es-GT` y que el entorno no exponga detalles de excepción.
4. Compilar antes de iniciar: `dotnet build --no-restore -v:minimal`.
5. Las pruebas PostgreSQL ya fueron validadas con PostgreSQL 18.6 en un clúster temporal. Para repetirlas, crear otro clúster desechable, comprobar su directorio y puerto, y usar las variables `SABIA_PRUEBAS_PG` y `SABIA_PRUEBAS_PG_CONFIRMAR=BASE_DESCARTABLE`; no reutilizar la configuración de la aplicación.

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
