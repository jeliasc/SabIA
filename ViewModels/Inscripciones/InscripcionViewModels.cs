using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Inscripciones;

public class FormularioInscripcion
{
    [Range(1, int.MaxValue)]
    public int AlumnoId { get; set; }

    [Range(1, int.MaxValue)]
    public int SeccionId { get; set; }

    public DateOnly Fecha { get; set; } =
        DateOnly.FromDateTime(DateTime.Today);

    public List<OpcionSeleccion> Alumnos { get; set; } = [];

    public List<OpcionSeleccion> Secciones { get; set; } = [];
}

public sealed class CrearInscripcion : FormularioInscripcion
{
}

public sealed class InscripcionLista
{
    public int Id { get; set; }

    public string Alumno { get; set; } = string.Empty;

    public string Seccion { get; set; } = string.Empty;

    public string Carrera { get; set; } = string.Empty;

    public int Ciclo { get; set; }

    public DateOnly Fecha { get; set; }

    public EstadoInscripcion Estado { get; set; }
}

public sealed class TrasladarInscripcion
{
    public int Id { get; set; }

    public string Alumno { get; set; } = string.Empty;

    public int SeccionOrigenId { get; set; }

    [Range(1, int.MaxValue)]
    public int SeccionDestinoId { get; set; }

    [Required, StringLength(500)]
    public string Motivo { get; set; } = string.Empty;

    public List<OpcionSeleccion> Secciones { get; set; } = [];
}
