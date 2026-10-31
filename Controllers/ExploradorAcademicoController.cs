using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.ExploradorAcademico;
using Proyecto_Final.Data;

namespace Proyecto_Final.Controllers;

/// <summary>Consulta institucional; el ciclo procede exclusivamente del contexto de sesión.</summary>
[Authorize(Policy = Permisos.Dashboard.CoordinacionVer)]
[Authorize(Policy = Permisos.Secciones.Ver)]
public sealed class ExploradorAcademicoController(
    IExploradorAcademicoServicio explorador,
    Contexto contexto) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var modelo = await explorador.ObtenerIndiceAsync();
        return modelo is null ? View("SinCiclo") : View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Grado(int id)
    {
        var modelo = await explorador.ObtenerGradoAsync(id);
        return modelo is null ? NotFound() : View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Seccion(int id)
    {
        var modelo = await explorador.ObtenerSeccionAsync(id, User);
        if (modelo is not null) return View(modelo);
        if (!await contexto.Secciones.AnyAsync(x => x.Id == id)) return NotFound();
        TempData["Error"] = "La sección solicitada no corresponde al ciclo escolar seleccionado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Curso(int id)
    {
        var modelo = await explorador.ObtenerCursoAsync(id, User);
        if (modelo is not null) return View(modelo);
        if (!await contexto.Asignaciones.AnyAsync(x => x.Id == id)) return NotFound();
        TempData["Error"] = "El curso solicitado no corresponde al ciclo escolar seleccionado o ya no está disponible en este contexto.";
        return RedirectToAction(nameof(Index));
    }
}
