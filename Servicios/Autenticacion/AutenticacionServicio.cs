using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Auditoria;
using Proyecto_Final.ViewModels.Autenticacion;

namespace Proyecto_Final.Servicios.Autenticacion;

public class AutenticacionServicio : IAutenticacionServicio
{
    private readonly SignInManager<Usuario> administradorSesion;
    private readonly UserManager<Usuario> administradorUsuarios;
    private readonly IAuditoriaServicio auditoria;

    public AutenticacionServicio(
        SignInManager<Usuario> administradorSesion,
        UserManager<Usuario> administradorUsuarios,
        IAuditoriaServicio auditoria)
    {
        this.administradorSesion = administradorSesion;
        this.administradorUsuarios = administradorUsuarios;
        this.auditoria = auditoria;
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
        if (usuario == null)
        {
            await RegistrarRechazoInicioSesionAsync("Credenciales inválidas.");
            return ResultadoAutenticacion
                .CredencialesInvalidas(
                    "Usuario o contraseña incorrectos."
                );
        }

        if (!usuario.Activo)
        {
            await RegistrarRechazoInicioSesionAsync("Cuenta inactiva.", usuario.Id);
            return ResultadoAutenticacion
                .CuentaInactiva(
                    "Su cuenta se encuentra inactiva. Comuníquese con el administrador."
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
            await RegistrarRechazoInicioSesionAsync("Cuenta bloqueada temporalmente.", usuario.Id);
            return ResultadoAutenticacion.Bloqueado(
                "La cuenta está bloqueada temporalmente por varios intentos fallidos."
            );
        }

        if (!resultado.Succeeded)
        {
            await RegistrarRechazoInicioSesionAsync("Credenciales inválidas.", usuario.Id);
            return ResultadoAutenticacion
                .CredencialesInvalidas(
                    "Usuario o contraseña incorrectos."
                );
        }

        var inicioExitoso = auditoria.CrearRegistro(
            "Autenticación",
            "Iniciar sesión",
            TipoEventoAuditoria.Seguridad,
            ResultadoAuditoria.Exitoso,
            usuario.CambiarContrasena
                ? "Inicio de sesión correcto; requiere cambio de contraseña."
                : "Inicio de sesión correcto.",
            entidad: nameof(Usuario),
            entidadId: usuario.Id,
            usuarioId: usuario.Id);
        await auditoria.RegistrarAsync(inicioExitoso);

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
            var rechazo = auditoria.CrearRegistro(
                "Autenticación", "Cambiar contraseña", TipoEventoAuditoria.Seguridad,
                ResultadoAuditoria.Rechazado,
                "No se autorizó el cambio de contraseña.",
                entidad: nameof(Usuario), entidadId: usuario.Id, usuarioId: usuario.Id);
            await auditoria.RegistrarIndependienteAsync(rechazo);
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

        var cambioExitoso = auditoria.CrearRegistro(
            "Autenticación", "Cambiar contraseña", TipoEventoAuditoria.Seguridad,
            ResultadoAuditoria.Exitoso,
            "La contraseña fue cambiada correctamente.",
            entidad: nameof(Usuario), entidadId: usuario.Id, usuarioId: usuario.Id);
        await auditoria.RegistrarAsync(cambioExitoso);

        return ResultadoAutenticacion.Correcto(
            "La contraseña fue actualizada correctamente."
        );
    }

    // CERRAR SESIÓN
    public async Task CerrarSesionAsync()
    {
        var cierre = auditoria.CrearRegistro(
            "Autenticación", "Cerrar sesión", TipoEventoAuditoria.Seguridad,
            ResultadoAuditoria.Exitoso, "Cierre de sesión correcto.");
        await auditoria.RegistrarAsync(cierre);
        await administradorSesion.SignOutAsync();
    }

    private async Task RegistrarRechazoInicioSesionAsync(string descripcion, string? usuarioId = null)
    {
        var registro = auditoria.CrearRegistro(
            "Autenticación", "Iniciar sesión", TipoEventoAuditoria.Seguridad,
            ResultadoAuditoria.Rechazado, descripcion,
            entidad: nameof(Usuario), entidadId: usuarioId, usuarioId: usuarioId);
        await auditoria.RegistrarIndependienteAsync(registro);
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
