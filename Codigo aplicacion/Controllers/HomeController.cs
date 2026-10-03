using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Servicios.Demostracion;
using Proyecto_Final.ViewModels;

namespace Proyecto_Final.Controllers;

public class HomeController : Controller
{
    // MOSTRAR DASHBOARD DE COORDINACIÓN
    public IActionResult Index()
    {
        return View(DatosFicticiosSabia.ObtenerDashboard());
    }

    // MOSTRAR POLÍTICA DE PRIVACIDAD
    public IActionResult Privacy()
    {
        return View();
    }

    // MOSTRAR PÁGINA DE ERROR
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true
    )]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            }
        );
    }
}
