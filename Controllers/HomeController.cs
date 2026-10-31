using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Servicios.Dashboard;
using Proyecto_Final.ViewModels;

namespace Proyecto_Final.Controllers;

public class HomeController(IDashboardServicio dashboardServicio) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? contexto)
    {
        var resultado = await dashboardServicio.ObtenerAsync(User, contexto);

        if (!resultado.Permitido)
        {
            return Forbid();
        }
        ViewBag.TituloDashboard = resultado.Titulo;
        ViewBag.DescripcionDashboard = resultado.Descripcion;
        return View(resultado.Modelo);
    }

    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [AcceptVerbs("GET", "POST")]
    public IActionResult Estado(int id)
    {
        Response.StatusCode = id;
        return View("EstadoHttp", id);
    }

    [AllowAnonymous]
    public IActionResult NoEncontrado()
    {
        if (SolicitudHtml(Request))
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("EstadoHttp", 404);
        }

        return NotFound();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });

    private static bool SolicitudHtml(HttpRequest solicitud) =>
        !string.Equals(solicitud.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) &&
        solicitud.Headers.Accept.Any(valor =>
            valor?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
}
