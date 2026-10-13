using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Auditoria;
using Proyecto_Final.ViewModels.Auditoria;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Politicas.AuditoriaSuperusuario)]
public sealed class AuditoriaController(IAuditoriaConsultaServicio consulta) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AuditoriaFiltro filtro) =>
        View(await consulta.ObtenerPaginaAsync(filtro));

    [HttpGet]
    public async Task<IActionResult> Detalle(long id)
    {
        var detalle = await consulta.ObtenerDetalleAsync(id);
        return detalle == null ? NotFound() : View(detalle);
    }
}
