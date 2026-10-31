using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionAsignaciones;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.Seguridad;
using Proyecto_Final.ViewModels.Calificaciones;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.Servicios.GestionCalificaciones;

public sealed class CalificacionServicio(
    Contexto contexto,
    IAccesoAcademicoServicio acceso,
    IDelegacionAcademicaServicio? delegaciones = null,
    IPertenenciaAcademicaHistoricaServicio? pertenenciaHistorica = null,
    IResultadoOficialTrasladoServicio? resultadosOficiales = null,
    IContextoCicloEscolar? contextoCiclo = null) : ICalificacionServicio
{
    private static readonly JsonSerializerOptions JsonOpciones = new(JsonSerializerDefaults.Web);

    private IPertenenciaAcademicaHistoricaServicio PertenenciaHistorica =>
        pertenenciaHistorica ?? new PertenenciaAcademicaHistoricaServicio(contexto);

    private IResultadoOficialTrasladoServicio ResultadosOficiales =>
        resultadosOficiales ?? new ResultadoOficialTrasladoServicio(contexto);

    public async Task<CalificacionesIndex> ObtenerIndexAsync(bool puedeConfigurar)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var consulta = contexto.ConfiguracionesEvaluacion.AsNoTracking().AsQueryable();
        var cicloSeleccionadoId = await ObtenerCicloSeleccionadoIdAsync();
        if (contextoCiclo is not null)
            consulta = cicloSeleccionadoId.HasValue
                ? consulta.Where(x => x.CicloEscolarId == cicloSeleccionadoId.Value)
                : consulta.Where(_ => false);

        if (perfil.DocenteId.HasValue)
        {
            consulta = consulta.Where(x =>
                x.Asignacion.DocenteId == perfil.DocenteId.Value ||
                contexto.DelegacionesAcademicas.Any(d =>
                    d.AsignacionId == x.AsignacionId &&
                    d.DocenteId == perfil.DocenteId.Value && d.Activa &&
                    d.FechaInicioUtc <= DateTime.UtcNow &&
                    (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= DateTime.UtcNow) &&
                    ((d.Operaciones & OperacionDelegacionAcademica.GestionarEvaluaciones) ==
                        OperacionDelegacionAcademica.GestionarEvaluaciones ||
                     (d.Operaciones & OperacionDelegacionAcademica.SolicitarCorrecciones) ==
                        OperacionDelegacionAcademica.SolicitarCorrecciones)));
        }
        else if (perfil.AlumnoId.HasValue)
        {
            consulta = consulta.Where(x => contexto.Inscripciones.Any(i =>
                i.AlumnoId == perfil.AlumnoId.Value &&
                i.SeccionId == x.SeccionId &&
                i.CicloEscolarId == x.CicloEscolarId));
        }

        return new CalificacionesIndex
        {
            PuedeConfigurar = puedeConfigurar && perfil.DocenteId.HasValue,
            Configuraciones = await consulta
                .OrderByDescending(x => x.CicloEscolarId)
                .ThenBy(x => x.Asignacion.Curso.Nombre)
                .Select(x => new CalificacionConfiguracionLista
                {
                    Id = x.Id,
                    AsignacionId = x.AsignacionId,
                    PeriodoId = x.PeriodoId,
                    Curso = x.Asignacion.Curso.Nombre,
                    GradoSeccion = x.Asignacion.Seccion.Grado.Nombre + " " + x.Asignacion.Seccion.Nombre,
                    Periodo = x.Periodo.Nombre,
                    Docente = x.Asignacion.Docente.Usuario.PrimerNombre + " " +
                        x.Asignacion.Docente.Usuario.PrimerApellido,
                    Estado = x.Estado,
                    EstadoCierre = contexto.CierresCalificaciones
                        .Where(c => c.ConfiguracionEvaluacionId == x.Id)
                        .Select(c => (EstadoCierreCalificaciones?)c.Estado)
                        .FirstOrDefault() ?? EstadoCierreCalificaciones.Abierto,
                    Actividades = x.Actividades.Count(a => a.Activa)
                })
                .ToListAsync(),
            Asignaciones = await ObtenerAsignacionesConfigurablesAsync(perfil.DocenteId),
            Periodos = await contexto.Periodos.AsNoTracking()
                .Where(x => contextoCiclo == null
                    ? x.CicloEscolar.Activo
                    : cicloSeleccionadoId.HasValue && x.CicloEscolarId == cicloSeleccionadoId.Value)
                .OrderBy(x => x.Numero)
                .Select(x => new OpcionSeleccion
                {
                    Id = x.Id,
                    Texto = x.CicloEscolar.Anio + " · " + x.Nombre
                })
                .ToListAsync()
        };
    }

    public async Task<ConfigurarEvaluacion?> PrepararConfiguracionAsync(
        int asignacionId,
        int periodoId,
        int? plantillaId = null)
    {
        if (!await AsignacionPerteneceAlCicloSeleccionadoAsync(asignacionId) ||
            !await PeriodoPerteneceAlCicloSeleccionadoAsync(periodoId))
            return null;
        if (!await PuedeConfigurarAsync(asignacionId)) return null;

        var existente = await contexto.ConfiguracionesEvaluacion
            .AsNoTracking()
            .Include(x => x.Categorias.OrderBy(c => c.Orden))
                .ThenInclude(x => x.Actividades.OrderBy(a => a.Orden))
            .FirstOrDefaultAsync(x =>
                x.AsignacionId == asignacionId &&
                x.PeriodoId == periodoId);

        var modelo = existente == null
            ? new ConfigurarEvaluacion
            {
                AsignacionId = asignacionId,
                PeriodoId = periodoId,
                MetodoCalculo = MetodoCalculoEvaluacion.SumaPuntos,
                Categorias =
                [
                    new CategoriaEvaluacionFormulario
                    {
                        Nombre = "Desempeño",
                        Tipo = TipoCategoriaEvaluacion.Desempeno,
                        Porcentaje = 80
                    },
                    new CategoriaEvaluacionFormulario
                    {
                        Nombre = "Actitudinal",
                        Tipo = TipoCategoriaEvaluacion.Actitudinal,
                        Porcentaje = 20,
                        Actividades = Enumerable.Range(1, 5)
                            .Select(_ => new ActividadEvaluableFormulario
                            {
                                PunteoMaximo = 4
                            })
                            .ToList()
                    }
                ]
            }
            : new ConfigurarEvaluacion
            {
                Id = existente.Id,
                AsignacionId = existente.AsignacionId,
                PeriodoId = existente.PeriodoId,
                MetodoCalculo = existente.MetodoCalculo,
                Categorias = existente.Categorias.Select(c => new CategoriaEvaluacionFormulario
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Tipo = c.Tipo,
                    Porcentaje = c.Porcentaje,
                    Actividades = c.Actividades.Select(a => new ActividadEvaluableFormulario
                    {
                        Id = a.Id,
                        Nombre = a.Nombre,
                        PunteoMaximo = a.PunteoMaximo,
                        TareaId = a.TareaId
                    }).ToList()
                }).ToList()
            };

        if (existente == null && plantillaId.HasValue)
        {
            var plantilla = await contexto.PlantillasEvaluacion.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == plantillaId && x.Activa);
            if (plantilla != null)
            {
                modelo.PlantillaId = plantilla.Id;
                modelo.MetodoCalculo = plantilla.MetodoCalculo;
                modelo.Categorias = JsonSerializer.Deserialize<List<CategoriaEvaluacionFormulario>>(
                    plantilla.DefinicionJson, JsonOpciones) ?? modelo.Categorias;
                foreach (var actividad in modelo.Categorias.SelectMany(x => x.Actividades))
                    actividad.TareaId = null;
            }
        }

        await PrepararOpcionesAsync(modelo);
        return modelo;
    }

    public async Task<ControlTareas?> ObtenerControlTareasAsync(int asignacionId)
    {
        if (!await AsignacionPerteneceAlCicloSeleccionadoAsync(asignacionId)) return null;
        var ciclo = contextoCiclo is null ? null : await contextoCiclo.ObtenerAsync();
        var puedeConsultar = ciclo?.EsHistorico == true
            ? await acceso.PuedeConsultarAsignacionHistoricaAsync(asignacionId)
            : await acceso.PuedeConsultarAsignacionVigenteAsync(asignacionId);
        if (!puedeConsultar) return null;
        var asignacion = await contexto.Asignaciones.AsNoTracking()
            .Include(x => x.Curso).Include(x => x.Seccion).ThenInclude(x => x.Grado)
            .FirstOrDefaultAsync(x => x.Id == asignacionId);
        if (asignacion == null) return null;

        var tareas = await contexto.Tareas.AsNoTracking()
            .Where(x => x.AsignacionId == asignacionId && x.Estado == EstadoTarea.Cerrada)
            .OrderBy(x => x.FechaCierre).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.Titulo, x.PunteoMaximo, x.CriterioCalificacionEntrega })
            .ToListAsync();
        var tareaIds = tareas.Select(x => x.Id).ToList();
        var entregas = await contexto.Entregas.AsNoTracking()
            .Where(x => tareaIds.Contains(x.TareaId))
            .Select(x => new { x.TareaId, x.AlumnoId, x.Calificacion, x.NumeroEnvio,
                x.OrigenCalificacionAdministrativa, x.InscripcionId,
                Alumno = x.Alumno.Usuario.PrimerNombre + " " + x.Alumno.Usuario.PrimerApellido,
                Codigo = x.Alumno.CodigoPersonal })
            .ToListAsync();
        var actual = await contexto.Inscripciones.AsNoTracking()
            .Where(x => x.AlumnoId != 0 && x.CicloEscolarId == asignacion.Seccion.CicloEscolarId &&
                x.SeccionId == asignacion.SeccionId && x.Estado == EstadoInscripcion.Activa)
            .Select(x => x.AlumnoId).ToListAsync();
        var modelo = new ControlTareas
        {
            AsignacionId = asignacionId, Curso = asignacion.Curso.Nombre,
            GradoSeccion = asignacion.Seccion.Grado.Nombre + " " + asignacion.Seccion.Nombre,
            Tareas = tareas.Select(x => new ControlTareaColumna { Id = x.Id, Titulo = x.Titulo,
                PunteoMaximo = x.PunteoMaximo }).ToList()
        };
        foreach (var grupo in entregas.GroupBy(x => x.AlumnoId).OrderBy(x => x.First().Alumno))
        {
            var fila = new ControlTareaFila { Alumno = grupo.First().Alumno,
                CodigoPersonal = grupo.First().Codigo ?? "Sin código", EsHistorico = !actual.Contains(grupo.Key) };
            foreach (var tarea in tareas)
            {
                var intentos = grupo.Where(x => x.TareaId == tarea.Id).ToList();
                fila.Notas[tarea.Id] = intentos.Count == 0 ? null : CalculoCriterioCalificacion.Aplicar(
                    tarea.CriterioCalificacionEntrega,
                    intentos.Select(x => (x.NumeroEnvio, 0, x.Calificacion,
                        x.OrigenCalificacionAdministrativa.HasValue)));
            }
            modelo.Alumnos.Add(fila);
        }
        return modelo;
    }

    public async Task PrepararOpcionesAsync(ConfigurarEvaluacion modelo)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        modelo.Asignaciones = await ObtenerAsignacionesConfigurablesAsync(perfil.DocenteId);

        var cicloId = await contexto.Asignaciones.AsNoTracking()
            .Where(x => x.Id == modelo.AsignacionId)
            .Select(x => (int?)x.Seccion.CicloEscolarId)
            .FirstOrDefaultAsync();
        var cicloSeleccionadoId = await ObtenerCicloSeleccionadoIdAsync();
        if (contextoCiclo is not null && cicloId != cicloSeleccionadoId)
        {
            modelo.Periodos = [];
            modelo.Tareas = [];
            return;
        }

        modelo.Periodos = await contexto.Periodos.AsNoTracking()
            .Where(x => !cicloId.HasValue || x.CicloEscolarId == cicloId.Value)
            .OrderBy(x => x.Numero)
            .Select(x => new OpcionSeleccion { Id = x.Id, Texto = x.Nombre })
            .ToListAsync();

        // TAREAS DE LA ASIGNACIÓN Y PERÍODO ACADÉMICO
        modelo.Tareas = await contexto.Tareas
            .AsNoTracking()
            .Where(x =>
                x.AsignacionId == modelo.AsignacionId &&
                x.UnidadAsignacionId.HasValue &&
                x.UnidadAsignacion != null &&
                x.UnidadAsignacion.AsignacionId == modelo.AsignacionId &&
                x.UnidadAsignacion.PeriodoId == modelo.PeriodoId &&
                x.UnidadAsignacion.Periodo != null &&
                x.UnidadAsignacion.Periodo.CicloEscolarId == cicloId)
            .OrderBy(x => x.Titulo)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.Titulo + " · " + x.PunteoMaximo + " pts."
            })
            .ToListAsync();

        var cursoId = await contexto.Asignaciones.AsNoTracking()
            .Where(x => x.Id == modelo.AsignacionId)
            .Select(x => (int?)x.CursoId)
            .FirstOrDefaultAsync();
        modelo.Plantillas = await contexto.PlantillasEvaluacion.AsNoTracking()
            .Where(x => x.Activa && (!x.CursoId.HasValue || x.CursoId == cursoId))
            .OrderBy(x => x.Nombre)
            .Select(x => new OpcionSeleccion { Id = x.Id, Texto = x.Nombre })
            .ToListAsync();
    }

    public async Task<ResultadoOperacion<int>> GuardarConfiguracionAsync(
        ConfigurarEvaluacion modelo)
    {
        if (!await AsignacionPerteneceAlCicloSeleccionadoAsync(modelo.AsignacionId) ||
            !await PeriodoPerteneceAlCicloSeleccionadoAsync(modelo.PeriodoId))
        {
            return ResultadoOperacion<int>.Error(
                "La asignación o el período no pertenecen al ciclo escolar seleccionado.");
        }
        var validacion = await ValidarConfiguracionAsync(modelo);
        if (validacion.Count > 0)
            return ResultadoOperacion<int>.Validacion(validacion);

        var usuarioId = acceso.UsuarioId;
        if (string.IsNullOrWhiteSpace(usuarioId))
            return ResultadoOperacion<int>.Error("No se pudo identificar al usuario.");

        var existe = await contexto.ConfiguracionesEvaluacion.AsNoTracking()
            .AnyAsync(x => x.AsignacionId == modelo.AsignacionId && x.PeriodoId == modelo.PeriodoId);
        if (!existe)
            return await GuardarConfiguracionLegacyAsync(modelo);

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);
        try
        {
            var configuracion = await contexto.ConfiguracionesEvaluacion
                .Include(x => x.Categorias)
                    .ThenInclude(x => x.Actividades)
                .Include(x => x.Actividades)
                .FirstAsync(x => x.AsignacionId == modelo.AsignacionId &&
                    x.PeriodoId == modelo.PeriodoId);

            if (modelo.Id != configuracion.Id)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<int>.Error(
                    "La configuración indicada no pertenece a la asignación y período seleccionados.");
            }

            if (await EstaCerradaAsync(configuracion.Id))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<int>.Error(
                    "La configuración pertenece a un período cerrado.");
            }

            var errores = await ValidarActualizacionConfiguracionAsync(configuracion, modelo);
            if (errores.Count > 0)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<int>.Validacion(errores);
            }

            var anterior = ResumenConfiguracion(configuracion);
            var cambio = ConfiguracionCambio(configuracion, modelo);
            if (!cambio)
            {
                await transaccion.CommitAsync();
                return ResultadoOperacion<int>.Correcto(configuracion.Id,
                    "No se detectaron cambios en la configuración de evaluación.");
            }

            if (string.IsNullOrWhiteSpace(modelo.MotivoCorreccion) ||
                modelo.MotivoCorreccion.Trim().Length < 10)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<int>.Validacion(nameof(modelo.MotivoCorreccion),
                    "El motivo de la corrección debe contener al menos 10 caracteres.");
            }

            var categoriasExistentes = configuracion.Categorias.ToDictionary(x => x.Id);
            var actividadesExistentes = configuracion.Actividades.ToDictionary(x => x.Id);
            var categoriasFormulario = modelo.Categorias.Where(x => x.Id > 0)
                .ToDictionary(x => x.Id);
            var actividadesFormulario = modelo.Categorias.SelectMany(x => x.Actividades)
                .Where(x => x.Id > 0).ToDictionary(x => x.Id);

            foreach (var actividad in configuracion.Actividades
                         .Where(x => !actividadesFormulario.ContainsKey(x.Id)).ToList())
                contexto.ActividadesEvaluables.Remove(actividad);
            foreach (var categoria in configuracion.Categorias
                         .Where(x => !categoriasFormulario.ContainsKey(x.Id)).ToList())
                contexto.CategoriasEvaluacion.Remove(categoria);

            var ordenActividad = 1;
            for (var indice = 0; indice < modelo.Categorias.Count; indice++)
            {
                var categoriaModelo = modelo.Categorias[indice];
                CategoriaEvaluacion categoria;
                if (categoriaModelo.Id > 0)
                {
                    categoria = categoriasExistentes[categoriaModelo.Id];
                    categoria.Nombre = categoriaModelo.Nombre.Trim();
                    categoria.Tipo = categoriaModelo.Tipo;
                    categoria.Porcentaje = categoriaModelo.Porcentaje;
                    categoria.Orden = indice + 1;
                }
                else
                {
                    categoria = new CategoriaEvaluacion
                    {
                        ConfiguracionEvaluacion = configuracion,
                        Nombre = categoriaModelo.Nombre.Trim(),
                        Tipo = categoriaModelo.Tipo,
                        Porcentaje = categoriaModelo.Porcentaje,
                        Orden = indice + 1
                    };
                    contexto.CategoriasEvaluacion.Add(categoria);
                }

                foreach (var actividadModelo in categoriaModelo.Actividades)
                {
                    ActividadEvaluable actividad;
                    if (actividadModelo.Id > 0)
                    {
                        actividad = actividadesExistentes[actividadModelo.Id];
                        actividad.Nombre = actividadModelo.Nombre.Trim();
                        actividad.PunteoMaximo = actividadModelo.PunteoMaximo;
                        // Las vinculaciones heredadas se preservan para consulta,
                        // pero no se crean ni se cambian en libros nuevos.
                        actividad.CategoriaEvaluacion = categoria;
                        actividad.Orden = ordenActividad++;
                    }
                    else
                    {
                        actividad = new ActividadEvaluable
                        {
                            ConfiguracionEvaluacion = configuracion,
                            AsignacionId = modelo.AsignacionId,
                            CategoriaEvaluacion = categoria,
                            Origen = OrigenActividadEvaluable.Manual,
                            TareaId = null,
                            Nombre = actividadModelo.Nombre.Trim(),
                            PunteoMaximo = actividadModelo.PunteoMaximo,
                            Orden = ordenActividad++,
                            Activa = true,
                            FechaCreacion = DateTime.UtcNow
                        };
                        contexto.ActividadesEvaluables.Add(actividad);
                    }
                }
            }

            configuracion.MetodoCalculo = modelo.MetodoCalculo;
            configuracion.Estado = EstadoConfiguracionEvaluacion.Activa;
            configuracion.FechaActivacion = DateTime.UtcNow;
            contexto.RegistrosAuditoria.Add(new RegistroAuditoria
            {
                FechaUtc = DateTime.UtcNow,
                UsuarioId = usuarioId,
                Modulo = "Calificaciones",
                Accion = "CorregirConfiguracion",
                Tipo = TipoEventoAuditoria.Operacion,
                Resultado = ResultadoAuditoria.Exitoso,
                Entidad = nameof(ConfiguracionEvaluacion),
                EntidadId = configuracion.Id.ToString(),
                Descripcion = "Corrección auditada de la configuración de evaluación.",
                Justificacion = modelo.MotivoCorreccion.Trim(),
                ValoresAnteriores = anterior,
                ValoresNuevos = ResumenFormulario(modelo)
            });

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion<int>.Correcto(configuracion.Id,
                "La configuración de evaluación fue corregida y auditada correctamente.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private async Task<ResultadoOperacion<int>> GuardarConfiguracionLegacyAsync(
        ConfigurarEvaluacion modelo)
    {
        var validacion = await ValidarConfiguracionAsync(modelo);
        if (validacion.Count > 0)
            return ResultadoOperacion<int>.Validacion(validacion);

        var usuarioId = acceso.UsuarioId;
        if (string.IsNullOrWhiteSpace(usuarioId))
            return ResultadoOperacion<int>.Error("No se pudo identificar al usuario.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);
        try
        {
            var configuracion = await contexto.ConfiguracionesEvaluacion
                .Include(x => x.Categorias)
                .Include(x => x.Actividades)
                .FirstOrDefaultAsync(x =>
                    x.AsignacionId == modelo.AsignacionId &&
                    x.PeriodoId == modelo.PeriodoId);

            if (configuracion != null && await contexto.CierresCalificaciones.AnyAsync(x =>
                x.ConfiguracionEvaluacionId == configuracion.Id &&
                x.Estado == EstadoCierreCalificaciones.Cerrado))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<int>.Error(
                    "La configuración pertenece a un período cerrado.");
            }

            if (configuracion != null && await contexto.CalificacionesManuales.AnyAsync(x =>
                x.ActividadEvaluable.ConfiguracionEvaluacionId == configuracion.Id))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<int>.Error(
                    "No puede reemplazar una configuración que ya contiene calificaciones.");
            }

            if (configuracion != null && await contexto.Entregas.AnyAsync(e =>
                e.Calificacion.HasValue &&
                configuracion.Actividades.Any(a => a.TareaId == e.TareaId)))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<int>.Error(
                    "No puede reemplazar una configuración que ya contiene calificaciones automáticas.");
            }

            if (configuracion == null)
            {
                var datos = await contexto.Asignaciones.AsNoTracking()
                    .Where(x => x.Id == modelo.AsignacionId)
                    .Select(x => new { x.SeccionId, x.Seccion.CicloEscolarId })
                    .FirstAsync();
                configuracion = new ConfiguracionEvaluacion
                {
                    AsignacionId = modelo.AsignacionId,
                    SeccionId = datos.SeccionId,
                    CicloEscolarId = datos.CicloEscolarId,
                    PeriodoId = modelo.PeriodoId,
                    FechaCreacion = DateTime.UtcNow,
                    ConfiguradoPorUsuarioId = usuarioId
                };
                contexto.ConfiguracionesEvaluacion.Add(configuracion);
            }
            else
            {
                contexto.ActividadesEvaluables.RemoveRange(configuracion.Actividades);
                contexto.CategoriasEvaluacion.RemoveRange(configuracion.Categorias);
            }

            configuracion.MetodoCalculo = modelo.MetodoCalculo;
            configuracion.Estado = EstadoConfiguracionEvaluacion.Activa;
            configuracion.FechaActivacion = DateTime.UtcNow;

            var ordenActividad = 1;
            for (var indice = 0; indice < modelo.Categorias.Count; indice++)
            {
                var categoriaModelo = modelo.Categorias[indice];
                var categoria = new CategoriaEvaluacion
                {
                    ConfiguracionEvaluacion = configuracion,
                    Nombre = categoriaModelo.Nombre.Trim(),
                    Tipo = categoriaModelo.Tipo,
                    Porcentaje = categoriaModelo.Porcentaje,
                    Orden = indice + 1
                };
                contexto.CategoriasEvaluacion.Add(categoria);

                foreach (var actividadModelo in categoriaModelo.Actividades)
                {
                    contexto.ActividadesEvaluables.Add(new ActividadEvaluable
                    {
                        ConfiguracionEvaluacion = configuracion,
                        AsignacionId = modelo.AsignacionId,
                        CategoriaEvaluacion = categoria,
                        Origen = OrigenActividadEvaluable.Manual,
                        TareaId = null,
                        Nombre = actividadModelo.Nombre.Trim(),
                        PunteoMaximo = actividadModelo.PunteoMaximo,
                        Orden = ordenActividad++,
                        Activa = true,
                        FechaCreacion = DateTime.UtcNow
                    });
                }
            }

            if (modelo.GuardarComoPlantilla)
            {
                var cursoId = await contexto.Asignaciones
                    .Where(x => x.Id == modelo.AsignacionId)
                    .Select(x => x.CursoId)
                    .FirstAsync();
                contexto.PlantillasEvaluacion.Add(new PlantillaEvaluacion
                {
                    Nombre = modelo.NombrePlantilla!.Trim(),
                    CursoId = cursoId,
                    MetodoCalculo = modelo.MetodoCalculo,
                    DefinicionJson = JsonSerializer.Serialize(modelo.Categorias, JsonOpciones),
                    FechaCreacion = DateTime.UtcNow,
                    CreadoPorUsuarioId = usuarioId
                });
            }

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion<int>.Correcto(
                configuracion.Id,
                "La configuración de evaluación fue guardada correctamente.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<LibroCalificaciones?> ObtenerLibroAsync(
        int configuracionId,
        bool permisoRegistrar,
        bool permisoCerrar,
        bool usarResultadosCerrados = true)
    {
        var configuracion = await contexto.ConfiguracionesEvaluacion.AsNoTracking()
            .Include(x => x.Asignacion).ThenInclude(x => x.Curso)
            .Include(x => x.Asignacion).ThenInclude(x => x.Seccion).ThenInclude(x => x.Grado)
            .Include(x => x.Asignacion).ThenInclude(x => x.Seccion).ThenInclude(x => x.CicloEscolar)
            .Include(x => x.Periodo)
            .Include(x => x.Categorias.OrderBy(c => c.Orden))
            .Include(x => x.Actividades.OrderBy(a => a.Orden))
            .ThenInclude(a => a.CategoriaEvaluacion)
            .FirstOrDefaultAsync(x => x.Id == configuracionId);
        if (configuracion == null ||
            !await PerteneceAlCicloSeleccionadoAsync(configuracion.CicloEscolarId) ||
            !await PuedeConsultarAsync(configuracion)) return null;

        var cierre = await contexto.CierresCalificaciones.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ConfiguracionEvaluacionId == configuracionId);
        var cerrado = cierre?.Estado == EstadoCierreCalificaciones.Cerrado;
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var pertenenciasDelPeriodo = cerrado
            ? new Dictionary<int, TipoPertenenciaLibro>()
            : (await PertenenciaHistorica.ObtenerParaSeccionAsync(configuracion, soloActivas: true))
                .ToDictionary(x => x.Key, x => x.Value);

        var inscripciones = contexto.Inscripciones.AsNoTracking()
            .Where(x =>
                x.CicloEscolarId == configuracion.CicloEscolarId);
        if (!cerrado)
            inscripciones = inscripciones.Where(x =>
                x.Estado == EstadoInscripcion.Activa &&
                pertenenciasDelPeriodo.Keys.Contains(x.Id));
        if (perfil.AlumnoId.HasValue)
            inscripciones = inscripciones.Where(x => x.AlumnoId == perfil.AlumnoId.Value);

        var alumnos = await inscripciones
            .OrderBy(x => x.Alumno.Usuario.PrimerApellido)
            .ThenBy(x => x.Alumno.Usuario.PrimerNombre)
            .Select(x => new
            {
                InscripcionId = x.Id,
                x.AlumnoId,
                Codigo = x.Alumno.CodigoPersonal ?? "Sin código",
                Nombre = x.Alumno.Usuario.PrimerNombre + " " + x.Alumno.Usuario.PrimerApellido
            })
            .ToListAsync();

        var resultadosCerrados = new Dictionary<int, ResultadoCalificacionPeriodo>();
        if (cerrado)
        {
            resultadosCerrados = await contexto.ResultadosCalificacionesPeriodos.AsNoTracking()
                .Where(x => x.ConfiguracionEvaluacionId == configuracionId)
                .ToDictionaryAsync(x => x.InscripcionId);
            alumnos = await contexto.ResultadosCalificacionesPeriodos.AsNoTracking()
                .Where(x => x.ConfiguracionEvaluacionId == configuracionId &&
                    (!perfil.AlumnoId.HasValue || x.AlumnoId == perfil.AlumnoId.Value))
                .OrderBy(x => x.Alumno.Usuario.PrimerApellido)
                .ThenBy(x => x.Alumno.Usuario.PrimerNombre)
                .Select(x => new
                {
                    InscripcionId = x.InscripcionId,
                    x.AlumnoId,
                    Codigo = x.Alumno.CodigoPersonal ?? "Sin codigo",
                    Nombre = x.Alumno.Usuario.PrimerNombre + " " + x.Alumno.Usuario.PrimerApellido
                })
                .ToListAsync();
        }

        var inscripcionesIds = alumnos.Select(x => x.InscripcionId).ToList();
        var alumnosIds = alumnos.Select(x => x.AlumnoId).ToList();
        var actividadesIds = configuracion.Actividades.Select(x => x.Id).ToList();
        var tareasIds = configuracion.Actividades
            .Where(x => x.TareaId.HasValue)
            .Select(x => x.TareaId!.Value)
            .ToList();

        var manuales = await contexto.CalificacionesManuales.AsNoTracking()
            .Where(x => actividadesIds.Contains(x.ActividadEvaluableId) &&
                ((x.InscripcionId.HasValue && inscripcionesIds.Contains(x.InscripcionId.Value)) ||
                 (!x.InscripcionId.HasValue && alumnosIds.Contains(x.AlumnoId))))
            .ToListAsync();
        var entregas = await contexto.Entregas.AsNoTracking()
            .Where(x => tareasIds.Contains(x.TareaId) && alumnosIds.Contains(x.AlumnoId))
            .Include(x => x.Tarea)
            .ToListAsync();
        var resultadosOficialesPorInscripcion = await ResultadosOficiales.ObtenerAsync(
            inscripcionesIds, configuracion.Asignacion.CursoId, configuracion.Periodo);

        var libro = new LibroCalificaciones
        {
            ConfiguracionId = configuracion.Id,
            Curso = configuracion.Asignacion.Curso.Nombre,
            GradoSeccion = configuracion.Asignacion.Seccion.Grado.Nombre + " " +
                configuracion.Asignacion.Seccion.Nombre,
            Periodo = configuracion.Periodo.Nombre,
            Ciclo = configuracion.Asignacion.Seccion.CicloEscolar.Anio.ToString(),
            MetodoCalculo = configuracion.MetodoCalculo,
            EstadoCierre = cierre?.Estado ?? EstadoCierreCalificaciones.Abierto,
            PuedeEditar = !cerrado && permisoRegistrar &&
                acceso.TienePermiso(Permisos.Calificaciones.Registrar) &&
                perfil.DocenteId.HasValue &&
                configuracion.Asignacion.DocenteId == perfil.DocenteId.Value &&
                configuracion.Asignacion.Estado == EstadoRegistro.Activo &&
                configuracion.Asignacion.Seccion.CicloEscolar.Activo,
            PuedeCerrar = !cerrado && permisoCerrar &&
                acceso.TienePermiso(Permisos.Calificaciones.Cerrar) &&
                perfil.DocenteId.HasValue &&
                configuracion.Asignacion.DocenteId == perfil.DocenteId.Value &&
                configuracion.Asignacion.Estado == EstadoRegistro.Activo &&
                configuracion.Asignacion.Seccion.CicloEscolar.Activo,
            Actividades = configuracion.Actividades.Select(x => new ColumnaActividadCalificacion
            {
                Id = x.Id,
                CategoriaId = x.CategoriaEvaluacionId ?? 0,
                Nombre = x.Nombre,
                Categoria = x.CategoriaEvaluacion?.Nombre ?? "Sin categoría",
                TipoCategoria = x.CategoriaEvaluacion?.Tipo ?? TipoCategoriaEvaluacion.Otra,
                PorcentajeCategoria = x.CategoriaEvaluacion?.Porcentaje ?? 0,
                MaximoCategoria = configuracion.Actividades
                    .Where(a => a.CategoriaEvaluacionId == x.CategoriaEvaluacionId)
                    .Sum(a => a.PunteoMaximo),
                PunteoMaximo = x.PunteoMaximo,
                EsAutomatica = x.Origen == OrigenActividadEvaluable.Tarea
            }).ToList()
        };

        foreach (var alumno in alumnos)
        {
            var fila = new FilaAlumnoCalificacion
            {
                AlumnoId = alumno.AlumnoId,
                InscripcionId = alumno.InscripcionId,
                CodigoPersonal = alumno.Codigo,
                Alumno = alumno.Nombre,
                TipoPertenencia = cerrado
                    ? TipoPertenenciaLibro.MatriculaOrdinaria
                    : pertenenciasDelPeriodo.GetValueOrDefault(alumno.InscripcionId,
                        TipoPertenenciaLibro.MatriculaOrdinaria)
            };
            foreach (var actividad in configuracion.Actividades)
            {
                decimal? nota;
                if (actividad.Origen == OrigenActividadEvaluable.Manual)
                {
                    nota = manuales
                        .Where(x => x.ActividadEvaluableId == actividad.Id &&
                            (x.InscripcionId == alumno.InscripcionId ||
                             (!x.InscripcionId.HasValue && x.AlumnoId == alumno.AlumnoId)))
                        .OrderByDescending(x => x.InscripcionId == alumno.InscripcionId)
                        .Select(x => x.Nota)
                        .FirstOrDefault();
                }
                else
                {
                    nota = CalcularNotaEntrega(entregas
                        .Where(x =>
                            x.TareaId == actividad.TareaId &&
                            x.AlumnoId == alumno.AlumnoId &&
                            (x.InscripcionId == alumno.InscripcionId || !x.InscripcionId.HasValue)));
                }
                fila.Calificaciones.Add(new CeldaCalificacion
                {
                    ActividadId = actividad.Id,
                    Nota = nota,
                    EsAutomatica = actividad.Origen == OrigenActividadEvaluable.Tarea
                });
            }

            AplicarCalculo(configuracion, fila);
            if (cerrado && usarResultadosCerrados && resultadosCerrados.TryGetValue(
                alumno.InscripcionId, out var resultadoCerrado))
            {
                fila.Desempeno = resultadoCerrado.Desempeno;
                fila.Actitudinal = resultadoCerrado.Actitudinal;
                fila.NotaBimestral = resultadoCerrado.NotaBimestral;
                fila.AbacusDesempeno = resultadoCerrado.AbacusDesempeno;
                fila.AbacusActitudinal = resultadoCerrado.AbacusActitudinal;
            }
            AplicarResultadoOficial(fila, resultadosOficialesPorInscripcion.GetValueOrDefault(
                alumno.InscripcionId), configuracion.AsignacionId);
            if (fila.TipoPertenencia == TipoPertenenciaLibro.EvidenciaHistorica &&
                !fila.Calificaciones.Any(x => x.Nota.HasValue))
                continue;
            libro.Alumnos.Add(fila);
        }
        return libro;
    }

    public async Task<ResultadoOperacion> GuardarLibroAsync(GuardarLibroCalificaciones modelo)
    {
        if (!acceso.TienePermiso(Permisos.Calificaciones.Registrar))
            return ResultadoOperacion.Error("No tiene autorización para registrar calificaciones.");

        var configuracion = await contexto.ConfiguracionesEvaluacion
            .Include(x => x.Asignacion).ThenInclude(x => x.Seccion).ThenInclude(x => x.CicloEscolar)
            .Include(x => x.Periodo)
            .Include(x => x.Actividades)
            .FirstOrDefaultAsync(x => x.Id == modelo.ConfiguracionId);
        if (configuracion == null || !await EsDocenteResponsableVigenteAsync(configuracion))
            return ResultadoOperacion.Error("No tiene autorización para registrar estas calificaciones.");
        if (await EstaCerradaAsync(configuracion.Id))
            return ResultadoOperacion.Error("El período está cerrado y no admite edición ordinaria.");

        var manuales = configuracion.Actividades
            .Where(x => x.Origen == OrigenActividadEvaluable.Manual)
            .ToDictionary(x => x.Id);
        var inscripcionesIds = modelo.Notas.Select(x => x.InscripcionId).Distinct().ToList();
        var inscripcionesPermitidas = (await PertenenciaHistorica
                .ObtenerParaSeccionAsync(configuracion, soloActivas: true))
            .Where(x => x.Value == TipoPertenenciaLibro.MatriculaOrdinaria)
            .Select(x => x.Key).ToList();
        var inscripciones = await contexto.Inscripciones
            .Where(x => inscripcionesIds.Contains(x.Id) &&
                inscripcionesPermitidas.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        var errores = new Dictionary<string, string>();
        if (modelo.Notas
            .GroupBy(x => new { x.ActividadId, x.InscripcionId })
            .Any(x => x.Count() > 1))
        {
            errores[nameof(modelo.Notas)] =
                "No se permiten calificaciones duplicadas para una misma actividad e inscripción.";
        }
        for (var i = 0; i < modelo.Notas.Count; i++)
        {
            var nota = modelo.Notas[i];
            if (!manuales.TryGetValue(nota.ActividadId, out var actividad) ||
                !inscripciones.ContainsKey(nota.InscripcionId))
                errores[$"Notas[{i}].Nota"] = "La actividad o inscripción no pertenece al libro autorizado.";
            else if (nota.Nota.HasValue && (nota.Nota < 0 || nota.Nota > actividad.PunteoMaximo))
                errores[$"Notas[{i}].Nota"] = $"La nota debe estar entre 0 y {actividad.PunteoMaximo}.";
        }
        if (errores.Count > 0) return ResultadoOperacion.Validacion(errores);

        var usuarioId = acceso.UsuarioId!;
        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            foreach (var nota in modelo.Notas)
            {
                if (!manuales.ContainsKey(nota.ActividadId)) continue;
                var inscripcion = inscripciones[nota.InscripcionId];
                var registro = await contexto.CalificacionesManuales.FirstOrDefaultAsync(x =>
                    x.ActividadEvaluableId == nota.ActividadId &&
                    x.InscripcionId == nota.InscripcionId);
                var anterior = registro?.Nota;
                if (anterior == nota.Nota) continue;

                if (registro == null)
                {
                    registro = new CalificacionManual
                    {
                        ActividadEvaluableId = nota.ActividadId,
                        AlumnoId = inscripcion.AlumnoId,
                        InscripcionId = inscripcion.Id
                    };
                    contexto.CalificacionesManuales.Add(registro);
                }
                registro.Nota = nota.Nota;
                registro.Estado = nota.Nota.HasValue
                    ? EstadoCalificacionManual.Calificada
                    : EstadoCalificacionManual.Pendiente;
                registro.FechaCalificacion = nota.Nota.HasValue ? DateTime.UtcNow : null;
                registro.CalificadoPorUsuarioId = nota.Nota.HasValue ? usuarioId : null;
                await contexto.SaveChangesAsync();

                contexto.HistorialesCalificaciones.Add(new HistorialCalificacion
                {
                    TipoOrigen = TipoOrigenHistorialCalificacion.Manual,
                    CalificacionManualId = registro.Id,
                    NotaAnterior = anterior,
                    NotaNueva = nota.Nota,
                    Motivo = "Actualización ordinaria antes del cierre del período.",
                    FechaCambio = DateTime.UtcNow,
                    ModificadoPorUsuarioId = usuarioId
                });
            }
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto("Las calificaciones fueron guardadas correctamente.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultadoOperacion> CerrarAsync(int configuracionId)
    {
        var libro = await ObtenerLibroAsync(configuracionId, false, true);
        if (libro == null || !libro.PuedeCerrar)
            return ResultadoOperacion.Error("No tiene autorización para cerrar este período.");
        var bloqueo = await ObtenerBloqueoCierreAsync(configuracionId);
        if (bloqueo != null)
            return ResultadoOperacion.Error(bloqueo);
        if (libro.Alumnos.Count == 0)
            return ResultadoOperacion.Error("No existen alumnos para cerrar el período.");
        if (libro.Alumnos.Any(x => x.Pendientes > 0))
            return ResultadoOperacion.Error("No puede cerrar mientras existan calificaciones pendientes.");

        var usuarioId = acceso.UsuarioId;
        if (string.IsNullOrWhiteSpace(usuarioId)) return ResultadoOperacion.Error("No se identificó al usuario.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var cierre = await contexto.CierresCalificaciones
                .FirstOrDefaultAsync(x => x.ConfiguracionEvaluacionId == configuracionId);
            if (cierre?.Estado == EstadoCierreCalificaciones.Cerrado)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error("El período ya se encuentra cerrado.");
            }

            bloqueo = await ObtenerBloqueoCierreAsync(configuracionId);
            if (bloqueo != null)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(bloqueo);
            }

            var libroActual = await ObtenerLibroAsync(configuracionId, false, true);
            if (libroActual == null || libroActual.Alumnos.Count == 0)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error("No existen alumnos para cerrar el período.");
            }
            if (libroActual.Alumnos.Any(x => x.Pendientes > 0))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "No puede cerrar mientras existan calificaciones pendientes.");
            }

            var fecha = DateTime.UtcNow;
            if (cierre == null)
            {
                cierre = new CierreCalificaciones { ConfiguracionEvaluacionId = configuracionId };
                contexto.CierresCalificaciones.Add(cierre);
            }
            var anterior = cierre.Estado;
            cierre.Estado = EstadoCierreCalificaciones.Cerrado;
            cierre.FechaCierre = fecha;
            cierre.CerradoPorUsuarioId = usuarioId;
            await contexto.SaveChangesAsync();

            contexto.MovimientosCierresCalificaciones.Add(new MovimientoCierreCalificaciones
            {
                CierreCalificacionesId = cierre.Id,
                EstadoAnterior = anterior,
                EstadoNuevo = EstadoCierreCalificaciones.Cerrado,
                Motivo = "Cierre bimestral confirmado.",
                Fecha = fecha,
                RealizadoPorUsuarioId = usuarioId
            });
            contexto.ResultadosCalificacionesPeriodos.AddRange(libroActual.Alumnos.Select(x =>
                new ResultadoCalificacionPeriodo
                {
                    ConfiguracionEvaluacionId = configuracionId,
                    InscripcionId = x.InscripcionId,
                    AlumnoId = x.AlumnoId,
                    Desempeno = x.Desempeno,
                    Actitudinal = x.Actitudinal,
                    NotaBimestral = x.NotaBimestral,
                    AbacusDesempeno = x.AbacusDesempeno,
                    AbacusActitudinal = x.AbacusActitudinal,
                    FechaCierre = fecha
                }));
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto("El período fue cerrado y sus resultados quedaron congelados.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<ResumenAbacus?> ObtenerResumenAbacusAsync(int configuracionId)
    {
        var libro = await ObtenerLibroAsync(configuracionId, false, false);
        if (libro == null) return null;
        var cerrados = await contexto.ResultadosCalificacionesPeriodos.AsNoTracking()
            .Where(x => x.ConfiguracionEvaluacionId == configuracionId)
            .Select(x => new FilaResumenAbacus
            {
                ResultadoId = x.Id,
                InscripcionId = x.InscripcionId,
                CodigoPersonal = x.Alumno.CodigoPersonal ?? "Sin código",
                Alumno = x.Alumno.Usuario.PrimerNombre + " " + x.Alumno.Usuario.PrimerApellido,
                Desempeno = x.Desempeno,
                Actitudinal = x.Actitudinal,
                NotaBimestral = x.NotaBimestral,
                AbacusDesempeno = x.AbacusDesempeno,
                AbacusActitudinal = x.AbacusActitudinal
            })
            .ToListAsync();
        return new ResumenAbacus
        {
            ConfiguracionId = configuracionId,
            Curso = libro.Curso,
            GradoSeccion = libro.GradoSeccion,
            CicloPeriodo = libro.Ciclo + " · " + libro.Periodo,
            EsResultadoCerrado = cerrados.Count > 0,
            Actividades = libro.Actividades.Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.Categoria + " · " + x.Nombre
            }).ToList(),
            Alumnos = cerrados.Count > 0
                ? cerrados
                : libro.Alumnos.Select(x => new FilaResumenAbacus
                {
                    InscripcionId = x.InscripcionId,
                    CodigoPersonal = x.CodigoPersonal,
                    Alumno = x.Alumno,
                    Desempeno = x.Desempeno,
                    Actitudinal = x.Actitudinal,
                    NotaBimestral = x.NotaBimestral,
                    AbacusDesempeno = x.AbacusDesempeno,
                    AbacusActitudinal = x.AbacusActitudinal
                }).ToList()
        };
    }

    public async Task<CorreccionesCalificaciones> ObtenerCorreccionesAsync(bool puedeRevisar)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var consulta = contexto.SolicitudesCorreccionesCalificaciones.AsNoTracking().AsQueryable();
        var cicloSeleccionadoId = await ObtenerCicloSeleccionadoIdAsync();
        if (contextoCiclo is not null)
            consulta = cicloSeleccionadoId.HasValue
                ? consulta.Where(x => x.ActividadEvaluable.ConfiguracionEvaluacion.CicloEscolarId == cicloSeleccionadoId.Value)
                : consulta.Where(_ => false);
        if (puedeRevisar && acceso.TienePermiso(Permisos.Calificaciones.AprobarCorreccion))
        {
            // La aprobación explícita permite supervisar todas las solicitudes.
        }
        else if (perfil.DocenteId.HasValue)
            consulta = consulta.Where(x => x.ActividadEvaluable.ConfiguracionEvaluacion.Asignacion.DocenteId == perfil.DocenteId.Value);
        else
            consulta = consulta.Where(_ => false);

        return new CorreccionesCalificaciones
        {
            PuedeRevisar = puedeRevisar &&
                acceso.TienePermiso(Permisos.Calificaciones.AprobarCorreccion),
            Solicitudes = await consulta.OrderByDescending(x => x.FechaSolicitud)
                .Select(x => new CorreccionCalificacionLista
                {
                    Id = x.Id,
                    Curso = x.ActividadEvaluable.ConfiguracionEvaluacion.Asignacion.Curso.Nombre,
                    Periodo = x.ActividadEvaluable.ConfiguracionEvaluacion.Periodo.Nombre,
                    Alumno = x.Alumno.Usuario.PrimerNombre + " " + x.Alumno.Usuario.PrimerApellido,
                    Actividad = x.ActividadEvaluable.Nombre,
                    NotaAnterior = x.NotaAnterior,
                    NotaPropuesta = x.NotaPropuesta,
                    Motivo = x.MotivoSolicitud,
                    Estado = x.Estado,
                    FechaSolicitud = x.FechaSolicitud,
                    VersionConcurrencia = x.VersionConcurrencia,
                    PuedeAplicar = perfil.DocenteId.HasValue &&
                        acceso.TienePermiso(Permisos.Calificaciones.SolicitarCorreccion) &&
                        x.Estado == EstadoSolicitudCorreccion.Aprobada &&
                        (x.ActividadEvaluable.ConfiguracionEvaluacion.Asignacion.DocenteId == perfil.DocenteId.Value ||
                         contexto.DelegacionesAcademicas.Any(d =>
                            d.AsignacionId == x.ActividadEvaluable.AsignacionId &&
                            d.DocenteId == perfil.DocenteId.Value && d.Activa &&
                            d.FechaInicioUtc <= DateTime.UtcNow &&
                            (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= DateTime.UtcNow) &&
                            (d.Operaciones & OperacionDelegacionAcademica.SolicitarCorrecciones) ==
                                OperacionDelegacionAcademica.SolicitarCorrecciones))
                }).ToListAsync()
        };
    }

    public async Task<ResultadoOperacion> SolicitarCorreccionAsync(SolicitarCorreccionCalificacion modelo)
    {
        if (!acceso.TienePermiso(Permisos.Calificaciones.SolicitarCorreccion))
            return ResultadoOperacion.Error("No tiene autorización para solicitar correcciones.");

        var resultado = await contexto.ResultadosCalificacionesPeriodos.AsNoTracking()
            .Include(x => x.ConfiguracionEvaluacion).ThenInclude(x => x.Asignacion)
            .FirstOrDefaultAsync(x => x.Id == modelo.ResultadoId);
        if (resultado == null ||
            !await PerteneceAlCicloSeleccionadoAsync(resultado.ConfiguracionEvaluacion.CicloEscolarId))
            return ResultadoOperacion.Error("No tiene autorización para solicitar esta corrección.");

        var actividad = await contexto.ActividadesEvaluables.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == modelo.ActividadId &&
                x.ConfiguracionEvaluacionId == resultado.ConfiguracionEvaluacionId);
        if (actividad == null ||
            !await EsDocenteResponsableVigenteOHistoricoAsync(resultado.ConfiguracionEvaluacion))
            return ResultadoOperacion.Error("No tiene autorización para solicitar esta corrección.");
        if (modelo.NotaPropuesta < 0 || modelo.NotaPropuesta > actividad.PunteoMaximo)
            return ResultadoOperacion.Validacion(nameof(modelo.NotaPropuesta),
                $"La nota debe estar entre 0 y {actividad.PunteoMaximo}.");
        if (string.IsNullOrWhiteSpace(modelo.Motivo) || modelo.Motivo.Trim().Length < 10)
            return ResultadoOperacion.Validacion(nameof(modelo.Motivo),
                "El motivo debe contener al menos 10 caracteres.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);
        try
        {
            if (await contexto.SolicitudesCorreccionesCalificaciones.AnyAsync(x =>
                x.ResultadoCalificacionPeriodoId == modelo.ResultadoId &&
                x.ActividadEvaluableId == modelo.ActividadId &&
                (x.Estado == EstadoSolicitudCorreccion.Pendiente ||
                 x.Estado == EstadoSolicitudCorreccion.Aprobada)))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "Ya existe una solicitud pendiente o aprobada para esta calificación.");
            }

            var actual = await ObtenerNotaActividadAsync(
                actividad, resultado.InscripcionId, resultado.AlumnoId);
            if (!actual.HasValue)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "La calificación seleccionada no tiene un valor registrado.");
            }
            if (actual.Value == modelo.NotaPropuesta)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "La nota propuesta debe ser diferente de la nota actual.");
            }

            contexto.SolicitudesCorreccionesCalificaciones.Add(
                new SolicitudCorreccionCalificacion
                {
                    ResultadoCalificacionPeriodoId = resultado.Id,
                    ActividadEvaluableId = actividad.Id,
                    InscripcionId = resultado.InscripcionId,
                    AlumnoId = resultado.AlumnoId,
                    NotaAnterior = actual.Value,
                    NotaPropuesta = modelo.NotaPropuesta,
                    MotivoSolicitud = modelo.Motivo.Trim(),
                    SolicitadaPorUsuarioId = acceso.UsuarioId!,
                    FechaSolicitud = DateTime.UtcNow
                });
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto(
                "La solicitud de corrección fue enviada a revisión.");
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return ResultadoOperacion.Error(
                "No fue posible registrar la solicitud por un conflicto concurrente. Intente nuevamente.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultadoOperacion> RevisarCorreccionAsync(RevisarCorreccionCalificacion modelo)
    {
        if (!acceso.TienePermiso(Permisos.Calificaciones.AprobarCorreccion))
            return ResultadoOperacion.Error("No tiene autorización para revisar correcciones.");
        if (!modelo.Aprobar && string.IsNullOrWhiteSpace(modelo.Observacion))
            return ResultadoOperacion.Validacion(nameof(modelo.Observacion),
                "Indique el motivo del rechazo.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);
        try
        {
            var solicitud = await contexto.SolicitudesCorreccionesCalificaciones
                .Include(x => x.ActividadEvaluable).ThenInclude(x => x.ConfiguracionEvaluacion)
                .FirstOrDefaultAsync(x => x.Id == modelo.Id);
            if (solicitud == null || solicitud.Estado != EstadoSolicitudCorreccion.Pendiente ||
                solicitud.VersionConcurrencia != modelo.VersionConcurrencia ||
                !await PerteneceAlCicloSeleccionadoAsync(
                    solicitud.ActividadEvaluable.ConfiguracionEvaluacion.CicloEscolarId))
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "La solicitud cambió o ya fue revisada. Actualice el listado.");
            }

            solicitud.Estado = modelo.Aprobar
                ? EstadoSolicitudCorreccion.Aprobada
                : EstadoSolicitudCorreccion.Rechazada;
            solicitud.RevisadaPorUsuarioId = acceso.UsuarioId;
            solicitud.FechaRevision = DateTime.UtcNow;
            solicitud.ObservacionRevision = string.IsNullOrWhiteSpace(modelo.Observacion)
                ? null : modelo.Observacion.Trim();
            solicitud.VersionConcurrencia = Guid.NewGuid();
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto(modelo.Aprobar
                ? "La corrección fue autorizada para una sola aplicación."
                : "La solicitud fue rechazada.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaccion.RollbackAsync();
            return ResultadoOperacion.Error(
                "La solicitud fue modificada por otro usuario. Actualice el listado.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultadoOperacion> AplicarCorreccionAsync(AplicarCorreccionCalificacion modelo)
    {
        if (!acceso.TienePermiso(Permisos.Calificaciones.SolicitarCorreccion))
            return ResultadoOperacion.Error("No tiene autorización para aplicar correcciones.");

        var solicitud = await contexto.SolicitudesCorreccionesCalificaciones
            .Include(x => x.ActividadEvaluable)
            .Include(x => x.ResultadoCalificacionPeriodo)
            .Include(x => x.ActividadEvaluable).ThenInclude(x => x.ConfiguracionEvaluacion)
                .ThenInclude(x => x.Asignacion)
            .FirstOrDefaultAsync(x => x.Id == modelo.Id);
        if (solicitud == null || solicitud.Estado != EstadoSolicitudCorreccion.Aprobada ||
            solicitud.VersionConcurrencia != modelo.VersionConcurrencia ||
            !await PerteneceAlCicloSeleccionadoAsync(
                solicitud.ActividadEvaluable.ConfiguracionEvaluacion.CicloEscolarId) ||
            solicitud.ActividadEvaluable.ConfiguracionEvaluacionId !=
                solicitud.ResultadoCalificacionPeriodo.ConfiguracionEvaluacionId ||
            !await EsDocenteResponsableVigenteOHistoricoAsync(
                solicitud.ActividadEvaluable.ConfiguracionEvaluacion))
            return ResultadoOperacion.Error("La autorización no existe, cambió o no puede ser aplicada por este usuario.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var notaActual = await ObtenerNotaActividadAsync(
                solicitud.ActividadEvaluable,
                solicitud.InscripcionId,
                solicitud.AlumnoId);
            if (!notaActual.HasValue || notaActual.Value != solicitud.NotaAnterior)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "La nota original cambió después de la solicitud. La autorización no puede aplicarse.");
            }

            var notaAnterior = await ActualizarNotaOrigenAsync(solicitud);
            if (!notaAnterior.HasValue)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error("No fue posible localizar la calificación original.");
            }
            solicitud.Estado = EstadoSolicitudCorreccion.Aplicada;
            solicitud.AplicadaPorUsuarioId = acceso.UsuarioId;
            solicitud.FechaAplicacion = DateTime.UtcNow;
            solicitud.VersionConcurrencia = Guid.NewGuid();
            await contexto.SaveChangesAsync();

            var libro = await ObtenerLibroAsync(
                solicitud.ActividadEvaluable.ConfiguracionEvaluacionId,
                false,
                false,
                false);
            var fila = libro?.Alumnos.FirstOrDefault(x => x.InscripcionId == solicitud.InscripcionId);
            if (fila == null)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error("No fue posible recalcular el resultado histórico.");
            }
            var resultado = solicitud.ResultadoCalificacionPeriodo;
            resultado.Desempeno = fila.Desempeno;
            resultado.Actitudinal = fila.Actitudinal;
            resultado.NotaBimestral = fila.NotaBimestral;
            resultado.AbacusDesempeno = fila.AbacusDesempeno;
            resultado.AbacusActitudinal = fila.AbacusActitudinal;
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto("La corrección autorizada fue aplicada y auditada.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaccion.RollbackAsync();
            return ResultadoOperacion.Error("La solicitud fue modificada por otro usuario. Actualice el listado.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private async Task<Dictionary<string, string>> ValidarActualizacionConfiguracionAsync(
        ConfiguracionEvaluacion configuracion,
        ConfigurarEvaluacion modelo)
    {
        var errores = new Dictionary<string, string>();
        var categorias = configuracion.Categorias.ToDictionary(x => x.Id);
        var actividades = configuracion.Actividades.ToDictionary(x => x.Id);
        var categoriasEnviadas = modelo.Categorias.Where(x => x.Id > 0).ToList();
        var actividadesEnviadas = modelo.Categorias.SelectMany(x => x.Actividades)
            .Where(x => x.Id > 0).ToList();

        if (categoriasEnviadas.GroupBy(x => x.Id).Any(x => x.Count() > 1) ||
            actividadesEnviadas.GroupBy(x => x.Id).Any(x => x.Count() > 1))
        {
            errores[nameof(modelo.Categorias)] =
                "La solicitud contiene identificadores de categorías o actividades repetidos.";
            return errores;
        }

        if (categoriasEnviadas.Any(x => !categorias.ContainsKey(x.Id)) ||
            actividadesEnviadas.Any(x => !actividades.ContainsKey(x.Id)))
        {
            errores[nameof(modelo.Categorias)] =
                "Las categorías o actividades indicadas no pertenecen a esta configuración.";
            return errores;
        }

        foreach (var categoriaModelo in modelo.Categorias.Where(x => x.Id > 0))
        {
            foreach (var actividadModelo in categoriaModelo.Actividades.Where(x => x.Id > 0))
            {
                if (actividades[actividadModelo.Id].CategoriaEvaluacionId != categoriaModelo.Id)
                {
                    errores[nameof(modelo.Categorias)] =
                        "No puede trasladar una actividad existente a otra categoría.";
                    return errores;
                }
            }
        }

        var actividadesIds = actividades.Keys.ToList();
        var tareasIds = actividades.Values.Where(x => x.TareaId.HasValue)
            .Select(x => x.TareaId!.Value).Distinct().ToList();
        var manualesConDependencia = await contexto.CalificacionesManuales.AsNoTracking()
            .Where(x => actividadesIds.Contains(x.ActividadEvaluableId))
            .Select(x => x.ActividadEvaluableId).Distinct().ToListAsync();
        var historialesManuales = await contexto.HistorialesCalificaciones.AsNoTracking()
            .Where(x => x.CalificacionManualId.HasValue &&
                actividadesIds.Contains(x.CalificacionManual!.ActividadEvaluableId))
            .Select(x => x.CalificacionManual!.ActividadEvaluableId).Distinct().ToListAsync();
        var tareasConDependencia = tareasIds.Count == 0 ? [] : await contexto.Entregas.AsNoTracking()
            .Where(x => x.Calificacion.HasValue && tareasIds.Contains(x.TareaId))
            .Select(x => x.TareaId).Distinct().ToListAsync();
        var tareasConHistorial = tareasIds.Count == 0 ? [] : await contexto.HistorialesCalificaciones.AsNoTracking()
            .Where(x => x.EntregaId.HasValue && x.Entrega != null &&
                tareasIds.Contains(x.Entrega.TareaId))
            .Select(x => x.Entrega!.TareaId).Distinct().ToListAsync();

        var actividadesProtegidas = actividades.Values.Where(x =>
                manualesConDependencia.Contains(x.Id) ||
                historialesManuales.Contains(x.Id) ||
                (x.TareaId.HasValue && (tareasConDependencia.Contains(x.TareaId.Value) ||
                    tareasConHistorial.Contains(x.TareaId.Value))))
            .Select(x => x.Id).ToHashSet();
        var hayDependencias = actividadesProtegidas.Count > 0;

        if (hayDependencias && configuracion.MetodoCalculo != modelo.MetodoCalculo)
            errores[nameof(modelo.MetodoCalculo)] =
                "No puede cambiar el método de cálculo porque existen calificaciones o historiales vinculados.";

        foreach (var categoria in categorias.Values)
        {
            var enviada = categoriasEnviadas.FirstOrDefault(x => x.Id == categoria.Id);
            var categoriaProtegida = configuracion.Actividades.Any(x =>
                x.CategoriaEvaluacionId == categoria.Id && actividadesProtegidas.Contains(x.Id));
            if (enviada is null)
            {
                if (categoriaProtegida)
                    errores[nameof(modelo.Categorias)] =
                        "No puede eliminar una categoría que contiene calificaciones o historial académico.";
                continue;
            }
            if (categoriaProtegida && (categoria.Porcentaje != enviada.Porcentaje ||
                categoria.Tipo != enviada.Tipo))
                errores[nameof(modelo.Categorias)] =
                    "No puede cambiar la ponderación o el tipo de una categoría con calificaciones existentes.";
        }

        foreach (var actividad in actividades.Values)
        {
            var enviada = actividadesEnviadas.FirstOrDefault(x => x.Id == actividad.Id);
            if (enviada is null)
            {
                if (actividadesProtegidas.Contains(actividad.Id))
                    errores[nameof(modelo.Categorias)] =
                        "No puede eliminar una actividad con calificaciones o historial académico.";
                continue;
            }
            if (actividadesProtegidas.Contains(actividad.Id) &&
                (actividad.PunteoMaximo != enviada.PunteoMaximo ||
                 actividad.TareaId != enviada.TareaId))
                errores[nameof(modelo.Categorias)] =
                    "No puede cambiar el punteo ni la tarea vinculada de una actividad ya calificada.";
        }

        return errores;
    }

    private static bool ConfiguracionCambio(
        ConfiguracionEvaluacion configuracion, ConfigurarEvaluacion modelo)
    {
        if (configuracion.MetodoCalculo != modelo.MetodoCalculo ||
            configuracion.Categorias.Count != modelo.Categorias.Count ||
            configuracion.Actividades.Count != modelo.Categorias.Sum(x => x.Actividades.Count))
            return true;

        var categorias = configuracion.Categorias.ToDictionary(x => x.Id);
        var actividades = configuracion.Actividades.ToDictionary(x => x.Id);
        foreach (var categoria in modelo.Categorias)
        {
            if (categoria.Id == 0 || !categorias.TryGetValue(categoria.Id, out var actual) ||
                actual.Nombre != categoria.Nombre.Trim() || actual.Tipo != categoria.Tipo ||
                actual.Porcentaje != categoria.Porcentaje)
                return true;
            foreach (var actividad in categoria.Actividades)
            {
                if (actividad.Id == 0 || !actividades.TryGetValue(actividad.Id, out var existente) ||
                    existente.Nombre != actividad.Nombre.Trim() ||
                    existente.PunteoMaximo != actividad.PunteoMaximo ||
                    existente.TareaId != actividad.TareaId)
                    return true;
            }
        }
        return false;
    }

    private static string ResumenConfiguracion(ConfiguracionEvaluacion configuracion) =>
        JsonSerializer.Serialize(new
        {
            configuracion.MetodoCalculo,
            Categorias = configuracion.Categorias.OrderBy(x => x.Orden).Select(c => new
            {
                c.Id, c.Nombre, c.Tipo, c.Porcentaje,
                Actividades = configuracion.Actividades.Where(a => a.CategoriaEvaluacionId == c.Id)
                    .OrderBy(a => a.Orden).Select(a => new { a.Id, a.Nombre, a.PunteoMaximo, a.TareaId })
            })
        }, JsonOpciones);

    private static string ResumenFormulario(ConfigurarEvaluacion modelo) =>
        JsonSerializer.Serialize(new
        {
            modelo.MetodoCalculo,
            Categorias = modelo.Categorias.Select(c => new
            {
                c.Id, c.Nombre, c.Tipo, c.Porcentaje,
                Actividades = c.Actividades.Select(a => new { a.Id, a.Nombre, a.PunteoMaximo, a.TareaId })
            })
        }, JsonOpciones);

    private async Task<Dictionary<string, string>> ValidarConfiguracionAsync(ConfigurarEvaluacion modelo)
    {
        var errores = new Dictionary<string, string>();
        if (!await PuedeConfigurarAsync(modelo.AsignacionId))
            errores[nameof(modelo.AsignacionId)] = "La asignación no está autorizada o vigente.";
        var cicloId = await contexto.Asignaciones.AsNoTracking()
            .Where(x => x.Id == modelo.AsignacionId)
            .Select(x => (int?)x.Seccion.CicloEscolarId)
            .FirstOrDefaultAsync();
        if (!cicloId.HasValue || !await contexto.Periodos.AsNoTracking().AnyAsync(x =>
            x.Id == modelo.PeriodoId && x.CicloEscolarId == cicloId.Value))
            errores[nameof(modelo.PeriodoId)] = "El período no pertenece al ciclo de la asignación.";
        if (modelo.Categorias.Count == 0)
            errores[nameof(modelo.Categorias)] = "Agregue al menos una categoría.";
        else if (modelo.Categorias.Sum(x => x.Porcentaje) != 100m)
            errores[nameof(modelo.Categorias)] = "Las ponderaciones deben sumar exactamente 100.";
        if (modelo.Categorias.Any(x => string.IsNullOrWhiteSpace(x.Nombre) || x.Actividades.Count == 0))
            errores[nameof(modelo.Categorias)] = "Cada categoría necesita nombre y al menos una actividad.";
        var actividades = modelo.Categorias.SelectMany(x => x.Actividades).ToList();
        if (actividades.Any(x => string.IsNullOrWhiteSpace(x.Nombre) || x.PunteoMaximo <= 0))
            errores[nameof(modelo.Categorias)] = "Todas las actividades necesitan nombre y punteo máximo.";
        if (modelo.MetodoCalculo == MetodoCalculoEvaluacion.SumaPuntos &&
            modelo.Categorias.Any(x => x.Actividades.Sum(a => a.PunteoMaximo) != x.Porcentaje))
        {
            errores[nameof(modelo.Categorias)] =
                "En suma de puntos, el máximo de las actividades de cada categoría debe coincidir con su ponderación.";
        }
        if (modelo.MetodoCalculo == MetodoCalculoEvaluacion.SumaPuntos)
        {
            var desempeno = modelo.Categorias
                .Where(x => x.Tipo == TipoCategoriaEvaluacion.Desempeno)
                .Sum(x => x.Actividades.Count);
            var actitudinal = modelo.Categorias
                .Where(x => x.Tipo == TipoCategoriaEvaluacion.Actitudinal)
                .Sum(x => x.Actividades.Count);
            if (desempeno is < 1 or > 8)
                errores[nameof(modelo.Categorias)] =
                    "La metodología institucional admite entre 1 y 8 actividades de desempeño.";
            else if (actitudinal != 5)
                errores[nameof(modelo.Categorias)] =
                    "La metodología institucional requiere cinco aspectos actitudinales configurables.";
        }

        // VALIDAR TAREAS VINCULADAS AL PERÍODO ACADÉMICO
        var tareas = actividades
            .Where(x => x.TareaId.HasValue)
            .Select(x => x.TareaId!.Value)
            .ToList();

        if (modelo.Id == 0 && tareas.Count > 0)
        {
            errores[nameof(modelo.Categorias)] =
                "Las nuevas configuraciones oficiales sólo admiten actividades manuales.";
        }
        else if (tareas.Count != tareas.Distinct().Count())
        {
            errores[nameof(modelo.Categorias)] =
                "Una tarea no puede vincularse más de una vez.";
        }
        else if (tareas.Count > 0)
        {
            var tareasVinculables = await contexto.Tareas
                .AsNoTracking()
                .Where(x =>
                    tareas.Contains(x.Id) &&
                    x.AsignacionId == modelo.AsignacionId &&
                    x.UnidadAsignacionId.HasValue &&
                    x.UnidadAsignacion != null &&
                    x.UnidadAsignacion.AsignacionId == modelo.AsignacionId &&
                    x.UnidadAsignacion.PeriodoId == modelo.PeriodoId &&
                    x.UnidadAsignacion.Periodo != null &&
                    x.UnidadAsignacion.Periodo.CicloEscolarId == cicloId)
                .Select(x => new
                {
                    x.Id,
                    x.PunteoMaximo
                })
                .ToDictionaryAsync(x => x.Id);

            if (tareasVinculables.Count != tareas.Count)
            {
                errores[nameof(modelo.Categorias)] =
                    "Las tareas vinculadas deben pertenecer a la asignación y al período académico seleccionado.";
            }
            else if (actividades.Any(x =>
                x.TareaId.HasValue &&
                tareasVinculables[x.TareaId.Value].PunteoMaximo != x.PunteoMaximo))
            {
                errores[nameof(modelo.Categorias)] =
                    "El punteo máximo de una actividad vinculada debe coincidir con el de su tarea.";
            }
        }

        if (modelo.GuardarComoPlantilla &&
            string.IsNullOrWhiteSpace(modelo.NombrePlantilla))
        {
            errores[nameof(modelo.NombrePlantilla)] =
                "Indique el nombre de la plantilla.";
        }

        return errores;
    }


    private async Task<List<OpcionSeleccion>> ObtenerAsignacionesConfigurablesAsync(int? docenteId)
    {
        var consulta = contexto.Asignaciones.AsNoTracking().Where(x =>
            x.Estado == EstadoRegistro.Activo && x.Seccion.CicloEscolar.Activo);
        var cicloSeleccionadoId = await ObtenerCicloSeleccionadoIdAsync();
        if (contextoCiclo is not null)
            consulta = cicloSeleccionadoId.HasValue
                ? consulta.Where(x => x.Seccion.CicloEscolarId == cicloSeleccionadoId.Value)
                : consulta.Where(_ => false);
        if (docenteId.HasValue)
        {
            var ahora = DateTime.UtcNow;
            consulta = consulta.Where(x =>
                x.DocenteId == docenteId.Value ||
                contexto.DelegacionesAcademicas.Any(d =>
                    d.AsignacionId == x.Id &&
                    d.DocenteId == docenteId.Value &&
                    d.Activa && d.FechaInicioUtc <= ahora &&
                    (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                    (d.Operaciones & OperacionDelegacionAcademica.GestionarEvaluaciones) ==
                        OperacionDelegacionAcademica.GestionarEvaluaciones));
        }
        else
            consulta = consulta.Where(_ => false);
        return await consulta.OrderBy(x => x.Curso.Nombre)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.Curso.Nombre + " · " + x.Seccion.Grado.Nombre + " " +
                    x.Seccion.Nombre + " · " + x.Seccion.CicloEscolar.Anio
            }).ToListAsync();
    }

    private async Task<bool> PuedeConfigurarAsync(int asignacionId)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        if (!perfil.DocenteId.HasValue ||
            !acceso.TienePermiso(Permisos.Calificaciones.Configurar))
        {
            return false;
        }

        if (await acceso.EsDocenteDeAsignacionAsync(asignacionId))
            return true;

        return delegaciones != null && await delegaciones.PuedeActuarAsync(
            asignacionId,
            perfil.DocenteId.Value,
            OperacionDelegacionAcademica.GestionarEvaluaciones);
    }

    private async Task<int?> ObtenerCicloSeleccionadoIdAsync() =>
        contextoCiclo is null ? null : (await contextoCiclo.ObtenerAsync()).CicloEscolarId;

    private async Task<bool> PerteneceAlCicloSeleccionadoAsync(int cicloEscolarId) =>
        contextoCiclo is null || (await contextoCiclo.ObtenerAsync()).CicloEscolarId == cicloEscolarId;

    private async Task<bool> AsignacionPerteneceAlCicloSeleccionadoAsync(int asignacionId)
    {
        if (contextoCiclo is null) return true;
        var cicloId = await ObtenerCicloSeleccionadoIdAsync();
        return cicloId.HasValue && await contexto.Asignaciones.AsNoTracking()
            .AnyAsync(x => x.Id == asignacionId && x.Seccion.CicloEscolarId == cicloId.Value);
    }

    private async Task<bool> PeriodoPerteneceAlCicloSeleccionadoAsync(int periodoId)
    {
        if (contextoCiclo is null) return true;
        var cicloId = await ObtenerCicloSeleccionadoIdAsync();
        return cicloId.HasValue && await contexto.Periodos.AsNoTracking()
            .AnyAsync(x => x.Id == periodoId && x.CicloEscolarId == cicloId.Value);
    }

    private async Task<bool> PuedeConsultarAsync(ConfiguracionEvaluacion configuracion)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        if (perfil.DocenteId.HasValue)
        {
            if (configuracion.Asignacion.DocenteId == perfil.DocenteId.Value)
                return true;

            return delegaciones != null && await delegaciones.PuedeActuarAsync(
                configuracion.AsignacionId,
                perfil.DocenteId.Value,
                OperacionDelegacionAcademica.GestionarEvaluaciones);
        }
        if (perfil.AlumnoId.HasValue)
            return await contexto.Inscripciones.AsNoTracking().AnyAsync(x =>
                x.AlumnoId == perfil.AlumnoId.Value &&
                x.SeccionId == configuracion.SeccionId &&
                x.CicloEscolarId == configuracion.CicloEscolarId);
        return acceso.EsOperadorInstitucional &&
            acceso.TienePermiso(Permisos.Calificaciones.Ver);
    }

    private async Task<bool> EsDocenteResponsableVigenteAsync(ConfiguracionEvaluacion configuracion)
    {
        var docenteId = await acceso.ObtenerDocenteIdAsync();
        if (!docenteId.HasValue || !configuracion.Asignacion.Seccion.CicloEscolar.Activo)
            return false;
        if (configuracion.Asignacion.DocenteId == docenteId.Value &&
            configuracion.Asignacion.Estado == EstadoRegistro.Activo)
            return true;
        return false;
    }

    private async Task<bool> EsDocenteResponsableVigenteOHistoricoAsync(
        ConfiguracionEvaluacion configuracion)
    {
        var docenteId = await acceso.ObtenerDocenteIdAsync();
        if (!docenteId.HasValue) return false;
        if (configuracion.Asignacion.DocenteId == docenteId.Value) return true;
        return delegaciones != null && await delegaciones.PuedeActuarAsync(
            configuracion.AsignacionId, docenteId.Value,
            OperacionDelegacionAcademica.SolicitarCorrecciones);
    }

    private async Task<string?> ObtenerBloqueoCierreAsync(int configuracionId)
    {
        var configuracion = await contexto.ConfiguracionesEvaluacion.AsNoTracking()
            .Where(x => x.Id == configuracionId)
            .Select(x => new
            {
                x.AsignacionId,
                x.SeccionId,
                x.CicloEscolarId,
                x.PeriodoId,
                Inicio = x.Periodo.FechaInicio,
                Fin = x.Periodo.FechaFin,
                Periodo = x.Periodo.Nombre
            })
            .FirstOrDefaultAsync();
        if (configuracion == null)
            return "No se encontró la configuración de evaluación.";

        // Los períodos son fechas civiles de Guatemala. Se convierten sus
        // medianoches locales a UTC antes de compararlas con timestamptz.
        // Se conserva la regla vigente: el primer día no bloquea el cierre;
        // desde el día siguiente el límite es inclusivo y el fin exclusivo.
        var inicioTrasladosDurantePeriodo = HoraGuatemala.ParaGuardar(
            configuracion.Inicio.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var finExclusivo = HoraGuatemala.ParaGuardar(
            configuracion.Fin.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var trasladoPendiente = await contexto.HistorialTraslados.AsNoTracking().AnyAsync(x =>
            x.CicloEscolarId == configuracion.CicloEscolarId &&
            (x.SeccionOrigenId == configuracion.SeccionId ||
             x.SeccionDestinoId == configuracion.SeccionId) &&
            x.FechaTraslado >= inicioTrasladosDurantePeriodo &&
            x.FechaTraslado < finExclusivo);
        // Los traslados ya no requieren resolución para cerrar el resultado
        // manual vigente; la instantánea de cierre conserva el historial.
        if (false && trasladoPendiente)
        {
            return $"Existe un traslado durante {configuracion.Periodo} pendiente de resolución académica.";
        }

        var tareas = await contexto.Tareas.AsNoTracking()
            .Where(x => x.AsignacionId == configuracion.AsignacionId &&
                x.UnidadAsignacionId.HasValue &&
                x.UnidadAsignacion != null &&
                x.UnidadAsignacion.AsignacionId == configuracion.AsignacionId &&
                x.UnidadAsignacion.PeriodoId == configuracion.PeriodoId &&
                x.UnidadAsignacion.Periodo != null &&
                x.UnidadAsignacion.Periodo.CicloEscolarId == configuracion.CicloEscolarId)
            .Select(x => new { x.Id, x.Titulo, x.Estado })
            .ToListAsync();

        var tareaSinCerrar = tareas.FirstOrDefault(x => x.Estado != EstadoTarea.Cerrada);
        if (tareaSinCerrar != null)
        {
            var estado = tareaSinCerrar.Estado switch
            {
                EstadoTarea.Publicada => "publicada",
                EstadoTarea.Borrador => "en borrador",
                EstadoTarea.Archivada => "archivada sin cierre válido",
                _ => "sin cerrar"
            };
            return $"La tarea '{tareaSinCerrar.Titulo}' del {configuracion.Periodo} está {estado}.";
        }

        var tareasIds = tareas.Select(x => x.Id).ToList();
        if (tareasIds.Count == 0) return null;

        var entregaRealizadaSinCalificar = await contexto.Entregas.AsNoTracking().AnyAsync(x =>
            tareasIds.Contains(x.TareaId) && x.FechaEntrega.HasValue && !x.Calificacion.HasValue);
        if (entregaRealizadaSinCalificar)
            return $"Hay entregas del {configuracion.Periodo} pendientes de calificación.";

        var actividadSinResolver = await contexto.Entregas.AsNoTracking().AnyAsync(x =>
            tareasIds.Contains(x.TareaId) && !x.Calificacion.HasValue);
        if (actividadSinResolver)
        {
            return $"Existen tareas no entregadas del {configuracion.Periodo} sin resolución administrativa.";
        }

        var intentoEnCurso = await contexto.IntentosCuestionarios.AsNoTracking().AnyAsync(x =>
            tareasIds.Contains(x.Cuestionario.TareaId) &&
            x.Estado == EstadoIntentoCuestionario.EnCurso);
        if (intentoEnCurso)
            return $"Hay cuestionarios del {configuracion.Periodo} con intentos en curso.";

        return null;
    }

    private Task<bool> EstaCerradaAsync(int configuracionId) =>
        contexto.CierresCalificaciones.AsNoTracking().AnyAsync(x =>
            x.ConfiguracionEvaluacionId == configuracionId &&
            x.Estado == EstadoCierreCalificaciones.Cerrado);

    private static void AplicarResultadoOficial(
        FilaAlumnoCalificacion fila,
        ResultadoOficialTraslado? resultado,
        int asignacionActualId)
    {
        resultado ??= new ResultadoOficialTraslado(EstadoResultadoOficialTraslado.Ordinario);
        fila.EstadoResultadoOficial = resultado.Estado;
        fila.ResolucionTrasladoId = resultado.ResolucionId;
        fila.AsignacionFuenteOficialId = resultado.AsignacionFuenteId;
        fila.NotaOficialConsolidada = resultado.NotaConsolidada;
        fila.EscalaMaximaConsolidada = resultado.EscalaMaximaConsolidada;
        fila.EsFuenteResultadoOficial = resultado.Estado is EstadoResultadoOficialTraslado.Ordinario ||
            resultado.Estado == EstadoResultadoOficialTraslado.ConsolidacionOficial ||
            (resultado.Estado == EstadoResultadoOficialTraslado.FuenteOficial &&
             resultado.AsignacionFuenteId == asignacionActualId);
    }

    private async Task<List<int>> ObtenerIdsInscripcionesDelPeriodoAsync(
        ConfiguracionEvaluacion configuracion,
        bool soloActivas)
    {
        var consultaInscripciones = contexto.Inscripciones.AsNoTracking()
            .Where(x => x.CicloEscolarId == configuracion.CicloEscolarId);
        if (soloActivas)
            consultaInscripciones = consultaInscripciones.Where(x =>
                x.Estado == EstadoInscripcion.Activa);

        var inscripciones = await consultaInscripciones.ToListAsync();
        if (inscripciones.Count == 0) return [];

        var inscripcionesIds = inscripciones.Select(x => x.Id).ToList();
        var traslados = await contexto.HistorialTraslados.AsNoTracking()
            .Where(x => x.CicloEscolarId == configuracion.CicloEscolarId &&
                inscripcionesIds.Contains(x.InscripcionId))
            .OrderBy(x => x.FechaTraslado)
            .ThenBy(x => x.Id)
            .ToListAsync();

        var trasladosPorInscripcion = traslados
            .GroupBy(x => x.InscripcionId)
            .ToDictionary(x => x.Key, x => x.ToList());

        return inscripciones
            .Where(inscripcion => PerteneceAlPeriodo(
                inscripcion,
                trasladosPorInscripcion.GetValueOrDefault(inscripcion.Id) ?? [],
                configuracion))
            .Select(x => x.Id)
            .ToList();
    }

    private static bool PerteneceAlPeriodo(
        Inscripcion inscripcion,
        IReadOnlyList<HistorialTraslado> traslados,
        ConfiguracionEvaluacion configuracion)
    {
        if (traslados.Count == 0)
            return inscripcion.SeccionId == configuracion.SeccionId;

        // El primer origen es la sección inicial; cada movimiento debe continuar
        // exactamente desde el destino anterior para que el historial sea utilizable.
        var seccionEsperada = traslados[0].SeccionOrigenId;
        foreach (var traslado in traslados)
        {
            if (traslado.SeccionOrigenId != seccionEsperada ||
                DateOnly.FromDateTime(traslado.FechaTraslado) < inscripcion.Fecha)
                return false;
            seccionEsperada = traslado.SeccionDestinoId;
        }
        if (seccionEsperada != inscripcion.SeccionId)
            return false;

        var seccionAlInicioDelPeriodo = traslados[0].SeccionOrigenId;
        foreach (var traslado in traslados)
        {
            var fechaTraslado = DateOnly.FromDateTime(traslado.FechaTraslado);
            if (fechaTraslado <= configuracion.Periodo.FechaInicio)
            {
                seccionAlInicioDelPeriodo = traslado.SeccionDestinoId;
                continue;
            }

            // El modelo no expresa a cuál docente corresponde un traslado ocurrido
            // durante el bimestre. Se omite de ambos libros abiertos hasta que exista
            // una regla académica explícita; los libros cerrados usan su resultado.
            if (fechaTraslado <= configuracion.Periodo.FechaFin)
                return false;

            break;
        }

        return seccionAlInicioDelPeriodo == configuracion.SeccionId;
    }

    private static void AplicarCalculo(
        ConfiguracionEvaluacion configuracion,
        FilaAlumnoCalificacion fila)
    {
        var categorias = configuracion.Categorias.Select(c =>
            new CategoriaCalculoCalificacion(
                c.Tipo,
                c.Porcentaje,
                configuracion.Actividades
                    .Where(a => a.CategoriaEvaluacionId == c.Id)
                    .Select(a => new ActividadCalculoCalificacion(
                        a.Nombre,
                        a.PunteoMaximo,
                        fila.Calificaciones.First(x => x.ActividadId == a.Id).Nota))
                    .ToList()))
            .ToList();
        var calculo = CalculoCalificaciones.Calcular(configuracion.MetodoCalculo, categorias);
        fila.Desempeno = calculo.Desempeno;
        fila.Actitudinal = calculo.Actitudinal;
        fila.NotaBimestral = calculo.NotaBimestral;
        fila.AbacusDesempeno = calculo.AbacusDesempeno;
        fila.AbacusActitudinal = calculo.AbacusActitudinal;
        fila.Pendientes = calculo.Pendientes;
    }

    private async Task<decimal?> ObtenerNotaActividadAsync(
        ActividadEvaluable actividad,
        int inscripcionId,
        int alumnoId)
    {
        if (actividad.Origen == OrigenActividadEvaluable.Manual)
            return await contexto.CalificacionesManuales.AsNoTracking()
                .Where(x => x.ActividadEvaluableId == actividad.Id &&
                    (x.InscripcionId == inscripcionId ||
                     (!x.InscripcionId.HasValue && x.AlumnoId == alumnoId)))
                .OrderByDescending(x => x.InscripcionId == inscripcionId)
                .Select(x => x.Nota)
                .FirstOrDefaultAsync();
        var entregas = await contexto.Entregas.AsNoTracking()
            .Include(x => x.Tarea)
            .Where(x =>
                x.TareaId == actividad.TareaId && x.AlumnoId == alumnoId &&
                (x.InscripcionId == inscripcionId || !x.InscripcionId.HasValue))
            .ToListAsync();
        return CalcularNotaEntrega(entregas);
    }

    private static decimal? CalcularNotaEntrega(IEnumerable<Entrega> entregas)
    {
        var intentos = entregas.ToList();
        return intentos.Count == 0
            ? null
            : CalculoCriterioCalificacion.Aplicar(
                intentos[0].Tarea.CriterioCalificacionEntrega,
                intentos.Select(x => (x.NumeroEnvio, x.Id, x.Calificacion,
                    x.OrigenCalificacionAdministrativa.HasValue)));
    }

    private async Task<decimal?> ActualizarNotaOrigenAsync(SolicitudCorreccionCalificacion solicitud)
    {
        var actividad = solicitud.ActividadEvaluable;
        if (actividad.Origen == OrigenActividadEvaluable.Manual)
        {
            var registro = await contexto.CalificacionesManuales
                .Where(x => x.ActividadEvaluableId == actividad.Id &&
                    (x.InscripcionId == solicitud.InscripcionId ||
                     (!x.InscripcionId.HasValue && x.AlumnoId == solicitud.AlumnoId)))
                .OrderByDescending(x => x.InscripcionId == solicitud.InscripcionId)
                .FirstOrDefaultAsync();
            if (registro == null) return null;
            var anterior = registro.Nota;
            registro.Nota = solicitud.NotaPropuesta;
            registro.Estado = EstadoCalificacionManual.Calificada;
            registro.FechaCalificacion = DateTime.UtcNow;
            registro.CalificadoPorUsuarioId = acceso.UsuarioId;
            contexto.HistorialesCalificaciones.Add(new HistorialCalificacion
            {
                TipoOrigen = TipoOrigenHistorialCalificacion.Manual,
                CalificacionManualId = registro.Id,
                NotaAnterior = anterior,
                NotaNueva = solicitud.NotaPropuesta,
                Motivo = solicitud.MotivoSolicitud,
                FechaCambio = DateTime.UtcNow,
                ModificadoPorUsuarioId = acceso.UsuarioId!
            });
            return anterior;
        }

        var entrega = await contexto.Entregas
            .Where(x => x.Calificacion.HasValue &&
                !x.OrigenCalificacionAdministrativa.HasValue &&
                x.TareaId == actividad.TareaId && x.AlumnoId == solicitud.AlumnoId &&
                (x.InscripcionId == solicitud.InscripcionId || !x.InscripcionId.HasValue))
            .OrderByDescending(x => x.InscripcionId == solicitud.InscripcionId)
            .ThenByDescending(x => x.NumeroEnvio)
            .FirstOrDefaultAsync();
        if (entrega == null || !entrega.Calificacion.HasValue) return null;
        var notaAnterior = entrega.Calificacion;
        entrega.Calificacion = solicitud.NotaPropuesta;
        entrega.FechaCalificacion = DateTime.UtcNow;
        entrega.CalificadoPorUsuarioId = acceso.UsuarioId;
        contexto.HistorialesCalificaciones.Add(new HistorialCalificacion
        {
            TipoOrigen = TipoOrigenHistorialCalificacion.Entrega,
            EntregaId = entrega.Id,
            NotaAnterior = notaAnterior,
            NotaNueva = solicitud.NotaPropuesta,
            Motivo = solicitud.MotivoSolicitud,
            FechaCambio = DateTime.UtcNow,
            ModificadoPorUsuarioId = acceso.UsuarioId!
        });
        return notaAnterior;
    }
}
