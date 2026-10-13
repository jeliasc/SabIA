using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.ViewModels.Ciclos;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Ciclos.Ver)]
public class CiclosController(ICicloServicio servicio) : Controller
{
    public async Task<IActionResult> Index() => View(await servicio.ObtenerTodosAsync());

    [HttpGet, Authorize(Policy = Permisos.Ciclos.Crear)]
    public IActionResult Crear() => View(new CrearCiclo { Anio = DateTime.Today.Year + 1 });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Ciclos.Crear)]
    public async Task<IActionResult> Crear(CrearCiclo modelo)
    {
        if (!ModelState.IsValid) return View(modelo);
        var r = await servicio.CrearAsync(modelo);
        if (!r.Exitoso) { Agregar(r); return View(modelo); }
        TempData["Exito"] = r.Mensaje; return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Policy = Permisos.Ciclos.Editar)]
    public async Task<IActionResult> Editar(int id)
    {
        var m = await servicio.ObtenerParaEditarAsync(id);
        return m == null ? NotFound() : View(m);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Ciclos.Editar)]
    public async Task<IActionResult> Editar(EditarCiclo modelo)
    {
        if (!ModelState.IsValid) return View(modelo);
        var r = await servicio.EditarAsync(modelo);
        if (!r.Exitoso) { Agregar(r); return View(modelo); }
        TempData["Exito"] = r.Mensaje; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Ciclos.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        var r = await servicio.ActivarAsync(id);
        TempData[r.Exitoso ? "Exito" : "Error"] = r.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Policy = Permisos.Ciclos.Cerrar)]
    public async Task<IActionResult> RevisarCierre(int id)
    {
        var modelo = await servicio.ObtenerRevisionCierreAsync(id);
        return modelo == null ? NotFound() : View(modelo);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Ciclos.Cerrar)]
    public async Task<IActionResult> Cerrar(int id)
    {
        var resultado = await servicio.CerrarAsync(id);
        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        return resultado.Exitoso
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(RevisarCierre), new { id });
    }

    [HttpGet, Authorize(Policy = Permisos.Ciclos.Reabrir)]
    public async Task<IActionResult> Reabrir(int id)
    {
        var modelo = await servicio.ObtenerParaReabrirAsync(id);
        return modelo == null ? NotFound() : View(modelo);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Permisos.Ciclos.Reabrir)]
    public async Task<IActionResult> Reabrir(ReabrirCiclo modelo)
    {
        if (!ModelState.IsValid) return View(modelo);
        var resultado = await servicio.ReabrirAsync(modelo);
        if (!resultado.Exitoso)
        {
            Agregar(resultado);
            var ciclo = await servicio.ObtenerParaReabrirAsync(modelo.Id);
            if (ciclo == null) return NotFound();
            ciclo.Justificacion = modelo.Justificacion;
            return View(ciclo);
        }

        TempData["Exito"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Historial(int id)
    {
        var ciclo = (await servicio.ObtenerTodosAsync()).SingleOrDefault(x => x.Id == id);
        if (ciclo == null) return NotFound();
        ViewData["Anio"] = ciclo.Anio;
        return View(await servicio.ObtenerHistorialAsync(id));
    }

    private void Agregar(ResultadoOperacion r)
    {
        if (r.Errores.Count == 0) ModelState.AddModelError(string.Empty, r.Mensaje);
        else foreach (var e in r.Errores) ModelState.AddModelError(e.Key, e.Value);
    }
}
