using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            servicios.GetRequiredService<RoleManager<Rol>>();

        var administradorUsuarios =
            servicios.GetRequiredService<UserManager<Usuario>>();

        var contexto =
            servicios.GetRequiredService<Contexto>();

        string[] roles =
        [
            Roles.Superusuario,
            Roles.Administrador,
            Roles.Docente,
            Roles.Alumno,
            Roles.Usuario
        ];

        foreach (var nombreRol in roles)
        {
            if (await administradorRoles.RoleExistsAsync(nombreRol))
            {
                continue;
            }

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

        foreach (var codigoPermiso in Permisos.ObtenerTodos())
        {
            var existePermiso =
                await contexto.PermisosSistema
                    .AnyAsync(permiso =>
                        permiso.Codigo == codigoPermiso);

            if (existePermiso)
            {
                continue;
            }

            var partes = codigoPermiso.Split(
                '.',
                2,
                StringSplitOptions.RemoveEmptyEntries
            );

            contexto.PermisosSistema.Add(
                new PermisoSistema
                {
                    Codigo = codigoPermiso,
                    Modulo = partes.Length > 0
                        ? partes[0]
                        : "Sistema",
                    Descripcion =
                        ObtenerDescripcionPermiso(codigoPermiso),
                    Activo = true
                }
            );
        }

        await contexto.SaveChangesAsync();

        await AsignarTodosLosPermisosAlSuperusuarioAsync(
            contexto,
            administradorRoles
        );

        await AsignarDashboardCoordinacionARolesExistentesAsync(
            administradorRoles
        );

        await AsignarPermisosBaseAsync(
            administradorRoles,
            Roles.Docente,
            [
                Permisos.Alumnos.Ver,
                Permisos.Unidades.Ver,
                Permisos.Unidades.Crear,
                Permisos.Unidades.Editar,
                Permisos.Unidades.Publicar,
                Permisos.Materiales.Ver,
                Permisos.Materiales.Crear,
                Permisos.Materiales.Editar,
                Permisos.Materiales.Publicar,
                Permisos.Materiales.Descargar,
                Permisos.Planificaciones.Ver,
                Permisos.Planificaciones.Crear,
                Permisos.Planificaciones.Editar,
                Permisos.Planificaciones.EnviarRevision,
                Permisos.Documentos.Ver,
                Permisos.Documentos.Crear,
                Permisos.Documentos.Descargar,
                Permisos.Tareas.Ver,
                Permisos.Tareas.Crear,
                Permisos.Tareas.Editar,
                Permisos.Tareas.Publicar,
                Permisos.Tareas.Cerrar,
                Permisos.Entregas.Ver,
                Permisos.Entregas.Calificar,
                Permisos.Entregas.Reabrir,
                Permisos.Cuestionarios.Ver,
                Permisos.Cuestionarios.Crear,
                Permisos.Cuestionarios.Editar,
                Permisos.ContenidoIA.Generar,
                Permisos.Notificaciones.Ver,
                Permisos.Calificaciones.Ver,
                Permisos.Calificaciones.Configurar,
                Permisos.Calificaciones.Registrar,
                Permisos.Calificaciones.Cerrar,
                Permisos.Calificaciones.SolicitarCorreccion
            ]
        );

        await AsignarPermisosBaseAsync(
            administradorRoles,
            Roles.Alumno,
            [
                Permisos.Alumnos.Ver,
                Permisos.Unidades.Ver,
                Permisos.Materiales.Ver,
                Permisos.Materiales.Descargar,
                Permisos.Tareas.Ver,
                Permisos.Entregas.Ver,
                Permisos.Entregas.Entregar,
                Permisos.Cuestionarios.Ver,
                Permisos.Cuestionarios.Resolver,
                Permisos.Notificaciones.Ver,
                Permisos.Calificaciones.Ver
            ]
        );

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

    private static async Task AsignarTodosLosPermisosAlSuperusuarioAsync(
        Contexto contexto,
        RoleManager<Rol> administradorRoles)
    {
        var rol =
            await administradorRoles.FindByNameAsync(Roles.Superusuario)
            ?? throw new InvalidOperationException(
                "No se encontró el rol Superusuario."
            );

        var claims =
            await administradorRoles.GetClaimsAsync(rol);

        var permisosActivos =
            await contexto.PermisosSistema
                .AsNoTracking()
                .Where(permiso => permiso.Activo)
                .Select(permiso => permiso.Codigo)
                .ToListAsync();

        foreach (var permiso in permisosActivos)
        {
            if (claims.Any(claim =>
                claim.Type == TiposClaims.Permiso &&
                claim.Value == permiso))
            {
                continue;
            }

            var resultado =
                await administradorRoles.AddClaimAsync(
                    rol,
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

    private static async Task AsignarPermisosBaseAsync(
        RoleManager<Rol> administradorRoles,
        string nombreRol,
        IEnumerable<string> permisos)
    {
        var rol =
            await administradorRoles.FindByNameAsync(nombreRol);

        if (rol == null)
        {
            return;
        }

        var claims =
            await administradorRoles.GetClaimsAsync(rol);

        foreach (var permiso in permisos)
        {
            if (claims.Any(claim =>
                claim.Type == TiposClaims.Permiso &&
                claim.Value == permiso))
            {
                continue;
            }

            var resultado =
                await administradorRoles.AddClaimAsync(
                    rol,
                    new Claim(
                        TiposClaims.Permiso,
                        permiso
                    )
                );

            VerificarResultado(
                resultado,
                $"asignar el permiso {permiso} al rol {nombreRol}"
            );
        }
    }

    private static async Task AsignarDashboardCoordinacionARolesExistentesAsync(
        RoleManager<Rol> administradorRoles)
    {
        var roles = await administradorRoles.Roles
            .Where(rol => rol.Activo)
            .ToListAsync();

        foreach (var rol in roles)
        {
            var claims = await administradorRoles.GetClaimsAsync(rol);

            var esCoordinacion = claims.Any(claim =>
                claim.Type == TiposClaims.Permiso &&
                claim.Value == Permisos.Planificaciones.Revisar);

            if (!esCoordinacion)
            {
                continue;
            }

            foreach (var permiso in new[]
            {
                Permisos.Dashboard.CoordinacionVer,
                Permisos.Calificaciones.AprobarCorreccion,
                Permisos.Ciclos.Cerrar,
                Permisos.Ciclos.Reabrir
            })
            {
                if (claims.Any(claim =>
                    claim.Type == TiposClaims.Permiso &&
                    claim.Value == permiso))
                {
                    continue;
                }

                var resultado = await administradorRoles.AddClaimAsync(
                    rol,
                    new Claim(TiposClaims.Permiso, permiso));

                VerificarResultado(
                    resultado,
                    $"asignar el permiso {permiso} al rol {rol.Name}");
            }
        }
    }

    private static string ObtenerDescripcionPermiso(
        string codigoPermiso)
    {
        return codigoPermiso switch
        {
            Permisos.Usuarios.Ver => "Consultar usuarios.",
            Permisos.Usuarios.Crear => "Crear nuevos usuarios.",
            Permisos.Usuarios.Editar => "Modificar información de usuarios.",
            Permisos.Usuarios.CambiarEstado => "Activar o desactivar usuarios.",
            Permisos.Usuarios.RestablecerContrasena => "Restablecer la contraseña de usuarios.",

            Permisos.Roles.Ver => "Consultar roles.",
            Permisos.Roles.Crear => "Crear nuevos roles.",
            Permisos.Roles.Editar => "Modificar información de roles.",
            Permisos.Roles.CambiarEstado => "Activar o desactivar roles.",
            Permisos.Roles.AsignarPermisos => "Asignar o retirar permisos de los roles.",

            Permisos.PermisosSistema.Ver => "Consultar permisos del sistema.",
            Permisos.PermisosSistema.Crear => "Crear nuevos permisos.",
            Permisos.PermisosSistema.Editar => "Modificar información de permisos.",
            Permisos.PermisosSistema.CambiarEstado => "Activar o desactivar permisos.",

            Permisos.Ciclos.Cerrar => "Cerrar formalmente ciclos escolares.",
            Permisos.Ciclos.Reabrir => "Reabrir excepcionalmente ciclos escolares cerrados.",

            _ => CrearDescripcionGenerica(codigoPermiso)
        };
    }

    private static string CrearDescripcionGenerica(
        string codigoPermiso)
    {
        var partes = codigoPermiso.Split(
            '.',
            2,
            StringSplitOptions.RemoveEmptyEntries
        );

        if (partes.Length != 2)
        {
            return "Permiso del sistema.";
        }

        var accion = partes[1] switch
        {
            "Ver" => "Consultar",
            "Crear" => "Crear",
            "Editar" => "Modificar",
            "CambiarEstado" => "Activar o desactivar",
            "Activar" => "Activar",
            "Trasladar" => "Trasladar",
            "Publicar" => "Publicar",
            "Descargar" => "Descargar",
            "EnviarRevision" => "Enviar a revisión",
            "Revisar" => "Revisar",
            "Cerrar" => "Cerrar",
            "Entregar" => "Realizar entregas en",
            "Calificar" => "Calificar entregas en",
            "Reabrir" => "Reabrir entregas en",
            "Resolver" => "Resolver",
            "Generar" => "Generar contenido en",
            "Configurar" => "Configurar",
            "Registrar" => "Registrar",
            "SolicitarCorreccion" => "Solicitar correcciones en",
            "AprobarCorreccion" => "Aprobar correcciones en",
            _ => partes[1]
        };

        return $"{accion} {partes[0]}.";
    }

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
            resultado.Errors.Select(error => error.Description)
        );

        throw new InvalidOperationException(
            $"No fue posible {operacion}: {errores}"
        );
    }
}
