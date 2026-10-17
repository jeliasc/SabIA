using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public enum NivelAcademico
{
    [Display(Name = "Diversificado")]
    Diversificado,

    [Display(Name = "Técnico")]
    Tecnico,

    [Display(Name = "Profesorado")]
    Profesorado,
    [Display(Name = "Licenciatura")]
    Licenciatura,
    [Display(Name = "Maestría")]
    Maestria,
    [Display(Name = "Doctorado")]
    Doctorado
}
