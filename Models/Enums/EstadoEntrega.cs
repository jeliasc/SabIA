namespace Proyecto_Final.Models;

public enum EstadoEntrega
{
    [Display(Name = "Enviada")]
    Enviada = 1,
    [Display(Name = "Calificada")]
    Calificada = 2,
    [Display(Name = "Devuelta")]
    Devuelta = 3,
    [Display(Name = "Pendiente")]
    Pendiente = 4,
    [Display(Name = "Tardía")]
    Tardia = 5,
    [Display(Name = "Cero administrativo rectificado")]
    CeroAdministrativoRectificado = 6
}
