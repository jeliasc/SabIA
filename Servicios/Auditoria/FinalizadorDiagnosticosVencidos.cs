namespace Proyecto_Final.Servicios.Auditoria;

/// <summary>Marca sesiones vencidas aun cuando no existan solicitudes web activas.</summary>
public sealed class FinalizadorDiagnosticosVencidos(
    IServiceScopeFactory scopeFactory,
    ILogger<FinalizadorDiagnosticosVencidos> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await temporizador.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IDiagnosticoServicio>()
                    .VencerSesionesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception error)
            {
                logger.LogError(error, "No fue posible verificar sesiones de diagnóstico vencidas.");
            }
        }
    }
}
