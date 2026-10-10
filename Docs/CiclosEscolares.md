# Cierre y reapertura de ciclos escolares

## Modelo de estados

Los ciclos admiten únicamente estas transiciones:

- Preparación → Activo mediante la activación ordinaria.
- Activo → Cerrado mediante el cierre formal.
- Cerrado → Activo mediante la reapertura excepcional.

La columna heredada `Activo` se conserva por compatibilidad con las consultas de los módulos académicos. La base de datos exige que sea `true` únicamente cuando `Estado = Activo`; además, un índice único parcial garantiza que exista como máximo un ciclo activo. Es válido no tener ningún ciclo activo.

Cada transición crea, dentro de la misma transacción, un `MovimientoCicloEscolar` inmutable y un evento en `RegistroAuditoria`. El interceptor omite su evento automático para estas tres operaciones porque el servicio registra el evento central con el estado anterior, el nuevo estado y la justificación, evitando duplicados.

## Cierre formal

La revisión previa solo considera asignaciones activas con sección y curso activos que tengan estudiantes con inscripción activa. Las asignaciones sin estudiantes no generan obligaciones evaluables ni bloqueos.

Para cada asignación evaluable y período del ciclo se comprueba la configuración activa, la existencia de actividades evaluables, el cierre efectivo de calificaciones, los resultados consolidados de todos los estudiantes y la ausencia de correcciones pendientes o aprobadas sin aplicar. La revisión se repite dentro de la transacción serializable al confirmar el cierre.

Los ciclos cerrados permanecen disponibles para consulta histórica. Las escrituras ordinarias se bloquean mediante el estado compatible `Activo = false` y mediante controles explícitos en la gestión estructural de períodos, secciones, grados, cursos y asignaciones. El flujo específico de correcciones de calificaciones conserva sus propias autorizaciones y no se reemplaza por una edición ordinaria.

La reapertura no modifica períodos, inscripciones, asignaciones, resultados ni cierres de calificaciones. Solo cambia el estado del ciclo, exige una justificación y deja trazabilidad en movimientos y auditoría.

## Permisos

- `Ciclos.Cerrar`
- `Ciclos.Reabrir`

Ambas políticas aceptan al Superusuario o a un usuario de Coordinación que posea el permiso correspondiente y el permiso institucional `Planificaciones.Revisar`. La comprobación se aplica tanto en el controlador como en el servicio.

## Migración y despliegue

La migración `20261010033529_ImplementarCierreReaperturaCiclos`:

1. agrega `Estado` y transforma los ciclos existentes con `Activo = true` a estado Activo;
2. transforma los ciclos existentes con `Activo = false` a Preparación;
3. crea la tabla de movimientos, sus claves foráneas restrictivas y su índice histórico;
4. crea la restricción de coherencia entre `Estado` y `Activo`;
5. crea el índice único parcial para el único ciclo activo.

No es posible inferir de forma segura cuáles ciclos inactivos históricos estaban formalmente cerrados antes de existir este flujo. Por eso se migran a Preparación y no se inventan cierres ni movimientos retroactivos. La regularización histórica queda fuera de esta implementación.

Antes de crear el índice, la migración se detiene con un error descriptivo si encuentra más de un ciclo marcado como activo. No desactiva, reemplaza ni elimina datos automáticamente. Esa inconsistencia debe resolverse institucionalmente antes del despliegue.

Orden recomendado de despliegue:

1. respaldar la base de datos;
2. verificar que exista como máximo un registro con `Activo = true`;
3. detener temporalmente las escrituras de la aplicación;
4. aplicar la migración;
5. desplegar la aplicación y ejecutar las verificaciones funcionales.

La migración se genera como artefacto de desarrollo; esta implementación no la aplica automáticamente a una base de datos.
