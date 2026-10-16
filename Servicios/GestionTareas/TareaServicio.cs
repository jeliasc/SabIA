using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.Tareas;

namespace Proyecto_Final.Servicios.GestionTareas;

public sealed class TareaServicio(
    Contexto c,
    IArchivoFisicoServicio fisico,
    IAccesoAcademicoServicio acceso) : ITareaServicio
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
                x.Asignacion.DocenteId == d.Value &&
                (historial
                    ? x.Asignacion.Estado != EstadoRegistro.Activo ||
                      !x.Asignacion.Seccion.CicloEscolar.Activo
                    : x.Asignacion.Estado == EstadoRegistro.Activo &&
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
                    !(x.Asignacion.Estado == EstadoRegistro.Activo &&
                      x.Asignacion.Seccion.CicloEscolar.Activo &&
                      c.Inscripciones.Any(i =>
                          i.AlumnoId == a.Value &&
                          i.SeccionId == x.Asignacion.SeccionId &&
                          i.CicloEscolarId == x.Asignacion.Seccion.CicloEscolarId &&
                          i.Estado == EstadoInscripcion.Activa)))
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
                ? x.Asignacion.Estado != EstadoRegistro.Activo ||
                  !x.Asignacion.Seccion.CicloEscolar.Activo
                : x.Asignacion.Estado == EstadoRegistro.Activo &&
                  x.Asignacion.Seccion.CicloEscolar.Activo);
        }
        else
        {
            q = q.Where(_ => false);
        }

        return q;
    }

    public async Task<List<TareaLista>> ObtenerTodosAsync() =>
        await (await Q())
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
                Limite = x.FechaLimite,
                Pendientes = x.Entregas.Count(e =>
                    e.Estado == EstadoEntrega.Pendiente &&
                    !x.Entregas.Any(otra =>
                        otra.AlumnoId == e.AlumnoId &&
                        otra.NumeroEnvio > e.NumeroEnvio)
                ),
                Entregadas = x.Entregas.Count(e =>
                    e.Estado != EstadoEntrega.Pendiente &&
                    !x.Entregas.Any(otra =>
                        otra.AlumnoId == e.AlumnoId &&
                        otra.NumeroEnvio > e.NumeroEnvio)
                )
            })
            .ToListAsync();

    public async Task<List<TareaLista>> ObtenerHistorialAsync() =>
        await (await Q(historial: true))
            .OrderByDescending(x => x.FechaCreacion)
            .Select(x => new TareaLista
            {
                Id = x.Id,
                Titulo = x.Titulo,
                Curso = x.Asignacion.Curso.Nombre,
                Seccion = x.Asignacion.Seccion.Grado.Nombre + " " + x.Asignacion.Seccion.Nombre,
                Tipo = x.Tipo,
                Estado = x.Estado,
                Limite = x.FechaLimite,
                Pendientes = x.Entregas.Count(e => e.Estado == EstadoEntrega.Pendiente &&
                    !x.Entregas.Any(otra => otra.AlumnoId == e.AlumnoId && otra.NumeroEnvio > e.NumeroEnvio)),
                Entregadas = x.Entregas.Count(e => e.Estado != EstadoEntrega.Pendiente &&
                    !x.Entregas.Any(otra => otra.AlumnoId == e.AlumnoId && otra.NumeroEnvio > e.NumeroEnvio))
            })
            .ToListAsync();

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
                x.Estado == EstadoTarea.Borrador
            )
            .Select(x => new EditarTarea
            {
                Id = x.Id,
                AsignacionId = x.AsignacionId,
                UnidadAsignacionId = x.UnidadAsignacionId,
                Tipo = x.Tipo,
                Titulo = x.Titulo,
                Instrucciones = x.Instrucciones,
                PunteoMaximo = x.PunteoMaximo,
                MaximoIntentosEntrega = x.MaximoIntentosEntrega,
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
                    x.Estado == EstadoTarea.Borrador,
                ct
            );

        if (t == null)
        {
            return ResultadoOperacion.Error(
                "La tarea no existe o ya fue publicada."
            );
        }

        if (!await Puede(t.AsignacionId) ||
            !await Puede(m.AsignacionId))
        {
            return ResultadoOperacion.Error(
                "No tiene acceso a la tarea."
            );
        }

        var v = await Val(m);

        if (v != null)
        {
            return v;
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
            t.Tipo = m.Tipo;
            t.Titulo = m.Titulo.Trim();
            t.Instrucciones = Lim(m.Instrucciones);
            t.PunteoMaximo = m.PunteoMaximo;
            t.FechaDisponibilidad = HoraGuatemala.ParaGuardar(m.FechaDisponibilidad);
            t.FechaLimite = HoraGuatemala.ParaGuardar(m.FechaLimite);
            t.PermitirEntregaTardia = m.PermitirEntregaTardia;
            t.MaximoIntentosEntrega = MaximoIntentosEntrega(m);

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
        var t = await c.Tareas.FindAsync(id);

        if (t == null ||
            !await Puede(t.AsignacionId))
        {
            return ResultadoOperacion.Error(
                "La tarea no existe o no tiene acceso."
            );
        }

        await using var tx =
            await c.Database.BeginTransactionAsync();

        try
        {
            t.Estado = EstadoTarea.Cerrada;
            t.FechaCierre = DateTime.UtcNow;

            await c.SaveChangesAsync();
            await tx.CommitAsync();

            return ResultadoOperacion.Correcto(
                "La tarea fue cerrada."
            );
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
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

    private static int? MaximoIntentosEntrega(FormularioTarea modelo) =>
        modelo.Tipo == TipoTarea.Archivo
            ? modelo.MaximoIntentosEntrega
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
