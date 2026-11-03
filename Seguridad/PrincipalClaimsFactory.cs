using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Proyecto_Final.Models;

namespace Proyecto_Final.Seguridad;

/// <summary>Construye el principal con únicamente roles activos y sus permisos vigentes.</summary>
public sealed class PrincipalClaimsFactory(
    UserManager<Usuario> usuarios,
    RoleManager<Rol> roles,
    IOptions<IdentityOptions> opciones)
    : UserClaimsPrincipalFactory<Usuario, Rol>(usuarios, roles, opciones)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Usuario usuario)
    {
        var identidad = await base.GenerateClaimsAsync(usuario);

        // Los claims de rol quedan materializados dentro de la cookie. Los
        // reconstruimos para que un rol inactivo nunca sobreviva a una sesión.
        foreach (var claim in identidad.Claims
                     .Where(x => x.Type == ClaimTypes.Role || x.Type == TiposClaims.Permiso)
                     .ToList())
        {
            identidad.RemoveClaim(claim);
        }

        foreach (var nombreRol in await UserManager.GetRolesAsync(usuario))
        {
            var rol = await RoleManager.FindByNameAsync(nombreRol);
            if (rol is not { Activo: true })
                continue;

            identidad.AddClaim(new Claim(ClaimTypes.Role, nombreRol));
            foreach (var permiso in (await RoleManager.GetClaimsAsync(rol))
                         .Where(x => x.Type == TiposClaims.Permiso))
            {
                identidad.AddClaim(permiso);
            }
        }

        // Se conservan los permisos que hubieran sido asignados directamente
        // al usuario; los permisos administrados por rol ya fueron filtrados.
        foreach (var claim in (await UserManager.GetClaimsAsync(usuario))
                     .Where(x => x.Type == TiposClaims.Permiso))
        {
            identidad.AddClaim(claim);
        }

        return identidad;
    }
}
