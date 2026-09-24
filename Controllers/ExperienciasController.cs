using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Servicios.Demostracion;

namespace Proyecto_Final.Controllers;

[Authorize]
public class ExperienciasController : Controller
{
    [HttpGet]
    public IActionResult Docente()
    {
        return View(DatosFicticiosSabia.ObtenerExperienciaDocente());
    }

    [HttpGet]
    public IActionResult Alumno()
    {
        return View(DatosFicticiosSabia.ObtenerExperienciaAlumno());
    }
}
