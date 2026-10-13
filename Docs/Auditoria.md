# Auditoría centralizada

## Proxy inverso e IP del cliente

SabIA toma la IP efectiva desde `HttpContext.Connection.RemoteIpAddress`. Por defecto no procesa encabezados reenviados, por lo que un cliente no puede sustituir su IP enviando `X-Forwarded-For` directamente.

Si el despliegue utiliza un proxy inverso, se debe agregar **la dirección IP exacta del proxy administrado por la institución** en `ProxySeguro:ProxiesConfiables`. La aplicación procesa como máximo un salto y únicamente `X-Forwarded-For` y `X-Forwarded-Proto` provenientes de esas direcciones. No se deben usar rangos abiertos, comodines ni direcciones copiadas del encabezado recibido. Si existen varios saltos administrados, deben evaluarse y configurarse explícitamente antes de aumentar el límite.

Ejemplo de configuración de despliegue (la dirección es ilustrativa):

```json
{
  "ProxySeguro": {
    "ProxiesConfiables": ["10.0.0.10"]
  }
}
```

## Protección y conservación

Las direcciones IP y los datos técnicos de navegación son información de acceso restringido. Solo el rol Superusuario puede consultarlos. No deben exportarse a registros de aplicación de menor protección ni utilizarse para fines distintos de seguridad, trazabilidad y diagnóstico autorizado.

Política propuesta:

- conservar el historial institucional durante un mínimo propuesto de cinco años, salvo que la política institucional aprobada establezca un plazo mayor;
- anonimizar o suprimir los datos técnicos identificadores, especialmente IP y User-Agent, después de 12 meses, conservando el evento institucional cuando corresponda;
- revisar y aprobar institucionalmente esos plazos antes de automatizar la depuración;
- cifrar las copias de respaldo, limitar el acceso administrativo y registrar toda exportación;
- eliminar o anonimizar la IP al vencer el plazo, preservando únicamente estadísticas agregadas cuando sean necesarias;
- atender solicitudes de conservación por incidente mediante un procedimiento documentado, con responsable y fecha de cierre.

La depuración no se automatiza en esta migración para evitar eliminar evidencia sin una política institucional aprobada.

## Protección contra alteraciones

La interfaz de SabIA no expone acciones para editar o eliminar eventos. En producción se recomienda además usar cuentas separadas para migraciones y ejecución: la cuenta de la aplicación debe conservar únicamente `SELECT` e `INSERT` sobre `RegistrosAuditoria` y el uso de su secuencia, y no debe recibir `UPDATE`, `DELETE` ni `TRUNCATE` sobre esa tabla. El nombre real del rol debe obtenerse de la administración del despliegue; no se incluye SQL con un rol inventado.

El propietario de la base de datos seguirá teniendo capacidad técnica para alterar datos. Para detectar o investigar ese tipo de intervención se requieren respaldos cifrados e inmutables, registro administrativo de PostgreSQL y acceso restringido a la cuenta propietaria. Estos controles son operativos y deben configurarse fuera de la aplicación.
