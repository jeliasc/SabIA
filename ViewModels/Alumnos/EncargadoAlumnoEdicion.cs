using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.Alumnos;

public sealed class EncargadoAlumnoEdicion
{
    public int EncargadoId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public TipoParentesco Parentesco { get; set; }
}
