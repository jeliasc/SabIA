using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Proyecto_Final.Servicios.Correo;

public class ServicioCorreo : IServicioCorreo
{
    private readonly ConfiguracionCorreo configuracion;

    public ServicioCorreo(
        IOptions<ConfiguracionCorreo> opciones)
    {
        configuracion = opciones.Value;
    }

    // ENVIAR CORREO ELECTRÓNICO
    public async Task EnviarAsync(
        string destinatario,
        string asunto,
        string contenidoHtml)
    {
        if (!configuracion.Habilitado)
        {
            throw new InvalidOperationException(
                "El servicio de correo electrónico no está habilitado."
            );
        }

        if (string.IsNullOrWhiteSpace(configuracion.Usuario) ||
            string.IsNullOrWhiteSpace(configuracion.Contrasena))
        {
            throw new InvalidOperationException(
                "No se configuraron las credenciales del servicio de correo."
            );
        }

        using var mensaje = new MailMessage();

        mensaje.From = new MailAddress(
            configuracion.Usuario,
            configuracion.NombreRemitente
        );

        mensaje.To.Add(destinatario);
        mensaje.Subject = asunto;
        mensaje.Body = contenidoHtml;
        mensaje.IsBodyHtml = true;

        using var cliente = new SmtpClient(
            configuracion.Servidor,
            configuracion.Puerto
        );

        cliente.UseDefaultCredentials = false;

        cliente.Credentials = new NetworkCredential(
            configuracion.Usuario,
            configuracion.Contrasena
        );

        cliente.EnableSsl = configuracion.UsarSsl;

        await cliente.SendMailAsync(mensaje);
    }
}