namespace Proyecto_Final.ViewModels.Usuarios;

public class DetalleUsuario
{
    public string Id { get; set; } =
        string.Empty;

    public string PrimerNombre { get; set; } =
        string.Empty;

    public string? SegundoNombre { get; set; }

    public string? TercerNombre { get; set; }

    public string PrimerApellido { get; set; } =
        string.Empty;

    public string? SegundoApellido { get; set; }

    public string Usuario { get; set; } =
        string.Empty;

    public string Correo { get; set; } =
        string.Empty;

    public string Rol { get; set; } =
        string.Empty;

    public bool Activo { get; set; }

    public bool CambiarContrasena { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaUltimoCambioContrasena
    {
        get;
        set;
    }
}