
namespace Proyecto_Final.Models;

public enum TipoMaterial
{
    [Display(Name = "Documento")]
    Documento = 1,
    [Display(Name = "Presentación")]
    Presentacion = 2,
    [Display(Name = "Hoja de cálculo")]
    HojaCalculo = 3,
    [Display(Name = "Video")]
    Video = 4,
    [Display(Name = "Audio")]
    Audio = 5,
    [Display(Name = "Imagen")]
    Imagen = 6,
    [Display(Name = "Enlace")]
    Enlace = 7
}
