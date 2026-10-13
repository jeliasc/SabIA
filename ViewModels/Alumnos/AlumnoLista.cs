namespace Proyecto_Final.ViewModels.Alumnos;

public sealed class AlumnoLista
{
    public int Id { get; init; }
    public string CodigoPersonal { get; init; } = string.Empty;
    public string NombreCompleto { get; init; } = string.Empty;
    public string Correo { get; init; } = string.Empty;
    public string GradoSeccion { get; init; } = "Sin inscripción vigente";
    public int? Ciclo { get; init; }
    public int Encargados { get; init; }
    public bool Activo { get; init; }
}
