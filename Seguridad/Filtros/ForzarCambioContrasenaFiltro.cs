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
            contexto.Result = ResultadoNoAutenticado(contexto);

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
            contexto.Result = EsNavegacionHtmlGet(contexto)
                ? new RedirectToActionResult("CambiarContrasena", "Login", null)
                : new ObjectResult(new
                {
                    status = StatusCodes.Status403Forbidden,
                    title = "Debe cambiar su contraseña para continuar.",
                    detail = "Complete el cambio obligatorio de contraseña antes de realizar esta operación."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };

            return;
        }

        await siguiente();
    }

    private static IActionResult ResultadoNoAutenticado(ActionExecutingContext contexto) =>
        EsNavegacionHtmlGet(contexto)
            ? new RedirectToActionResult("Login", "Login", null)
            : new ObjectResult(new
            {
                status = StatusCodes.Status401Unauthorized,
                title = "La sesión ya no está disponible.",
                detail = "Inicie sesión nuevamente para continuar."
            }) { StatusCode = StatusCodes.Status401Unauthorized };

    private static bool EsNavegacionHtmlGet(ActionExecutingContext contexto) =>
        HttpMethods.IsGet(contexto.HttpContext.Request.Method) &&
        !string.Equals(contexto.HttpContext.Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) &&
        contexto.HttpContext.Request.Headers.Accept.Any(x =>
            x?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
}
