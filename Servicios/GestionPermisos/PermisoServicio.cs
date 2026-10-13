using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.ViewModels.PermisosSistema;
using System.Security.Claims;

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

    // OBTENER TODOS LOS PERMISOS
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

    // CREAR PERMISO
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
                Mensaje =
                    "Ya existe un permiso con ese código."
            };
        }

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync();

        try
        {
            var nuevoPermiso = new PermisoSistema
            {
                Codigo = codigo,
                Modulo = modulo,
                Descripcion = descripcion,
                Activo = true
            };

            contexto.PermisosSistema.Add(nuevoPermiso);

            await contexto.SaveChangesAsync();

            var resultadoAsignacion =
                await AsignarPermisoSuperusuarioAsync(
                    codigo
                );

            if (!resultadoAsignacion.Exitoso)
            {
                await transaccion.RollbackAsync();

                return resultadoAsignacion;
            }

            await transaccion.CommitAsync();

            return new ResultadoPermiso
            {
                Exitoso = true,
                Mensaje =
                    "El permiso fue creado correctamente."
            };
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    // OBTENER PERMISO PARA EDITAR
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

    // EDITAR PERMISO
    public async Task<ResultadoPermiso> EditarAsync(
        EditarPermiso modelo)
    {
        await using var transaccion =
            await contexto.Database.BeginTransactionAsync();

        try
        {
            var permiso =
                await contexto.PermisosSistema
                    .FirstOrDefaultAsync(
                        permiso => permiso.Id == modelo.Id
                    );

            if (permiso == null)
            {
                await transaccion.RollbackAsync();

                return new ResultadoPermiso
                {
                    Exitoso = false,
                    Mensaje =
                        "El permiso no fue encontrado."
                };
            }

            // El código NO se modifica.
            permiso.Modulo =
                modelo.Modulo.Trim();

            permiso.Descripcion =
                modelo.Descripcion.Trim();

            await contexto.SaveChangesAsync();

            await transaccion.CommitAsync();

            return new ResultadoPermiso
            {
                Exitoso = true,
                Mensaje =
                    "El permiso fue actualizado correctamente."
            };
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    // DESACTIVAR PERMISO
    public async Task<ResultadoPermiso> DesactivarAsync(
        int id)
    {
        await using var transaccion =
            await contexto.Database.BeginTransactionAsync();

        try
        {
            var permiso =
                await contexto.PermisosSistema
                    .FirstOrDefaultAsync(
                        permiso => permiso.Id == id
                    );

            if (permiso == null)
            {
                await transaccion.RollbackAsync();

                return new ResultadoPermiso
                {
                    Exitoso = false,
                    Mensaje =
                        "El permiso no fue encontrado."
                };
            }

            if (!permiso.Activo)
            {
                await transaccion.RollbackAsync();

                return new ResultadoPermiso
                {
                    Exitoso = false,
                    Mensaje =
                        "El permiso ya se encuentra desactivado."
                };
            }

            permiso.Activo = false;

            await contexto.SaveChangesAsync();

            var resultadoRetiro =
                await RetirarPermisoTodosLosRolesAsync(
                    permiso.Codigo
                );

            if (!resultadoRetiro.Exitoso)
            {
                await transaccion.RollbackAsync();

                return resultadoRetiro;
            }

            await transaccion.CommitAsync();

            return new ResultadoPermiso
            {
                Exitoso = true,
                Mensaje =
                    "El permiso fue desactivado correctamente."
            };
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    // ACTIVAR PERMISO
    public async Task<ResultadoPermiso> ActivarAsync(
        int id)
    {
        await using var transaccion =
            await contexto.Database.BeginTransactionAsync();

        try
        {
            var permiso =
                await contexto.PermisosSistema
                    .FirstOrDefaultAsync(
                        permiso => permiso.Id == id
                    );

            if (permiso == null)
            {
                await transaccion.RollbackAsync();

                return new ResultadoPermiso
                {
                    Exitoso = false,
                    Mensaje =
                        "El permiso no fue encontrado."
                };
            }

            if (permiso.Activo)
            {
                await transaccion.RollbackAsync();

                return new ResultadoPermiso
                {
                    Exitoso = false,
                    Mensaje =
                        "El permiso ya se encuentra activo."
                };
            }

            permiso.Activo = true;

            await contexto.SaveChangesAsync();

            var resultadoAsignacion =
                await AsignarPermisoSuperusuarioAsync(
                    permiso.Codigo
                );

            if (!resultadoAsignacion.Exitoso)
            {
                await transaccion.RollbackAsync();

                return resultadoAsignacion;
            }

            await transaccion.CommitAsync();

            return new ResultadoPermiso
            {
                Exitoso = true,
                Mensaje =
                    "El permiso fue activado correctamente."
            };
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    // ASIGNAR PERMISO AL SUPERUSUARIO
    private async Task<ResultadoPermiso>
        AsignarPermisoSuperusuarioAsync(
            string codigoPermiso)
    {
        var rolSuperusuario =
            await administradorRoles.FindByNameAsync(
                Roles.Superusuario
            );

        if (rolSuperusuario == null)
        {
            return new ResultadoPermiso
            {
                Exitoso = false,
                Mensaje =
                    "No se encontró el rol Superusuario."
            };
        }

        var claims =
            await administradorRoles
                .GetClaimsAsync(rolSuperusuario);

        var yaAsignado =
            claims.Any(claim =>
                claim.Type == TiposClaims.Permiso &&
                claim.Value == codigoPermiso
            );

        if (yaAsignado)
        {
            return new ResultadoPermiso
            {
                Exitoso = true,
                Mensaje = string.Empty
            };
        }

        var resultado =
            await administradorRoles.AddClaimAsync(
                rolSuperusuario,
                new Claim(
                    TiposClaims.Permiso,
                    codigoPermiso
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

            return new ResultadoPermiso
            {
                Exitoso = false,
                Mensaje =
                    $"No fue posible asignar el permiso al Superusuario: {errores}"
            };
        }

        return new ResultadoPermiso
        {
            Exitoso = true,
            Mensaje = string.Empty
        };
    }

    // RETIRAR PERMISO DE TODOS LOS ROLES
    private async Task<ResultadoPermiso>
        RetirarPermisoTodosLosRolesAsync(
            string codigoPermiso)
    {
        var roles =
            await administradorRoles.Roles
                .ToListAsync();

        foreach (var rol in roles)
        {
            var claims =
                await administradorRoles
                    .GetClaimsAsync(rol);

            var claimPermiso =
                claims.FirstOrDefault(claim =>
                    claim.Type == TiposClaims.Permiso &&
                    claim.Value == codigoPermiso
                );

            if (claimPermiso == null)
            {
                continue;
            }

            var resultado =
                await administradorRoles.RemoveClaimAsync(
                    rol,
                    claimPermiso
                );

            if (!resultado.Succeeded)
            {
                var errores = string.Join(
                    "; ",
                    resultado.Errors.Select(
                        error => error.Description
                    )
                );

                return new ResultadoPermiso
                {
                    Exitoso = false,
                    Mensaje =
                        $"No fue posible retirar el permiso del rol {rol.Name}: {errores}"
                };
            }
        }

        return new ResultadoPermiso
        {
            Exitoso = true,
            Mensaje = string.Empty
        };
    }
}