# Guía de ejecución y demostración

## Preparación segura

1. Trabajar desde la rama local `dev` y mantener las credenciales fuera del repositorio (`.env*`, `appsettings.Local.json` o secretos de usuario según el entorno).
2. Usar una base de datos de demostración aislada. No conectar a desarrollo compartido, Render ni producción.
3. Confirmar que la cultura de interfaz sea `es-GT` y que el entorno no exponga detalles de excepción.
4. Compilar antes de iniciar: `dotnet build --no-restore -v:minimal`.
5. Las pruebas PostgreSQL ya fueron validadas con PostgreSQL 18.6 en un clúster temporal. Para repetirlas, crear otro clúster desechable, comprobar su directorio y puerto, y usar las variables `SABIA_PRUEBAS_PG` y `SABIA_PRUEBAS_PG_CONFIRMAR=BASE_DESCARTABLE`; no reutilizar la configuración de la aplicación.

## Recorrido sugerido de demostración

1. **Login:** mostrar mensajes de usuario y contraseña obligatorios; probar una credencial incorrecta y verificar el texto genérico. No usar credenciales reales en capturas ni registros.
2. **Datos académicos:** crear o editar un dato no sensible y verificar que selectores y badges muestran los nombres de presentación en español, conservando sus valores internos.
3. **Planificación y materiales:** crear un borrador, revisar una publicación y comprobar que un alumno solo puede visualizar/descargar según disponibilidad y permiso.
4. **Tarea y entrega:** crear una actividad, hacer una entrega de prueba y comprobar límites, estados y control de archivos.
5. **Cuestionario y calificaciones:** crear un cuestionario de prueba, resolverlo con un alumno de demostración, revisar resultados y el flujo de calificación/cierre.
6. **Relevo docente:** abrir `Asignaciones > Reemplazar docente`; comprobar resumen de obligaciones, validaciones y confirmación. No confirmar un relevo sobre información real durante la demostración.
7. **IA y notificaciones:** usar solo entradas de prueba. Confirmar que la importación deja el cuestionario en borrador para revisión docente y revisar notificaciones/auditoría sin datos sensibles.

## Verificaciones de interfaz

- Repetir los pasos principales en ancho móvil y de escritorio.
- Confirmar que `asp-validation-summary` y `asp-validation-for` son legibles, no quedan ocultos y coinciden con los mensajes del servidor.
- Probar fechas, números, correo, archivos y selectores con búsqueda.
- Validar que enlaces de retorno después de iniciar sesión siguen siendo locales.

## Límites conocidos antes de la entrega

- La repetición de pruebas PostgreSQL requiere una instancia temporal desechable; no usar una base existente aunque su nombre parezca de pruebas.
- Los hallazgos de seguridad y persistencia de «Recordarme» se encuentran documentados en [AUDITORIA_ENTREGA_FINAL.md](AUDITORIA_ENTREGA_FINAL.md) y no deben modificarse sin autorización para el módulo protegido.
- No realizar migraciones, cambios de permisos, commits, pushes ni despliegues como parte de estas verificaciones.
