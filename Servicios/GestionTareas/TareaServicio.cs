using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.Servicios.GestionAsignaciones;
using Proyecto_Final.Seguridad;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.Tareas;

namespace Proyecto_Final.Servicios.GestionTareas;

public sealed class TareaServicio(
    Contexto c,
    IArchivoFisicoServicio fisico,
    IAccesoAcademicoServicio acceso,
    IDelegacionAcademicaServicio? delegaciones = null) : ITareaServicio
{
    private async Task<IQueryable<Tarea>> Q(bool historial = false)
    {
        var q = c.Tareas
            .AsNoTracking()
            .AsQueryable();

        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var d = perfil.DocenteId;
        var a = perfil.AlumnoId;

        if (d.HasValue)
        {
            q = q.Where(x =>
                (x.Asignacion.DocenteId == d.Value ||
                 c.DelegacionesAcademicas.Any(delegacion =>
                    delegacion.AsignacionId == x.AsignacionId &&
                    delegacion.DocenteId == d.Value && delegacion.Activa &&
                    delegacion.FechaInicioUtc <= DateTime.UtcNow &&
                    (!delegacion.FechaFinUtc.HasValue || delegacion.FechaFinUtc >= DateTime.UtcNow) &&
                    (delegacion.Operaciones & OperacionDelegacionAcademica.ReabrirEntregas) ==
                        OperacionDelegacionAcademica.ReabrirEntregas)) &&
                (historial
                    ? x.Estado == EstadoTarea.Cerrada ||
                      x.Asignacion.Estado != EstadoRegistro.Activo ||
                      !x.Asignacion.Seccion.CicloEscolar.Activo
                    : x.Estado != EstadoTarea.Cerrada &&
                      x.Asignacion.Estado == EstadoRegistro.Activo &&
                      x.Asignacion.Seccion.CicloEscolar.Activo)
            );
        }
        else if (a.HasValue)
        {
            var ahora = DateTime.UtcNow;

            q = historial
                ? q.Where(x =>
                    x.Estado != EstadoTarea.Borrador &&
                    c.Inscripciones.Any(i =>
                        i.AlumnoId == a.Value &&
                        i.SeccionId == x.Asignacion.SeccionId &&
                        i.CicloEscolarId == x.Asignacion.Seccion.CicloEscolarId) &&
                    (x.Estado == EstadoTarea.Cerrada ||
                     !(x.Asignacion.Estado == EstadoRegistro.Activo &&
                       x.Asignacion.Seccion.CicloEscolar.Activo &&
                       c.Inscripciones.Any(i =>
                           i.AlumnoId == a.Value &&
                           i.SeccionId == x.Asignacion.SeccionId &&
                           i.CicloEscolarId == x.Asignacion.Seccion.CicloEscolarId &&
                           i.Estado == EstadoInscripcion.Activa))))
                : q.Where(x =>
                    (x.Asignacion.Estado == EstadoRegistro.Activo ||
                     c.DelegacionesAcademicas.Any(d =>
                        d.AsignacionId == x.AsignacionId && d.Activa &&
                        d.FechaInicioUtc <= ahora &&
                        (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                        (d.Operaciones & OperacionDelegacionAcademica.ContinuarActividadesAlumnos) ==
                            OperacionDelegacionAcademica.ContinuarActividadesAlumnos)) &&
                    x.Asignacion.Seccion.CicloEscolar.Activo &&
                    x.Estado == EstadoTarea.Publicada &&
                    (!x.FechaDisponibilidad.HasValue || x.FechaDisponibilidad <= ahora) &&
                    c.Inscripciones.Any(i =>
                    i.AlumnoId == a.Value &&
                    i.SeccionId == x.Asignacion.SeccionId &&
                    i.CicloEscolarId == x.Asignacion.Seccion.CicloEscolarId &&
                    i.Estado == EstadoInscripcion.Activa &&
                    i.Seccion.CicloEscolar.Activo));
        }
        else if (acceso.EsOperadorInstitucional)
        {
            q = q.Where(x => historial
                ? x.Estado == EstadoTarea.Cerrada ||
                  x.Asignacion.Estado != EstadoRegistro.Activo ||
                  !x.Asignacion.Seccion.CicloEscolar.Activo
                : x.Estado != EstadoTarea.Cerrada &&
                  x.Asignacion.Estado == EstadoRegistro.Activo &&
                  x.Asignacion.Seccion.CicloEscolar.Activo);
        }
        else
        {
            q = q.Where(_ => false);
        }

        return q;
    }

    public async Task<List<TareaLista>> ObtenerTodosAsync()
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var docenteId = perfil.DocenteId;
        var alumnoId = perfil.AlumnoId;
        var ahora = DateTime.UtcNow;
        return await (await Q())
            .OrderByDescending(x => x.FechaCreacion)
            .Select(x => new TareaLista
            {
                Id = x.Id,
                Titulo = x.Titulo,
                Curso = x.Asignacion.Curso.Nombre,
                Seccion =
                    x.Asignacion.Seccion.Grado.Nombre
                    + " "
                    + x.Asignacion.Seccion.Nombre,
                Tipo = x.Tipo,
                Estado = x.Estado,
                IndicadorDisponibilidad = x.Estado == EstadoTarea.Cerrada
                    ? "Cerrada"
                    : x.Estado == EstadoTarea.Borrador
                        ? "No publicada"
                        : x.Reaperturas.Any(r => r.FechaLimite >= ahora &&
                            (r.Alcance == AlcanceReaperturaTarea.Todos ||
                             (alumnoId.HasValue && r.Alumnos.Any(a =>
                                a.AlumnoId == alumnoId.Value))))
                            ? "Reabierta"
                            : x.FechaLimite.HasValue && x.FechaLimite < ahora
                                ? "Plazo vencido"
                                : x.FechaDisponibilidad.HasValue &&
                                  x.FechaDisponibilidad > ahora
                                    ? "Aún no disponible"
                                    : "Disponible",
                Limite = x.FechaLimite,
                Pendientes = x.Entregas
                .Select(e => new
                {
                    e.AlumnoId,
                    e.InscripcionId
                })
                .Distinct()
                .Count()
                -
                x.Entregas
                    .Where(e => e.FechaEntrega.HasValue)
                    .Select(e => new
                    {
                        e.AlumnoId,
                        e.InscripcionId
                    })
                    .Distinct()
                    .Count(),
                Entregadas = x.Entregas
                .Where(e => e.FechaEntrega.HasValue)
                .Select(e => new
                {
                    e.AlumnoId,
                    e.InscripcionId
                })
                .Distinct()
                .Count(),
                PuedeReabrirse = docenteId.HasValue &&
                    x.Estado != EstadoTarea.Borrador &&
                    (x.Asignacion.DocenteId == docenteId.Value ||
                     c.DelegacionesAcademicas.Any(delegacion =>
                        delegacion.AsignacionId == x.AsignacionId &&
                        delegacion.DocenteId == docenteId.Value && delegacion.Activa &&
                        delegacion.FechaInicioUtc <= ahora &&
                        (!delegacion.FechaFinUtc.HasValue || delegacion.FechaFinUtc >= ahora) &&
                        (delegacion.Operaciones & OperacionDelegacionAcademica.ReabrirEntregas) ==
                            OperacionDelegacionAcademica.ReabrirEntregas))
            })
            .ToListAsync();
    }

    public async Task<List<TareaLista>> ObtenerHistorialAsync()
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var alumnoId = perfil.AlumnoId;
        var docenteId = perfil.DocenteId;
        var ahora = DateTime.UtcNow;
        var tareas = await (await Q(historial: true))
            .OrderByDescending(x => x.FechaCreacion)
            .Select(x => new TareaLista
            {
                Id = x.Id,
                AsignacionId = x.AsignacionId,
                Titulo = x.Titulo,
                Curso = x.Asignacion.Curso.Nombre,
                Seccion = x.Asignacion.Seccion.Grado.Nombre + " " + x.Asignacion.Seccion.Nombre,
                Tipo = x.Tipo,
                Estado = x.Estado,
                IndicadorDisponibilidad = x.Estado == EstadoTarea.Cerrada
                    ? "Cerrada"
                    : x.Reaperturas.Any(r => r.FechaLimite >= ahora &&
                        (r.Alcance == AlcanceReaperturaTarea.Todos ||
                         (alumnoId.HasValue && r.Alumnos.Any(a =>
                            a.AlumnoId == alumnoId.Value))))
                        ? "Reabierta"
                        : x.FechaLimite.HasValue && x.FechaLimite < ahora
                            ? "Plazo vencido"
                            : "No disponible en el contexto vigente",
                Limite = x.FechaLimite,
                Pendientes = x.Entregas
                .Select(e => new
                {
                    e.AlumnoId,
                    e.InscripcionId
                })
                .Distinct()
                .Count()
                -
                x.Entregas
                    .Where(e => e.FechaEntrega.HasValue)
                    .Select(e => new
                    {
                        e.AlumnoId,
                        e.InscripcionId
                    })
                    .Distinct()
                    .Count(),

                Entregadas = x.Entregas
                .Where(e => e.FechaEntrega.HasValue)
                .Select(e => new
                {
                    e.AlumnoId,
                    e.InscripcionId
                })
                .Distinct()
                .Count(),
                // El historial sigue siendo de consulta salvo esta operación:
                // una tarea cerrada puede reabrirse si el responsable aún
                // gestiona la asignación vigente o tiene una delegación activa.
                PuedeReabrirse = docenteId.HasValue &&
                    x.Estado == EstadoTarea.Cerrada &&
                    x.Asignacion.Estado == EstadoRegistro.Activo &&
                    x.Asignacion.Seccion.CicloEscolar.Activo &&
                    (x.Asignacion.DocenteId == docenteId.Value ||
                     c.DelegacionesAcademicas.Any(delegacion =>
                        delegacion.AsignacionId == x.AsignacionId &&
                        delegacion.DocenteId == docenteId.Value &&
                        delegacion.Activa && delegacion.FechaInicioUtc <= ahora &&
                        (!delegacion.FechaFinUtc.HasValue || delegacion.FechaFinUtc >= ahora) &&
                        (delegacion.Operaciones & OperacionDelegacionAcademica.ReabrirEntregas) ==
                            OperacionDelegacionAcademica.ReabrirEntregas))
            })
            .ToListAsync();

        // La proyección determina candidatas sin cargar entidades. Esta
        // comprobación final aplica exactamente la misma autorización que el
        // POST, para no anunciar la acción a un responsable ya no vigente.
        foreach (var tarea in tareas.Where(x => x.PuedeReabrirse))
            tarea.PuedeReabrirse = await PuedeReabrirAsync(tarea.AsignacionId);

        return tareas;
    }

    public async Task PrepararAsync(FormularioTarea m)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var d = perfil.DocenteId;

        if (!d.HasValue && !acceso.EsOperadorInstitucional)
        {
            m.Asignaciones = [];
            m.Unidades = [];
            return;
        }

        var aq = c.Asignaciones
            .AsNoTracking()
            .Where(x =>
                x.Estado == EstadoRegistro.Activo &&
                x.Seccion.CicloEscolar.Activo);

        if (d.HasValue)
        {
            aq = aq.Where(x =>
                x.DocenteId == d.Value
            );
        }

        m.Asignaciones = await aq
            .OrderBy(x => x.Curso.Nombre)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto =
                    x.Curso.Nombre
                    + " · "
                    + x.Seccion.Grado.Nombre
                    + " "
                    + x.Seccion.Nombre
                    + " · "
                    + x.Seccion.CicloEscolar.Anio
            })
            .ToListAsync();

        var uq = c.UnidadesAsignaciones
            .AsNoTracking()
            .Where(x =>
                x.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Asignacion.Seccion.CicloEscolar.Activo);

        if (d.HasValue)
        {
            uq = uq.Where(x =>
                x.Asignacion.DocenteId == d.Value
            );
        }

        m.Unidades = await uq
            .OrderBy(x => x.Asignacion.Curso.Nombre)
            .ThenBy(x => x.Orden)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto =
                    x.Asignacion.Curso.Nombre
                    + " · "
                    + x.Orden
                    + ". "
                    + x.Unidad.Titulo
            })
            .ToListAsync();
    }

    public async Task<ResultadoOperacion<int>> CrearAsync(
        CrearTarea m,
        CancellationToken ct)
    {
        if (m.TokenCreacion == Guid.Empty)
        {
            return ResultadoOperacion<int>.Validacion(
                nameof(m.TokenCreacion),
                "No fue posible identificar esta solicitud. Recargue el formulario e intente nuevamente."
            );
        }

        var v = await Val(m);

        if (v != null)
        {
            return ConvertirError<int>(v);
        }

        var uid = acceso.UsuarioId;

        if (uid == null)
        {
            return ResultadoOperacion<int>.Error(
                "No se pudo identificar al usuario."
            );
        }

        var tareaExistenteId = await BuscarCreacionExistenteAsync(
            m.TokenCreacion,
            uid,
            m.Tipo,
            ct);

        if (tareaExistenteId.HasValue)
        {
            return ResultadoOperacion<int>.Correcto(
                tareaExistenteId.Value,
                MensajeCreacion(m.Tipo)
            );
        }

        ArchivoGuardado? g = null;

        if (m.Archivo != null)
        {
            var r = await fisico.GuardarAsync(
                m.Archivo,
                "tareas",
                ct
            );

            if (!r.Exitoso || r.Datos == null)
            {
                return new ResultadoOperacion<int>
                {
                    Exitoso = false,
                    Mensaje = r.Mensaje,
                    Errores = r.Errores
                };
            }

            g = r.Datos;
        }

        await using var tx =
            await c.Database.BeginTransactionAsync(ct);

        try
        {
            var t = new Tarea
            {
                AsignacionId = m.AsignacionId,
                UnidadAsignacionId = m.UnidadAsignacionId,
                Tipo = m.Tipo,
                Titulo = m.Titulo.Trim(),
                Instrucciones = Lim(m.Instrucciones),
                Estado = EstadoTarea.Borrador,
                PunteoMaximo = m.PunteoMaximo,
                FechaDisponibilidad = HoraGuatemala.ParaGuardar(m.FechaDisponibilidad),
                FechaLimite = HoraGuatemala.ParaGuardar(m.FechaLimite),
                PermitirEntregaTardia = m.PermitirEntregaTardia,
                MaximoIntentosEntrega = MaximoIntentosEntrega(m),
                CriterioCalificacionEntrega = CriterioCalificacionEntrega(m),
                FechaCreacion = DateTime.UtcNow,
                CreadoPorUsuarioId = uid,
                GrupoVersionId = m.TokenCreacion,
                NumeroVersion = 1
            };

            c.Tareas.Add(t);

            if (m.Tipo == TipoTarea.Cuestionario)
            {
                c.Cuestionarios.Add(new Cuestionario
                {
                    Tarea = t,
                    Origen = OrigenCuestionario.Manual
                });
            }

            await c.SaveChangesAsync(ct);

            if (g != null)
            {
                var ar = new Archivo
                {
                    NombreOriginal = g.NombreOriginal,
                    ClaveAlmacenamiento = g.ClaveAlmacenamiento,
                    TipoMime = g.TipoMime,
                    TamanoBytes = g.TamanoBytes,
                    HashSha256 = g.HashSha256,
                    SubidoPorUsuarioId = uid,
                    FechaCreacion = DateTime.UtcNow
                };

                c.Archivos.Add(ar);

                await c.SaveChangesAsync(ct);

                c.TareasArchivos.Add(
                    new TareaArchivo
                    {
                        TareaId = t.Id,
                        ArchivoId = ar.Id
                    }
                );

                await c.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);

            return ResultadoOperacion<int>.Correcto(
                t.Id,
                MensajeCreacion(m.Tipo)
            );
        }
        catch
        {
            await tx.RollbackAsync(ct);

            if (g != null)
            {
                await fisico.EliminarAsync(
                    g.ClaveAlmacenamiento,
                    ct
                );
            }

            c.ChangeTracker.Clear();
            await tx.DisposeAsync();

            tareaExistenteId = await BuscarCreacionExistenteAsync(
                m.TokenCreacion,
                uid,
                m.Tipo,
                ct);

            if (tareaExistenteId.HasValue)
            {
                return ResultadoOperacion<int>.Correcto(
                    tareaExistenteId.Value,
                    MensajeCreacion(m.Tipo)
                );
            }

            throw;
        }
    }

    public async Task<EditarTarea?> ObtenerEditarAsync(int id)
    {
        var q = await Q();

        var m = await q
            .Where(x =>
                x.Id == id &&
                (x.Estado == EstadoTarea.Borrador ||
                 x.Estado == EstadoTarea.Publicada)
            )
            .Select(x => new EditarTarea
            {
                Id = x.Id,
                EstadoActual = x.Estado,
                TieneRegistrosAcademicos = x.Entregas.Any() ||
                    (x.Cuestionario != null && x.Cuestionario.Intentos.Any()),
                TieneReaperturas = x.Reaperturas.Any() ||
                    x.Entregas.Any(e => e.Reaperturas.Any()),
                AsignacionId = x.AsignacionId,
                UnidadAsignacionId = x.UnidadAsignacionId,
                Tipo = x.Tipo,
                Titulo = x.Titulo,
                Instrucciones = x.Instrucciones,
                PunteoMaximo = x.PunteoMaximo,
                MaximoIntentosEntrega = x.MaximoIntentosEntrega,
                CriterioCalificacionEntrega = x.CriterioCalificacionEntrega ??
                    CriterioCalificacionIntentos.UltimoIntento,
                FechaDisponibilidad = x.FechaDisponibilidad,
                FechaLimite = x.FechaLimite,
                PermitirEntregaTardia = x.PermitirEntregaTardia
            })
            .FirstOrDefaultAsync();

        if (m != null)
        {
            m.FechaDisponibilidad = HoraGuatemala.ParaFormulario(m.FechaDisponibilidad);
            m.FechaLimite = HoraGuatemala.ParaFormulario(m.FechaLimite);
            await PrepararAsync(m);
        }

        return m;
    }

    public async Task<ResultadoOperacion> EditarAsync(
        EditarTarea m,
        CancellationToken ct)
    {
        var t = await c.Tareas
            .FirstOrDefaultAsync(
                x =>
                    x.Id == m.Id &&
                    (x.Estado == EstadoTarea.Borrador ||
                     x.Estado == EstadoTarea.Publicada),
                ct
            );

        if (t == null)
        {
            return ResultadoOperacion.Error(
                "La tarea no existe o no puede editarse en su estado actual."
            );
        }

        if (!await Puede(t.AsignacionId) ||
            !await Puede(m.AsignacionId))
        {
            return ResultadoOperacion.Error(
                "No tiene acceso a la tarea."
            );
        }

        if (m.Tipo != t.Tipo)
        {
            return ResultadoOperacion.Validacion(nameof(m.Tipo),
                "El tipo de una tarea creada no puede modificarse.");
        }

        var v = await Val(m);

        if (v != null)
        {
            return v;
        }

        if (t.Estado == EstadoTarea.Publicada)
        {
            var restriccion = await ValidarEdicionPublicadaAsync(t, m, ct);
            if (restriccion is not null)
                return restriccion;
        }

        ArchivoGuardado? g = null;

        if (m.Archivo != null)
        {
            var r = await fisico.GuardarAsync(
                m.Archivo,
                "tareas",
                ct
            );

            if (!r.Exitoso || r.Datos == null)
            {
                return new ResultadoOperacion
                {
                    Exitoso = false,
                    Mensaje = r.Mensaje,
                    Errores = r.Errores
                };
            }

            g = r.Datos;
        }

        await using var tx =
            await c.Database.BeginTransactionAsync(ct);

        try
        {
            t.AsignacionId = m.AsignacionId;
            t.UnidadAsignacionId = m.UnidadAsignacionId;
            t.Titulo = m.Titulo.Trim();
            t.Instrucciones = Lim(m.Instrucciones);
            t.PunteoMaximo = m.PunteoMaximo;
            t.FechaDisponibilidad = HoraGuatemala.ParaGuardar(m.FechaDisponibilidad);
            t.FechaLimite = HoraGuatemala.ParaGuardar(m.FechaLimite);
            t.PermitirEntregaTardia = m.PermitirEntregaTardia;
            t.MaximoIntentosEntrega = MaximoIntentosEntrega(m);
            t.CriterioCalificacionEntrega = t.CriterioCalificacionEntrega is null &&
                m.CriterioCalificacionEntrega == CriterioCalificacionIntentos.UltimoIntento
                    ? null
                    : CriterioCalificacionEntrega(m);

            if (g != null)
            {
                var ar = new Archivo
                {
                    NombreOriginal = g.NombreOriginal,
                    ClaveAlmacenamiento = g.ClaveAlmacenamiento,
                    TipoMime = g.TipoMime,
                    TamanoBytes = g.TamanoBytes,
                    HashSha256 = g.HashSha256,
                    SubidoPorUsuarioId = acceso.UsuarioId!,
                    FechaCreacion = DateTime.UtcNow
                };

                c.Archivos.Add(ar);

                await c.SaveChangesAsync(ct);

                c.TareasArchivos.Add(
                    new TareaArchivo
                    {
                        TareaId = t.Id,
                        ArchivoId = ar.Id
                    }
                );
            }

            await c.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return ResultadoOperacion.Correcto(
                "La tarea fue actualizada correctamente."
            );
        }
        catch
        {
            await tx.RollbackAsync(ct);

            if (g != null)
            {
                await fisico.EliminarAsync(
                    g.ClaveAlmacenamiento,
                    ct
                );
            }

            throw;
        }
    }

    public async Task<ResultadoOperacion> PublicarAsync(int id)
    {
        var t = await c.Tareas
            .Include(x => x.Asignacion)
            .ThenInclude(x => x.Seccion)
            .FirstOrDefaultAsync(x =>
                x.Id == id
            );

        if (t == null)
        {
            return ResultadoOperacion.Error(
                "La tarea no existe."
            );
        }

        if (!await Puede(t.AsignacionId))
        {
            return ResultadoOperacion.Error(
                "No tiene acceso a la tarea."
            );
        }

        if (t.Estado != EstadoTarea.Borrador)
        {
            return ResultadoOperacion.Error(
                "Solo puede publicarse una tarea en borrador."
            );
        }

        if (
            t.Tipo == TipoTarea.Cuestionario &&
            !await CuestionarioListoParaPublicarAsync(id)
        )
        {
            return ResultadoOperacion.Error(
                "Debe completar la configuración del cuestionario y agregar al menos una pregunta válida antes de publicar la tarea."
            );
        }

        var alumnos = await c.Inscripciones
            .Where(i =>
                i.SeccionId == t.Asignacion.SeccionId &&
                i.CicloEscolarId == t.Asignacion.Seccion.CicloEscolarId &&
                i.Estado == EstadoInscripcion.Activa &&
                i.Seccion.CicloEscolar.Activo
            )
            .Select(i => new { i.AlumnoId, InscripcionId = i.Id })
            .ToListAsync();

        if (alumnos.Count == 0)
        {
            return ResultadoOperacion.Error(
                "No existen alumnos inscritos en la sección."
            );
        }

        await using var tx =
            await c.Database.BeginTransactionAsync();

        try
        {
            var existentes = await c.Entregas
                .Where(e => e.TareaId == id)
                .Where(e => e.InscripcionId.HasValue)
                .Select(e => e.InscripcionId!.Value)
                .ToListAsync();

            foreach (var alumno in alumnos.Where(x => !existentes.Contains(x.InscripcionId)))
            {
                c.Entregas.Add(
                    new Entrega
                    {
                        TareaId = id,
                        AlumnoId = alumno.AlumnoId,
                        InscripcionId = alumno.InscripcionId,
                        NumeroEnvio = 1,
                        Estado = EstadoEntrega.Pendiente,
                        FechaEntrega = null
                    }
                );
            }

            t.Estado = EstadoTarea.Publicada;
            t.FechaPublicacion = DateTime.UtcNow;

            await c.SaveChangesAsync();
            await tx.CommitAsync();

            return ResultadoOperacion.Correcto(
                $"La tarea fue publicada para {alumnos.Count} alumno(s)."
            );
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultadoOperacion> CerrarAsync(int id)
    {
        return await CerrarAsync(new CerrarTarea { Id = id });
    }

    public async Task<CerrarTarea?> ObtenerParaCerrarAsync(int id)
    {
        var tarea = await c.Tareas.AsNoTracking()
            .Where(x => x.Id == id && x.Estado == EstadoTarea.Publicada)
            .Select(x => new { x.Id, x.AsignacionId, x.Titulo, x.Tipo,
                Curso = x.Asignacion.Curso.Nombre })
            .FirstOrDefaultAsync();
        if (tarea is null || !await Puede(tarea.AsignacionId))
            return null;

        return new CerrarTarea
        {
            Id = tarea.Id,
            Titulo = tarea.Titulo,
            Curso = tarea.Curso,
            Tipo = tarea.Tipo,
            AlumnosSinActividad = await ConsultaSinActividad(tarea.Id).CountAsync(),
            PuedeCalificarAutomaticamente =
                acceso.TienePermiso(Permisos.Entregas.Calificar)
        };
    }

    public async Task<ResultadoOperacion> CerrarAsync(CerrarTarea modelo)
    {
        if (modelo.CalificarSinActividad &&
            !acceso.TienePermiso(Permisos.Entregas.Calificar))
        {
            return ResultadoOperacion.Error(
                "No tiene permiso para calificar entregas automáticamente.");
        }

        await using var tx = await c.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        try
        {
            var tarea = await c.Tareas.Include(x => x.Asignacion)
                .ThenInclude(x => x.Seccion).ThenInclude(x => x.CicloEscolar)
                .FirstOrDefaultAsync(x => x.Id == modelo.Id);
            if (tarea is null || tarea.Estado != EstadoTarea.Publicada ||
                !tarea.Asignacion.Seccion.CicloEscolar.Activo ||
                !await Puede(tarea.AsignacionId))
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error(
                    "La tarea no existe o ya no puede cerrarse en su contexto académico.");
            }

            var afectados = 0;
            if (modelo.CalificarSinActividad)
            {
                if (string.IsNullOrWhiteSpace(acceso.UsuarioId))
                {
                    await tx.RollbackAsync();
                    return ResultadoOperacion.Error(
                        "No se pudo identificar al usuario que califica la actividad.");
                }

                var pendientes = await ConsultaSinActividad(tarea.Id).ToListAsync();
                var ahora = DateTime.UtcNow;
                foreach (var entrega in pendientes)
                {
                    entrega.Calificacion = 0;
                    entrega.FechaCalificacion = ahora;
                    entrega.CalificadoPorUsuarioId = acceso.UsuarioId;
                    entrega.Estado = EstadoEntrega.Calificada;
                    entrega.OrigenCalificacionAdministrativa =
                        OrigenCalificacionAdministrativa.AutomaticaAlCerrar;
                    c.HistorialesCalificaciones.Add(new HistorialCalificacion
                    {
                        TipoOrigen = TipoOrigenHistorialCalificacion.Entrega,
                        Entrega = entrega,
                        NotaAnterior = null,
                        NotaNueva = 0,
                        Motivo = "Calificación administrativa automática al cerrar la tarea sin actividad registrada.",
                        FechaCambio = ahora,
                        ModificadoPorUsuarioId = acceso.UsuarioId
                    });
                    afectados++;
                }
            }

            tarea.Estado = EstadoTarea.Cerrada;
            tarea.FechaCierre = DateTime.UtcNow;
            await c.SaveChangesAsync();
            await tx.CommitAsync();
            return ResultadoOperacion.Correcto(modelo.CalificarSinActividad
                ? $"La tarea fue cerrada y {afectados} alumno(s) sin actividad fueron calificados con cero."
                : "La tarea fue cerrada y se conservaron las entregas pendientes.");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private IQueryable<Entrega> ConsultaSinActividad(int tareaId) =>
        c.Entregas.Where(x => x.TareaId == tareaId &&
            x.Estado == EstadoEntrega.Pendiente && !x.FechaEntrega.HasValue &&
            !x.Calificacion.HasValue &&
            x.Inscripcion != null && x.Inscripcion.Estado == EstadoInscripcion.Activa &&
            x.Inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
            x.Inscripcion.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
            x.Inscripcion.Seccion.CicloEscolar.Activo &&
            !c.Entregas.Any(otra => otra.TareaId == x.TareaId &&
                otra.AlumnoId == x.AlumnoId && otra.InscripcionId == x.InscripcionId &&
                otra.Id != x.Id &&
                (otra.FechaEntrega.HasValue || otra.Calificacion.HasValue)) &&
            !c.IntentosCuestionarios.Any(intento =>
                intento.Cuestionario.TareaId == x.TareaId &&
                intento.AlumnoId == x.AlumnoId &&
                intento.InscripcionId == x.InscripcionId));

    public async Task<ReabrirTarea?> ObtenerParaReabrirAsync(int id)
    {
        var tarea = await c.Tareas.AsNoTracking()
            .Where(x => x.Id == id && x.Estado != EstadoTarea.Borrador)
            .Select(x => new
            {
                x.Id,
                x.Titulo,
                x.Tipo,
                x.Estado,
                x.AsignacionId,
                Curso = x.Asignacion.Curso.Nombre
            })
            .FirstOrDefaultAsync();

        if (tarea is null || !await PuedeReabrirAsync(tarea.AsignacionId))
            return null;

        return new ReabrirTarea
        {
            Id = tarea.Id,
            Titulo = tarea.Titulo,
            Curso = tarea.Curso,
            Tipo = tarea.Tipo,
            EstadoActual = tarea.Estado,
            FechaLimite = HoraGuatemala.ParaFormulario(DateTime.UtcNow.AddDays(2)),
            TokenSolicitud = Guid.NewGuid()
        };
    }

    public async Task<ResultadoOperacion> ReabrirAsync(ReabrirTarea modelo)
    {
        if (string.IsNullOrWhiteSpace(acceso.UsuarioId))
            return ResultadoOperacion.Error("No tiene autorización para reabrir esta tarea.");

        if (modelo.TokenSolicitud == Guid.Empty)
            return ResultadoOperacion.Validacion(nameof(modelo.TokenSolicitud), "La solicitud de reapertura no es válida.");

        if (!Enum.IsDefined(modelo.Alcance))
            return ResultadoOperacion.Validacion(nameof(modelo.Alcance),
                "Seleccione un alcance de reapertura válido.");

        var motivo = NormalizarMotivo(modelo.Motivo);
        if (motivo is null)
            return ResultadoOperacion.Validacion(nameof(modelo.Motivo),
                "El motivo debe contener entre 5 y 500 caracteres.");

        var fechaLimite = HoraGuatemala.ParaGuardar(modelo.FechaLimite);
        if (fechaLimite <= DateTime.UtcNow)
            return ResultadoOperacion.Validacion(nameof(modelo.FechaLimite), "La nueva fecha límite debe ser posterior al momento actual.");

        await using var tx = await c.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var previa = await c.ReaperturasTareas.AsNoTracking()
                .FirstOrDefaultAsync(x => x.TareaId == modelo.Id && x.TokenSolicitud == modelo.TokenSolicitud);
            if (previa is not null)
            {
                await tx.CommitAsync();
                return ResultadoOperacion.Correcto("La reapertura ya había sido registrada.");
            }

            var tarea = await c.Tareas
                .Include(x => x.Asignacion).ThenInclude(x => x.Seccion).ThenInclude(x => x.CicloEscolar)
                .FirstOrDefaultAsync(x => x.Id == modelo.Id);
            if (tarea is null || tarea.Estado == EstadoTarea.Borrador ||
                !tarea.Asignacion.Seccion.CicloEscolar.Activo ||
                !await PuedeReabrirAsync(tarea.AsignacionId))
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error("La tarea no existe o no está dentro de su contexto académico autorizado.");
            }

            if (tarea.Tipo == TipoTarea.Cuestionario && !await CuestionarioListoParaPublicarAsync(tarea.Id))
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error("El cuestionario no está completo y no puede reabrirse.");
            }

            var alumnos = await c.Inscripciones
                .Where(x => x.SeccionId == tarea.Asignacion.SeccionId &&
                    x.CicloEscolarId == tarea.Asignacion.Seccion.CicloEscolarId &&
                    x.Estado == EstadoInscripcion.Activa)
                .Select(x => new { x.Id, x.AlumnoId })
                .ToListAsync();
            if (alumnos.Count == 0)
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error("No existen alumnos inscritos para reabrir la actividad.");
            }

            var reapertura = new ReaperturaTarea
            {
                TareaId = tarea.Id,
                FechaReapertura = DateTime.UtcNow,
                FechaLimite = fechaLimite,
                Alcance = modelo.Alcance,
                Motivo = motivo,
                ReabiertaPorUsuarioId = acceso.UsuarioId,
                TokenSolicitud = modelo.TokenSolicitud
            };
            c.ReaperturasTareas.Add(reapertura);

            foreach (var alumno in alumnos)
            {
                var ultima = await c.Entregas
                    .Where(x => x.TareaId == tarea.Id && x.AlumnoId == alumno.AlumnoId && x.InscripcionId == alumno.Id)
                    .OrderByDescending(x => x.NumeroEnvio).FirstOrDefaultAsync();
                if (ultima is null)
                    continue;

                var pendiente = ultima.Estado == EstadoEntrega.Pendiente && !ultima.FechaEntrega.HasValue;
                if (modelo.Alcance == AlcanceReaperturaTarea.SoloPendientes && !pendiente)
                    continue;

                Entrega destino;
                var oportunidadAdicional = !pendiente;
                if (pendiente)
                {
                    destino = ultima;
                    destino.FechaLimiteIndividual = fechaLimite;
                }
                else
                {
                    destino = new Entrega
                    {
                        TareaId = tarea.Id,
                        AlumnoId = alumno.AlumnoId,
                        InscripcionId = alumno.Id,
                        NumeroEnvio = ultima.NumeroEnvio + 1,
                        Estado = EstadoEntrega.Pendiente,
                        FechaLimiteIndividual = fechaLimite
                    };
                    c.Entregas.Add(destino);
                    await c.SaveChangesAsync();
                }

                // Toda extensión queda auditada. Sólo las que siguen a una entrega
                // terminada habilitan una oportunidad extraordinaria.
                var individual = new ReaperturaEntrega
                {
                    Entrega = destino,
                    FechaReapertura = reapertura.FechaReapertura,
                    FechaLimite = fechaLimite,
                    AutorizaOportunidadAdicional = oportunidadAdicional,
                    Motivo = reapertura.Motivo,
                    ReabiertaPorUsuarioId = acceso.UsuarioId
                };
                c.ReaperturasEntregas.Add(individual);
                reapertura.Alumnos.Add(new ReaperturaTareaAlumno
                {
                    AlumnoId = alumno.AlumnoId,
                    Entrega = destino,
                    OportunidadAdicional = oportunidadAdicional
                });
            }

            tarea.FechaLimite = fechaLimite;
            tarea.Estado = EstadoTarea.Publicada;
            tarea.FechaCierre = null;
            await c.SaveChangesAsync();
            await tx.CommitAsync();
            return ResultadoOperacion.Correcto("La tarea fue reabierta y las autorizaciones fueron registradas.");
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            return ResultadoOperacion.Error("La reapertura se procesó simultáneamente. Verifique el listado antes de intentarlo de nuevo.");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<ResultadoOperacion?> ValidarEdicionPublicadaAsync(
        Tarea tarea,
        EditarTarea modelo,
        CancellationToken ct)
    {
        // Cambiar contexto o tipo puede dejar cuestionarios, adjuntos y
        // registros históricos sin una interpretación académica válida.
        if (modelo.AsignacionId != tarea.AsignacionId ||
            modelo.UnidadAsignacionId != tarea.UnidadAsignacionId ||
            modelo.Tipo != tarea.Tipo)
        {
            return ResultadoOperacion.Error(
                "Una tarea publicada no puede cambiar de asignación, unidad ni tipo.");
        }

        var tieneRegistros = await c.Entregas.AnyAsync(x => x.TareaId == tarea.Id, ct) ||
            await c.IntentosCuestionarios.AnyAsync(x => x.Cuestionario.TareaId == tarea.Id, ct);

        if (tieneRegistros &&
            (modelo.PunteoMaximo != tarea.PunteoMaximo ||
             modelo.MaximoIntentosEntrega != tarea.MaximoIntentosEntrega ||
             modelo.CriterioCalificacionEntrega !=
                 (tarea.CriterioCalificacionEntrega ?? CriterioCalificacionIntentos.UltimoIntento) ||
             modelo.PermitirEntregaTardia != tarea.PermitirEntregaTardia ||
             modelo.Archivo is not null))
        {
            return ResultadoOperacion.Error(
                "No puede modificar punteo, intentos, entrega tardía ni adjuntos porque la tarea ya tiene entregas o intentos registrados.");
        }

        var disponibilidad = HoraGuatemala.ParaGuardar(modelo.FechaDisponibilidad);
        var limite = HoraGuatemala.ParaGuardar(modelo.FechaLimite);
        var cambiaFechas = disponibilidad != tarea.FechaDisponibilidad ||
            limite != tarea.FechaLimite;
        if (cambiaFechas &&
            (await c.ReaperturasTareas.AnyAsync(x => x.TareaId == tarea.Id, ct) ||
             await c.ReaperturasEntregas.AnyAsync(x => x.Entrega.TareaId == tarea.Id, ct)))
        {
            return ResultadoOperacion.Error(
                "No puede cambiar las fechas generales mientras existan reaperturas registradas; use la operación de reapertura correspondiente.");
        }

        return null;
    }

    private async Task<ResultadoOperacion?> Val(
        FormularioTarea m)
    {
        if (!Enum.IsDefined(m.Tipo))
        {
            return ResultadoOperacion.Validacion(
                nameof(m.Tipo),
                "Seleccione un tipo de tarea válido."
            );
        }

        if (m.Tipo == TipoTarea.Cuestionario && m.Archivo != null)
        {
            return ResultadoOperacion.Validacion(
                nameof(m.Archivo),
                "Los cuestionarios no admiten archivos adjuntos."
            );
        }

        if (m.Tipo == TipoTarea.Archivo &&
            m.MaximoIntentosEntrega is not null and not (1 or 2 or 3))
        {
            return ResultadoOperacion.Validacion(
                nameof(m.MaximoIntentosEntrega),
                "Seleccione 1, 2, 3 o intentos ilimitados."
            );
        }

        if (m.Tipo == TipoTarea.Archivo && m.MaximoIntentosEntrega != 1 &&
            !Enum.IsDefined(m.CriterioCalificacionEntrega))
        {
            return ResultadoOperacion.Validacion(
                nameof(m.CriterioCalificacionEntrega),
                "Seleccione un criterio de calificación válido.");
        }

        if (!await Puede(m.AsignacionId))
        {
            return ResultadoOperacion.Validacion(
                nameof(m.AsignacionId),
                "Seleccione una asignación válida."
            );
        }

        if (
            m.FechaDisponibilidad.HasValue &&
            m.FechaLimite.HasValue &&
            m.FechaDisponibilidad > m.FechaLimite
        )
        {
            return ResultadoOperacion.Validacion(
                nameof(m.FechaLimite),
                "La fecha límite debe ser posterior a la disponibilidad."
            );
        }

        if (
            m.UnidadAsignacionId.HasValue &&
            !await c.UnidadesAsignaciones.AnyAsync(x =>
                x.Id == m.UnidadAsignacionId &&
                x.AsignacionId == m.AsignacionId
            )
        )
        {
            return ResultadoOperacion.Validacion(
                nameof(m.UnidadAsignacionId),
                "La unidad no pertenece a la asignación."
            );
        }

        return null;
    }

    private async Task<bool> Puede(int id)
        => await acceso.PuedeGestionarAsignacionVigenteAsync(id);

    private async Task<bool> PuedeReabrirAsync(int asignacionId)
    {
        if (await acceso.PuedeGestionarAsignacionVigenteAsync(asignacionId))
            return true;

        var docenteId = await acceso.ObtenerDocenteIdAsync();
        return docenteId.HasValue && delegaciones is not null &&
            await delegaciones.PuedeActuarAsync(asignacionId, docenteId.Value,
                OperacionDelegacionAcademica.ReabrirEntregas);
    }

    private static string? NormalizarMotivo(string? motivo)
    {
        var normalizado = string.Join(' ', (motivo ?? string.Empty)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalizado.Length is >= 5 and <= 500 ? normalizado : null;
    }

    private static int? MaximoIntentosEntrega(FormularioTarea modelo) =>
        modelo.Tipo == TipoTarea.Archivo
            ? modelo.MaximoIntentosEntrega
            : null;

    private static CriterioCalificacionIntentos? CriterioCalificacionEntrega(
        FormularioTarea modelo) =>
        modelo.Tipo == TipoTarea.Archivo
            ? modelo.MaximoIntentosEntrega == 1
                ? CriterioCalificacionIntentos.MejorNota
                : modelo.CriterioCalificacionEntrega
            : null;

    private async Task<int?> BuscarCreacionExistenteAsync(
        Guid tokenCreacion,
        string usuarioId,
        TipoTarea tipo,
        CancellationToken ct) =>
        await c.Tareas
            .AsNoTracking()
            .Where(x =>
                x.GrupoVersionId == tokenCreacion &&
                x.NumeroVersion == 1 &&
                x.CreadoPorUsuarioId == usuarioId &&
                x.Tipo == tipo)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(ct);

    private Task<bool> CuestionarioListoParaPublicarAsync(int tareaId) =>
        c.Cuestionarios.AnyAsync(cuestionario =>
            cuestionario.TareaId == tareaId &&
            cuestionario.MaximoIntentos >= 0 &&
            (
                cuestionario.ModalidadTiempo == ModalidadTiempoCuestionario.SinLimite ||
                cuestionario.ModalidadTiempo == ModalidadTiempoCuestionario.TiempoGeneral &&
                cuestionario.TiempoGeneralSegundos > 0 ||
                cuestionario.ModalidadTiempo == ModalidadTiempoCuestionario.TiempoPorPregunta &&
                cuestionario.TiempoPorPreguntaSegundos > 0
            ) &&
            cuestionario.Preguntas.Any() &&
            cuestionario.Preguntas.All(pregunta =>
                pregunta.Punteo > 0 &&
                (
                    pregunta.Tipo == TipoPregunta.TextoCorto &&
                    pregunta.RespuestasAceptadas.Any() ||
                    (
                        pregunta.Tipo == TipoPregunta.SeleccionUnica ||
                        pregunta.Tipo == TipoPregunta.SeleccionMultiple ||
                        pregunta.Tipo == TipoPregunta.VerdaderoFalso
                    ) &&
                    pregunta.Opciones.Count >= 2 &&
                    pregunta.Opciones.Any(opcion => opcion.EsCorrecta) &&
                    (
                        pregunta.Tipo == TipoPregunta.SeleccionMultiple ||
                        pregunta.Opciones.Count(opcion => opcion.EsCorrecta) == 1
                    )
                )));

    private static string MensajeCreacion(TipoTarea tipo) =>
        tipo == TipoTarea.Cuestionario
            ? "La tarea fue guardada como borrador. Continúe con la configuración del cuestionario."
            : "La tarea fue guardada como borrador.";

    private static ResultadoOperacion<T> ConvertirError<T>(ResultadoOperacion resultado) =>
        new()
        {
            Exitoso = false,
            Mensaje = resultado.Mensaje,
            Errores = resultado.Errores
        };

    private static string? Lim(string? x) =>
        string.IsNullOrWhiteSpace(x)
            ? null
            : x.Trim();
}
