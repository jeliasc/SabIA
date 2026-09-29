using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.ViewModels.PermisosSistema;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;

namespace Proyecto_Final.Servicios.GestionPermisos;

public class PermisoServicio : IPermisoServicio
{
    private readonly Contexto contexto;
    private readonly RoleManager<Rol> administradorRoles;

    public PermisoServicio(
        Contexto contexto,
        RoleManager<Rol> administradorRoles)
    {
        this.contexto = contexto;
        this.administradorRoles = administradorRoles;
    }

    public async Task<PermisosSistemaIndex> ObtenerTodosAsync()
    {
        var permisos =
            await contexto.PermisosSistema
                .AsNoTracking()
                .OrderBy(permiso => permiso.Modulo)
                .ThenBy(permiso => permiso.Codigo)
                .Select(permiso => new PermisoSistemaLista
                {
                    Id = permiso.Id,
                    Codigo = permiso.Codigo,
                    Modulo = permiso.Modulo,
                    Descripcion = permiso.Descripcion,
                    Activo = permiso.Activo
                })
                .ToListAsync();

        return new PermisosSistemaIndex
        {
            Permisos = permisos
        };
    }

    public async Task<ResultadoPermiso> CrearAsync(
    CrearPermiso modelo)
    {
        var codigo = modelo.Codigo.Trim();
        var modulo = modelo.Modulo.Trim();
        var descripcion = modelo.Descripcion.Trim();

        var existe =
            await contexto.PermisosSistema
                .AnyAsync(permiso =>
                    permiso.Codigo == codigo);

        if (existe)
        {
            return new ResultadoPermiso
            {
                Exitoso = false,
                Mensaje = "Ya existe un permiso con ese código."
            };
        }

        var nuevoPermiso = new PermisoSistema
        {
            Codigo = codigo,
            Modulo = modulo,
            Descripcion = descripcion,
            Activo = true
        };

        contexto.PermisosSistema.Add(nuevoPermiso);

        await contexto.SaveChangesAsync();

        var rolSuperusuario =
            await administradorRoles.FindByNameAsync(
                Roles.Superusuario
            );

        if (rolSuperusuario == null)
        {
            throw new InvalidOperationException(
                "No se encontró el rol Superusuario."
            );
        }

        var claims =
            await administradorRoles.GetClaimsAsync(
                rolSuperusuario
            );

        var permisoAsignado = claims.Any(claim =>
            claim.Type == TiposClaims.Permiso &&
            claim.Value == codigo
        );

        if (!permisoAsignado)
        {
            var resultado =
                await administradorRoles.AddClaimAsync(
                    rolSuperusuario,
                    new Claim(
                        TiposClaims.Permiso,
                        codigo
                    )
                );

            if (!resultado.Succeeded)
            {
                var errores = string.Join(
                    "; ",
                    resultado.Errors.Select(
                        error => error.Description
                    )
                );

                throw new InvalidOperationException(
                    $"No se pudo asignar el permiso al Superusuario: {errores}"
                );
            }
        }

        return new ResultadoPermiso
        {
            Exitoso = true,
            Mensaje = "El permiso fue creado correctamente."
        };
    }

    public async Task<EditarPermiso?>
    ObtenerParaEditarAsync(int id)
    {
        var permiso =
            await contexto.PermisosSistema
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    permiso => permiso.Id == id
                );

        if (permiso == null)
        {
            return null;
        }

