using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public enum EstadoSesionDiagnostico
{
    [Display(Name = "Activa")]
    Activa = 1,
    [Display(Name = "Finalizada")]
    Finalizada = 2,
    [Display(Name = "Vencida")]
    Vencida = 3,
    [Display(Name = "Cancelada")]
    Cancelada = 4
}
