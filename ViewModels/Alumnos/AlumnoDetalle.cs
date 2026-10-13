namespace Proyecto_Final.ViewModels.Alumnos;

public sealed class AlumnoDetalle
{
    public int Id { get; init; }

    public string PrimerNombre { get; init; } = string.Empty;

    public string? SegundoNombre { get; init; }

    public string? TercerNombre { get; init; }

    public string PrimerApellido { get; init; } = string.Empty;

    public string? SegundoApellido { get; init; }

    public string CodigoPersonal { get; init; } = string.Empty;

    public string NombreCompleto { get; init; } = string.Empty;

    public string Usuario { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    public bool Activo { get; init; }

    public DateTime FechaCreacion { get; set; }

    public int UnidadesDisponibles { get; set; }

    public int TareasPublicadas { get; set; }

    public int EntregasRealizadas { get; set; }

    public InscripcionAlumnoDetalle? InscripcionVigente { get; set; }

    public IReadOnlyList<InscripcionAlumnoDetalle> Inscripciones { get; set; } = [];

    public List<EncargadoAlumnoDetalle> Encargados { get; set; } = [];

    public bool MostrarContacto { get; set; } = true;
    public bool MostrarHistorial { get; set; } = true;
    public bool MostrarEncargados { get; set; } = true;
    public bool MostrarInformacionAdministrativa { get; set; } = true;
}

public sealed class EncargadoAlumnoDetalle
{
    public string NombreCompleto { get; init; } = string.Empty;

    public string Telefono { get; init; } = string.Empty;

    public string? TelefonoAlterno { get; init; }

    public string? Correo { get; init; }

    public string Parentesco { get; init; } = string.Empty;
}
