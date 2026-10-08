using System.Diagnostics;
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
}
