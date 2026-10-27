using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionEntregas;
using Proyecto_Final.Servicios.GestionNotificaciones;
using Proyecto_Final.ViewModels.Entregas;
using Proyecto_Final.Servicios.SeguridadAcademica;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Entregas.Ver)]
public class EntregasController(
    IEntregaServicio s,
    INotificacionServicio notificaciones,
    IAccesoAcademicoServicio acceso) : Controller
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
        if (m == null)
        {
            TempData["Error"] =
                "No puede realizar esta entrega. La actividad puede estar cerrada, fuera de plazo o no corresponder a su inscripción activa.";
            return RedirectToAction(nameof(Index));
        }
        await notificaciones.RegistrarVisualizacionActividadAsync(m.TareaId);
        return View(m);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var modelo = await s.ObtenerDetalleAsync(id);
        return modelo == null ? NotFound() : View(modelo);
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
            await RecuperarContextoCalificacionAsync(m);
            return View(m);
        }

        var r = await s.CalificarAsync(m);

        if (!r.Exitoso)
        {
            Err(r);
            await RecuperarContextoCalificacionAsync(m);
            return View(m);
        }

        TempData["Exito"] = r.Mensaje;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Entregas.Calificar)]
    public async Task<IActionResult> CalificarCero(int id)
    {
        var m = await s.ObtenerParaCalificarCeroAsync(id);
        return m is null ? NotFound() : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Entregas.Calificar)]
    public async Task<IActionResult> CalificarCero(CalificarCeroEntrega m)
    {
        if (!ModelState.IsValid)
            return View(m);

        var r = await s.CalificarCeroAsync(m);
        if (!r.Exitoso)
        {
            Err(r);
            var recuperado = await s.ObtenerParaCalificarCeroAsync(m.Id);
            return recuperado is null ? NotFound() : View(recuperado);
        }

        TempData["Exito"] = r.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Entregas.Calificar)]
    public async Task<IActionResult> RectificarCero(int id)
    {
        var modelo = await s.ObtenerParaRectificarCeroAsync(id);
        return modelo is null ? NotFound() : View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Entregas.Calificar)]
    public async Task<IActionResult> RectificarCero(RectificarCeroAdministrativo m)
    {
        if (!ModelState.IsValid)
        {
            await RehidratarRectificacionAsync(m);
            return View(m);
        }
        var resultado = await s.RectificarCeroAsync(m);
        if (!resultado.Exitoso)
        {
            Err(resultado);
            await RehidratarRectificacionAsync(m);
            return View(m);
        }
        TempData["Exito"] = resultado.Mensaje;
        return RedirectToAction(nameof(Detalle), new { id = m.Id });
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Entregas.Reabrir)]
    public async Task<IActionResult> Reabrir(int id)
    {
        var docenteId = await acceso.ObtenerDocenteIdAsync();

        if (!docenteId.HasValue)
        {
            TempData["Error"] =
                "Acceso denegado. Para reabrir una entrega debe tener un perfil de docente autorizado.";

            return RedirectToAction(nameof(Index));
        }

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

    [HttpGet]
    public async Task<IActionResult> VisualizarArchivo(int id)
    {
        var a = await s.VisualizarArchivoAsync(id);
        if (a == null)
        {
            return NotFound();
        }

        Response.Headers.ContentDisposition =
            $"inline; filename*=UTF-8''{Uri.EscapeDataString(a.Value.Nombre)}";
        return File(a.Value.Stream, a.Value.Mime, enableRangeProcessing: true);
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

    private async Task RecuperarContextoCalificacionAsync(CalificarEntrega modelo)
    {
        var recuperado = await s.ObtenerParaCalificarAsync(modelo.Id);
        if (recuperado == null)
        {
            return;
        }

        modelo.Alumno = recuperado.Alumno;
        modelo.Tarea = recuperado.Tarea;
        modelo.PunteoMaximo = recuperado.PunteoMaximo;
        modelo.ComentarioAlumno = recuperado.ComentarioAlumno;
        modelo.FechaEntrega = recuperado.FechaEntrega;
        modelo.TareaId = recuperado.TareaId;
        modelo.AlumnoId = recuperado.AlumnoId;
        modelo.InscripcionId = recuperado.InscripcionId;
        modelo.Intentos = recuperado.Intentos;
    }

    private async Task RehidratarRectificacionAsync(RectificarCeroAdministrativo modelo)
    {
        var original = await s.ObtenerParaRectificarCeroAsync(modelo.Id);
        if (original is null) return;
        modelo.Alumno = original.Alumno;
        modelo.Tarea = original.Tarea;
        modelo.Curso = original.Curso;
        modelo.Origen = original.Origen;
    }
}
