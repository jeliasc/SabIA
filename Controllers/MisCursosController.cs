using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Servicios.NavegacionAcademica;

namespace Proyecto_Final.Controllers;

[Authorize]
public sealed class MisCursosController(INavegacionAcademicaServicio navegacion) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var modelo = await navegacion.ObtenerMisCursosAsync();

        return modelo is null
            ? View("SinCiclo")
            : View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Espacio(int asignacionId)
    {
        var modelo = await navegacion.ObtenerEspacioAsync(asignacionId);
        return modelo is null ? NotFound() : View(modelo);
    }
}
