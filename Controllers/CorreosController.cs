using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Servicios.Correo;

namespace Proyecto_Final.Controllers;

[Authorize]
public class CorreosController : Controller
{
    private readonly IServicioCorreo servicioCorreo;

    public CorreosController(
        IServicioCorreo servicioCorreo)
    {
        this.servicioCorreo = servicioCorreo;
    }

    // PROBAR ENVÍO DE CORREO
    [HttpGet]
    public async Task<IActionResult> ProbarCorreo()
    {
        await servicioCorreo.EnviarAsync(
            "jeliasc.9@gmail.com",
            "Prueba de correo - Proyecto Final",
            """
            <h2>Correo de prueba</h2>

            <p>
                El servicio de correo del sistema está funcionando correctamente.
            </p>
            """
        );

        TempData["Exito"] =
            "El correo de prueba fue enviado correctamente.";

        return RedirectToAction(
            "Index",
            "Home"
        );
    }
}