using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.Encargados;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Proyecto_Final.ViewModels.Alumnos;

public sealed class CrearAlumno : FormularioAlumno
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una sección.")]
    [Display(Name = "Sección")]
    public int SeccionId { get; set; }

    [Display(Name = "Registrar encargado ahora")]
    public bool RegistrarEncargadoAhora { get; set; }

    [Display(Name = "Parentesco")]
    public TipoParentesco? ParentescoEncargado { get; set; }

    [ValidateNever]
    public CrearEncargado DatosEncargado { get; set; } = new();

    public List<OpcionSeleccion> Secciones { get; set; } = [];

    public List<int> CiclosInactivosConSecciones { get; set; } = [];

}
