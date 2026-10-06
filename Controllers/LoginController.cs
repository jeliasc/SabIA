using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Autenticacion;
using Proyecto_Final.Servicios.Correo;
using Proyecto_Final.ViewModels.Autenticacion;
using Proyecto_Final.Data;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Controllers;

public class LoginController : Controller
{
    private readonly IAutenticacionServicio autenticacionServicio;
    private readonly UserManager<Usuario> administradorUsuarios;
    private readonly IServicioCorreo servicioCorreo;
    private readonly Contexto contexto;

    public LoginController(
        IAutenticacionServicio autenticacionServicio,
        UserManager<Usuario> administradorUsuarios,
        IServicioCorreo servicioCorreo,
        Contexto contexto)
    {
        this.autenticacionServicio =
            autenticacionServicio;

        this.administradorUsuarios =
            administradorUsuarios;

        this.servicioCorreo =
            servicioCorreo;

        this.contexto =
            contexto;
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
            return RedirectToAction(nameof(Login));
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

    // RESTABLECER CONTRASEÑA - MOSTRAR FORMULARIO
    [AllowAnonymous]
    [HttpGet]
    public IActionResult RestablecerContrasena(
        string usuarioId,
        string token)
    {
        if (string.IsNullOrWhiteSpace(usuarioId) ||
            string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(
                nameof(Login)
            );
        }

        var modelo =
            new RestablecerContrasena
            {
                UsuarioId = usuarioId,
                Token = token
            };

        return View(modelo);
    }

    // RESTABLECER CONTRASEÑA - GUARDAR NUEVA CONTRASEÑA
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestablecerContrasena(
        RestablecerContrasena modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario =
            await administradorUsuarios
                .FindByIdAsync(
                    modelo.UsuarioId
                );

        if (usuario == null ||
            !usuario.Activo)
        {
            ModelState.AddModelError(
                string.Empty,
                "El enlace de recuperación no es válido."
            );

            return View(modelo);
        }

        string token;

        try
        {
            token =
                Encoding.UTF8.GetString(
                    WebEncoders.Base64UrlDecode(
                        modelo.Token
                    )
                );
        }
        catch
        {
            ModelState.AddModelError(
                string.Empty,
                "El enlace de recuperación no es válido."
            );

            return View(modelo);
        }

        await using var transaccion =
            await contexto.Database
                .BeginTransactionAsync();

        try
        {
            var resultado =
                await administradorUsuarios
                    .ResetPasswordAsync(
                        usuario,
                        token,
                        modelo.Contrasena
                    );

            if (!resultado.Succeeded)
            {
                await transaccion.RollbackAsync();

                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                return View(modelo);
            }

            usuario.CambiarContrasena = false;
            usuario.FechaUltimoCambioContrasena =
                DateTime.UtcNow;

            var resultadoActualizacion =
                await administradorUsuarios
                    .UpdateAsync(usuario);

            if (!resultadoActualizacion.Succeeded)
            {
                await transaccion.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "No fue posible completar el restablecimiento de la contraseña."
                );

                return View(modelo);
            }

            await transaccion.CommitAsync();

            return RedirectToAction(
                nameof(RestablecimientoCompletado)
            );
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    // RECUPERAR CONTRASEÑA - MOSTRAR FORMULARIO
    [AllowAnonymous]
    [HttpGet]
    public IActionResult RecuperarContrasena()
    {
        return View();
    }

    // RECUPERAR CONTRASEÑA - ENVIAR ENLACE
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecuperarContrasena(
        RecuperarContrasena modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario =
            await administradorUsuarios
                .FindByEmailAsync(
                    modelo.Correo
                );

        if (usuario != null &&
            usuario.Activo)
        {
            var token =
                await administradorUsuarios
                    .GeneratePasswordResetTokenAsync(
                        usuario
                    );

            var tokenCodificado =
                WebEncoders.Base64UrlEncode(
                    Encoding.UTF8.GetBytes(
                        token
                    )
                );

            var enlace =
                Url.Action(
                    nameof(RestablecerContrasena),
                    "Login",
                    new
                    {
                        usuarioId = usuario.Id,
                        token = tokenCodificado
                    },
                    Request.Scheme
                );

            if (!string.IsNullOrWhiteSpace(enlace))
            {
                var contenidoHtml =
                    $"""
                    <h2>Restablecer contraseña</h2>

                    <p>
                        Se recibió una solicitud para restablecer
                        la contraseña de su cuenta en SabIA.
                    </p>

                    <p>
                        <a href="{enlace}">
                            Restablecer contraseña
                        </a>
                    </p>

                    <p>
                        Si usted no realizó esta solicitud,
                        puede ignorar este correo.
                    </p>
                    """;

                await servicioCorreo.EnviarAsync(
                    modelo.Correo,
                    "Restablecer contraseña - SabIA",
                    contenidoHtml
                );
            }
        }

        return RedirectToAction(
            nameof(RecuperacionSolicitada)
        );
    }

    // RECUPERACIÓN SOLICITADA
    [AllowAnonymous]
    [HttpGet]
    public IActionResult RecuperacionSolicitada()
    {
        return View();
    }

    // RESTABLECIMIENTO COMPLETADO
    [AllowAnonymous]
    [HttpGet]
    public IActionResult RestablecimientoCompletado()
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