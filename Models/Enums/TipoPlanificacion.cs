
namespace Proyecto_Final.Models;

public enum TipoPlanificacion
{
    [Display(Name = "Semanal")]
    Semanal = 1,
    [Display(Name = "Mensual")]
    Mensual = 2,
    [Display(Name = "Bimestral")]
    Bimestral = 3,
    [Display(Name = "Por unidad")]
    PorUnidad = 4,
    [Display(Name = "Anual")]
    Anual = 5
}
