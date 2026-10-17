namespace Proyecto_Final.Models;

public enum EstadoTarea
{
    [Display(Name = "Borrador")]
    Borrador = 1,
    [Display(Name = "Publicada")]
    Publicada = 2,
    [Display(Name = "Cerrada")]
    Cerrada = 3,
    [Display(Name = "Archivada")]
    Archivada = 4
}
