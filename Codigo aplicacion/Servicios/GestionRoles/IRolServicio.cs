using Proyecto_Final.ViewModels.Roles;
using Proyecto_Final.Models;

namespace Proyecto_Final.Servicios.GestionRoles;

public interface IRolServicio
{
    Task<List<Rol>> ObtenerTodosAsync();

    Task<ResultadoRol<bool>> CrearAsync(
        CrearRol modelo
    );

    Task<ResultadoRol<EditarRol>> ObtenerParaEditarAsync(
        string id
    );

    Task<ResultadoRol<bool>> EditarAsync(
        EditarRol modelo
    );

    Task<ResultadoRol<bool>> DesactivarAsync(
        string id
    );

    Task<ResultadoRol<bool>> ActivarAsync(
        string id
    );

    Task<CrearRol> ObtenerParaCrearAsync();
}