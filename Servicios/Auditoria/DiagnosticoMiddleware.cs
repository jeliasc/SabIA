using System.Diagnostics;
using System.Security.Claims;
using Proyecto_Final.ViewModels.Auditoria;

namespace Proyecto_Final.Servicios.Auditoria;

/// <summary>
/// Captura metadatos técnicos de solicitudes cubiertas por una sesión activa.
/// Excluye solicitudes sin controlador o acción identificables.
/// </summary>
public sealed class DiagnosticoMiddleware(
    RequestDelegate next,
    ILogger<DiagnosticoMiddleware> logger)
{
    public async Task InvokeAsync(
        HttpContext contexto,
        IServiceScopeFactory scopeFactory)
    {
        var usuarioId = contexto.User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            await next(contexto);
            return;
        }

        var reloj = Stopwatch.StartNew();
        Exception? error = null;

        try
        {
            await next(contexto);
        }
        catch (Exception exception)
        {
            error = exception;
            throw;
        }
        finally
        {
            reloj.Stop();

            try
            {
                var rutas = contexto.Request.RouteValues;

                var controlador = rutas["controller"]?.ToString();
                var accion = rutas["action"]?.ToString();

                // OMITIR SOLICITUDES SIN CONTEXTO MVC
                if (!string.IsNullOrWhiteSpace(controlador) &&
                    !string.IsNullOrWhiteSpace(accion))
                {
                    var resultadoHttp = error == null
                        ? contexto.Response.StatusCode
                        : StatusCodes.Status500InternalServerError;

                    var categoria = error != null
                        ? "Excepción"
                        : resultadoHttp >= 400
                            ? "Resultado HTTP"
                            : "Solicitud";

                    var solicitud = new DiagnosticoSolicitudTecnica
                    {
                        UsuarioId = usuarioId,
                        Modulo = controlador,
                        Controlador = controlador,
                        Accion = accion,
                        ResultadoHttp = resultadoHttp,
                        Operacion = contexto.Request.Method + " " + accion,
                        TipoExcepcion = error?.GetType().Name,
                        MensajeTecnico = error?.Message,
                        Componente = error?.Source,
                        CorrelationId = contexto.TraceIdentifier,
                        DuracionMilisegundos = reloj.ElapsedMilliseconds,
                        Categoria = categoria
                    };

                    await using var scope =
                        scopeFactory.CreateAsyncScope();

                    var diagnostico = scope.ServiceProvider
                        .GetRequiredService<IDiagnosticoServicio>();

                    await diagnostico.RegistrarSolicitudAsync(
                        solicitud,
                        contexto.RequestAborted);
                }
            }
            catch (Exception errorDiagnostico)
            {
                logger.LogWarning(
                    errorDiagnostico,
                    "No fue posible guardar el evento de diagnóstico.");
            }
        }
    }
}