        return new EditarPermiso
        {
            Id = permiso.Id,
            Codigo = permiso.Codigo,
            Modulo = permiso.Modulo,
            Descripcion = permiso.Descripcion
        };
    }

    public async Task<ResultadoPermiso> EditarAsync(
    EditarPermiso modelo)
    {
        var permiso =
            await contexto.PermisosSistema
                .FirstOrDefaultAsync(
                    permiso => permiso.Id == modelo.Id
                );

        if (permiso == null)
        {
            return new ResultadoPermiso
            {
                Exitoso = false,
                Mensaje = "El permiso no fue encontrado."
            };
        }

        // El código NO se modifica.
        permiso.Modulo =
            modelo.Modulo.Trim();

        permiso.Descripcion =
            modelo.Descripcion.Trim();

        await contexto.SaveChangesAsync();

        return new ResultadoPermiso
        {
            Exitoso = true,
            Mensaje =
                "El permiso fue actualizado correctamente."
        };
    }

    public async Task<ResultadoPermiso> DesactivarAsync(
    int id)
    {
        var permiso =
            await contexto.PermisosSistema
                .FirstOrDefaultAsync(
                    permiso => permiso.Id == id
                );

        if (permiso == null)
        {
            return new ResultadoPermiso
            {
                Exitoso = false,
                Mensaje = "El permiso no fue encontrado."
            };
        }

        if (!permiso.Activo)
        {
            return new ResultadoPermiso
            {
                Exitoso = false,
                Mensaje = "El permiso ya se encuentra desactivado."
            };
        }

        permiso.Activo = false;

        await contexto.SaveChangesAsync();

        // Retirar el permiso de todos los roles.
        var roles =
            await administradorRoles.Roles
                .ToListAsync();

        foreach (var rol in roles)
        {
            var claims =
                await administradorRoles
                    .GetClaimsAsync(rol);

            var claimPermiso =
                claims.FirstOrDefault(
                    claim =>
                        claim.Type == TiposClaims.Permiso &&
                        claim.Value == permiso.Codigo
                );

            if (claimPermiso == null)
            {
                continue;
            }

            var resultado =
                await administradorRoles
                    .RemoveClaimAsync(
                        rol,
                        claimPermiso
                    );

            if (!resultado.Succeeded)
            {
                return new ResultadoPermiso
                {
                    Exitoso = false,
                    Mensaje =
                        "El permiso fue desactivado, pero ocurrió un error al retirarlo de uno de los roles."
                };
            }
        }

        return new ResultadoPermiso
        {
            Exitoso = true,
            Mensaje =
                "El permiso fue desactivado correctamente."
        };
    }

    public async Task<ResultadoPermiso> ActivarAsync(
    int id)
    {
        var permiso =
            await contexto.PermisosSistema
                .FirstOrDefaultAsync(
                    permiso => permiso.Id == id
                );

        if (permiso == null)
        {
            return new ResultadoPermiso
            {
                Exitoso = false,
                Mensaje = "El permiso no fue encontrado."
            };
        }

        if (permiso.Activo)
        {
            return new ResultadoPermiso
            {
                Exitoso = false,
                Mensaje = "El permiso ya se encuentra activo."
            };
        }

        permiso.Activo = true;

        await contexto.SaveChangesAsync();

        // Todo permiso activo pertenece al Superusuario.
        var rolSuperusuario =
            await administradorRoles
                .FindByNameAsync(Roles.Superusuario);

        if (rolSuperusuario == null)
        {
            return new ResultadoPermiso
            {
                Exitoso = false,
                Mensaje =
                    "El permiso fue activado, pero no se encontró el rol Superusuario."
            };
        }

        var claims =
            await administradorRoles
                .GetClaimsAsync(rolSuperusuario);

        var yaAsignado =
            claims.Any(
                claim =>
                    claim.Type == TiposClaims.Permiso &&
                    claim.Value == permiso.Codigo
            );

        if (!yaAsignado)
        {
            var resultado =
                await administradorRoles
                    .AddClaimAsync(
                        rolSuperusuario,
                        new Claim(
                            TiposClaims.Permiso,
                            permiso.Codigo
                        )
                    );

            if (!resultado.Succeeded)
            {
                return new ResultadoPermiso
                {
                    Exitoso = false,
                    Mensaje =
                        "El permiso fue activado, pero no pudo asignarse al rol Superusuario."
                };
            }
        }

        return new ResultadoPermiso
        {
            Exitoso = true,
            Mensaje =
                "El permiso fue activado correctamente."
        };
    }
}