using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

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

        if (string.IsNullOrWhiteSpace(configuracion.Servidor) ||
            configuracion.Puerto <= 0)
        {
            throw new InvalidOperationException(
                "La configuración del servidor SMTP no es válida."
            );
        }

        if (string.IsNullOrWhiteSpace(configuracion.Remitente))
        {
            throw new InvalidOperationException(
                "No se configuró el correo remitente."
            );
        }

        if (string.IsNullOrWhiteSpace(configuracion.Usuario) ||
            string.IsNullOrWhiteSpace(configuracion.Contrasena))
        {
            throw new InvalidOperationException(
                "No se configuraron las credenciales del servicio de correo."
            );
        }

        var mensaje = new MimeMessage();

        mensaje.From.Add(
            new MailboxAddress(
                configuracion.NombreRemitente,
                configuracion.Remitente
            )
        );

        mensaje.To.Add(
            MailboxAddress.Parse(
                destinatario
            )
        );

        mensaje.Subject =
            asunto;

        mensaje.Body =
            new BodyBuilder
            {
                HtmlBody =
                    contenidoHtml
            }
            .ToMessageBody();

        using var cliente =
            new SmtpClient();

        await cliente.ConnectAsync(
            configuracion.Servidor,
            configuracion.Puerto,
            configuracion.UsarSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls
        );

        await cliente.AuthenticateAsync(
            configuracion.Usuario,
            configuracion.Contrasena
        );

        await cliente.SendAsync(
            mensaje
        );

        await cliente.DisconnectAsync(
            true
        );
    }
}