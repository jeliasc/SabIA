
namespace Proyecto_Final.Models;

public enum TipoParentesco
{
    [Display(Name = "Madre")]
    Madre,
    [Display(Name = "Padre")]
    Padre,
    [Display(Name = "Abuelo")]
    Abuelo,
    [Display(Name = "Abuela")]
    Abuela,
    [Display(Name = "Tío")]
    Tio,
    [Display(Name = "Tía")]
    Tia,
    [Display(Name = "Hermano")]
    Hermano,
    [Display(Name = "Hermana")]
    Hermana,
    [Display(Name = "Tutor legal")]
    TutorLegal,
    [Display(Name = "Otro")]
    Otro
}
