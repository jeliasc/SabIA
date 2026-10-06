using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public enum NivelAcademico
{
    Diversificado,

    [Display(Name = "Técnico")]
    Tecnico,

    Profesorado,
    Licenciatura,
    Maestria,
    Doctorado
}