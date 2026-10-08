using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionNotificaciones;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Notificaciones.Ver)]
public class NotificacionesController(INotificacionServicio s) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await s.ObtenerAsync());
}