using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Servicios.Autenticacion;
using Proyecto_Final.ViewModels.Autenticacion;

namespace Proyecto_Final.Controllers;

public class LoginController : Controller
{
    private readonly IAutenticacionServicio
        autenticacionServicio;

    public LoginController(
        IAutenticacionServicio autenticacionServicio)
    {
        this.autenticacionServicio =
            autenticacionServicio;
    }

    // LOGIN - MOSTRAR FORMULARIO
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(
        string? urlRetorno = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(
                "Index",
                "Home"
            );
        }

        ViewData["UrlRetorno"] = urlRetorno;

        return View();
    }

    // LOGIN - VALIDAR CREDENCIALES
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        Login modelo,
        string? urlRetorno = null)
    {
        ViewData["UrlRetorno"] = urlRetorno;

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado =
            await autenticacionServicio
                .IniciarSesionAsync(modelo);

        if (resultado.Tipo ==
            TipoResultadoAutenticacion.Bloqueado)
        {
            ModelState.AddModelError(
                string.Empty,
                resultado.Mensaje ?? string.Empty
            );

            return View(modelo);
        }

        if (resultado.Tipo ==
            TipoResultadoAutenticacion.CuentaInactiva)
        {
            ModelState.AddModelError(
                string.Empty,
                resultado.Mensaje ?? string.Empty
            );

            return View(modelo);
        }

        if (resultado.Tipo ==
            TipoResultadoAutenticacion
                .CredencialesInvalidas)
        {
            ModelState.AddModelError(
                string.Empty,
                resultado.Mensaje ?? string.Empty
            );

            return View(modelo);
        }

        if (resultado.Tipo ==
            TipoResultadoAutenticacion
                .RequiereCambioContrasena)
        {
            return RedirectToAction(
                "CambiarContrasena"
            );
        }

        // Regresa a la dirección solicitada originalmente
        // si es una URL local segura.
        if (!string.IsNullOrWhiteSpace(urlRetorno) &&
            Url.IsLocalUrl(urlRetorno))
        {
            return Redirect(urlRetorno);
        }

        return RedirectToAction(
            "Index",
            "Home"
        );
    }

    // CAMBIAR CONTRASEÑA - MOSTRAR FORMULARIO
    [Authorize]
    [HttpGet]
    public IActionResult CambiarContrasena()
    {
        return View();
    }

    // CAMBIAR CONTRASEÑA - GUARDAR NUEVA CONTRASEÑA
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarContrasena(
        CambiarContrasena modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado =
            await autenticacionServicio
                .CambiarContrasenaAsync(
                    modelo,
                    User
                );

        if (resultado.Tipo ==
            TipoResultadoAutenticacion.NoAutenticado)
        {
            return RedirectToAction("Login");
        }

        if (resultado.Tipo ==
            TipoResultadoAutenticacion.Validacion)
        {
            AgregarErrores(
                resultado.Errores
            );

            return View(modelo);
        }

        TempData["Exito"] =
            resultado.Mensaje;

        return RedirectToAction(
            "Index",
            "Home"
        );
    }

    // RECUPERAR CONTRASEÑA - MOSTRAR INFORMACIÓN
    [AllowAnonymous]
    [HttpGet]
    public IActionResult RecuperarContrasena()
    {
        return View();
    }

    // CERRAR SESIÓN
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarSesion()
    {
        await autenticacionServicio
            .CerrarSesionAsync();

        return RedirectToAction("Login");
    }

    // ACCESO DENEGADO
    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccesoDenegado()
    {
        return View();
    }

    // AGREGAR ERRORES AL MODELO
    private void AgregarErrores(
        Dictionary<string, List<string>> errores)
    {
        foreach (var errorCampo in errores)
        {
            foreach (var mensaje in errorCampo.Value)
            {
                ModelState.AddModelError(
                    errorCampo.Key,
                    mensaje
                );
            }
        }
    }
}