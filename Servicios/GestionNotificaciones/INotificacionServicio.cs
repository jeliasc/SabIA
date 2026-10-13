using Proyecto_Final.ViewModels.Notificaciones;

namespace Proyecto_Final.Servicios.GestionNotificaciones;

public interface INotificacionServicio
{
    Task<List<NotificacionLista>> ObtenerAsync();
}