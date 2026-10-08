using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionCuestionarios;
using Proyecto_Final.ViewModels.Cuestionarios;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Cuestionarios.Ver)]
public class CuestionariosController(ICuestionarioServicio s) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await s.ObtenerTodosAsync());

    [HttpGet]
    public async Task<IActionResult> Historial()
    {
        ViewData["SoloLectura"] = true;
        return View("Index", await s.ObtenerHistorialAsync());
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Cuestionarios.Crear)]
    public async Task<IActionResult> Configurar(int tareaId)
    {
        var m = await s.ObtenerConfiguracionAsync(tareaId);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Cuestionarios.Crear)]
    public async Task<IActionResult> Configurar(ConfigurarCuestionario m)
    {
        if (!ModelState.IsValid)
        {
            return View(m);
        }

        var r = await s.GuardarConfiguracionAsync(m);

        if (!r.Exitoso || r.Datos == 0)
        {
            ModelState.AddModelError("", r.Mensaje);
            return View(m);
        }

        TempData["Exito"] = r.Mensaje;

        return RedirectToAction(
            nameof(AgregarPregunta),
            new { cuestionarioId = r.Datos }
        );
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Cuestionarios.Editar)]
    public async Task<IActionResult> AgregarPregunta(int cuestionarioId)
    {
        var m = await s.PrepararPreguntaAsync(cuestionarioId);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Cuestionarios.Editar)]
    public async Task<IActionResult> AgregarPregunta(CrearPregunta m)
    {
        if (!ModelState.IsValid)
        {
            return View(m);
        }

        var r = await s.AgregarPreguntaAsync(m);

        if (!r.Exitoso)
        {
            foreach (var e in r.Errores)
            {
                ModelState.AddModelError(e.Key, e.Value);
            }

            if (r.Errores.Count == 0)
            {
                ModelState.AddModelError("", r.Mensaje);
            }

            return View(m);
        }

        TempData["Exito"] = r.Mensaje;

        return RedirectToAction(
            nameof(AgregarPregunta),
            new { cuestionarioId = m.CuestionarioId }
        );
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Cuestionarios.Resolver)]
    public async Task<IActionResult> Resolver(int id)
    {
        var m = await s.ObtenerParaResolverAsync(id);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Cuestionarios.Resolver)]
    public async Task<IActionResult> Resolver(ResolverCuestionario m)
    {
        var r = await s.ResolverAsync(m);

        if (!r.Exitoso || r.Datos == null)
        {
            TempData["Error"] = r.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        return View("Resultado", r.Datos);
    }
}
