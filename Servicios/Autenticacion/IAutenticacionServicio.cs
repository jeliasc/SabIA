using System.Security.Claims;
using Proyecto_Final.ViewModels.Autenticacion;

namespace Proyecto_Final.Servicios.Autenticacion;

public interface IAutenticacionServicio
{
    Task<ResultadoAutenticacion> IniciarSesionAsync(
        Login modelo);

    Task<ResultadoAutenticacion> CambiarContrasenaAsync(
        CambiarContrasena modelo,
        ClaimsPrincipal usuarioActual);

    Task CerrarSesionAsync();
}