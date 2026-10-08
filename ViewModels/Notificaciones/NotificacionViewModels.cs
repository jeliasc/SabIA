namespace Proyecto_Final.ViewModels.Notificaciones;

public sealed class NotificacionLista
{
    public string Titulo { get; set; } = string.Empty;

    public string Mensaje { get; set; } = string.Empty;

    public string Icono { get; set; } = "bi bi-bell";

    public string Tipo { get; set; } = "info";

    public string? Controlador { get; set; }

    public string? Accion { get; set; }

    public int? Id { get; set; }
}