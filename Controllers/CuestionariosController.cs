using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionCuestionarios;
using Proyecto_Final.Servicios.GestionNotificaciones;
using Proyecto_Final.Servicios.InteligenciaArtificial;
using Proyecto_Final.ViewModels.Cuestionarios;
using Proyecto_Final.Servicios.SeguridadAcademica;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Cuestionarios.Ver)]
public class CuestionariosController(
    ICuestionarioServicio s,
    INotificacionServicio notificaciones,
    IIaServicio ia,
    IAccesoAcademicoServicio acceso) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await s.ObtenerTodosAsync());

    [HttpGet]
    public async Task<IActionResult> Historial()
    {
        ViewData["SoloLectura"] = true;
        return View("Index", await s.ObtenerHistorialAsync());
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Cuestionarios.Crear)]
    public async Task<IActionResult> Configurar(int tareaId)
    {
        var m = await s.ObtenerConfiguracionAsync(tareaId);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Cuestionarios.Crear)]
    public async Task<IActionResult> Configurar(ConfigurarCuestionario m)
    {
        if (!ModelState.IsValid)
        {
            return View(m);
        }

        var r = await s.GuardarConfiguracionAsync(m);

        if (!r.Exitoso || r.Datos == 0)
        {
            ModelState.AddModelError("", r.Mensaje);
            return View(m);
        }

        TempData["Exito"] = r.Mensaje;

        return RedirectToAction(
            nameof(AgregarPregunta),
            new { cuestionarioId = r.Datos }
        );
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Cuestionarios.Editar)]
    public async Task<IActionResult> AgregarPregunta(int cuestionarioId)
    {
        var m = await s.PrepararPreguntaAsync(cuestionarioId);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Cuestionarios.Editar)]
    public async Task<IActionResult> AgregarPregunta(CrearPregunta m)
    {
        if (!ModelState.IsValid)
        {
            return View(m);
        }

        var r = await s.AgregarPreguntaAsync(m);

        if (!r.Exitoso)
        {
            foreach (var e in r.Errores)
            {
                ModelState.AddModelError(e.Key, e.Value);
            }

            if (r.Errores.Count == 0)
            {
                ModelState.AddModelError("", r.Mensaje);
            }

            return View(m);
        }

        TempData["Exito"] = r.Mensaje;

        return RedirectToAction(
            nameof(AgregarPregunta),
            new { cuestionarioId = m.CuestionarioId }
        );
    }

    // =====================================================================
    // RESOLUCIÓN (ALUMNO)
    // =====================================================================

    // Pantalla previa: reglas del cuestionario, tiempo e intentos.
    [HttpGet]
    [Authorize(Policy = Permisos.Cuestionarios.Resolver)]
    public async Task<IActionResult> Resolver(int id)
    {
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();

        if (!alumnoId.HasValue)
        {
            TempData["Error"] =
                "Acceso denegado. Para resolver cuestionarios debe tener un perfil de alumno e inscripción vigente.";

            return RedirectToAction(nameof(Index));
        }

        var m = await s.ObtenerInicioAsync(id);

        if (m == null)
        {
            TempData["Error"] =
                "No puede resolver este cuestionario. La actividad puede estar cerrada, " +
                "fuera del período de disponibilidad o no corresponder a su inscripción académica.";

            return RedirectToAction(nameof(Index));
        }

        await notificaciones.RegistrarVisualizacionActividadAsync(m.TareaId);

        return View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Cuestionarios.Resolver)]
    public async Task<IActionResult> Iniciar(int id)
    {
        var r = await s.IniciarAsync(id);

        if (!r.Exitoso)
        {
            TempData["Error"] = r.Mensaje;
            return RedirectToAction(nameof(Resolver), new { id });
        }

        return RedirectToAction(nameof(Intento), new { id = r.Datos });
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Cuestionarios.Resolver)]
    public async Task<IActionResult> Intento(int id)
    {
        var vista = await s.ObtenerIntentoAsync(id);

        if (!vista.Existe)
            return NotFound();

        if (vista.Finalizado || vista.Modelo == null)
            return RedirectToAction(nameof(Resultado), new { id = vista.IntentoId });

        return View(vista.Modelo);
    }

    // Guardado automático de una respuesta en modo navegable.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Cuestionarios.Resolver)]
    public async Task<IActionResult> GuardarRespuesta(ResponderPregunta m)
    {
        var r = await s.GuardarRespuestaAsync(m.IntentoId, m.Respuesta);

        return Json(new
        {
            exitoso = r.Exitoso,
            finalizado = r.Datos,
            mensaje = r.Mensaje,
            urlResultado = Url.Action(nameof(Resultado), new { id = m.IntentoId })
        });
    }

    // Respuesta de la pregunta en curso en modo secuencial.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Cuestionarios.Resolver)]
    public async Task<IActionResult> Responder(ResponderPregunta m)
    {
        var r = await s.ResponderAsync(m);

        if (!r.Exitoso)
        {
            TempData["Error"] = r.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        if (r.Datos)
            return RedirectToAction(nameof(Resultado), new { id = m.IntentoId });

        if (r.Mensaje.StartsWith("El tiempo", StringComparison.Ordinal))
            TempData["Advertencia"] = r.Mensaje;

        return RedirectToAction(nameof(Intento), new { id = m.IntentoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Cuestionarios.Resolver)]
    public async Task<IActionResult> Finalizar(FinalizarIntento m)
    {
        var r = await s.FinalizarAsync(m);

        if (!r.Exitoso)
        {
            TempData["Error"] = r.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        if (r.Mensaje.StartsWith("El tiempo", StringComparison.Ordinal))
            TempData["Advertencia"] = r.Mensaje;

        return RedirectToAction(nameof(Resultado), new { id = r.Datos });
    }

    [HttpGet]
    [Authorize(Policy = Permisos.Cuestionarios.Resolver)]
    public async Task<IActionResult> Resultado(int id)
    {
        var m = await s.ObtenerResultadoAsync(id);

        return m == null
            ? NotFound()
            : View(m);
    }

    // =====================================================================
    // REVISIÓN (DOCENTE Y COORDINACIÓN)
    // =====================================================================

    [HttpGet]
    public async Task<IActionResult> Resultados(int id)
    {
        var m = await s.ObtenerResultadosAsync(id);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpGet]
    public async Task<IActionResult> RevisarIntento(int id)
    {
        var m = await s.ObtenerRevisionIntentoAsync(id);

        return m == null
            ? NotFound()
            : View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Entregas.Calificar)]
    public async Task<IActionResult> AjustarRespuesta(int respuestaId, int intentoId, bool aceptar)
    {
        var r = await s.AjustarRespuestaDirectaAsync(respuestaId, aceptar);

        if (r.Exitoso)
            TempData["Exito"] = r.Mensaje;
        else
            TempData["Error"] = r.Mensaje;

        return RedirectToAction(nameof(RevisarIntento), new { id = r.Exitoso ? r.Datos : intentoId });
    }

    // Acceso directo desde Entregas.
    [HttpGet]
    public async Task<IActionResult> RespuestasEntrega(int id)
    {
        var (intentoId, cuestionarioId) = await s.ObtenerIntentoDeEntregaAsync(id);

        if (intentoId.HasValue)
            return RedirectToAction(nameof(RevisarIntento), new { id = intentoId });

        if (cuestionarioId.HasValue)
        {
            TempData["Informacion"] = "El alumno todavía no ha iniciado el cuestionario.";
            return RedirectToAction(nameof(Resultados), new { id = cuestionarioId });
        }

        return NotFound();
    }

    // =====================================================================
    // IMPORTAR DESDE IA
    // =====================================================================

    // Vista previa a partir de un trabajo de IA terminado (o vacía para pegar texto).
    [HttpGet]
    [Authorize(Policy = Permisos.Cuestionarios.Editar)]
    public async Task<IActionResult> ImportarIa(string? jobId, int? tareaId, CancellationToken cancellationToken)
    {
        var m = new ImportarCuestionarioIa { JobId = jobId, TareaId = tareaId };

        if (!string.IsNullOrWhiteSpace(jobId))
        {
            var estado = await ia.ConsultarEstadoAsync(jobId, cancellationToken);
            if (estado.Exitoso && !string.IsNullOrWhiteSpace(estado.Datos?.Resultado))
                m.Texto = estado.Datos.Resultado;
            else
                m.ResultadoNoDisponible = true;
        }

        m.Preguntas = LectorCuestionarioIa.Analizar(m.Texto);
        m.Tareas = await s.ObtenerTareasParaImportarAsync();
        return View(m);
    }

    // Vuelve a interpretar el texto (pegado o corregido por el docente).
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Cuestionarios.Editar)]
    public async Task<IActionResult> ImportarIa(ImportarCuestionarioIa m)
    {
        ModelState.Clear();
        m.Preguntas = LectorCuestionarioIa.Analizar(m.Texto);
        m.Tareas = await s.ObtenerTareasParaImportarAsync();
        return View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permisos.Cuestionarios.Editar)]
    public async Task<IActionResult> ConfirmarImportacionIa(ImportarCuestionarioIa m)
    {
        var r = await s.ImportarPreguntasAsync(m);

        if (!r.Exitoso)
        {
            ModelState.Clear();
            foreach (var (campo, mensaje) in r.Errores)
                ModelState.AddModelError(campo, mensaje);
            if (r.Errores.Count == 0 || r.Errores.Keys.All(x => x.StartsWith("Preguntas[")))
                ModelState.AddModelError(string.Empty, r.Errores.Count == 0 ? r.Mensaje : "Revise las preguntas marcadas en rojo.");

            m.Tareas = await s.ObtenerTareasParaImportarAsync();
            return View(nameof(ImportarIa), m);
        }

        TempData["Informacion"] = r.Mensaje;
        return RedirectToAction(nameof(Configurar), new { tareaId = m.TareaId });
    }
}
