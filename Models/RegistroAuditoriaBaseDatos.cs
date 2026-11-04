namespace Proyecto_Final.Models;

/// <summary>Proyección de sólo lectura de la evidencia creada por triggers PostgreSQL.</summary>
public class RegistroAuditoriaBaseDatos
{
    public long Id { get; set; }
    public DateTime FechaUtc { get; set; }
    public string Esquema { get; set; } = string.Empty;
    public string Tabla { get; set; } = string.Empty;
    public string Operacion { get; set; } = string.Empty;
    public string? RegistroId { get; set; }
    public string UsuarioConexion { get; set; } = string.Empty;
    public string TransaccionId { get; set; } = string.Empty;
    public string? Origen { get; set; }
    public string CamposModificados { get; set; } = "[]";
    public string? ValoresAnteriores { get; set; }
    public string? ValoresNuevos { get; set; }
}
