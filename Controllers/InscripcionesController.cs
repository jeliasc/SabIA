using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionInscripciones;
using Proyecto_Final.ViewModels.Inscripciones;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Inscripciones.Ver)]
public class InscripcionesController(IInscripcionServicio s) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await s.ObtenerTodosAsync());

    [HttpGet]
    [Authorize(Policy = Permisos.Inscripciones.Crear)]
    public async Task<IActionResult> Crear()
    {
        var m = new CrearInscripcion();

        await s.PrepararAsync(m);

        return View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Inscripciones.Crear)]
    public async Task<IActionResult> Crear(CrearInscripcion m)
    {
        if (ModelState.IsValid)
        {
            var r = await s.CrearAsync(m);

            if (r.Exitoso)
            {
                TempData["Exito"] = r.Mensaje;
                return RedirectToAction(nameof(Index));
            }

            Err(r);
        }

        await s.PrepararAsync(m);

        return View(m);
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Inscripciones.Trasladar)]
    public async Task<IActionResult> Trasladar(int id)
    {
        var m = await s.ObtenerTrasladoAsync(id);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Inscripciones.Trasladar)]
    public async Task<IActionResult> Trasladar(TrasladarInscripcion m)
    {
        if (!ModelState.IsValid)
        {
            var rec = await s.ObtenerTrasladoAsync(m.Id);

            if (rec != null)
            {
                m.Alumno = rec.Alumno;
                m.Secciones = rec.Secciones;
            }

            return View(m);
        }

        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(uid))
        {
            return Challenge();
        }

        var r = await s.TrasladarAsync(m, uid);

        if (!r.Exitoso)
        {
            Err(r);

            var rec = await s.ObtenerTrasladoAsync(m.Id);

            if (rec != null)
            {
                m.Secciones = rec.Secciones;
            }

            return View(m);
        }

        TempData["Exito"] = r.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Inscripciones.CambiarEstado)]
    public async Task<IActionResult> Retirar(int id)
    {
        var r = await s.CambiarEstadoAsync(id, false);

        TempData[r.Exitoso ? "Exito" : "Error"] = r.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    private void Err(Servicios.Comunes.ResultadoOperacion r)
    {
        if (r.Errores.Count == 0)
        {
            ModelState.AddModelError("", r.Mensaje);
        }
        else
        {
            foreach (var e in r.Errores)
            {
                ModelState.AddModelError(e.Key, e.Value);
            }
        }
    }
}