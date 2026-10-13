
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Final.Models;

public class Encargado
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombres { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Apellidos { get; set; } = string.Empty;

    [StringLength(13)]
    public string? Dpi { get; set; }

    [Required]
    [StringLength(15)]
    public string Telefono { get; set; } = string.Empty;

    [StringLength(15)]
    public string? TelefonoAlterno { get; set; }

    [EmailAddress]
    [StringLength(150)]
    public string? Correo { get; set; }

    [StringLength(200)]
    public string? Direccion { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public ICollection<AlumnoEncargado> AlumnoEncargados { get; set; } = new List<AlumnoEncargado>();
}
