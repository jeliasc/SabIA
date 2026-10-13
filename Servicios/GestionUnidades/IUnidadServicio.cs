using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Unidades;

namespace Proyecto_Final.Servicios.GestionUnidades;

public interface IUnidadServicio
{
    Task<List<UnidadLista>> ObtenerTodosAsync();
    Task<List<UnidadLista>> ObtenerHistorialAsync();

    Task PrepararAsync(FormularioUnidad m);

    Task<ResultadoOperacion> CrearAsync(
        CrearUnidad m
    );

    Task<EditarUnidad?> ObtenerEditarAsync(int id);

    Task<ResultadoOperacion> EditarAsync(
        EditarUnidad m
    );

    Task<ResultadoOperacion> PublicarAsync(
        int unidadAsignacionId
    );

    Task<ResultadoOperacion> OcultarAsync(
        int unidadAsignacionId
    );
}
