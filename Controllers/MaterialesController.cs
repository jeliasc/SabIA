using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionMateriales;
using Proyecto_Final.ViewModels.Materiales;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Materiales.Ver)]
public class MaterialesController(IMaterialServicio s) : Controller
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
    [Authorize(Policy = Permisos.Materiales.Crear)]
    public async Task<IActionResult> Crear()
    {
        var m = new CrearMaterial();

        await s.PrepararAsync(m);

        return View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Materiales.Crear)]
    public async Task<IActionResult> Crear(
        CrearMaterial m,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var r = await s.CrearAsync(m, ct);

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
    [Authorize(Policy = Permisos.Materiales.Editar)]
    public async Task<IActionResult> Editar(int id)
    {
        var m = await s.ObtenerEditarAsync(id);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Materiales.Editar)]
    public async Task<IActionResult> Editar(
        EditarMaterial m,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var r = await s.EditarAsync(m, ct);

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Materiales.Publicar)]
    public async Task<IActionResult> Publicar(int id)
    {
        var r = await s.PublicarAsync(id);

        TempData[r.Exitoso ? "Exito" : "Error"] = r.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Materiales.Publicar)]
    public async Task<IActionResult> Ocultar(int id)
    {
        var r = await s.OcultarAsync(id);

        TempData[r.Exitoso ? "Exito" : "Error"] = r.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Visualizar(int id, CancellationToken ct)
    {
        var archivo = await s.VisualizarAsync(id, ct);

        if (archivo is null)
            return NotFound();

        if (!EsVistaPreviaCompatible(archivo.Mime))
        {
            await archivo.Stream.DisposeAsync();
            return View("VistaPreviaNoDisponible", new VistaPreviaMaterial
            {
                Id = id,
                Nombre = archivo.Nombre,
                PuedeDescargar = archivo.PuedeDescargar
            });
        }

        Response.Headers.ContentDisposition =
            $"inline; filename*=UTF-8''{Uri.EscapeDataString(archivo.Nombre)}";
        return File(archivo.Stream, archivo.Mime, enableRangeProcessing: true);
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Materiales.Descargar)]
    public async Task<IActionResult> Descargar(int id, CancellationToken ct)
    {
        var archivo = await s.DescargarAsync(id, ct);

        return archivo is null
            ? NotFound()
            : File(archivo.Stream, archivo.Mime, archivo.Nombre);
    }

    private static bool EsVistaPreviaCompatible(string mime) =>
        mime.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("text/plain", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("text/csv", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/png", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/webp", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("audio/mpeg", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("audio/wav", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("video/mp4", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("video/webm", StringComparison.OrdinalIgnoreCase);

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
