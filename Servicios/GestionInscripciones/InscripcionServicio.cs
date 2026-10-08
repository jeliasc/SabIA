using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Academico;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.Inscripciones;

namespace Proyecto_Final.Servicios.GestionInscripciones;

public sealed class InscripcionServicio(
    Contexto contexto,
    IEntregaPendienteServicio entregaPendienteServicio,
    IAccesoAcademicoServicio acceso) : IInscripcionServicio
{
    public async Task<List<InscripcionLista>> ObtenerTodosAsync()
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var consulta = contexto.Inscripciones.AsNoTracking().AsQueryable();
        if (!acceso.EsOperadorInstitucional && perfil.DocenteId.HasValue)
        {
            consulta = consulta.Where(x =>
                x.Estado == EstadoInscripcion.Activa &&
                x.Seccion.CicloEscolar.Activo &&
                contexto.Asignaciones.Any(a =>
                    a.DocenteId == perfil.DocenteId.Value &&
                    a.SeccionId == x.SeccionId &&
                    a.Estado == EstadoRegistro.Activo));
        }
        else if (!acceso.EsOperadorInstitucional && perfil.AlumnoId.HasValue)
        {
            consulta = consulta.Where(x => x.AlumnoId == perfil.AlumnoId.Value);
        }

        return await consulta
            .OrderByDescending(x => x.CicloEscolarId)
            .ThenBy(x => x.Alumno.Usuario.PrimerNombre)
            .Select(x => new InscripcionLista
            {
                Id = x.Id,
                Alumno = x.Alumno.Usuario.PrimerNombre + " " +
                    x.Alumno.Usuario.PrimerApellido,
                Seccion = x.Seccion.Grado.Nombre + " " + x.Seccion.Nombre,
                Ciclo = x.Seccion.CicloEscolar.Anio,
                Fecha = x.Fecha,
                Estado = x.Estado
            })
            .ToListAsync();
    }

    public async Task PrepararAsync(FormularioInscripcion modelo)
    {
        if (!acceso.EsOperadorInstitucional)
        {
            modelo.Alumnos = [];
            modelo.Secciones = [];
            return;
        }
        modelo.Alumnos = await contexto.Alumnos
            .AsNoTracking()
            .Where(x => x.Usuario.Activo)
            .OrderBy(x => x.Usuario.PrimerNombre)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.Usuario.PrimerNombre + " " +
                    x.Usuario.PrimerApellido +
                    (x.CodigoPersonal == null ? "" : " · " + x.CodigoPersonal)
            })
            .ToListAsync();

        modelo.Secciones = await contexto.Secciones
            .AsNoTracking()
            .Where(x =>
                x.Estado == EstadoRegistro.Activo &&
                x.CicloEscolar.Activo)
            .OrderByDescending(x => x.CicloEscolar.Anio)
            .ThenBy(x => x.Grado.Orden)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.CicloEscolar.Anio + " · " +
                    x.Grado.Nombre + " " + x.Nombre
            })
            .ToListAsync();
    }

    public async Task<ResultadoOperacion> CrearAsync(CrearInscripcion modelo)
    {
        if (!acceso.EsOperadorInstitucional)
            return ResultadoOperacion.Error("No tiene autorización administrativa para gestionar inscripciones.");

        var seccion = await contexto.Secciones
            .AsNoTracking()
            .Include(x => x.CicloEscolar)
            .FirstOrDefaultAsync(x =>
                x.Id == modelo.SeccionId &&
                x.Estado == EstadoRegistro.Activo &&
                x.CicloEscolar.Activo);

        if (seccion == null)
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.SeccionId),
                "Seleccione una sección activa.");
        }

        if (modelo.Fecha < seccion.CicloEscolar.FechaInicio ||
            modelo.Fecha > seccion.CicloEscolar.FechaFin)
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.Fecha),
                "La fecha de inscripción debe pertenecer al ciclo escolar seleccionado.");
        }

        if (!await contexto.Alumnos.AnyAsync(x =>
            x.Id == modelo.AlumnoId && x.Usuario.Activo))
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.AlumnoId),
                "Seleccione un alumno activo.");
        }

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        try
        {
            if (!await contexto.Secciones.AnyAsync(x =>
                x.Id == seccion.Id &&
                x.Estado == EstadoRegistro.Activo &&
                x.CicloEscolar.Activo) ||
                !await contexto.Alumnos.AnyAsync(x =>
                    x.Id == modelo.AlumnoId &&
                    x.Usuario.Activo))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "El alumno, la sección o el ciclo dejaron de estar activos. Actualice el formulario e intente nuevamente.");
            }

            var estados = await contexto.Inscripciones
                .Where(x =>
                    x.AlumnoId == modelo.AlumnoId &&
                    x.CicloEscolarId == seccion.CicloEscolarId)
                .Select(x => x.Estado)
                .ToListAsync();

            if (estados.Contains(EstadoInscripcion.Activa))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Validacion(
                    nameof(modelo.AlumnoId),
                    "El alumno ya tiene una inscripción activa en ese ciclo. Utilice el traslado para cambiarlo de sección.");
            }

            if (estados.Any(estado => estado != EstadoInscripcion.Retirada))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Validacion(
                    nameof(modelo.AlumnoId),
                    "No es posible una reinscripción ordinaria porque existe una inscripción finalizada en ese ciclo.");
            }

            contexto.Inscripciones.Add(new Inscripcion
            {
                AlumnoId = modelo.AlumnoId,
                SeccionId = seccion.Id,
                CicloEscolarId = seccion.CicloEscolarId,
                Fecha = modelo.Fecha,
                Estado = EstadoInscripcion.Activa
            });

            await entregaPendienteServicio.CrearParaTareasDisponiblesAsync(
                modelo.AlumnoId,
                seccion.Id);

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion.Correcto(
                estados.Count == 0
                    ? "La inscripción fue registrada correctamente."
                    : "La reinscripción fue registrada correctamente y se conservó el historial anterior.");
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            contexto.ChangeTracker.Clear();

            if (await contexto.Inscripciones.AsNoTracking().AnyAsync(x =>
                x.AlumnoId == modelo.AlumnoId &&
                x.CicloEscolarId == seccion.CicloEscolarId &&
                x.Estado == EstadoInscripcion.Activa))
            {
                return ResultadoOperacion.Validacion(
                    nameof(modelo.AlumnoId),
                    "El alumno ya tiene una inscripción activa en ese ciclo.");
            }

            throw;
        }
        catch
        {
            await transaccion.RollbackAsync();
            contexto.ChangeTracker.Clear();

            if (await contexto.Inscripciones.AsNoTracking().AnyAsync(x =>
                x.AlumnoId == modelo.AlumnoId &&
                x.CicloEscolarId == seccion.CicloEscolarId &&
                x.Estado == EstadoInscripcion.Activa))
            {
                return ResultadoOperacion.Validacion(
                    nameof(modelo.AlumnoId),
                    "El alumno ya tiene una inscripción activa en ese ciclo.");
            }

            throw;
        }
    }

    public async Task<TrasladarInscripcion?> ObtenerTrasladoAsync(int id)
    {
        if (!acceso.EsOperadorInstitucional)
            return null;

        var inscripcion = await contexto.Inscripciones
            .AsNoTracking()
            .Include(x => x.Alumno)
            .ThenInclude(x => x.Usuario)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Estado == EstadoInscripcion.Activa &&
                x.Seccion.CicloEscolar.Activo);

        if (inscripcion == null)
        {
            return null;
        }

        return new TrasladarInscripcion
        {
            Id = inscripcion.Id,
            Alumno = inscripcion.Alumno.Usuario.PrimerNombre + " " +
                inscripcion.Alumno.Usuario.PrimerApellido,
            SeccionOrigenId = inscripcion.SeccionId,
            Secciones = await contexto.Secciones
                .AsNoTracking()
                .Where(x =>
                    x.CicloEscolarId == inscripcion.CicloEscolarId &&
                    x.Estado == EstadoRegistro.Activo &&
                    x.CicloEscolar.Activo &&
                    x.Id != inscripcion.SeccionId)
                .OrderBy(x => x.Grado.Orden)
                .ThenBy(x => x.Nombre)
                .Select(x => new OpcionSeleccion
                {
                    Id = x.Id,
                    Texto = x.Grado.Nombre + " " + x.Nombre
                })
                .ToListAsync()
        };
    }

    public async Task<ResultadoOperacion> TrasladarAsync(
        TrasladarInscripcion modelo,
        string usuarioId)
    {
        if (!acceso.EsOperadorInstitucional)
            return ResultadoOperacion.Error("No tiene autorización administrativa para trasladar inscripciones.");

        var inscripcion = await contexto.Inscripciones
            .FirstOrDefaultAsync(x =>
                x.Id == modelo.Id &&
                x.Estado == EstadoInscripcion.Activa &&
                x.Seccion.CicloEscolar.Activo);

        if (inscripcion == null)
        {
            return ResultadoOperacion.Error("La inscripción activa no existe.");
        }

        var destino = await contexto.Secciones
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == modelo.SeccionDestinoId &&
                x.CicloEscolarId == inscripcion.CicloEscolarId &&
                x.Estado == EstadoRegistro.Activo &&
                x.CicloEscolar.Activo);

        if (destino == null)
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.SeccionDestinoId),
                "La sección de destino no es válida para este ciclo.");
        }

        if (destino.Id == inscripcion.SeccionId)
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.SeccionDestinoId),
                "Seleccione una sección diferente.");
        }

        var origen = inscripcion.SeccionId;
        await using var transaccion = await contexto.Database.BeginTransactionAsync();

        try
        {
            inscripcion.SeccionId = destino.Id;
            contexto.HistorialTraslados.Add(new HistorialTraslado
            {
                InscripcionId = inscripcion.Id,
                CicloEscolarId = inscripcion.CicloEscolarId,
                SeccionOrigenId = origen,
                SeccionDestinoId = destino.Id,
                FechaTraslado = DateTime.UtcNow,
                Motivo = modelo.Motivo.Trim(),
                RealizadoPorUsuarioId = usuarioId
            });

            await entregaPendienteServicio.CrearParaTareasDisponiblesAsync(
                inscripcion.AlumnoId,
                destino.Id);

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto("El alumno fue trasladado correctamente.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultadoOperacion> CambiarEstadoAsync(int id, bool activa)
    {
        if (!acceso.EsOperadorInstitucional)
            return ResultadoOperacion.Error("No tiene autorización administrativa para cambiar inscripciones.");

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        try
        {
            var inscripcion = await contexto.Inscripciones.FindAsync(id);

            if (inscripcion == null)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error("La inscripción no existe.");
            }

            if (inscripcion.Estado == EstadoInscripcion.Finalizada)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "Una inscripción finalizada no puede reactivarse ni retirarse desde este flujo.");
            }

            if (activa && !await contexto.Secciones.AnyAsync(x =>
                x.Id == inscripcion.SeccionId &&
                x.CicloEscolarId == inscripcion.CicloEscolarId &&
                x.Estado == EstadoRegistro.Activo &&
                x.CicloEscolar.Activo))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "No puede reactivarse una inscripción cuya sección o ciclo no están activos.");
            }

            if (activa && await contexto.Inscripciones.AsNoTracking().AnyAsync(x =>
                x.Id != inscripcion.Id &&
                x.AlumnoId == inscripcion.AlumnoId &&
                x.CicloEscolarId == inscripcion.CicloEscolarId &&
                x.Estado == EstadoInscripcion.Activa))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "El alumno ya tiene otra inscripción activa en este ciclo.");
            }

            inscripcion.Estado = activa
                ? EstadoInscripcion.Activa
                : EstadoInscripcion.Retirada;

            if (activa)
            {
                await entregaPendienteServicio.CrearParaTareasDisponiblesAsync(
                    inscripcion.AlumnoId,
                    inscripcion.SeccionId);
            }

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion.Correcto(
                activa
                    ? "La inscripción fue reactivada."
                    : "La inscripción fue retirada.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            contexto.ChangeTracker.Clear();

            if (activa)
            {
                var actual = await contexto.Inscripciones
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new { x.AlumnoId, x.CicloEscolarId })
                    .FirstOrDefaultAsync();

                if (actual != null &&
                    await contexto.Inscripciones.AsNoTracking().AnyAsync(x =>
                        x.Id != id &&
                        x.AlumnoId == actual.AlumnoId &&
                        x.CicloEscolarId == actual.CicloEscolarId &&
                        x.Estado == EstadoInscripcion.Activa))
                {
                    return ResultadoOperacion.Error(
                        "No fue posible reactivar la inscripción porque ya existe otra inscripción activa en el ciclo.");
                }
            }

            throw;
        }
    }
}
