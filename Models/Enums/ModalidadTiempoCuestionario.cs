namespace Proyecto_Final.Models;

public enum ModalidadTiempoCuestionario
{
    [Display(Name = "Sin límite")]
    SinLimite = 1,
    [Display(Name = "Tiempo general")]
    TiempoGeneral = 2,
    [Display(Name = "Tiempo por pregunta")]
    TiempoPorPregunta = 3
}
