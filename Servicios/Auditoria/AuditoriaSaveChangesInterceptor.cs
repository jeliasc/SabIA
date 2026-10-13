using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Proyecto_Final.Models;

namespace Proyecto_Final.Servicios.Auditoria;

public sealed class AuditoriaSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AgregarRegistros(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AgregarRegistros(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AgregarRegistros(DbContext? contexto)
    {
        if (contexto == null) return;

        contexto.ChangeTracker.DetectChanges();
        var entradas = contexto.ChangeTracker.Entries()
            .Where(x => x.Entity is not RegistroAuditoria &&
                        x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entradas.Count == 0) return;

        var http = httpContextAccessor.HttpContext;
        var solicitud = http?.Request;
        var moduloSolicitud = solicitud?.RouteValues["controller"]?.ToString();
        var accionSolicitud = solicitud?.RouteValues["action"]?.ToString();
        if (string.Equals(moduloSolicitud, "Login", StringComparison.OrdinalIgnoreCase))
            return;

        var usuarioId = Limitar(http?.User.FindFirstValue(ClaimTypes.NameIdentifier), 450);

        foreach (var entrada in entradas)
        {
            var esTransicionCiclo = string.Equals(moduloSolicitud, "Ciclos", StringComparison.OrdinalIgnoreCase) &&
                accionSolicitud is "Activar" or "Cerrar" or "Reabrir";
            if (esTransicionCiclo && entrada.Entity is CicloEscolar or MovimientoCicloEscolar)
                continue;

            var accionPersistencia = entrada.State switch
            {
                EntityState.Added => "Crear",
                EntityState.Modified => "Editar",
                EntityState.Deleted => "Eliminar",
                _ => "Modificar"
            };
            var entidad = entrada.Metadata.ClrType.Name;
            var modulo = string.IsNullOrWhiteSpace(moduloSolicitud) ? entidad : moduloSolicitud;
            var accion = string.IsNullOrWhiteSpace(accionSolicitud) ? accionPersistencia : accionSolicitud;

            contexto.Set<RegistroAuditoria>().Add(new RegistroAuditoria
            {
                FechaUtc = DateTime.UtcNow,
                UsuarioId = usuarioId,
                Modulo = Limitar(modulo, 80) ?? "Sistema",
                Accion = Limitar(accion, 120) ?? accionPersistencia,
                Tipo = EsEventoSeguridad(modulo, accion)
                    ? TipoEventoAuditoria.Seguridad
                    : TipoEventoAuditoria.Operacion,
                Resultado = ResultadoAuditoria.Exitoso,
                Entidad = Limitar(entidad, 120),
                EntidadId = Limitar(ObtenerClave(entrada), 128),
                Descripcion = $"{accionPersistencia} {entidad} durante la operación {accion}.",
                ValoresAnteriores = entrada.State is EntityState.Modified or EntityState.Deleted
                    ? SerializarValores(entrada, originales: true)
                    : null,
                ValoresNuevos = entrada.State is EntityState.Added or EntityState.Modified
                    ? SerializarValores(entrada, originales: false)
                    : null,
                DireccionIp = Limitar(http?.Connection.RemoteIpAddress?.ToString(), 45),
                UserAgent = Limitar(solicitud?.Headers.UserAgent.ToString(), 512),
                Ruta = Limitar(solicitud == null ? null : solicitud.PathBase + solicitud.Path, 512),
                MetodoHttp = Limitar(solicitud?.Method, 16),
                CorrelationId = Limitar(http?.TraceIdentifier, 64)
            });
        }
    }

    private static string? SerializarValores(EntityEntry entrada, bool originales)
    {
        var valores = new Dictionary<string, object?>();

        foreach (var propiedad in entrada.Properties)
        {
            if (!AuditoriaProteccionDatos.EsPropiedadPermitida(propiedad.Metadata.Name) ||
                propiedad.Metadata.IsShadowProperty())
                continue;

            if (entrada.State == EntityState.Modified && !propiedad.IsModified && !propiedad.Metadata.IsPrimaryKey())
                continue;

            var valor = originales ? propiedad.OriginalValue : propiedad.CurrentValue;
            valores[propiedad.Metadata.Name] = valor is byte[] ? "[Dato binario omitido]" : valor;
        }

        if (valores.Count == 0) return null;
        var json = JsonSerializer.Serialize(valores);
        return json.Length <= 8000
            ? json
            : "[Valores omitidos por exceder el límite permitido]";
    }

    private static string? ObtenerClave(EntityEntry entrada)
    {
        var clave = entrada.Metadata.FindPrimaryKey();
        if (clave == null) return null;

        var partes = clave.Properties
            .Select(x => entrada.Property(x.Name).CurrentValue?.ToString())
            .Where(x => !string.IsNullOrWhiteSpace(x));
        var resultado = string.Join(",", partes);
        return string.IsNullOrWhiteSpace(resultado) ? null : resultado;
    }

    private static bool EsEventoSeguridad(string modulo, string accion) =>
        string.Equals(modulo, "Roles", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(modulo, "PermisosSistema", StringComparison.OrdinalIgnoreCase) ||
        accion.Contains("Contrasena", StringComparison.OrdinalIgnoreCase) ||
        accion.Contains("Contraseña", StringComparison.OrdinalIgnoreCase);

    private static string? Limitar(string? valor, int maximo)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var limpio = valor.Trim();
        return limpio.Length <= maximo ? limpio : limpio[..maximo];
    }
}
