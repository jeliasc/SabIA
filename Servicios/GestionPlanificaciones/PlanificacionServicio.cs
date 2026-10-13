using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.Planificaciones;

namespace Proyecto_Final.Servicios.GestionPlanificaciones;

public sealed class PlanificacionServicio(
    Contexto contexto,
    IArchivoFisicoServicio archivosFisicos,
    IAccesoAcademicoServicio acceso) : IPlanificacionServicio
{
    private async Task<IQueryable<Planificacion>> ConsultaPermitidaAsync(bool historial = false)
    {
        var consulta = contexto.Planificaciones.AsNoTracking().AsQueryable();
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        if (perfil.DocenteId.HasValue)
        {
            consulta = consulta.Where(x =>
                x.DocenteId == perfil.DocenteId.Value &&
                (historial
                    ? !x.Asignaciones.Any(a => a.Asignacion.Estado == EstadoRegistro.Activo &&
                        a.Asignacion.Seccion.CicloEscolar.Activo)
                    : x.Asignaciones.Any(a => a.Asignacion.Estado == EstadoRegistro.Activo &&
                        a.Asignacion.Seccion.CicloEscolar.Activo)));
        }
        else if (perfil.AlumnoId.HasValue)
        {
            consulta = consulta.Where(_ => false);
        }
        else
        {
            consulta = consulta.Where(x => historial
                ? !x.Asignaciones.Any(a => a.Asignacion.Estado == EstadoRegistro.Activo &&
                    a.Asignacion.Seccion.CicloEscolar.Activo)
                : x.Asignaciones.Any(a => a.Asignacion.Estado == EstadoRegistro.Activo &&
                    a.Asignacion.Seccion.CicloEscolar.Activo));
        }
        return consulta;
    }

    public async Task<List<PlanificacionLista>> ObtenerTodosAsync() =>
        await (await ConsultaPermitidaAsync())
            .OrderByDescending(x => x.FechaCreacion)
            .Select(x => new PlanificacionLista
            {
                Id = x.Id,
                Titulo = x.Titulo,
                Tipo = x.Tipo,
                Docente = x.Docente.Usuario.PrimerNombre + " " + x.Docente.Usuario.PrimerApellido,
                Curso = x.Asignaciones.Select(a => a.Asignacion.Curso.Nombre).FirstOrDefault() ?? "Sin asignación",
                Estado = x.Estado,
                Version = x.NumeroVersion,
                FechaCreacion = x.FechaCreacion
            })
            .ToListAsync();

    public async Task<List<PlanificacionLista>> ObtenerHistorialAsync() =>
        await (await ConsultaPermitidaAsync(historial: true))
            .OrderByDescending(x => x.FechaCreacion)
            .Select(x => new PlanificacionLista
            {
                Id = x.Id,
                Titulo = x.Titulo,
                Tipo = x.Tipo,
                Docente = x.Docente.Usuario.PrimerNombre + " " + x.Docente.Usuario.PrimerApellido,
                Curso = x.Asignaciones.Select(a => a.Asignacion.Curso.Nombre).FirstOrDefault() ?? "Sin asignación",
                Estado = x.Estado,
                Version = x.NumeroVersion,
                FechaCreacion = x.FechaCreacion
            })
            .ToListAsync();

    public async Task PrepararAsync(FormularioPlanificacion modelo)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var docenteId = perfil.DocenteId;
        if (!docenteId.HasValue && perfil.AlumnoId.HasValue && !acceso.EsOperadorInstitucional)
        {
            modelo.Asignaciones = [];
            modelo.Periodos = [];
            modelo.Unidades = [];
            return;
        }
        var asignaciones = contexto.Asignaciones.AsNoTracking().Where(x =>
            x.Estado == EstadoRegistro.Activo &&
            x.Seccion.CicloEscolar.Activo);
        if (docenteId.HasValue)
            asignaciones = asignaciones.Where(x => x.DocenteId == docenteId.Value);

        modelo.Asignaciones = await asignaciones
            .OrderByDescending(x => x.Seccion.CicloEscolar.Anio)
            .ThenBy(x => x.Curso.Nombre)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.Curso.Nombre + " · " + x.Seccion.Grado.Nombre + " " + x.Seccion.Nombre + " · " + x.Seccion.CicloEscolar.Anio
            })
            .ToListAsync();

        var periodos = contexto.Periodos.AsNoTracking()
            .Where(x => x.CicloEscolar.Activo);
        if (docenteId.HasValue)
        {
            var ciclos = asignaciones.Select(x => x.Seccion.CicloEscolarId).Distinct();
            periodos = periodos.Where(x => ciclos.Contains(x.CicloEscolarId));
        }

        modelo.Periodos = await periodos
            .OrderByDescending(x => x.CicloEscolar.Anio)
            .ThenBy(x => x.Numero)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.CicloEscolar.Anio + " · " + x.Nombre
            })
            .ToListAsync();

        var unidades = contexto.UnidadesAsignaciones.AsNoTracking()
            .Where(x => x.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Asignacion.Seccion.CicloEscolar.Activo);
        if (docenteId.HasValue)
            unidades = unidades.Where(x => x.Asignacion.DocenteId == docenteId.Value);

        modelo.Unidades = await unidades
            .OrderBy(x => x.Asignacion.Curso.Nombre)
            .ThenBy(x => x.Orden)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.Asignacion.Curso.Nombre + " · " + x.Orden + ". " + x.Unidad.Titulo
            })
            .ToListAsync();
    }

    public async Task<ResultadoOperacion> CrearAsync(CrearPlanificacion modelo, CancellationToken cancellationToken)
    {
        var validacion = await ValidarAcademicoAsync(modelo);
        if (validacion is not null)
            return validacion;

        var asignacion = await contexto.Asignaciones
            .AsNoTracking()
            .Where(x => x.Id == modelo.AsignacionId)
            .Select(x => new { x.DocenteId })
            .FirstOrDefaultAsync(cancellationToken);

        if (asignacion is null)
            return ResultadoOperacion.Error("La asignación no existe.");

        var archivoGuardado = await GuardarPdfOpcionalAsync(modelo.Archivo, cancellationToken);
        if (!archivoGuardado.Exitoso)
            return archivoGuardado.Error!;

        var usuarioId = acceso.UsuarioId;
        if (usuarioId is null)
            return ResultadoOperacion.Error("No se pudo identificar al usuario.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var planificacion = new Planificacion
            {
                GrupoVersionId = Guid.NewGuid(),
                NumeroVersion = 1,
                DocenteId = asignacion.DocenteId,
                Tipo = modelo.Tipo,
                Titulo = modelo.Titulo.Trim(),
                Objetivos = Limpiar(modelo.Objetivos),
                Descripcion = Limpiar(modelo.Descripcion),
                Estado = EstadoPlanificacion.Borrador,
                FechaCreacion = DateTime.UtcNow
            };

            contexto.Planificaciones.Add(planificacion);
            await contexto.SaveChangesAsync(cancellationToken);

            contexto.PlanificacionesDetalles.Add(new PlanificacionDetalle
            {
                PlanificacionId = planificacion.Id,
                Orden = 1,
                Contenido = modelo.Contenido.Trim(),
                Actividades = Limpiar(modelo.Actividades),
                Recursos = Limpiar(modelo.Recursos),
                EstrategiaEvaluacion = Limpiar(modelo.EstrategiaEvaluacion)
            });

            contexto.PlanificacionesAsignaciones.Add(new PlanificacionAsignacion
            {
                PlanificacionId = planificacion.Id,
                AsignacionId = modelo.AsignacionId,
                PeriodoId = modelo.PeriodoId,
                UnidadAsignacionId = modelo.UnidadAsignacionId,
                FechaInicio = modelo.FechaInicio,
                FechaFin = modelo.FechaFin
            });

            if (archivoGuardado.Datos is not null)
                await AsociarArchivoAsync(planificacion.Id, archivoGuardado.Datos, usuarioId, cancellationToken);

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return ResultadoOperacion.Correcto("La planificación fue creada correctamente.");
        }
        catch
        {
            await transaccion.RollbackAsync(cancellationToken);
            if (archivoGuardado.Datos is not null)
                await archivosFisicos.EliminarAsync(archivoGuardado.Datos.ClaveAlmacenamiento, cancellationToken);
            throw;
        }
    }

    public async Task<EditarPlanificacion?> ObtenerEditarAsync(int id)
    {
        var consulta = await ConsultaPermitidaAsync();
        var modelo = await consulta
            .Where(x =>
                x.Id == id &&
                x.Estado != EstadoPlanificacion.Aprobada &&
                x.Estado != EstadoPlanificacion.Archivada &&
                x.Estado != EstadoPlanificacion.EnRevision)
            .Select(x => new EditarPlanificacion
            {
                Id = x.Id,
                Tipo = x.Tipo,
                Titulo = x.Titulo,
                Objetivos = x.Objetivos,
                Descripcion = x.Descripcion,
                Contenido = x.Detalles.OrderBy(d => d.Orden).Select(d => d.Contenido).FirstOrDefault() ?? string.Empty,
                Actividades = x.Detalles.OrderBy(d => d.Orden).Select(d => d.Actividades).FirstOrDefault(),
                Recursos = x.Detalles.OrderBy(d => d.Orden).Select(d => d.Recursos).FirstOrDefault(),
                EstrategiaEvaluacion = x.Detalles.OrderBy(d => d.Orden).Select(d => d.EstrategiaEvaluacion).FirstOrDefault(),
                AsignacionId = x.Asignaciones.Select(a => a.AsignacionId).FirstOrDefault(),
                PeriodoId = x.Asignaciones.Select(a => a.PeriodoId).FirstOrDefault(),
                UnidadAsignacionId = x.Asignaciones.Select(a => a.UnidadAsignacionId).FirstOrDefault(),
                FechaInicio = x.Asignaciones.Select(a => a.FechaInicio).FirstOrDefault(),
                FechaFin = x.Asignaciones.Select(a => a.FechaFin).FirstOrDefault(),
                ArchivoActual = x.Archivos
                    .OrderByDescending(a => a.Archivo.FechaCreacion)
                    .Select(a => a.Archivo.NombreOriginal)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (modelo is not null)
            await PrepararAsync(modelo);
        return modelo;
    }

    public async Task<ResultadoOperacion> EditarAsync(EditarPlanificacion modelo, CancellationToken cancellationToken)
    {
        var planificacion = await contexto.Planificaciones
            .Include(x => x.Detalles)
            .Include(x => x.Asignaciones)
            .Include(x => x.Archivos)
                .ThenInclude(x => x.Archivo)
            .FirstOrDefaultAsync(x => x.Id == modelo.Id, cancellationToken);

        if (planificacion is null)
            return ResultadoOperacion.Error("La planificación no existe.");

        if (!await PuedeGestionarPlanificacionVigenteAsync(planificacion))
            return ResultadoOperacion.Error("No tiene acceso a la planificación.");

        if (planificacion.Estado is EstadoPlanificacion.Aprobada or EstadoPlanificacion.Archivada or EstadoPlanificacion.EnRevision)
            return ResultadoOperacion.Error("La planificación ya no puede editarse en su estado actual.");

        var validacion = await ValidarAcademicoAsync(modelo);
        if (validacion is not null)
            return validacion;

        var archivoGuardado = await GuardarPdfOpcionalAsync(modelo.Archivo, cancellationToken);
        if (!archivoGuardado.Exitoso)
            return archivoGuardado.Error!;

        var usuarioId = acceso.UsuarioId;
        if (usuarioId is null)
            return ResultadoOperacion.Error("No se pudo identificar al usuario.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            planificacion.Tipo = modelo.Tipo;
            planificacion.Titulo = modelo.Titulo.Trim();
            planificacion.Objetivos = Limpiar(modelo.Objetivos);
            planificacion.Descripcion = Limpiar(modelo.Descripcion);
            planificacion.Estado = EstadoPlanificacion.Borrador;

            var detalle = planificacion.Detalles.OrderBy(x => x.Orden).FirstOrDefault();
            if (detalle is null)
            {
                detalle = new PlanificacionDetalle
                {
                    PlanificacionId = planificacion.Id,
                    Orden = 1
                };
                contexto.PlanificacionesDetalles.Add(detalle);
            }

            detalle.Contenido = modelo.Contenido.Trim();
            detalle.Actividades = Limpiar(modelo.Actividades);
            detalle.Recursos = Limpiar(modelo.Recursos);
            detalle.EstrategiaEvaluacion = Limpiar(modelo.EstrategiaEvaluacion);

            var asignacion = planificacion.Asignaciones.FirstOrDefault();
            if (asignacion is null)
            {
                asignacion = new PlanificacionAsignacion { PlanificacionId = planificacion.Id };
                contexto.PlanificacionesAsignaciones.Add(asignacion);
            }

            asignacion.AsignacionId = modelo.AsignacionId;
            asignacion.PeriodoId = modelo.PeriodoId;
            asignacion.UnidadAsignacionId = modelo.UnidadAsignacionId;
            asignacion.FechaInicio = modelo.FechaInicio;
            asignacion.FechaFin = modelo.FechaFin;

            if (archivoGuardado.Datos is not null)
                await AsociarArchivoAsync(planificacion.Id, archivoGuardado.Datos, usuarioId, cancellationToken);

            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return ResultadoOperacion.Correcto("La planificación fue actualizada correctamente.");
        }
        catch
        {
            await transaccion.RollbackAsync(cancellationToken);
            if (archivoGuardado.Datos is not null)
                await archivosFisicos.EliminarAsync(archivoGuardado.Datos.ClaveAlmacenamiento, cancellationToken);
            throw;
        }
    }

    public async Task<ResultadoOperacion> EnviarRevisionAsync(int id)
    {
        var planificacion = await contexto.Planificaciones
            .Include(x => x.Detalles)
            .Include(x => x.Asignaciones)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (planificacion is null)
            return ResultadoOperacion.Error("La planificación no existe.");

        if (!await PuedeGestionarPlanificacionVigenteAsync(planificacion))
            return ResultadoOperacion.Error("No tiene acceso a la planificación.");

        if (planificacion.Estado is not (EstadoPlanificacion.Borrador or EstadoPlanificacion.Devuelta))
            return ResultadoOperacion.Error("La planificación no está disponible para envío.");

        if (!planificacion.Detalles.Any() || !planificacion.Asignaciones.Any())
            return ResultadoOperacion.Error("La planificación no tiene contenido académico completo.");

        var usuarioId = acceso.UsuarioId;
        if (usuarioId is null)
            return ResultadoOperacion.Error("No se pudo identificar al usuario.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            planificacion.Estado = EstadoPlanificacion.EnRevision;
            planificacion.FechaEnvioRevision = DateTime.UtcNow;
            contexto.RevisionesPlanificaciones.Add(new RevisionPlanificacion
            {
                PlanificacionId = planificacion.Id,
                Accion = AccionRevisionPlanificacion.EnviadaRevision,
                RealizadoPorUsuarioId = usuarioId,
                Fecha = DateTime.UtcNow
            });

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto("La planificación fue enviada a revisión.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public Task<RevisarPlanificacion?> ObtenerRevisionAsync(int id) =>
        contexto.Planificaciones
            .AsNoTracking()
            .Where(x => x.Id == id &&
                x.Estado == EstadoPlanificacion.EnRevision &&
                x.Asignaciones.Any(a =>
                    a.Asignacion.Estado == EstadoRegistro.Activo &&
                    a.Asignacion.Seccion.CicloEscolar.Activo))
            .Select(x => new RevisarPlanificacion
            {
                Id = x.Id,
                Titulo = x.Titulo,
                Docente = x.Docente.Usuario.PrimerNombre + " " + x.Docente.Usuario.PrimerApellido,
                Contenido = x.Detalles.OrderBy(d => d.Orden).Select(d => d.Contenido).FirstOrDefault() ?? string.Empty
            })
            .FirstOrDefaultAsync();

    public async Task<ResultadoOperacion> RevisarAsync(RevisarPlanificacion modelo, string usuarioId)
    {
        var planificacion = await contexto.Planificaciones
            .Include(x => x.Asignaciones)
            .FirstOrDefaultAsync(x => x.Id == modelo.Id);
        if (planificacion is null ||
            planificacion.Estado != EstadoPlanificacion.EnRevision ||
            !await TieneAsignacionVigenteAsync(planificacion))
            return ResultadoOperacion.Error("La planificación ya no está pendiente de revisión.");

        if (!modelo.Aprobar && string.IsNullOrWhiteSpace(modelo.Observaciones))
            return ResultadoOperacion.Validacion(nameof(modelo.Observaciones), "Indique el motivo por el que se devuelve la planificación.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            planificacion.Estado = modelo.Aprobar ? EstadoPlanificacion.Aprobada : EstadoPlanificacion.Devuelta;
            planificacion.FechaAprobacion = modelo.Aprobar ? DateTime.UtcNow : null;
            planificacion.AprobadoPorUsuarioId = modelo.Aprobar ? usuarioId : null;

            contexto.RevisionesPlanificaciones.Add(new RevisionPlanificacion
            {
                PlanificacionId = planificacion.Id,
                Accion = modelo.Aprobar ? AccionRevisionPlanificacion.Aprobada : AccionRevisionPlanificacion.Devuelta,
                RealizadoPorUsuarioId = usuarioId,
                Observaciones = Limpiar(modelo.Observaciones),
                Fecha = DateTime.UtcNow
            });

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto(modelo.Aprobar
                ? "La planificación fue aprobada."
                : "La planificación fue devuelta al docente.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<int?> ObtenerArchivoPdfIdAsync(int id)
    {
        var vigentes = await ConsultaPermitidaAsync();
        var historicas = await ConsultaPermitidaAsync(historial: true);
        var planificacionesPermitidas = vigentes.Select(x => x.Id)
            .Concat(historicas.Select(x => x.Id));

        return await contexto.PlanificacionesArchivos
            .AsNoTracking()
            .Where(x =>
                x.PlanificacionId == id &&
                planificacionesPermitidas.Contains(x.PlanificacionId) &&
                x.Archivo.TipoMime == "application/pdf")
            .OrderByDescending(x => x.Archivo.FechaCreacion)
            .Select(x => (int?)x.ArchivoId)
            .FirstOrDefaultAsync();
    }

    private async Task<ResultadoOperacion?> ValidarAcademicoAsync(FormularioPlanificacion modelo)
    {
        if (modelo.FechaInicio > modelo.FechaFin)
            return ResultadoOperacion.Validacion(nameof(modelo.FechaFin), "La fecha final debe ser posterior a la inicial.");

        var asignacion = await contexto.Asignaciones
            .AsNoTracking()
            .Where(x => x.Id == modelo.AsignacionId &&
                x.Estado == EstadoRegistro.Activo &&
                x.Seccion.CicloEscolar.Activo)
            .Select(x => new
            {
                x.Id,
                x.DocenteId,
                CicloEscolarId = x.Seccion.CicloEscolarId
            })
            .FirstOrDefaultAsync();

        if (asignacion is null)
            return ResultadoOperacion.Validacion(nameof(modelo.AsignacionId), "Seleccione una asignación activa válida.");

        if (!await acceso.PuedeGestionarAsignacionVigenteAsync(modelo.AsignacionId))
            return ResultadoOperacion.Error("No tiene acceso a esa asignación.");

        if (modelo.PeriodoId.HasValue)
        {
            var periodoValido = await contexto.Periodos.AnyAsync(x =>
                x.Id == modelo.PeriodoId.Value && x.CicloEscolarId == asignacion.CicloEscolarId);
            if (!periodoValido)
                return ResultadoOperacion.Validacion(nameof(modelo.PeriodoId), "El periodo no pertenece al ciclo de la asignación.");
        }

        if (modelo.UnidadAsignacionId.HasValue)
        {
            var unidadValida = await contexto.UnidadesAsignaciones.AnyAsync(x =>
                x.Id == modelo.UnidadAsignacionId.Value &&
                x.AsignacionId == modelo.AsignacionId &&
                (!modelo.PeriodoId.HasValue || !x.PeriodoId.HasValue || x.PeriodoId == modelo.PeriodoId));
            if (!unidadValida)
                return ResultadoOperacion.Validacion(nameof(modelo.UnidadAsignacionId), "La unidad no pertenece a la asignación o al periodo seleccionado.");
        }

        return null;
    }

    private async Task<(bool Exitoso, ArchivoGuardado? Datos, ResultadoOperacion? Error)> GuardarPdfOpcionalAsync(
        IFormFile? archivo,
        CancellationToken cancellationToken)
    {
        if (archivo is null)
            return (true, null, null);

        if (!Path.GetExtension(archivo.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return (false, null, ResultadoOperacion.Validacion(
                nameof(FormularioPlanificacion.Archivo),
                "La planificación para IA debe cargarse en formato PDF."));
        }

        var resultado = await archivosFisicos.GuardarAsync(archivo, "planificaciones", cancellationToken);
        if (resultado.Exitoso && resultado.Datos is not null)
            return (true, resultado.Datos, null);

        return (false, null, new ResultadoOperacion
        {
            Exitoso = false,
            Mensaje = resultado.Mensaje,
            Errores = resultado.Errores
        });
    }

    private async Task AsociarArchivoAsync(
        int planificacionId,
        ArchivoGuardado guardado,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var archivo = new Archivo
        {
            NombreOriginal = guardado.NombreOriginal,
            ClaveAlmacenamiento = guardado.ClaveAlmacenamiento,
            TipoMime = guardado.TipoMime,
            TamanoBytes = guardado.TamanoBytes,
            HashSha256 = guardado.HashSha256,
            SubidoPorUsuarioId = usuarioId,
            FechaCreacion = DateTime.UtcNow
        };

        contexto.Archivos.Add(archivo);
        await contexto.SaveChangesAsync(cancellationToken);
        contexto.PlanificacionesArchivos.Add(new PlanificacionArchivo
        {
            PlanificacionId = planificacionId,
            ArchivoId = archivo.Id,
            Tipo = TipoArchivoPlanificacion.Adjunto
        });
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private Task<bool> TieneAsignacionVigenteAsync(Planificacion planificacion) =>
        contexto.PlanificacionesAsignaciones.AsNoTracking().AnyAsync(x =>
            x.PlanificacionId == planificacion.Id &&
            x.Asignacion.Estado == EstadoRegistro.Activo &&
            x.Asignacion.Seccion.CicloEscolar.Activo);

    private async Task<bool> PuedeGestionarPlanificacionVigenteAsync(
        Planificacion planificacion)
    {
        if (!await TieneAsignacionVigenteAsync(planificacion))
            return false;

        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        if (acceso.EsOperadorInstitucional)
            return true;

        if (perfil.DocenteId.HasValue)
            return planificacion.DocenteId == perfil.DocenteId.Value;

        return !perfil.TienePerfilAcademico;
    }
}
