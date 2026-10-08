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
                    x.Asignacion.Estado == EstadoRegistro.Activo &&
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
        else
        {
            q = q.Where(x => historial
                ? x.Asignacion.Estado != EstadoRegistro.Activo ||
                  !x.Asignacion.Seccion.CicloEscolar.Activo
                : x.Asignacion.Estado == EstadoRegistro.Activo &&
                  x.Asignacion.Seccion.CicloEscolar.Activo);
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

        if (!d.HasValue && perfil.AlumnoId.HasValue && !acceso.EsOperadorInstitucional)
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

    public async Task<ResultadoOperacion> CrearAsync(
        CrearTarea m,
        CancellationToken ct)
    {
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

        var uid = acceso.UsuarioId;

        if (uid == null)
        {
            return ResultadoOperacion.Error(
                "No se pudo identificar al usuario."
            );
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
                FechaDisponibilidad = m.FechaDisponibilidad,
                FechaLimite = m.FechaLimite,
                PermitirEntregaTardia = m.PermitirEntregaTardia,
                FechaCreacion = DateTime.UtcNow,
                CreadoPorUsuarioId = uid,
                GrupoVersionId = Guid.NewGuid(),
                NumeroVersion = 1
            };

            c.Tareas.Add(t);

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

            return ResultadoOperacion.Correcto(
                "La tarea fue guardada como borrador."
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
                FechaDisponibilidad = x.FechaDisponibilidad,
                FechaLimite = x.FechaLimite,
                PermitirEntregaTardia = x.PermitirEntregaTardia
            })
            .FirstOrDefaultAsync();

        if (m != null)
        {
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
            t.FechaDisponibilidad = m.FechaDisponibilidad;
            t.FechaLimite = m.FechaLimite;
            t.PermitirEntregaTardia = m.PermitirEntregaTardia;

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
            !await c.Cuestionarios.AnyAsync(x =>
                x.TareaId == id &&
                x.Preguntas.Any()
            )
        )
        {
            return ResultadoOperacion.Error(
                "Debe configurar el cuestionario y agregar al menos una pregunta antes de publicar la tarea."
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

    private static string? Lim(string? x) =>
        string.IsNullOrWhiteSpace(x)
            ? null
            : x.Trim();
}
