using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Servicios.Correo;
using Proyecto_Final.Seguridad;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Usuarios.Editar)]
public class CorreosController : Controller
{
    private readonly IServicioCorreo servicioCorreo;
    private readonly IHostEnvironment entorno;

    public CorreosController(
        IServicioCorreo servicioCorreo,
        IHostEnvironment entorno)
    {
        this.servicioCorreo = servicioCorreo;
        this.entorno = entorno;
    }

    // PROBAR ENVÍO DE CORREO
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProbarCorreo()
    {
        if (entorno.IsProduction())
            return NotFound();

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
