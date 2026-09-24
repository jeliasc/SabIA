using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Autenticacion;

namespace Proyecto_Final.Servicios.Autenticacion;

public class AutenticacionServicio : IAutenticacionServicio
{
    private readonly SignInManager<Usuario> administradorSesion;
    private readonly UserManager<Usuario> administradorUsuarios;

    public AutenticacionServicio(
        SignInManager<Usuario> administradorSesion,
        UserManager<Usuario> administradorUsuarios)
    {
        this.administradorSesion = administradorSesion;
        this.administradorUsuarios = administradorUsuarios;
    }

    // INICIAR SESIÓN
    public async Task<ResultadoAutenticacion>
        IniciarSesionAsync(Login modelo)
    {
        var usuario =
            await administradorUsuarios.FindByNameAsync(
                modelo.Usuario
            );

        // No indicar si el usuario existe o está inactivo.
        if (usuario == null || !usuario.Activo)
        {
            return ResultadoAutenticacion
                .CredencialesInvalidas(
                    "Usuario o contraseña incorrectos."
                );
        }

        var resultado =
            await administradorSesion.PasswordSignInAsync(
                usuario,
                modelo.Contrasena,
                isPersistent: false,
                lockoutOnFailure: true
            );

        if (resultado.IsLockedOut)
        {
            return ResultadoAutenticacion.Bloqueado(
                "La cuenta está bloqueada temporalmente por varios intentos fallidos."
            );
        }

        if (!resultado.Succeeded)
        {
            return ResultadoAutenticacion
                .CredencialesInvalidas(
                    "Usuario o contraseña incorrectos."
                );
        }

        if (usuario.CambiarContrasena)
        {
            return ResultadoAutenticacion
                .RequiereCambioContrasena();
        }

        return ResultadoAutenticacion.Correcto();
    }

    // CAMBIAR CONTRASEÑA
    public async Task<ResultadoAutenticacion>
        CambiarContrasenaAsync(
            CambiarContrasena modelo,
            ClaimsPrincipal usuarioActual)
    {
        var usuario =
            await administradorUsuarios.GetUserAsync(
                usuarioActual
            );

        if (usuario == null)
        {
            await administradorSesion.SignOutAsync();

            return ResultadoAutenticacion
                .NoAutenticado();
        }

        var resultado =
            await administradorUsuarios.ChangePasswordAsync(
                usuario,
                modelo.ContrasenaActual,
                modelo.NuevaContrasena
            );

        if (!resultado.Succeeded)
        {
            return ResultadoAutenticacion.Validacion(
                CrearErroresIdentity(
                    resultado.Errors
                )
            );
        }

        usuario.CambiarContrasena = false;

        usuario.FechaUltimoCambioContrasena =
            DateTime.UtcNow;

        var resultadoActualizacion =
            await administradorUsuarios.UpdateAsync(
                usuario
            );

        if (!resultadoActualizacion.Succeeded)
        {
            return ResultadoAutenticacion.Validacion(
                CrearErroresIdentity(
                    resultadoActualizacion.Errors
                )
            );
        }

        await administradorSesion.RefreshSignInAsync(
            usuario
        );

        return ResultadoAutenticacion.Correcto(
            "La contraseña fue actualizada correctamente."
        );
    }

    // CERRAR SESIÓN
    public async Task CerrarSesionAsync()
    {
        await administradorSesion.SignOutAsync();
    }

    // CREAR ERRORES DE IDENTITY
    private static Dictionary<string, List<string>>
        CrearErroresIdentity(
            IEnumerable<IdentityError> erroresIdentity)
    {
        var errores =
            new Dictionary<string, List<string>>();

        foreach (var error in erroresIdentity)
        {
            if (!errores.TryGetValue(
                string.Empty,
                out var lista))
            {
                lista = [];
                errores[string.Empty] = lista;
            }

            lista.Add(
                TraducirErrorIdentity(error.Code)
            );
        }

        return errores;
    }

    // TRADUCIR ERRORES INTERNOS DE IDENTITY
    private static string TraducirErrorIdentity(
        string codigo)
    {
        return codigo switch
        {
            "PasswordMismatch" =>
                "La contraseña actual es incorrecta.",

            "PasswordTooShort" =>
                "La contraseña debe tener al menos 10 caracteres.",

            "PasswordRequiresNonAlphanumeric" =>
                "La contraseña debe contener al menos un carácter especial.",

            "PasswordRequiresDigit" =>
                "La contraseña debe contener al menos un número.",

            "PasswordRequiresLower" =>
                "La contraseña debe contener al menos una letra minúscula.",

            "PasswordRequiresUpper" =>
                "La contraseña debe contener al menos una letra mayúscula.",

            "PasswordRequiresUniqueChars" =>
                "La contraseña debe contener suficientes caracteres diferentes.",

            _ =>
                "No fue posible completar la operación. Verifique los datos e intente nuevamente."
        };
    }
}