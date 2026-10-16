using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Periodos;

namespace Proyecto_Final.Servicios.GestionPeriodos;

public sealed class PeriodoServicio(Contexto contexto) : IPeriodoServicio
{
    private const int MaximoPeriodosPorCiclo = 4;
    private const int MaximoIntentosNumero = 3;

    public Task<List<PeriodoLista>> ObtenerTodosAsync() =>
        contexto.Periodos.AsNoTracking()
            .OrderByDescending(x => x.CicloEscolar.Anio).ThenBy(x => x.Numero)
            .Select(x => new PeriodoLista
            {
                Id = x.Id,
                CicloId = x.CicloEscolarId,
                Ciclo = x.CicloEscolar.Anio,
                Numero = x.Numero,
                Nombre = x.Nombre,
                Inicio = x.FechaInicio,
                Fin = x.FechaFin
            }).ToListAsync();

    public async Task<ResultadoOperacion> CrearAsync(CrearPeriodo modelo)
    {
        Normalizar(modelo);

        for (var intento = 1; intento <= MaximoIntentosNumero; intento++)
        {
            await using var tx = await contexto.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);
            try
            {
                var ultimoNumero = await contexto.Periodos
                    .Where(x => x.CicloEscolarId == modelo.CicloEscolarId)
                    .MaxAsync(x => (int?)x.Numero) ?? 0;
                var numero = checked(ultimoNumero + 1);

                if (numero > MaximoPeriodosPorCiclo)
                {
                    await tx.RollbackAsync();
                    return ResultadoOperacion.Validacion(
                        nameof(modelo.CicloEscolarId),
                        $"El ciclo escolar ya tiene el máximo de {MaximoPeriodosPorCiclo} períodos permitido.");
                }

                var error = await ValidarAsync(modelo, null, numero);
                if (error != null)
                {
                    await tx.RollbackAsync();
                    return error;
                }

                contexto.Periodos.Add(new Periodo
                {
                    CicloEscolarId = modelo.CicloEscolarId,
                    Numero = numero,
                    Nombre = modelo.Nombre,
                    FechaInicio = modelo.FechaInicio,
                    FechaFin = modelo.FechaFin
                });
                await contexto.SaveChangesAsync();
                await tx.CommitAsync();
                return ResultadoOperacion.Correcto("El periodo fue creado correctamente.");
            }
            catch (Exception excepcion) when (EsConflictoNumeracion(excepcion))
            {
                await tx.RollbackAsync();
                contexto.ChangeTracker.Clear();

                if (intento == MaximoIntentosNumero)
                {
                    return ResultadoOperacion.Error(
                        "No fue posible asignar el número del período debido a otra operación simultánea. Intente nuevamente.");
                }
            }
            catch
            {
                await tx.RollbackAsync();
                contexto.ChangeTracker.Clear();
                throw;
            }
        }

