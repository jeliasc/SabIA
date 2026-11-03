using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.ExploradorAcademico;

namespace Proyecto_Final.Controllers;

/// <summary>Consulta institucional; el ciclo procede exclusivamente del contexto de sesión.</summary>
[Authorize(Policy = Permisos.Dashboard.CoordinacionVer)]
[Authorize(Policy = Permisos.Secciones.Ver)]
public sealed class ExploradorAcademicoController(
    IExploradorAcademicoServicio explorador) : Controller
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
        return modelo is null ? NotFound() : View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Curso(int id)
    {
        var modelo = await explorador.ObtenerCursoAsync(id, User);
        return modelo is null ? NotFound() : View(modelo);
    }
}
