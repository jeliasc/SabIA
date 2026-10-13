namespace Proyecto_Final.Servicios.GestionCuestionarios;

// Revisa cada minuto los intentos de cuestionario cuyo tiempo terminó sin
// que el alumno los finalizara (cerró el navegador, perdió la conexión) y
// los califica con las respuestas que alcanzó a guardar. Así la entrega
// del alumno no queda pendiente de forma indefinida.
public sealed class FinalizadorIntentosVencidos(
    IServiceScopeFactory fabricaAmbitos,
    ILogger<FinalizadorIntentosVencidos> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Espera inicial para no competir con las migraciones y los seeders del arranque.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        using var temporizador = new PeriodicTimer(Intervalo);

        do
        {
            try
            {
                await using var ambito = fabricaAmbitos.CreateAsyncScope();
                var servicio = ambito.ServiceProvider.GetRequiredService<ICuestionarioServicio>();
                var finalizados = await servicio.FinalizarVencidosAsync(DateTime.UtcNow, stoppingToken);

                if (finalizados > 0)
                {
                    logger.LogInformation(
                        "Se finalizaron {Cantidad} intento(s) de cuestionario con tiempo vencido.",
                        finalizados);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al finalizar intentos de cuestionario vencidos.");
            }
        }
        while (await temporizador.WaitForNextTickAsync(stoppingToken));
    }
}
