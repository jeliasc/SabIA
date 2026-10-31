using System.Security.Claims;

namespace Proyecto_Final.Seguridad;

/// <summary>
/// Regla de acceso del espacio institucional de coordinación.
/// Un permiso de consulta académica, o incluso el permiso del dashboard por
/// sí solo, no convierte un perfil académico en operador institucional.
/// </summary>
public static class AutorizacionDashboardCoordinacion
{
    public static bool PuedeVer(ClaimsPrincipal usuario)
    {
        if (usuario.Identity?.IsAuthenticated != true ||
            !usuario.HasClaim(TiposClaims.Permiso, Permisos.Dashboard.CoordinacionVer))
        {
            return false;
        }

        return usuario.IsInRole(Roles.Superusuario) ||
            usuario.IsInRole(Roles.Administrador) ||
            // La coordinación se identifica en el modelo vigente por la
            // capacidad de revisar planificaciones. El seeder usa esta misma
            // capacidad para otorgar el permiso del dashboard a ese perfil.
            usuario.HasClaim(TiposClaims.Permiso, Permisos.Planificaciones.Revisar);
    }
}
