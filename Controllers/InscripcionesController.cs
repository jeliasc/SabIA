
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionInscripciones;
using Proyecto_Final.ViewModels.Inscripciones;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Inscripciones.Ver)]
public class InscripcionesController(IInscripcionServicio s, IDataProtectionProvider proteccionDatos) : Controller
{
    private readonly IDataProtector protectorRevision = proteccionDatos.CreateProtector("SabIA.Traslados.Revision.v1");

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
    public async Task<IActionResult> RevisarTraslado(TrasladarInscripcion m)
    {
        if (!m.FechaEfectiva.HasValue)
            ModelState.AddModelError(nameof(m.FechaEfectiva), "Indique la fecha efectiva académica del traslado.");

        if (!ModelState.IsValid)
        {
            var rec = await s.ObtenerTrasladoAsync(m.Id);

            if (rec != null)
            {
                m.Alumno = rec.Alumno;
                m.Secciones = rec.Secciones;
            }

            return rec is null ? NotFound() : View("Trasladar", m);
        }

        RevisionTrasladoInscripcion? revision;

        try
        {
            revision = await s.EvaluarTrasladoAsync(m);
        }
        catch (ValidacionTrasladoException ex)
        {
            ModelState.AddModelError("", ex.Message);

            var rec = await s.ObtenerTrasladoAsync(m.Id);

            if (rec != null)
            {
                m.Alumno = rec.Alumno;
                m.Secciones = rec.Secciones;
            }

            return rec is null ? NotFound() : View("Trasladar", m);
        }

        if (revision is null)
        {
            ModelState.AddModelError(
                "",
                "No fue posible evaluar el traslado. Verifique los datos registrados.");

            var rec = await s.ObtenerTrasladoAsync(m.Id);

            if (rec != null)
            {
                m.Alumno = rec.Alumno;
                m.Secciones = rec.Secciones;
            }

            return rec is null ? NotFound() : View("Trasladar", m);
        }

        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(uid)) return Challenge();

        revision.TokenRevision = protectorRevision.CreateProtector(uid).Protect(revision.HuellaImpacto);
        return View("ConfirmarTraslado", revision);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Inscripciones.Trasladar)]
    public async Task<IActionResult> ConfirmarTraslado(RevisionTrasladoInscripcion m)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(uid))
        {
            return Challenge();
        }

        string huellaRevisada;
        try
        {
            huellaRevisada = protectorRevision.CreateProtector(uid).Unprotect(m.TokenRevision);
        }
        catch (CryptographicException)
        {
            TempData["Error"] = "La revisión del traslado no es válida o pertenece a otra sesión. Solicite una nueva revisión.";
            return RedirectToAction(nameof(Trasladar), new { id = m.Id });
        }

        var r = await s.ConfirmarTrasladoAsync(m, uid, huellaRevisada);

        if (!r.Exitoso)
        {
            Err(r);

            RevisionTrasladoInscripcion? revision;

            try
            {
                revision = await s.EvaluarTrasladoAsync(m);
            }
            catch (ValidacionTrasladoException)
            {
                revision = null;
            }

            if (revision != null)
            {
                TempData["Error"] = "Las condiciones académicas cambiaron. Revise nuevamente el impacto antes de confirmar.";
                return RedirectToAction(nameof(Trasladar), new { id = m.Id });
            }

            return RedirectToAction(nameof(Trasladar), new { id = m.Id });
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
