namespace Proyecto_Final.Models;

public enum CriterioCalificacionIntentos
{
    [Display(Name = "Mejor nota")]
    MejorNota = 1,
    [Display(Name = "Último intento")]
    UltimoIntento = 2,
    [Display(Name = "Promedio")]
    Promedio = 3
}
