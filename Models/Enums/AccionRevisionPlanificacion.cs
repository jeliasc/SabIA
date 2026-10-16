
namespace Proyecto_Final.Models;

public enum AccionRevisionPlanificacion
{
    [Display(Name = "Enviada a revisión")]
    EnviadaRevision = 1,
    [Display(Name = "Aprobada")]
    Aprobada = 2,
    [Display(Name = "Devuelta")]
    Devuelta = 3,
    [Display(Name = "Reabierta")]
    Reabierta = 4,
    [Display(Name = "Archivada")]
    Archivada = 5
}
