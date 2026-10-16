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

    public DateTime? Fecha { get; set; }

    // Solo las actividades publicadas sin primera visualización alimentan la
    // insignia de la campana; las alertas agrupadas no.
    public bool EsNovedad { get; set; }
}

public sealed class ResumenNotificaciones
{
    public bool PuedeVer { get; set; }
    public List<NotificacionLista> Notificaciones { get; set; } = [];
    public int NovedadesNoVisualizadas { get; set; }
}