        return ResultadoOperacion.Error("No fue posible crear el período.");
    }

    public Task<EditarPeriodo?> ObtenerParaEditarAsync(int id) =>
        contexto.Periodos.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new EditarPeriodo
            {
                Id = x.Id,
                CicloEscolarId = x.CicloEscolarId,
                Numero = x.Numero,
                Nombre = x.Nombre,
                FechaInicio = x.FechaInicio,
                FechaFin = x.FechaFin
            }).FirstOrDefaultAsync();

    public async Task<ResultadoOperacion> EditarAsync(EditarPeriodo modelo)
    {
        Normalizar(modelo);

        var original = await contexto.Periodos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == modelo.Id);
        if (original is null)
            return ResultadoOperacion.Error("El período no existe.");

        if (await contexto.CiclosEscolares.AnyAsync(x =>
            x.Id == original.CicloEscolarId && x.Estado == EstadoCicloEscolar.Cerrado))
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.CicloEscolarId),
                "No puede modificarse un período perteneciente a un ciclo cerrado.");
        }

        for (var intento = 1; intento <= MaximoIntentosNumero; intento++)
        {
            await using var tx = await contexto.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);
            try
            {
                var entidad = await contexto.Periodos
                    .FirstOrDefaultAsync(x => x.Id == modelo.Id);
                if (entidad is null)
                {
                    await tx.RollbackAsync();
                    return ResultadoOperacion.Error("El período ya no existe.");
                }

                var cambiaCiclo = entidad.CicloEscolarId != modelo.CicloEscolarId;
                var numero = entidad.Numero;
                if (cambiaCiclo)
                {
                    if (await PeriodoTieneReferenciasAsync(entidad.Id))
                    {
                        await tx.RollbackAsync();
                        return ResultadoOperacion.Validacion(
                            nameof(modelo.CicloEscolarId),
                            "No puede cambiarse de ciclo un período que ya tiene información académica asociada.");
                    }

                    var ultimoNumero = await contexto.Periodos
                        .Where(x => x.CicloEscolarId == modelo.CicloEscolarId)
                        .MaxAsync(x => (int?)x.Numero) ?? 0;
                    numero = checked(ultimoNumero + 1);
                }

                if (numero > MaximoPeriodosPorCiclo)
                {
                    await tx.RollbackAsync();
                    return ResultadoOperacion.Validacion(
                        nameof(modelo.CicloEscolarId),
                        $"El ciclo escolar de destino ya tiene el máximo de {MaximoPeriodosPorCiclo} períodos permitido.");
                }

                var error = await ValidarAsync(modelo, modelo.Id, numero);
                if (error != null)
                {
                    await tx.RollbackAsync();
                    return error;
                }

                if (cambiaCiclo)
                {
                    // CicloEscolarId forma parte de una clave alternativa usada por
                    // evaluaciones. EF Core no permite modificar una clave rastreada,
                    // por lo que un período sin referencias se reemplaza dentro de la
                    // misma transacción, conservando su Id.
                    contexto.Periodos.Remove(entidad);
                    await contexto.SaveChangesAsync();
                    contexto.ChangeTracker.Clear();
                    contexto.Periodos.Add(new Periodo
                    {
                        Id = entidad.Id,
                        CicloEscolarId = modelo.CicloEscolarId,
                        Numero = numero,
                        Nombre = modelo.Nombre,
                        FechaInicio = modelo.FechaInicio,
                        FechaFin = modelo.FechaFin
                    });
                    await contexto.SaveChangesAsync();
                }
                else
                {
                    entidad.Numero = numero;
                    entidad.Nombre = modelo.Nombre;
                    entidad.FechaInicio = modelo.FechaInicio;
                    entidad.FechaFin = modelo.FechaFin;
                    await contexto.SaveChangesAsync();
                }

                await tx.CommitAsync();
                return ResultadoOperacion.Correcto("El periodo fue actualizado correctamente.");
            }
            catch (Exception excepcion) when (EsConflictoNumeracion(excepcion))
            {
                await tx.RollbackAsync();
                contexto.ChangeTracker.Clear();

                if (intento == MaximoIntentosNumero)
                {
                    return ResultadoOperacion.Error(
                        "No fue posible asignar el número en el ciclo de destino debido a otra operación simultánea. Intente nuevamente.");
                }
            }
            catch
            {
                await tx.RollbackAsync();
                contexto.ChangeTracker.Clear();
                throw;
            }
        }

        return ResultadoOperacion.Error("No fue posible actualizar el período.");
    }

    private async Task<bool> PeriodoTieneReferenciasAsync(int periodoId) =>
        await contexto.UnidadesAsignaciones.AnyAsync(x => x.PeriodoId == periodoId) ||
        await contexto.PlanificacionesAsignaciones.AnyAsync(x => x.PeriodoId == periodoId) ||
        await contexto.ConfiguracionesEvaluacion.AnyAsync(x => x.PeriodoId == periodoId) ||
        await contexto.PonderacionesPeriodosAnuales.AnyAsync(x => x.PeriodoId == periodoId);

    private async Task<ResultadoOperacion?> ValidarAsync(
        FormularioPeriodo modelo,
        int? id,
        int numero)
    {
        if (modelo.FechaInicio > modelo.FechaFin)
            return ResultadoOperacion.Validacion(nameof(modelo.FechaFin), "La fecha de finalización debe ser posterior a la fecha de inicio.");

        var ciclo = await contexto.CiclosEscolares.AsNoTracking().FirstOrDefaultAsync(x => x.Id == modelo.CicloEscolarId);
        if (ciclo?.Estado == EstadoCicloEscolar.Cerrado)
            return ResultadoOperacion.Validacion(nameof(modelo.CicloEscolarId), "No pueden modificarse períodos de un ciclo cerrado.");
        if (ciclo == null)
            return ResultadoOperacion.Validacion(nameof(modelo.CicloEscolarId), "El ciclo escolar seleccionado no existe.");
        if (modelo.FechaInicio < ciclo.FechaInicio || modelo.FechaFin > ciclo.FechaFin)
            return ResultadoOperacion.Validacion(nameof(modelo.FechaFin), "Las fechas del periodo deben estar dentro del ciclo escolar.");
        if (await contexto.Periodos.AnyAsync(x =>
            x.CicloEscolarId == modelo.CicloEscolarId && x.Numero == numero &&
            (!id.HasValue || x.Id != id.Value)))
            return ResultadoOperacion.Error(
                "No fue posible asignar el número del período porque ya está ocupado.");

        if (await contexto.Periodos.AnyAsync(x =>
            x.CicloEscolarId == modelo.CicloEscolarId &&
            (!id.HasValue || x.Id != id.Value) &&
            x.Nombre.ToUpper() == modelo.Nombre.ToUpper()))
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.Nombre),
                "Ya existe un período con ese nombre en el ciclo escolar.");
        }

        var conflicto = await contexto.Periodos.AsNoTracking()
            .Where(x => x.CicloEscolarId == modelo.CicloEscolarId &&
                (!id.HasValue || x.Id != id.Value) &&
                modelo.FechaInicio <= x.FechaFin &&
                modelo.FechaFin >= x.FechaInicio)
            .OrderBy(x => x.FechaInicio)
            .Select(x => new { x.Nombre, x.FechaInicio, x.FechaFin })
            .FirstOrDefaultAsync();

        if (conflicto != null)
            return ResultadoOperacion.Validacion(nameof(modelo.FechaInicio),
                $"Las fechas se superponen con el período {conflicto.Nombre} " +
                $"({conflicto.FechaInicio:dd/MM/yyyy} al {conflicto.FechaFin:dd/MM/yyyy}).");

        return null;
    }

    private static void Normalizar(FormularioPeriodo modelo) => modelo.Nombre = modelo.Nombre.Trim();

    private static bool EsConflictoNumeracion(Exception excepcion)
    {
        for (var actual = excepcion; actual is not null; actual = actual.InnerException!)
        {
            if (actual is PostgresException postgres &&
                (postgres.SqlState == PostgresErrorCodes.SerializationFailure ||
                 postgres.SqlState == PostgresErrorCodes.DeadlockDetected ||
                 postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
                 string.Equals(
                     postgres.ConstraintName,
                     "IX_Periodos_CicloEscolarId_Numero",
                     StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (actual.Message.Contains(
                    "IX_Periodos_CicloEscolarId_Numero",
                    StringComparison.OrdinalIgnoreCase) ||
                actual.Message.Contains(
                    "Periodos.CicloEscolarId, Periodos.Numero",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
