# Auditoría centralizada

## Proxy inverso e IP del cliente

SabIA toma la IP efectiva desde `HttpContext.Connection.RemoteIpAddress`. Por defecto no procesa encabezados reenviados, por lo que un cliente no puede sustituir su IP enviando `X-Forwarded-For` directamente.

Si el despliegue utiliza un proxy inverso, se debe agregar **la dirección IP exacta del proxy administrado por la institución** en `ProxySeguro:ProxiesConfiables` o su red CIDR en `ProxySeguro:RedesConfiables`. La aplicación procesa como máximo un salto y únicamente `X-Forwarded-For` y `X-Forwarded-Proto` provenientes de esos orígenes. Por defecto solo se confía en las direcciones de loopback. No se deben usar rangos abiertos, comodines ni direcciones copiadas del encabezado recibido. Si existen varios saltos administrados, deben evaluarse y configurarse explícitamente antes de aumentar el límite.

Ejemplo de configuración de despliegue (la dirección es ilustrativa):

```json
{
  "ProxySeguro": {
    "ConfiarEnProxyRender": false,
    "ProxiesConfiables": ["10.0.0.10"],
    "RedesConfiables": ["10.20.0.0/16"]
  }
}
```

Las direcciones anteriores son solamente ejemplos y no corresponden a Render.

### Despliegue en Render

Render no publica direcciones IP estáticas para configurar sus proxies en esta lista. En un **Web Service de Render** puede habilitarse de forma explícita:

```text
ProxySeguro__ConfiarEnProxyRender=true
```

La aplicación exige además que Render haya definido su variable automática `RENDER=true`; no se debe crear ni imitar esa variable fuera de Render. Al activar esta opción se confía en las cabeceras enviadas por cualquier conexión que alcance directamente el puerto de la aplicación, porque no existe una lista publicada de proxies de Render que pueda autorizarse de forma precisa.

Antes de activarla se debe verificar en Render que:

- el servicio sea un Web Service y el puerto de la aplicación no sea accesible directamente desde Internet, sino únicamente a través del balanceador de Render;
- ningún servicio no confiable de la red privada, sidecar o proxy adicional pueda conectarse directamente a ese puerto;
- cualquier proxy adicional elimine y reconstruya `X-Forwarded-For` y `X-Forwarded-Proto` en lugar de reenviar valores proporcionados por el cliente;
- no esté definida la opción global `ASPNETCORE_FORWARDEDHEADERS_ENABLED`, que habilita una confianza permisiva fuera de esta configuración controlada.

Si existe alguna ruta directa al puerto, `ConfiarEnProxyRender` debe permanecer en `false` y se deben configurar exclusivamente proxies o redes conocidos. El hecho de enlazar Kestrel a `0.0.0.0` es necesario en Render, pero no constituye por sí mismo una protección; el aislamiento depende de la red administrada por la plataforma.

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
