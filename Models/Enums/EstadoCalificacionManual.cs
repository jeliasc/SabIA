namespace Proyecto_Final.Models;

public enum EstadoCalificacionManual
{
    [Display(Name = "Pendiente")]
    Pendiente = 1,
    [Display(Name = "Calificada")]
    Calificada = 2,
    [Display(Name = "No presentada")]
    NoPresentada = 3,
    [Display(Name = "Justificada")]
    Justificada = 4,
    [Display(Name = "Exenta")]
    Exenta = 5
}
