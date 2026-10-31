using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.ComentariosAcademicos;

namespace Proyecto_Final.Servicios.GestionComentariosAcademicos;

public interface IComentarioAcademicoServicio
{
    Task<ConversacionAcademica?> ObtenerAsync(int tareaId, int alumnoId);
    Task<ResultadoOperacion> CrearAsync(CrearComentarioAcademico modelo,
        CancellationToken cancellationToken = default);
    Task<(Stream Stream, string Mime, string Nombre)?> AbrirArchivoAsync(int archivoId,
        CancellationToken cancellationToken = default);
}
