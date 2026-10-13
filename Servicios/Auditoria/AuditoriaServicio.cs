using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Proyecto_Final.Data;
using Proyecto_Final.Models;

namespace Proyecto_Final.Servicios.Auditoria;

public sealed class AuditoriaServicio(
    Contexto contexto,
    IHttpContextAccessor httpContextAccessor,
    IServiceScopeFactory scopeFactory) : IAuditoriaServicio
{
    public RegistroAuditoria CrearRegistro(
        string modulo, string accion, TipoEventoAuditoria tipo,
        ResultadoAuditoria resultado, string descripcion,
        string? entidad = null, string? entidadId = null,
        string? justificacion = null, string? valoresAnteriores = null,
        string? valoresNuevos = null, string? usuarioId = null)
    {
        var http = httpContextAccessor.HttpContext;
        var request = http?.Request;

        return new RegistroAuditoria
        {
            FechaUtc = DateTime.UtcNow,
            UsuarioId = Limitar(usuarioId ?? http?.User.FindFirstValue(ClaimTypes.NameIdentifier), 450),
            Modulo = Requerido(modulo, 80),
            Accion = Requerido(accion, 120),
            Tipo = tipo,
            Resultado = resultado,
            Entidad = Limitar(entidad, 120),
            EntidadId = Limitar(entidadId, 128),
            Descripcion = Requerido(descripcion, 2000),
            ValoresAnteriores = AuditoriaProteccionDatos.SanitizarJson(valoresAnteriores, 8000),
            ValoresNuevos = AuditoriaProteccionDatos.SanitizarJson(valoresNuevos, 8000),
            Justificacion = Limitar(justificacion, 1000),
            DireccionIp = Limitar(http?.Connection.RemoteIpAddress?.ToString(), 45),
            UserAgent = Limitar(request?.Headers.UserAgent.ToString(), 512),
            Ruta = Limitar(request == null ? null : request.PathBase + request.Path, 512),
            MetodoHttp = Limitar(request?.Method, 16),
            CorrelationId = Limitar(http?.TraceIdentifier, 64)
        };
    }

    public void AgregarATransaccion(RegistroAuditoria registro) =>
        contexto.RegistrosAuditoria.Add(registro);

    public async Task RegistrarAsync(RegistroAuditoria registro)
    {
        contexto.RegistrosAuditoria.Add(registro);
        await contexto.SaveChangesAsync();
    }

    public async Task RegistrarIndependienteAsync(RegistroAuditoria registro)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var contextoIndependiente = scope.ServiceProvider.GetRequiredService<Contexto>();
        contextoIndependiente.RegistrosAuditoria.Add(registro);
        await contextoIndependiente.SaveChangesAsync();
    }

    private static string Requerido(string valor, int maximo) =>
        Limitar(valor, maximo) ?? throw new ArgumentException("El valor es obligatorio.");

    private static string? Limitar(string? valor, int maximo)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var limpio = valor.Trim();
        return limpio.Length <= maximo ? limpio : limpio[..maximo];
    }

}
