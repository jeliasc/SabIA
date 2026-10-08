using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionEntregas;
using Proyecto_Final.ViewModels.Entregas;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Entregas.Ver)]
public class EntregasController(IEntregaServicio s) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await s.ObtenerTodasAsync());

    [HttpGet]
    public async Task<IActionResult> Historial()
    {
        ViewData["SoloLectura"] = true;
        return View("Index", await s.ObtenerHistorialAsync());
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Entregas.Entregar)]
    public async Task<IActionResult> Entregar(int id)
    {
        var m = await s.ObtenerParaEntregarAsync(id);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Entregas.Entregar)]
    public async Task<IActionResult> Entregar(
        EntregarTarea m,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(m);
        }

        var r = await s.EntregarAsync(m, ct);

        if (!r.Exitoso)
        {
            Err(r);

            var rec = await s.ObtenerParaEntregarAsync(m.Id);

            if (rec != null)
            {
                m.Tarea = rec.Tarea;
                m.Instrucciones = rec.Instrucciones;
                m.FechaLimite = rec.FechaLimite;
                m.ArchivosApoyo = rec.ArchivosApoyo;
            }

            return View(m);
        }

        TempData["Exito"] = r.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Entregas.Calificar)]
    public async Task<IActionResult> Calificar(int id)
    {
        var m = await s.ObtenerParaCalificarAsync(id);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Entregas.Calificar)]
    public async Task<IActionResult> Calificar(CalificarEntrega m)
    {
        if (!ModelState.IsValid)
        {
            return View(m);
        }

        var r = await s.CalificarAsync(m);

        if (!r.Exitoso)
        {
            Err(r);
            return View(m);
        }

        TempData["Exito"] = r.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Entregas.Reabrir)]
    public async Task<IActionResult> Reabrir(int id)
    {
        var modelo = await s.ObtenerParaReabrirAsync(id);
        return modelo == null ? NotFound() : View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Entregas.Reabrir)]
    public async Task<IActionResult> Reabrir(ReabrirEntrega m)
    {
        if (!ModelState.IsValid)
        {
            return View(m);
        }

        var resultado = await s.ReabrirAsync(m);

        if (!resultado.Exitoso)
        {
            Err(resultado);
            var recuperado = await s.ObtenerParaReabrirAsync(m.Id);
            if (recuperado != null)
            {
                m.Alumno = recuperado.Alumno;
                m.Tarea = recuperado.Tarea;
                m.Curso = recuperado.Curso;
                m.NumeroEnvioActual = recuperado.NumeroEnvioActual;
                m.EstadoActual = recuperado.EstadoActual;
            }

            return View(m);
        }

        TempData["Exito"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ArchivoTarea(int id)
    {
        var a = await s.DescargarArchivoTareaAsync(id);

        return a == null
            ? NotFound()
            : File(a.Value.Stream, a.Value.Mime, a.Value.Nombre);
    }

    [HttpGet]
    public async Task<IActionResult> Archivo(int id)
    {
        var a = await s.DescargarArchivoAsync(id);

        return a == null
            ? NotFound()
            : File(a.Value.Stream, a.Value.Mime, a.Value.Nombre);
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
