using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionSecciones;
using Proyecto_Final.ViewModels.Secciones;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Secciones.Ver)]
public class SeccionesController(ISeccionServicio servicio, Contexto contexto) : Controller
{
    public async Task<IActionResult> Index() => View(await servicio.ObtenerTodosAsync());

    [HttpGet, Authorize(Policy = Permisos.Secciones.Crear)]
    public async Task<IActionResult> Crear() { await Catalogos(); return View(new CrearSeccion()); }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Secciones.Crear)]
    public async Task<IActionResult> Crear(CrearSeccion modelo)
    {
        if (!ModelState.IsValid) { await Catalogos(); return View(modelo); }
        var r = await servicio.CrearAsync(modelo);
        if (!r.Exitoso) { Agregar(r); await Catalogos(); return View(modelo); }
        TempData["Exito"] = r.Mensaje; return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Policy = Permisos.Secciones.Editar)]
    public async Task<IActionResult> Editar(int id)
    {
        var m = await servicio.ObtenerParaEditarAsync(id); if (m == null) return NotFound();
        await Catalogos(); return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Secciones.Editar)]
    public async Task<IActionResult> Editar(EditarSeccion modelo)
    {
        if (!ModelState.IsValid) { await Catalogos(); return View(modelo); }
        var r = await servicio.EditarAsync(modelo);
        if (!r.Exitoso) { Agregar(r); await Catalogos(); return View(modelo); }
        TempData["Exito"] = r.Mensaje; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Secciones.CambiarEstado)]
    public async Task<IActionResult> CambiarEstado(int id, bool activar)
    {
        var r = await servicio.CambiarEstadoAsync(id, activar);
        TempData[r.Exitoso ? "Exito" : "Error"] = r.Mensaje; return RedirectToAction(nameof(Index));
    }

    private async Task Catalogos()
    {
        var ciclos = await contexto.CiclosEscolares.AsNoTracking()
            .Where(x => x.Estado != Proyecto_Final.Models.EstadoCicloEscolar.Cerrado)
            .OrderByDescending(x => x.Anio)
            .Select(x => new { x.Id, Texto = x.Anio.ToString() }).ToListAsync();
        var grados = await contexto.Grados.AsNoTracking().OrderBy(x => x.Nivel).ThenBy(x => x.Orden)
            .Select(x => new { x.Id, Texto = x.Nombre + " · " + x.Carrera }).ToListAsync();
        ViewBag.Ciclos = new SelectList(ciclos, "Id", "Texto");
        ViewBag.Grados = new SelectList(grados, "Id", "Texto");
    }

    private void Agregar(ResultadoOperacion r)
    {
        if (r.Errores.Count == 0) ModelState.AddModelError(string.Empty, r.Mensaje);
        else foreach (var e in r.Errores) ModelState.AddModelError(e.Key, e.Value);
    }
}
