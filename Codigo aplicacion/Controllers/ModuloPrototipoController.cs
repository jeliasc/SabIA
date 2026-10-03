using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Servicios.Demostracion;

namespace Proyecto_Final.Controllers;

[Authorize]
public abstract class ModuloPrototipoController : Controller
{
    protected abstract string ClaveModulo { get; }

    [HttpGet]
    public IActionResult Index()
    {
        return View(
            "~/Views/Prototipo/Index.cshtml",
            DatosFicticiosSabia.ObtenerModulo(ClaveModulo)
        );
    }

    [HttpGet]
    public IActionResult Ver(int id)
    {
        return View(
            "~/Views/Prototipo/Ver.cshtml",
            DatosFicticiosSabia.ObtenerDetalle(ClaveModulo, id)
        );
    }

    [HttpGet]
    public IActionResult Crear()
    {
        var modulo = DatosFicticiosSabia.ObtenerModulo(ClaveModulo);

        if (!modulo.PermitirCrear)
        {
            TempData["Advertencia"] = "Este módulo no contempla creación directa en el prototipo.";
            return RedirectToAction(nameof(Index));
        }

        return View(
            "~/Views/Prototipo/Formulario.cshtml",
            DatosFicticiosSabia.ObtenerFormulario(ClaveModulo, false)
        );
    }

    [HttpGet]
    public IActionResult Editar(int id)
    {
        var modulo = DatosFicticiosSabia.ObtenerModulo(ClaveModulo);

        if (!modulo.PermitirEditar)
        {
            TempData["Advertencia"] = "Este módulo es de consulta en el prototipo actual.";
            return RedirectToAction(nameof(Index));
        }

        return View(
            "~/Views/Prototipo/Formulario.cshtml",
            DatosFicticiosSabia.ObtenerFormulario(ClaveModulo, true, id)
        );
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Guardar()
    {
        TempData["Advertencia"] =
            "Prototipo: los datos mostrados son ficticios y todavía no se guardan en la base de datos.";

        return RedirectToAction(nameof(Index));
    }
}
