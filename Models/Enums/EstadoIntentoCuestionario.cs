namespace Proyecto_Final.Models;

public enum EstadoIntentoCuestionario
{
    [Display(Name = "En curso")]
    EnCurso = 1,
    [Display(Name = "Finalizado")]
    Finalizado = 2,
    [Display(Name = "Expirado")]
    Expirado = 3,
    [Display(Name = "Anulado")]
    Anulado = 4
}
