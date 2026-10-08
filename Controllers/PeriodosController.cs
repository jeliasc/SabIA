using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionPeriodos;
using Proyecto_Final.ViewModels.Periodos;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Periodos.Ver)]
public class PeriodosController(IPeriodoServicio servicio, Contexto contexto) : Controller
{
    public async Task<IActionResult> Index() => View(await servicio.ObtenerTodosAsync());

    [HttpGet, Authorize(Policy = Permisos.Periodos.Crear)]
    public async Task<IActionResult> Crear()
    {
        await Catalogos(); return View(new CrearPeriodo());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Periodos.Crear)]
    public async Task<IActionResult> Crear(CrearPeriodo modelo)
    {
        if (!ModelState.IsValid) { await Catalogos(); return View(modelo); }
        var r = await servicio.CrearAsync(modelo);
        if (!r.Exitoso) { Agregar(r); await Catalogos(); return View(modelo); }
        TempData["Exito"] = r.Mensaje; return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Policy = Permisos.Periodos.Editar)]
    public async Task<IActionResult> Editar(int id)
    {
        var m = await servicio.ObtenerParaEditarAsync(id);
        if (m == null) return NotFound();
        await Catalogos(); return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Periodos.Editar)]
    public async Task<IActionResult> Editar(EditarPeriodo modelo)
    {
        if (!ModelState.IsValid) { await Catalogos(); return View(modelo); }
        var r = await servicio.EditarAsync(modelo);
        if (!r.Exitoso) { Agregar(r); await Catalogos(); return View(modelo); }
        TempData["Exito"] = r.Mensaje; return RedirectToAction(nameof(Index));
    }

    private async Task Catalogos()
    {
        var ciclos = await contexto.CiclosEscolares.AsNoTracking().OrderByDescending(x => x.Anio)
            .Select(x => new { x.Id, Texto = x.Anio.ToString() }).ToListAsync();
        ViewBag.Ciclos = new SelectList(ciclos, "Id", "Texto");
    }

    private void Agregar(ResultadoOperacion r)
    {
        if (r.Errores.Count == 0) ModelState.AddModelError(string.Empty, r.Mensaje);
        else foreach (var e in r.Errores) ModelState.AddModelError(e.Key, e.Value);
    }
}
