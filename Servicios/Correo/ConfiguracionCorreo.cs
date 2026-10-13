namespace Proyecto_Final.Servicios.Correo;

public class ConfiguracionCorreo
{
    public bool Habilitado { get; set; }

    public string Servidor { get; set; } =
        string.Empty;

    public int Puerto { get; set; }

    public bool UsarSsl { get; set; }

    public string NombreRemitente { get; set; } =
        string.Empty;

    public string Remitente { get; set; } =
        string.Empty;

    public string Usuario { get; set; } =
        string.Empty;

    public string Contrasena { get; set; } =
        string.Empty;
}