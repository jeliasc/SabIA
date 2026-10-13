namespace Proyecto_Final.Servicios.Correo;

public interface IServicioCorreo
{
    Task EnviarAsync(
        string destinatario,
        string asunto,
        string contenidoHtml
    );

}