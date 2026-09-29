using Proyecto_Final.ViewModels.PermisosSistema;

namespace Proyecto_Final.Servicios.GestionPermisos;

public interface IPermisoServicio
{
    Task<PermisosSistemaIndex> ObtenerTodosAsync();

    Task<ResultadoPermiso> CrearAsync(
        CrearPermiso modelo
    );

    Task<EditarPermiso?> ObtenerParaEditarAsync(
        int id
    );

    Task<ResultadoPermiso> EditarAsync(
        EditarPermiso modelo
    );

    Task<ResultadoPermiso> DesactivarAsync(
    int id
);

    Task<ResultadoPermiso> ActivarAsync(
        int id
    );
}
