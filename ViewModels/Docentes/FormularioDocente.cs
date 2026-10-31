using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Personas;

namespace Proyecto_Final.ViewModels.Docentes;

public abstract class FormularioDocente
    : FormularioPersona
{
    [Required(ErrorMessage = "El carnet es obligatorio.")]
    [StringLength(30, ErrorMessage = "El carnet no puede superar los 30 caracteres.")]
    [Display(Name = "Carnet")]
    public string Carnet { get; set; } = string.Empty;

    [StringLength(13, ErrorMessage = "El NIT no puede superar los 13 caracteres.")]
    [RegularExpression(
        @"^\d{1,13}$",
        ErrorMessage = "El NIT debe contener únicamente números y un máximo de 13 dígitos."
    )]
    [Display(Name = "NIT")]
    public string? Nit { get; set; }

    [Display(Name = "Nivel académico")]
    public NivelAcademico? NivelAcademico { get; set; }

    [StringLength(200, ErrorMessage = "El título no puede superar los 200 caracteres.")]
    [Display(Name = "Título")]
    public string? Titulo { get; set; }

    [StringLength(150, ErrorMessage = "La institución otorgante no puede superar los 150 caracteres.")]
    [Display(Name = "Institución otorgante")]
    public string? InstitucionOtorgante { get; set; }
}
