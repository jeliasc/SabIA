using Proyecto_Final.Servicios.Comunes;

namespace Proyecto_Final.Servicios.InteligenciaArtificial;

public interface IIaServicio
{
    Task<ResultadoOperacion<List<TrabajoIa>>> GenerarAsync(
        int planificacionId,
        IReadOnlyCollection<string> tipos,
        CancellationToken cancellationToken = default);

    Task<ResultadoOperacion<List<TrabajoIa>>> GenerarDesdeDocumentoAsync(
        int planificacionId,
        IReadOnlyCollection<SolicitudContenidoIa> solicitudes,
        Stream? documento = null,
        string? nombreDocumento = null,
        string? tipoMimeDocumento = null,
        CancellationToken cancellationToken = default);

    Task<ResultadoOperacion<List<TrabajoIa>>> GenerarDesdeArchivoLocalAsync(
        IReadOnlyCollection<SolicitudContenidoIa> solicitudes,
        Stream documento,
        string nombreDocumento,
        string? tipoMimeDocumento,
        long tamanoDocumento,
        CancellationToken cancellationToken = default);

    Task<ResultadoOperacion<List<TrabajoIa>>> GenerarDirectoAsync(
        string prompt,
        int maxTokens,
        CancellationToken cancellationToken = default);

    Task<ResultadoOperacion<EstadoTrabajoIa>> ConsultarEstadoAsync(
        string jobId,
        CancellationToken cancellationToken = default);
}

public sealed record SolicitudContenidoIa(string Tipo, string Instrucciones = "");

public sealed class TrabajoIa
{
    public string JobId { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
}

public sealed class EstadoTrabajoIa
{
    public string JobId { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public string? Resultado { get; init; }
}
