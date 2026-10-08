using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionCursos;
using Proyecto_Final.ViewModels.Cursos;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Cursos.Ver)]
public class CursosController(ICursoServicio servicio, Contexto contexto) : Controller
{
    public async Task<IActionResult> Index() => View(await servicio.ObtenerTodosAsync());

    [HttpGet, Authorize(Policy = Permisos.Cursos.Crear)]
    public async Task<IActionResult> Crear() { await Catalogos(); return View(new CrearCurso()); }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Cursos.Crear)]
    public async Task<IActionResult> Crear(CrearCurso modelo)
    {
        if (!ModelState.IsValid) { await Catalogos(); return View(modelo); }
        var r = await servicio.CrearAsync(modelo);
        if (!r.Exitoso) { Agregar(r); await Catalogos(); return View(modelo); }
        TempData["Exito"] = r.Mensaje; return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Policy = Permisos.Cursos.Editar)]
    public async Task<IActionResult> Editar(int id)
    {
        var m = await servicio.ObtenerParaEditarAsync(id); if (m == null) return NotFound();
        await Catalogos(); return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Cursos.Editar)]
    public async Task<IActionResult> Editar(EditarCurso modelo)
    {
        if (!ModelState.IsValid) { await Catalogos(); return View(modelo); }
        var r = await servicio.EditarAsync(modelo);
        if (!r.Exitoso) { Agregar(r); await Catalogos(); return View(modelo); }
        TempData["Exito"] = r.Mensaje; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Cursos.CambiarEstado)]
    public async Task<IActionResult> CambiarEstado(int id, bool activar)
    {
        var r = await servicio.CambiarEstadoAsync(id, activar);
        TempData[r.Exitoso ? "Exito" : "Error"] = r.Mensaje; return RedirectToAction(nameof(Index));
    }

    private async Task Catalogos()
    {
        var grados = await contexto.Grados.AsNoTracking().OrderBy(x => x.Nivel).ThenBy(x => x.Orden)
            .Select(x => new { x.Id, Texto = x.Nombre + " · " + x.Carrera }).ToListAsync();
        ViewBag.Grados = new SelectList(grados, "Id", "Texto");
    }

    private void Agregar(ResultadoOperacion r)
    {
        if (r.Errores.Count == 0) ModelState.AddModelError(string.Empty, r.Mensaje);
        else foreach (var e in r.Errores) ModelState.AddModelError(e.Key, e.Value);
    }
}
