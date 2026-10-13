using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Documentos;

namespace Proyecto_Final.Servicios.GestionDocumentos;

public interface IDocumentoServicio
{
    Task<List<DocumentoLista>> ObtenerTodosAsync();

    Task<ResultadoOperacion> CrearAsync(
        CrearDocumento m,
        string usuarioId,
        CancellationToken ct
    );

    Task<(Stream Stream, string Mime, string Nombre)?> AbrirAsync(int id);
}