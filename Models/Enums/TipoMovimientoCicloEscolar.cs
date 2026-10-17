namespace Proyecto_Final.Models;

public enum TipoMovimientoCicloEscolar
{
    [Display(Name = "Activación")]
    Activacion = 1,
    [Display(Name = "Cierre")]
    Cierre = 2,
    [Display(Name = "Reapertura")]
    Reapertura = 3
}
