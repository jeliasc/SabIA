# Auditoría de PostgreSQL por triggers

La migración `20261104000000_AgregarAuditoriaPostgreSqlTriggers` agrega la tabla independiente `public.auditoria_bd` y la función `public.sabia_registrar_auditoria_bd()`. No se incorpora al `DbContext` ni a la interfaz de Auditoría actual.

Cada entrada es evidencia de una mutación física DML confirmada en PostgreSQL. Se registra dentro de la misma transacción que la operación: una reversión también revierte su evidencia. `usuario_conexion` usa `session_user`, que identifica el rol de conexión real y no se atribuye al usuario autenticado de SabIA. `origen` queda nulo intencionalmente: `application_name` y variables de sesión pueden ser alterados por clientes y no son evidencia confiable de una operación manual o de EF.

## Tablas cubiertas

- Seguridad: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `PermisosSistema`.
- Gestión académica: `Inscripciones`, `Asignaciones`.
- Evaluación y correcciones: `ConfiguracionesEvaluacion`, `CategoriasEvaluacion`, `ActividadesEvaluables`, `CalificacionesManuales`, `ResultadosCalificacionesPeriodos`, `SolicitudesCorreccionesCalificaciones`, `CierresCalificaciones`, `MovimientosCierresCalificaciones`, `HistorialesCalificaciones`.

La función recibe por argumentos del trigger las columnas autorizadas. No construye SQL dinámico ni copia la fila completa; en un `UPDATE` sólo guarda las columnas de esa lista que realmente cambiaron. Las actualizaciones que sólo toquen columnas no autorizadas no crean un registro.

## Datos excluidos

No se almacena ningún campo de credenciales de Identity, incluidos `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, tokens, logins, claims, correo y teléfono. También se excluyen campos libres que pueden contener información sensible o no necesaria para evidencia técnica: `Observaciones` de calificaciones, `MotivoSolicitud`, `ObservacionRevision`, `Motivo` de movimientos e historiales, y todos los textos de contenido académico. La tabla de auditoría tampoco tiene trigger, y la función corta ejecuciones anidadas para evitar recursividad.

## Relación con la auditoría de EF Core

`RegistrosAuditoria` conserva el evento funcional de SabIA (usuario autenticado, ruta, acción, correlación y resultado). `auditoria_bd` conserva el hecho técnico de que PostgreSQL recibió un `INSERT`, `UPDATE` o `DELETE` y el rol de conexión que lo ejecutó. Una operación EF puede producir ambos: no son duplicados y no se elimina ninguno. Para una investigación se correlacionan por ventana temporal, tabla/registro y, cuando aplica, por la transacción de PostgreSQL; la interfaz actual sigue mostrando solamente la auditoría funcional.

## Despliegue seguro

La migración revoca todos los privilegios de `PUBLIC` sobre la tabla, su secuencia y la función. La función usa `SECURITY DEFINER` para que un rol de aplicación con sólo DML sobre las tablas protegidas pueda insertar evidencia sin recibir privilegios sobre `auditoria_bd`.

En producción, el propietario que aplica migraciones debe ser un rol distinto del rol de aplicación y del rol operativo. Debe ser el único propietario de la función, tabla y triggers; el DBA debe conceder al rol de aplicación únicamente `SELECT/INSERT/UPDATE/DELETE` mínimos sobre las tablas de negocio, sin `TRIGGER`, `ALTER`, propiedad de tablas ni pertenencia al rol propietario. También debe restringirse `CREATE` en `public` a roles de migración/administración. Superusuarios, propietarios de objetos y roles con privilegios equivalentes siempre pueden alterar triggers o registros; PostgreSQL no permite protegerse contra esos roles mediante una migración de aplicación.
