using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Servicios.GestionComentariosAcademicos;
using Proyecto_Final.ViewModels.ComentariosAcademicos;
using Proyecto_Final.Seguridad;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Politicas.ComentariosAcademicos)]
public sealed class ComentariosAcademicosController(IComentarioAcademicoServicio servicio) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Conversacion(int tareaId, int alumnoId)
    {
        var modelo = await servicio.ObtenerAsync(tareaId, alumnoId);
        return modelo is null ? NotFound() : View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearComentarioAcademico modelo, CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearAsync(modelo, cancellationToken);
        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Conversacion), new { modelo.TareaId, modelo.AlumnoId });
    }

    [HttpGet]
    public async Task<IActionResult> Archivo(int id, CancellationToken cancellationToken)
    {
        var archivo = await servicio.AbrirArchivoAsync(id, cancellationToken);
        return archivo is null ? NotFound() : File(archivo.Value.Stream, archivo.Value.Mime, archivo.Value.Nombre);
    }

    [HttpGet]
    public async Task<IActionResult> VisualizarArchivo(int id, CancellationToken cancellationToken)
    {
        var archivo = await servicio.AbrirArchivoAsync(id, cancellationToken);
        if (archivo is null) return NotFound();
        if (!EsVistaPreviaCompatible(archivo.Value.Mime)) return BadRequest();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] = "sandbox";
        Response.Headers.ContentDisposition = $"inline; filename*=UTF-8''{Uri.EscapeDataString(archivo.Value.Nombre)}";
        return File(archivo.Value.Stream, archivo.Value.Mime, enableRangeProcessing: true);
    }

    private static bool EsVistaPreviaCompatible(string mime) =>
        mime.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/png", StringComparison.OrdinalIgnoreCase) ||
        mime.Equals("image/webp", StringComparison.OrdinalIgnoreCase);
}
