using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Asignaciones;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.Servicios.GestionAsignaciones;

public sealed class AsignacionServicio(
    Contexto c,
    IAccesoAcademicoServicio acceso) : IAsignacionServicio
{
    public async Task<List<AsignacionLista>> ObtenerTodosAsync()
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var consulta = c.Asignaciones.AsNoTracking().AsQueryable();

        if (!acceso.EsOperadorInstitucional && perfil.DocenteId.HasValue)
        {
            consulta = consulta.Where(x =>
                x.DocenteId == perfil.DocenteId.Value &&
                x.Estado == EstadoRegistro.Activo &&
                x.Seccion.CicloEscolar.Activo);
        }
        else if (!acceso.EsOperadorInstitucional && perfil.AlumnoId.HasValue)
        {
            consulta = consulta.Where(_ => false);
        }

        return await consulta
            .OrderByDescending(x => x.Seccion.CicloEscolar.Anio)
            .ThenBy(x => x.Seccion.Grado.Orden)
            .ThenBy(x => x.Curso.Nombre)
            .Select(x => new AsignacionLista
            {
                Id = x.Id,
                Ciclo = x.Seccion.CicloEscolar.Anio.ToString(),
                Seccion = x.Seccion.Grado.Nombre + " " + x.Seccion.Nombre,
                Curso = x.Curso.Nombre,
                Docente = x.Docente.Usuario.PrimerNombre + " " + x.Docente.Usuario.PrimerApellido,
                Estado = x.Estado
            })
            .ToListAsync();
    }

    public async Task PrepararAsync(FormularioAsignacion m)
    {
        if (!acceso.EsOperadorInstitucional)
        {
            m.Secciones = [];
            m.Cursos = [];
            m.Docentes = [];
            return;
        }
        m.Secciones = await c.Secciones
            .AsNoTracking()
            .Where(x => x.Estado == EstadoRegistro.Activo && x.CicloEscolar.Activo)
            .OrderByDescending(x => x.CicloEscolar.Anio)
            .ThenBy(x => x.Grado.Orden)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.CicloEscolar.Anio
                    + " · "
                    + x.Grado.Nombre
                    + " "
                    + x.Nombre
                    + " · "
                    + x.Grado.Carrera
            })
            .ToListAsync();

        m.Cursos = await c.Cursos
            .AsNoTracking()
            .Where(x => x.Estado == EstadoRegistro.Activo)
            .OrderBy(x => x.Grado.Orden)
            .ThenBy(x => x.Nombre)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.Grado.Nombre + " · " + x.Nombre
            })
            .ToListAsync();

        m.Docentes = await c.Docentes
            .AsNoTracking()
            .Where(x => x.Usuario.Activo)
            .OrderBy(x => x.Usuario.PrimerNombre)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.Carnet
                    + " · "
                    + x.Usuario.PrimerNombre
                    + " "
                    + x.Usuario.PrimerApellido
            })
            .ToListAsync();
    }

    public async Task<ResultadoOperacion> CrearAsync(CrearAsignacion m)
    {
        if (!acceso.EsOperadorInstitucional)
            return ResultadoOperacion.Error("No tiene autorización administrativa para gestionar asignaciones.");

        var v = await Val(m, 0);

        if (v.r != null)
        {
            return v.r;
        }

        await using var tx = await c.Database.BeginTransactionAsync();

        try
        {
            c.Asignaciones.Add(new Asignacion
            {
                SeccionId = m.SeccionId,
                CursoId = m.CursoId,
                GradoId = v.grado,
                DocenteId = m.DocenteId,
                Estado = EstadoRegistro.Activo
            });

            await c.SaveChangesAsync();
            await tx.CommitAsync();

            return ResultadoOperacion.Correcto(
                "La asignación fue creada correctamente."
            );
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<EditarAsignacion?> ObtenerEditarAsync(int id)
    {
        if (!acceso.EsOperadorInstitucional)
            return null;

        var m = await c.Asignaciones
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new EditarAsignacion
            {
                Id = x.Id,
                SeccionId = x.SeccionId,
                CursoId = x.CursoId,
                DocenteId = x.DocenteId
            })
            .FirstOrDefaultAsync();

        if (m != null)
        {
            await PrepararAsync(m);
        }

        return m;
    }

    public async Task<ResultadoOperacion> EditarAsync(EditarAsignacion m)
    {
        if (!acceso.EsOperadorInstitucional)
            return ResultadoOperacion.Error("No tiene autorización administrativa para gestionar asignaciones.");

        var v = await Val(m, m.Id);

        if (v.r != null)
        {
            return v.r;
        }

        var x = await c.Asignaciones.FindAsync(m.Id);

        if (x == null)
        {
            return ResultadoOperacion.Error(
                "La asignación no existe."
            );
        }

        await using var tx = await c.Database.BeginTransactionAsync();

        try
        {
            x.SeccionId = m.SeccionId;
            x.CursoId = m.CursoId;
            x.GradoId = v.grado;
            x.DocenteId = m.DocenteId;

            await c.SaveChangesAsync();
            await tx.CommitAsync();

            return ResultadoOperacion.Correcto(
                "La asignación fue actualizada correctamente."
            );
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultadoOperacion> CambiarEstadoAsync(
        int id,
        bool activo)
    {
        if (!acceso.EsOperadorInstitucional)
            return ResultadoOperacion.Error("No tiene autorización administrativa para gestionar asignaciones.");

        var x = await c.Asignaciones.FindAsync(id);

        if (x == null)
        {
            return ResultadoOperacion.Error(
                "La asignación no existe."
            );
        }

        await using var tx = await c.Database.BeginTransactionAsync();

        try
        {
            x.Estado = activo
                ? EstadoRegistro.Activo
                : EstadoRegistro.Inactivo;

            await c.SaveChangesAsync();
            await tx.CommitAsync();

            return ResultadoOperacion.Correcto(
                activo
                    ? "La asignación fue activada."
                    : "La asignación fue desactivada."
            );
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<(ResultadoOperacion? r, int grado)> Val(
        FormularioAsignacion m,
        int id)
    {
        var s = await c.Secciones
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.Id == m.SeccionId &&
                    x.Estado == EstadoRegistro.Activo &&
                    x.CicloEscolar.Activo
            );

        if (s == null)
        {
            return (
                ResultadoOperacion.Validacion(
                    nameof(m.SeccionId),
                    "Seleccione una sección activa."
                ),
                0
            );
        }

        var cu = await c.Cursos
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.Id == m.CursoId &&
                    x.Estado == EstadoRegistro.Activo
            );

        if (cu == null)
        {
            return (
                ResultadoOperacion.Validacion(
                    nameof(m.CursoId),
                    "Seleccione un curso activo."
                ),
                0
            );
        }

        if (s.GradoId != cu.GradoId)
        {
            return (
                ResultadoOperacion.Validacion(
                    nameof(m.CursoId),
                    "El curso debe pertenecer al mismo grado de la sección."
                ),
                0
            );
        }

        if (!await c.Docentes.AnyAsync(
            x =>
                x.Id == m.DocenteId &&
                x.Usuario.Activo))
        {
            return (
                ResultadoOperacion.Validacion(
                    nameof(m.DocenteId),
                    "Seleccione un docente activo."
                ),
                0
            );
        }

        if (await c.Asignaciones.AnyAsync(
            x =>
                x.Id != id &&
                x.SeccionId == m.SeccionId &&
                x.CursoId == m.CursoId))
        {
            return (
                ResultadoOperacion.Validacion(
                    nameof(m.CursoId),
                    "Ese curso ya tiene una asignación en la sección."
                ),
                0
            );
        }

        return (null, s.GradoId);
    }
}
