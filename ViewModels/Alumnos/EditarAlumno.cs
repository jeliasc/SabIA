using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Alumnos;

public sealed class EditarAlumno : FormularioAlumno
{
    [Required]
    public int Id { get; set; }

    public List<EncargadoAlumnoEdicion> Encargados { get; set; } = [];
    public int? NuevoEncargadoId { get; set; }
    public TipoParentesco? NuevoParentesco { get; set; }
    public List<OpcionSeleccion> OpcionesEncargados { get; set; } = [];
}
