using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Entregas;

namespace Proyecto_Final.Servicios.GestionEntregas;

public interface IEntregaServicio
{
    Task<List<EntregaLista>> ObtenerTodasAsync(int? asignacionId = null);
    Task<List<EntregaLista>> ObtenerHistorialAsync(int? asignacionId = null);

    Task<EntregarTarea?> ObtenerParaEntregarAsync(int id);

    Task<DetalleEntrega?> ObtenerDetalleAsync(int id);

    Task<ResultadoConsultaDetalleEntrega> ObtenerDetalleParaNavegacionAsync(int id);

    Task<ResultadoOperacion> EntregarAsync(
        EntregarTarea m,
        CancellationToken ct
    );

    Task<CalificarEntrega?> ObtenerParaCalificarAsync(int id);

    Task<ResultadoOperacion> CalificarAsync(
        CalificarEntrega m
    );

    Task<CalificarCeroEntrega?> ObtenerParaCalificarCeroAsync(int id);

    Task<ResultadoOperacion> CalificarCeroAsync(CalificarCeroEntrega m);

    Task<RectificarCeroAdministrativo?> ObtenerParaRectificarCeroAsync(int id);

    Task<ResultadoOperacion> RectificarCeroAsync(RectificarCeroAdministrativo m);

    Task<ReabrirEntrega?> ObtenerParaReabrirAsync(int id);

    Task<ResultadoOperacion> ReabrirAsync(ReabrirEntrega m);

    Task<(Stream Stream, string Mime, string Nombre)?> DescargarArchivoAsync(
        int archivoId
    );

    Task<(Stream Stream, string Mime, string Nombre)?> VisualizarArchivoAsync(
        int archivoId
    );

    Task<(Stream Stream, string Mime, string Nombre)?> DescargarArchivoTareaAsync(
        int archivoId
    );
}

public enum EstadoConsultaDetalleEntrega
{
    NoDisponible,
    Disponible,
    FueraDeCicloAutorizado
}

public sealed class ResultadoConsultaDetalleEntrega
{
    private ResultadoConsultaDetalleEntrega(
        EstadoConsultaDetalleEntrega estado,
        DetalleEntrega? detalle = null)
    {
        Estado = estado;
        Detalle = detalle;
    }

    public EstadoConsultaDetalleEntrega Estado { get; }

    public DetalleEntrega? Detalle { get; }

    public static ResultadoConsultaDetalleEntrega NoDisponible() =>
        new(EstadoConsultaDetalleEntrega.NoDisponible);

    public static ResultadoConsultaDetalleEntrega Disponible(DetalleEntrega detalle) =>
        new(EstadoConsultaDetalleEntrega.Disponible, detalle);

    public static ResultadoConsultaDetalleEntrega FueraDeCicloAutorizado() =>
        new(EstadoConsultaDetalleEntrega.FueraDeCicloAutorizado);
}
