namespace Proyecto_Final.Servicios.Archivos;

public static class InicializadorArchivos
{
    public static void Inicializar(
        IWebHostEnvironment ambiente,
        ILogger logger)
    {
        try
        {
            var appData = Path.Combine(
                ambiente.ContentRootPath,
                "App_Data"
            );

            var archivos = Path.Combine(
                appData,
                "archivos"
            );

            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }

            if (!Directory.Exists(archivos))
            {
                Directory.CreateDirectory(archivos);
            }

            logger.LogInformation(
                "Almacenamiento de archivos verificado en {Ruta}.",
                archivos
            );
        }
        catch (Exception ex)
        {
            logger.LogCritical(
                ex,
                "No fue posible inicializar el almacenamiento de archivos."
            );

            throw;
        }
    }
}