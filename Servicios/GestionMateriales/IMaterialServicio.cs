using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Materiales;

namespace Proyecto_Final.Servicios.GestionMateriales;

public interface IMaterialServicio
{
    Task<List<MaterialLista>> ObtenerTodosAsync();
    Task<List<MaterialLista>> ObtenerHistorialAsync();

    Task PrepararAsync(FormularioMaterial m);

    Task<ResultadoOperacion> CrearAsync(
        CrearMaterial m,
        CancellationToken ct
    );

    Task<EditarMaterial?> ObtenerEditarAsync(int id);

    Task<ResultadoOperacion> EditarAsync(
        EditarMaterial m,
        CancellationToken ct
    );

    Task<ResultadoOperacion> PublicarAsync(int id);

    Task<ResultadoOperacion> OcultarAsync(int id);

    Task<(Stream Stream, string Mime, string Nombre)?> AbrirAsync(int id);
}
