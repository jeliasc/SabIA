namespace Proyecto_Final.Models;

public enum TipoPregunta
{
    [Display(Name = "Selección única")]
    SeleccionUnica = 1,
    [Display(Name = "Selección múltiple")]
    SeleccionMultiple = 2,
    [Display(Name = "Verdadero o falso")]
    VerdaderoFalso = 3,
    [Display(Name = "Texto corto")]
    TextoCorto = 4
}
