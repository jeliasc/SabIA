using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.Alumnos;

public sealed class InscripcionAlumnoDetalle
{
    public int Id { get; init; }

    public int SeccionId { get; init; }

    public int CicloEscolarId { get; init; }

    public int Ciclo { get; init; }

    public string Grado { get; init; } = string.Empty;

    public string Seccion { get; init; } = string.Empty;

    public string Carrera { get; init; } = string.Empty;

    public DateOnly Fecha { get; init; }

    public EstadoInscripcion Estado { get; init; }

    public bool EsVigente { get; init; }
}
