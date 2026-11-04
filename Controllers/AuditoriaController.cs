using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Proyecto_Final.Data;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Auditoria;
using Proyecto_Final.ViewModels.Auditoria;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Politicas.AuditoriaSuperusuario)]
public sealed class AuditoriaController(
    IAuditoriaConsultaServicio consulta,
    IDiagnosticoServicio diagnostico,
    Contexto contexto) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AuditoriaFiltro filtro) =>
        View(new AuditoriaCentroViewModel
        {
            Historial = await consulta.ObtenerPaginaAsync(filtro),
            Diagnostico = await diagnostico.ObtenerPanelAsync(),
            Solicitud = new SolicitudDiagnostico()
        });

    [HttpGet]
    public async Task<IActionResult> Detalle(long id)
    {
        var detalle = await consulta.ObtenerDetalleAsync(id);
        return detalle == null ? NotFound() : View(detalle);
    }

    [HttpGet]
    public async Task<IActionResult> DetalleBaseDatos(long id)
    {
        var detalle = await contexto.RegistrosAuditoriaBaseDatos.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id);
        return detalle == null ? NotFound() : View(detalle);
    }

    [HttpGet]
    public async Task<IActionResult> BuscarUsuarios(string termino)
    {
        termino = (termino ?? string.Empty).Trim();
        if (termino.Length < 2) return Json(Array.Empty<object>());
        var usuarios = await contexto.Users.AsNoTracking().Where(x =>
            (x.UserName != null && x.UserName.Contains(termino)) ||
            (x.Email != null && x.Email.Contains(termino)) ||
            x.PrimerNombre.Contains(termino) || x.PrimerApellido.Contains(termino))
            .OrderBy(x => x.PrimerNombre).ThenBy(x => x.PrimerApellido).Take(20)
            .Select(x => new { id = x.Id, texto = x.PrimerNombre + " " + x.PrimerApellido + " (" + (x.UserName ?? x.Email ?? x.Id) + ")" })
            .ToListAsync();
        return Json(usuarios);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivarDiagnostico(SolicitudDiagnostico solicitud)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revise los datos del diagnóstico.";
            return RedirectToAction(nameof(Index), new { pestana = "diagnostico" });
        }
        var resultado = await diagnostico.ActivarAsync(solicitud, UsuarioActualId());
        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index), new { pestana = "diagnostico" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FinalizarDiagnostico(Guid id)
    {
        var resultado = await diagnostico.FinalizarAsync(id, UsuarioActualId());
        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index), new { pestana = "diagnostico" });
    }

    private string UsuarioActualId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
}
