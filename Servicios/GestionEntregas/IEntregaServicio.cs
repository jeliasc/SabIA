using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Entregas;

namespace Proyecto_Final.Servicios.GestionEntregas;

public interface IEntregaServicio
{
    Task<List<EntregaLista>> ObtenerTodasAsync();
    Task<List<EntregaLista>> ObtenerHistorialAsync();

    Task<EntregarTarea?> ObtenerParaEntregarAsync(int id);

    Task<ResultadoOperacion> EntregarAsync(
        EntregarTarea m,
        CancellationToken ct
    );

    Task<CalificarEntrega?> ObtenerParaCalificarAsync(int id);

    Task<ResultadoOperacion> CalificarAsync(
        CalificarEntrega m
    );

    Task<ReabrirEntrega?> ObtenerParaReabrirAsync(int id);

    Task<ResultadoOperacion> ReabrirAsync(ReabrirEntrega m);

    Task<(Stream Stream, string Mime, string Nombre)?> DescargarArchivoAsync(
        int archivoId
    );

    Task<(Stream Stream, string Mime, string Nombre)?> DescargarArchivoTareaAsync(
        int archivoId
    );
}
