
namespace Proyecto_Final.Models;

public enum EstadoPlanificacion
{
    [Display(Name = "Borrador")]
    Borrador = 1,
    [Display(Name = "En revisión")]
    EnRevision = 2,
    [Display(Name = "Aprobada")]
    Aprobada = 3,
    [Display(Name = "Devuelta")]
    Devuelta = 4,
    [Display(Name = "Archivada")]
    Archivada = 5
}
