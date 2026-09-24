namespace Proyecto_Final.Servicios.Correo;

public interface IServicioCorreo
{
    // ENVIAR CORREO ELECTRÓNICO
    Task EnviarAsync(
        string destinatario,
        string asunto,
        string contenidoHtml
    );
}