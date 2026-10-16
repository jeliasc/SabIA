namespace Proyecto_Final.Models;

public enum TipoOrigenHistorialCalificacion
{
    [Display(Name = "Manual")]
    Manual = 1,
    [Display(Name = "Entrega")]
    Entrega = 2,
    [Display(Name = "Intento de cuestionario")]
    IntentoCuestionario = 3,
    [Display(Name = "Respuesta de cuestionario")]
    RespuestaCuestionario = 4
}
