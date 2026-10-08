using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionDocumentos;
using Proyecto_Final.ViewModels.Documentos;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Documentos.Ver)]
public class DocumentosController(IDocumentoServicio s) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await s.ObtenerTodosAsync());

    [HttpGet]
    [Authorize(Policy = Permisos.Documentos.Crear)]
    public IActionResult Crear() =>
        View(new CrearDocumento());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Documentos.Crear)]
    public async Task<IActionResult> Crear(
        CrearDocumento m,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(m);
        }

        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (uid == null)
        {
            return Challenge();
        }

        var r = await s.CrearAsync(m, uid, ct);

        if (!r.Exitoso)
        {
            ModelState.AddModelError("", r.Mensaje);
            return View(m);
        }

        TempData["Exito"] = r.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Documentos.Descargar)]
    public async Task<IActionResult> Descargar(int id)
    {
        var a = await s.AbrirAsync(id);

        return a == null
            ? NotFound()
            : File(a.Value.Stream, a.Value.Mime, a.Value.Nombre);
    }
}