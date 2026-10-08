using Microsoft.AspNetCore.Http;
using Proyecto_Final.Servicios.Comunes;

namespace Proyecto_Final.Servicios.Archivos;

public interface IArchivoFisicoServicio
{
    Task<ResultadoOperacion<ArchivoGuardado>> GuardarAsync(
        IFormFile archivo,
        string categoria,
        CancellationToken cancellationToken = default);

    Task<Stream?> AbrirLecturaAsync(
        string claveAlmacenamiento,
        CancellationToken cancellationToken = default);

    Task EliminarAsync(
        string claveAlmacenamiento,
        CancellationToken cancellationToken = default);
}

public sealed class ArchivoGuardado
{
    public string NombreOriginal { get; init; } = string.Empty;
    public string ClaveAlmacenamiento { get; init; } = string.Empty;
    public string TipoMime { get; init; } = string.Empty;
    public long TamanoBytes { get; init; }
    public string HashSha256 { get; init; } = string.Empty;
}
