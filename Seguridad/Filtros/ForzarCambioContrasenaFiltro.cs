using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Proyecto_Final.Models;

namespace Proyecto_Final.Seguridad.Filtros;

public class ForzarCambioContrasenaFiltro : IAsyncActionFilter
{
    private readonly UserManager<Usuario> administradorUsuarios;
    private readonly SignInManager<Usuario> administradorSesion;

    public ForzarCambioContrasenaFiltro(
        UserManager<Usuario> administradorUsuarios,
        SignInManager<Usuario> administradorSesion)
    {
        this.administradorUsuarios = administradorUsuarios;
        this.administradorSesion = administradorSesion;
    }

    // VALIDAR CAMBIO OBLIGATORIO DE CONTRASEÑA
    public async Task OnActionExecutionAsync(
        ActionExecutingContext contexto,
        ActionExecutionDelegate siguiente)
    {
        // Si no existe una sesión iniciada, continúa normalmente.
        if (contexto.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            await siguiente();
            return;
        }

        // Obtiene el usuario asociado a la sesión actual.
        var usuario = await administradorUsuarios
            .GetUserAsync(contexto.HttpContext.User);

        // Si el usuario ya no existe o fue desactivado,
        // se cierra inmediatamente la sesión.
        if (usuario == null || !usuario.Activo)
        {
            await administradorSesion.SignOutAsync();

            contexto.Result = new RedirectToActionResult(
                "Login",
                "Login",
                null
            );

            return;
        }

        var controlador =
            contexto.ActionDescriptor.RouteValues["controller"];

        var accion =
            contexto.ActionDescriptor.RouteValues["action"];

        // Estas acciones deben permanecer disponibles mientras
        // el usuario realiza el cambio obligatorio de contraseña.
        var accionPermitida =
            string.Equals(
                controlador,
                "Login",
                StringComparison.OrdinalIgnoreCase
            )
            &&
            (
                string.Equals(
                    accion,
                    "CambiarContrasena",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    accion,
                    "CerrarSesion",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    accion,
                    "AccesoDenegado",
                    StringComparison.OrdinalIgnoreCase
                )
            );

        if (accionPermitida)
        {
            await siguiente();
            return;
        }

        // Si tiene una contraseña temporal o restablecida,
        // no puede acceder al resto del sistema.
        if (usuario.CambiarContrasena)
        {
            contexto.Result = new RedirectToActionResult(
                "CambiarContrasena",
                "Login",
                null
            );

            return;
        }

        await siguiente();
    }
}