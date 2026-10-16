using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.InteligenciaArtificial;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.ContenidoIA;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.ContenidoIA.Generar)]
public class ContenidoIAController(
    IIaServicio iaServicio,
    Contexto contexto,
    IAccesoAcademicoServicio acceso) : Controller
{
    public async Task<IActionResult> Index() => View(await PrepararAsync(new GenerarContenidoIa()));

    [HttpGet]
    public async Task<IActionResult> Generar(int? planificacionId)
    {
        var modelo = new GenerarContenidoIa { PlanificacionId = planificacionId };
        return View(await PrepararAsync(modelo));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generar(GenerarContenidoIa modelo, CancellationToken cancellationToken)
    {
        var hayPlanificacion = modelo.PlanificacionId.HasValue && modelo.PlanificacionId.Value > 0;
        var hayDocumento = modelo.Documento != null;
        if (modelo.OrigenDocumento == OrigenDocumentoIa.Planificacion && (!hayPlanificacion || hayDocumento))
            ModelState.AddModelError(string.Empty, "Seleccione exclusivamente una planificación autorizada.");
        else if (modelo.OrigenDocumento == OrigenDocumentoIa.ArchivoLocal && (hayPlanificacion || !hayDocumento))
            ModelState.AddModelError(string.Empty, "Seleccione exclusivamente un archivo PDF local.");
        else if (modelo.OrigenDocumento is not OrigenDocumentoIa.Planificacion and not OrigenDocumentoIa.ArchivoLocal)
            ModelState.AddModelError(string.Empty, "Seleccione un origen válido para el documento.");

        if (!ModelState.IsValid)
            return View(await PrepararAsync(modelo));

        var solicitudes = new List<SolicitudContenidoIa>();
        if (modelo.Cuestionario) solicitudes.Add(new("cuestionario", modelo.InstruccionesCuestionario ?? string.Empty));
        if (modelo.Resumen) solicitudes.Add(new("resumen", modelo.InstruccionesResumen ?? string.Empty));
        if (modelo.HojaTrabajo) solicitudes.Add(new("hoja_trabajo", modelo.InstruccionesHojaTrabajo ?? string.Empty));
        if (modelo.Glosario) solicitudes.Add(new("glosario", modelo.InstruccionesGlosario ?? string.Empty));

        ResultadoOperacion<List<TrabajoIa>> resultado;
        if (modelo.OrigenDocumento == OrigenDocumentoIa.Planificacion)
        {
            resultado = await iaServicio.GenerarDesdeDocumentoAsync(
                modelo.PlanificacionId!.Value,
                solicitudes,
                cancellationToken: cancellationToken);
        }
        else
        {
            await using var documento = modelo.Documento!.OpenReadStream();
            resultado = await iaServicio.GenerarDesdeArchivoLocalAsync(
                solicitudes,
                documento,
                modelo.Documento.FileName,
                modelo.Documento.ContentType,
                modelo.Documento.Length,
                cancellationToken);
        }
        if (!resultado.Exitoso || resultado.Datos == null)
        {
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
            return View(await PrepararAsync(modelo));
        }

        var nombre = modelo.OrigenDocumento == OrigenDocumentoIa.Planificacion
            ? await contexto.Planificaciones.AsNoTracking()
                .Where(x => x.Id == modelo.PlanificacionId)
                .Select(x => x.Titulo)
                .FirstOrDefaultAsync(cancellationToken) ?? "Planificación"
            : "Archivo local: " + modelo.Documento!.FileName;

        return View("Trabajos", new TrabajosContenidoIa { Planificacion = nombre, Trabajos = resultado.Datos });
    }

    [HttpGet]
    public IActionResult GenerarDirecto() => View(new GenerarContenidoDirectoIa());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerarDirecto(
        GenerarContenidoDirectoIa modelo,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        var resultado = await iaServicio.GenerarDirectoAsync(
            modelo.Prompt,
            modelo.MaxTokens,
            cancellationToken);
        if (!resultado.Exitoso || resultado.Datos == null)
        {
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
            return View(modelo);
        }

        return View("Trabajos", new TrabajosContenidoIa
        {
            Planificacion = "Generación directa",
            Trabajos = resultado.Datos
        });
    }

    [HttpGet]
    public async Task<IActionResult> Estado(string id, CancellationToken cancellationToken)
    {
        var resultado = await iaServicio.ConsultarEstadoAsync(id, cancellationToken);
        return Json(new
        {
            exitoso = resultado.Exitoso,
            mensaje = resultado.Mensaje,
            estado = resultado.Datos?.Estado,
            resultado = resultado.Datos?.Resultado
        });
    }

    private async Task<GenerarContenidoIa> PrepararAsync(GenerarContenidoIa modelo)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var consulta = contexto.Planificaciones.AsNoTracking()
            .Where(x =>
                x.Asignaciones.Any(a =>
                    a.Asignacion.Estado == Proyecto_Final.Models.EstadoRegistro.Activo &&
                    a.Asignacion.Seccion.CicloEscolar.Activo));
        if (perfil.DocenteId.HasValue)
            consulta = consulta.Where(x => x.DocenteId == perfil.DocenteId.Value);
        else if (!acceso.EsOperadorInstitucional)
            consulta = consulta.Where(_ => false);

        modelo.Planificaciones = await consulta
            .OrderByDescending(x => x.FechaCreacion)
            .Select(x => new OpcionSeleccion { Id = x.Id, Texto = x.Titulo + " · " + x.Estado })
            .ToListAsync();
        return modelo;
    }
}
