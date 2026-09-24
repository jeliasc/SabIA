using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;

namespace Proyecto_Final.Data.Seeders;

public static class SeguridadSeeders
{
    public static async Task InicializarAsync(
        IServiceProvider servicios,
        IConfiguration configuracion)
    {
        var administradorRoles =
            servicios.GetRequiredService<RoleManager<IdentityRole>>();

        var administradorUsuarios =
            servicios.GetRequiredService<UserManager<Usuario>>();

        // ROLES INICIALES

        string[] roles =
        [
            Roles.Superusuario,
            Roles.Administrador,
            Roles.Usuario
        ];

        foreach (var nombreRol in roles)
        {
            if (!await administradorRoles.RoleExistsAsync(nombreRol))
            {
                var resultado = await administradorRoles.CreateAsync(
                    new IdentityRole(nombreRol)
                );

                VerificarResultado(
                    resultado,
                    $"crear el rol {nombreRol}"
                );
            }
        }

        // PERMISOS DEL SUPERUSUARIO

        var rolSuperusuario =
            await administradorRoles.FindByNameAsync(Roles.Superusuario)
            ?? throw new InvalidOperationException(
                "No se encontró el rol Superusuario."
            );

        var permisosActuales =
            await administradorRoles.GetClaimsAsync(rolSuperusuario);

        foreach (var permiso in Permisos.ObtenerTodos())
        {
            var existe = permisosActuales.Any(x =>
                x.Type == TiposClaims.Permiso &&
                x.Value == permiso
            );

            if (!existe)
            {
                var resultado =
                    await administradorRoles.AddClaimAsync(
                        rolSuperusuario,
                        new Claim(
                            TiposClaims.Permiso,
                            permiso
                        )
                    );

                VerificarResultado(
                    resultado,
                    $"asignar el permiso {permiso}"
                );
            }
        }

        // SUPERUSUARIO INICIAL

        // CREDENCIALES INICIALES DE DESARROLLO
        // Usuario: admin
        // Correo: admin@proyecto.local
        // Contraseña: Admin@12345

        var nombreUsuario =
            configuracion["SuperusuarioInicial:Usuario"];

        var correo =
            configuracion["SuperusuarioInicial:Correo"];

        var contrasena =
            configuracion["SuperusuarioInicial:Contrasena"];

        if (string.IsNullOrWhiteSpace(nombreUsuario) ||
            string.IsNullOrWhiteSpace(correo) ||
            string.IsNullOrWhiteSpace(contrasena))
        {
            throw new InvalidOperationException(
                "No se configuraron las credenciales del superusuario inicial."
            );
        }

        var superusuario =
            await administradorUsuarios.FindByNameAsync(nombreUsuario);

        if (superusuario == null)
        {
            superusuario = new Usuario
            {
                UserName = nombreUsuario,
                Email = correo,
                EmailConfirmed = true,

                PrimerNombre = "Superusuario",
                PrimerApellido = "Sistema",
                Activo = true,

                // La contraseña inicial debe cambiarse
                // en el primer inicio de sesión.
                CambiarContrasena = true,

                FechaCreacion = DateTime.UtcNow
            };

            var resultado =
                await administradorUsuarios.CreateAsync(
                    superusuario,
                    contrasena
                );

            VerificarResultado(
                resultado,
                "crear el superusuario inicial"
            );
        }

        // ASIGNAR ROL AL SUPERUSUARIO

        if (!await administradorUsuarios.IsInRoleAsync(
            superusuario,
            Roles.Superusuario))
        {
            var resultado =
                await administradorUsuarios.AddToRoleAsync(
                    superusuario,
                    Roles.Superusuario
                );

            VerificarResultado(
                resultado,
                "asignar el rol Superusuario"
            );
        }
    }

    // VALIDAR RESULTADOS DE IDENTITY

    private static void VerificarResultado(
        IdentityResult resultado,
        string operacion)
    {
        if (resultado.Succeeded)
        {
            return;
        }

        var errores = string.Join(
            "; ",
            resultado.Errors.Select(x => x.Description)
        );

        throw new InvalidOperationException(
            $"Error al {operacion}: {errores}"
        );
    }
}