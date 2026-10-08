using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Inscripciones;

namespace Proyecto_Final.Servicios.GestionInscripciones;

public interface IInscripcionServicio
{
    Task<List<InscripcionLista>> ObtenerTodosAsync();

    Task PrepararAsync(FormularioInscripcion m);

    Task<ResultadoOperacion> CrearAsync(
        CrearInscripcion m
    );

    Task<TrasladarInscripcion?> ObtenerTrasladoAsync(
        int id
    );

    Task<ResultadoOperacion> TrasladarAsync(
        TrasladarInscripcion m,
        string usuarioId
    );

    Task<ResultadoOperacion> CambiarEstadoAsync(
        int id,
        bool activa
    );
}