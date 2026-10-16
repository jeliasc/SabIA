namespace Proyecto_Final.Models;

public enum EstadoSolicitudCorreccion
{
    [Display(Name = "Pendiente")]
    Pendiente = 1,
    [Display(Name = "Aprobada")]
    Aprobada = 2,
    [Display(Name = "Rechazada")]
    Rechazada = 3,
    [Display(Name = "Aplicada")]
    Aplicada = 4
}
