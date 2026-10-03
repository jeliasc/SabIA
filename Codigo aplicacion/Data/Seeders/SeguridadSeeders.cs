using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Data.Seeders;

public static class SeguridadSeeders
{
    public static async Task InicializarAsync(
        IServiceProvider servicios,
        IConfiguration configuracion)
    {

        var administradorRoles =
            servicios.GetRequiredService<RoleManager<Rol>>();

        var administradorUsuarios =
            servicios.GetRequiredService<UserManager<Usuario>>();

        var contexto =
            servicios.GetRequiredService<Contexto>();

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
                    new Rol
                    {
                        Name = nombreRol,
                        Activo = true
                    }
                );

                VerificarResultado(
                    resultado,
                    $"crear el rol {nombreRol}"
                );
            }
        }

        // PERMISOS BASE DEL SISTEMA

        foreach (var codigoPermiso in Permisos.ObtenerTodos())
        {
            var existePermiso =
                await contexto.PermisosSistema
                    .AnyAsync(x => x.Codigo == codigoPermiso);

            if (existePermiso)
            {
                continue;
            }

            var partes = codigoPermiso.Split(
                '.',
                2,
                StringSplitOptions.RemoveEmptyEntries
            );

            var modulo = partes.Length > 0
                ? partes[0]
                : "Sistema";

            contexto.PermisosSistema.Add(
                new PermisoSistema
                {
                    Codigo = codigoPermiso,
                    Modulo = modulo,
                    Descripcion = ObtenerDescripcionPermiso(codigoPermiso),
                    Activo = true
                }
            );
        }

        await contexto.SaveChangesAsync();

        // PERMISOS DEL SUPERUSUARIO

        var rolSuperusuario =
            await administradorRoles.FindByNameAsync(Roles.Superusuario)
            ?? throw new InvalidOperationException(
                "No se encontró el rol Superusuario."
            );

        var permisosActuales =
    await administradorRoles.GetClaimsAsync(rolSuperusuario);

        var permisosActivos =
            await contexto.PermisosSistema
                .AsNoTracking()
                .Where(permiso => permiso.Activo)
                .Select(permiso => permiso.Codigo)
                .ToListAsync();

        foreach (var permiso in permisosActivos)
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

    private static string ObtenerDescripcionPermiso(
        string codigoPermiso)
    {
        return codigoPermiso switch
        {
            Permisos.Usuarios.Ver =>
                "Consultar usuarios.",

            Permisos.Usuarios.Crear =>
                "Crear nuevos usuarios.",

            Permisos.Usuarios.Editar =>
                "Modificar información de usuarios.",

            Permisos.Usuarios.CambiarEstado =>
                "Activar o desactivar usuarios.",

            Permisos.Usuarios.RestablecerContrasena =>
                "Restablecer la contraseña de usuarios.",

            Permisos.Roles.Ver =>
                "Consultar roles.",

            Permisos.Roles.Crear =>
                "Crear nuevos roles.",

            Permisos.Roles.Editar =>
                "Modificar información de roles.",

            Permisos.Roles.CambiarEstado =>
                "Activar o desactivar roles.",

            Permisos.Roles.AsignarPermisos =>
                "Asignar o retirar permisos de los roles.",

            Permisos.PermisosSistema.Ver =>
                "Consultar permisos del sistema.",

            Permisos.PermisosSistema.Crear =>
                "Crear nuevos permisos.",

            Permisos.PermisosSistema.Editar =>
                "Modificar información de permisos.",

            Permisos.PermisosSistema.CambiarEstado =>
                "Activar o desactivar permisos.",

            _ => "Permiso del sistema."
        };
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