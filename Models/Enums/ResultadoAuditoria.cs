namespace Proyecto_Final.Models;

public enum ResultadoAuditoria
{
    [Display(Name = "Exitoso")]
    Exitoso = 1,
    [Display(Name = "Rechazado")]
    Rechazado = 2,
    [Display(Name = "Fallido")]
    Fallido = 3
}
