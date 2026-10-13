using System.Security.Claims;
using Proyecto_Final.ViewModels.Prototipo;

namespace Proyecto_Final.Servicios.Dashboard;

public interface IDashboardServicio
{
    Task<ResultadoDashboard> ObtenerAsync(
        ClaimsPrincipal usuarioActual,
        string? contexto = null
    );
}
