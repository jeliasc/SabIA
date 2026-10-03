using System.Security.Claims;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Usuarios;

namespace Proyecto_Final.Servicios.Usuarios;

public interface IUsuarioServicio
{
    Task<List<Usuario>> ObtenerTodosAsync();

    Task<List<string>> ObtenerRolesDisponiblesAsync(
        ClaimsPrincipal usuarioActual);

    Task<ResultadoUsuario<ResultadoContrasenaTemporal>>
        CrearAsync(
            CrearUsuario modelo,
            ClaimsPrincipal usuarioActual);

    Task<ResultadoUsuario<EditarUsuario>>
        ObtenerParaEditarAsync(
            string id,
            ClaimsPrincipal usuarioActual);

    Task<ResultadoUsuario<bool>>
        EditarAsync(
            EditarUsuario modelo,
            ClaimsPrincipal usuarioActual);

    Task<ResultadoUsuario<ResultadoContrasenaTemporal>>
        RestablecerContrasenaAsync(
            string id,
            ClaimsPrincipal usuarioActual);

    Task<ResultadoUsuario<bool>>
        DesactivarAsync(
            string id,
            ClaimsPrincipal usuarioActual);

    Task<ResultadoUsuario<bool>>
        ActivarAsync(string id);

    string? ValidarCorreo(string correo);


    Task<ResultadoUsuario<bool>> ValidarDatosAsync(
    string nombreUsuario,
    string correo,
    string? idUsuario = null);

    Task<ResultadoUsuario<DetalleUsuario>>
    ObtenerDetalleAsync(string id);
}