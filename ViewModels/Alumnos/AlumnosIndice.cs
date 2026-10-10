namespace Proyecto_Final.ViewModels.Alumnos;

public sealed class AlumnosIndice
{
    public List<AlumnoLista> Alumnos { get; init; } = [];

    public List<AlertaOcupacionSeccion> AlertasOcupacion { get; init; } = [];
}

public sealed class AlertaOcupacionSeccion
{
    public string Grado { get; init; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;

    public string Seccion { get; init; } = string.Empty;

    public int Ciclo { get; init; }

    public int CantidadAlumnos { get; init; }
}
