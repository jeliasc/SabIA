using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Proyecto_Final.Seguridad;

public class ProveedorPoliticasPermisos
    : DefaultAuthorizationPolicyProvider
{
    public ProveedorPoliticasPermisos(
        IOptions<AuthorizationOptions> opciones)
        : base(opciones)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(
        string nombrePolitica)
    {
        // Primero respetamos cualquier política
        // registrada explícitamente en ASP.NET.
        var politicaExistente =
            await base.GetPolicyAsync(nombrePolitica);

        if (politicaExistente != null)
        {
            return politicaExistente;
        }

        if (string.IsNullOrWhiteSpace(nombrePolitica))
        {
            return null;
        }

        // Una política dinámica de SabIA exige un claim
        // "Permiso" cuyo valor sea exactamente el nombre
        // solicitado por [Authorize(Policy = "...")].
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(
                TiposClaims.Permiso,
                nombrePolitica
            )
            .Build();
    }
}