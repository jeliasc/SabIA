using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Servicios.GestionNotificaciones;

namespace Proyecto_Final.ViewComponents;

public sealed class NotificacionesViewComponent(INotificacionServicio notificaciones) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync() =>
        View(await notificaciones.ObtenerResumenAsync());
}
