# Adaptación inicial de AdminLTE para SabIA

## Objetivo

Esta versión conserva el backend existente de `Proyecto_Final` (Identity, PostgreSQL, usuarios, permisos, servicios y validaciones) y adapta la interfaz a AdminLTE 4.

No se crearon nuevas tablas ni migraciones. Los módulos académicos nuevos funcionan únicamente con datos ficticios para validar navegación, vistas y flujo antes de diseñar la base de datos definitiva.

## Qué se modificó

- Nuevo layout AdminLTE con topbar, sidebar y footer.
- Login adaptado visualmente a AdminLTE sin cambiar su lógica de autenticación.
- Dashboard de Coordinación con datos ficticios.
- Se conserva el módulo real de Usuarios.
- Se agregaron prototipos navegables para:
  - Roles y permisos.
  - Docentes, alumnos y encargados.
  - Ciclos, períodos, grados, secciones y cursos.
  - Asignaciones e inscripciones.
  - Unidades y materiales.
  - Planificación docente.
  - Documentos de coordinación.
  - Tareas y entregas/calificación.
  - Cuestionarios.
  - Contenido generado con IA.
  - Alertas y notificaciones.
- Se agregaron prototipos de experiencia por rol para Docente y Alumno.

## Base documental usada

La navegación y las vistas siguen el flujo definido para SabIA:

1. Coordinación configura estructura académica, usuarios, asignaciones e inscripciones.
2. Docente gestiona cursos, unidades, planificación, materiales, tareas y entregas.
3. Alumno consulta contenido liberado, recursos, tareas y entrega actividades.
4. Docente revisa/califica y retroalimenta.
5. Coordinación consulta indicadores de acceso, tareas, entregas y cumplimiento.

Planificación, contenido IA, cuestionarios, notificaciones y documentos de coordinación se presentan como prototipos porque su modelado definitivo todavía debe afinarse antes de crear tablas.

## Importante

Los formularios de los módulos de demostración no persisten información. Al guardar, regresan al listado mostrando una advertencia de que todavía son datos ficticios.

## AdminLTE

Por ahora AdminLTE 4 y Bootstrap Icons se cargan desde CDN para que la integración no altere las dependencias NuGet del proyecto. Se incluye `libman.json` como alternativa para descargar los archivos localmente más adelante.

## Siguiente forma de trabajo recomendada

Tomar un módulo a la vez, por ejemplo `Alumnos`:

1. Validar sus vistas y campos.
2. Confirmar reglas de negocio y relaciones.
3. Crear entidad y relaciones en EF Core.
4. Generar migración.
5. Crear servicio y controlador real.
6. Sustituir datos ficticios por datos de PostgreSQL.
7. Agregar permisos específicos del módulo.

De esta forma la base de datos se construye conforme se validan los requerimientos, sin adelantar tablas que todavía no están definidas.
