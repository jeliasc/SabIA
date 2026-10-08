using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Cuestionarios;

namespace Proyecto_Final.Servicios.GestionCuestionarios;

public sealed class CuestionarioServicio(
    Contexto contexto,
    IAccesoAcademicoServicio acceso) : ICuestionarioServicio
{
    public async Task<List<CuestionarioLista>> ObtenerTodosAsync() =>
        await ObtenerListadoAsync(historial: false);

    public async Task<List<CuestionarioLista>> ObtenerHistorialAsync() =>
        await ObtenerListadoAsync(historial: true);

    private async Task<List<CuestionarioLista>> ObtenerListadoAsync(bool historial)
    {
        var consulta = contexto.Cuestionarios.AsNoTracking().AsQueryable();
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var docenteId = perfil.DocenteId;
        var alumnoId = perfil.AlumnoId;

        if (docenteId.HasValue)
        {
            consulta = consulta.Where(x =>
                x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                (historial
                    ? x.Tarea.Asignacion.Estado != EstadoRegistro.Activo ||
                      !x.Tarea.Asignacion.Seccion.CicloEscolar.Activo
                    : x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                      x.Tarea.Asignacion.Seccion.CicloEscolar.Activo));
        }
        else if (alumnoId.HasValue)
        {
            var ahora = DateTime.UtcNow;
            consulta = historial
                ? consulta.Where(x =>
                    x.Tarea.Estado != EstadoTarea.Borrador &&
                    contexto.Inscripciones.Any(i =>
                        i.AlumnoId == alumnoId.Value &&
                        i.SeccionId == x.Tarea.Asignacion.SeccionId &&
                        i.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId) &&
                    !(x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                      x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                      contexto.Inscripciones.Any(i =>
                          i.AlumnoId == alumnoId.Value &&
                          i.SeccionId == x.Tarea.Asignacion.SeccionId &&
                          i.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                          i.Estado == EstadoInscripcion.Activa)))
                : consulta.Where(x =>
                    x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                    x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                    x.Tarea.Estado == EstadoTarea.Publicada &&
                    (!x.Tarea.FechaDisponibilidad.HasValue || x.Tarea.FechaDisponibilidad <= ahora) &&
                    (!x.Tarea.FechaLimite.HasValue || x.Tarea.FechaLimite >= ahora || x.Tarea.PermitirEntregaTardia) &&
                    contexto.Inscripciones.Any(i =>
                    i.AlumnoId == alumnoId.Value &&
                    i.SeccionId == x.Tarea.Asignacion.SeccionId &&
                    i.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                    i.Estado == EstadoInscripcion.Activa &&
                    i.Seccion.CicloEscolar.Activo));
        }
        else
        {
            consulta = consulta.Where(x => historial
                ? x.Tarea.Asignacion.Estado != EstadoRegistro.Activo ||
                  !x.Tarea.Asignacion.Seccion.CicloEscolar.Activo
                : x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                  x.Tarea.Asignacion.Seccion.CicloEscolar.Activo);
        }

        return await consulta
            .OrderByDescending(x => x.Id)
            .Select(x => new CuestionarioLista
            {
                Id = x.Id,
                TareaId = x.TareaId,
                Tarea = x.Tarea.Titulo,
                Curso = x.Tarea.Asignacion.Curso.Nombre,
                Preguntas = x.Preguntas.Count,
                MaximoIntentos = x.MaximoIntentos,
                Origen = x.Origen,
                EstadoTarea = x.Tarea.Estado
            })
            .ToListAsync();
    }

    public async Task<ConfigurarCuestionario?> ObtenerConfiguracionAsync(int tareaId)
    {
        var tarea = await contexto.Tareas
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == tareaId &&
                x.Tipo == TipoTarea.Cuestionario &&
                x.Estado == EstadoTarea.Borrador);

        if (tarea is null)
            return null;

        if (!await acceso.PuedeGestionarAsignacionVigenteAsync(tarea.AsignacionId))
            return null;

        var cuestionario = await contexto.Cuestionarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TareaId == tareaId);

        return new ConfigurarCuestionario
        {
            TareaId = tareaId,
            Tarea = tarea.Titulo,
            ModalidadTiempo = cuestionario?.ModalidadTiempo ?? ModalidadTiempoCuestionario.SinLimite,
            MaximoIntentos = cuestionario?.MaximoIntentos ?? 1,
            CriterioCalificacion = cuestionario?.CriterioCalificacion ?? CriterioCalificacionIntentos.MejorNota,
            MostrarResultadosAlFinalizar = cuestionario?.MostrarResultadosAlFinalizar ?? true,
            MostrarRespuestasCorrectas = cuestionario?.MostrarRespuestasCorrectas ?? false
        };
    }

    public async Task<ResultadoOperacion<int>> GuardarConfiguracionAsync(ConfigurarCuestionario modelo)
    {
        var tarea = await contexto.Tareas.FirstOrDefaultAsync(x =>
            x.Id == modelo.TareaId &&
            x.Tipo == TipoTarea.Cuestionario &&
            x.Estado == EstadoTarea.Borrador);

        if (tarea is null)
            return ResultadoOperacion<int>.Error("La tarea de cuestionario no existe o ya fue publicada.");

        if (!await acceso.PuedeGestionarAsignacionVigenteAsync(tarea.AsignacionId))
            return ResultadoOperacion<int>.Error("No tiene acceso a ese cuestionario.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            var cuestionario = await contexto.Cuestionarios
                .FirstOrDefaultAsync(x => x.TareaId == modelo.TareaId);

            if (cuestionario is null)
            {
                cuestionario = new Cuestionario
                {
                    TareaId = modelo.TareaId,
                    Origen = OrigenCuestionario.Manual
                };
                contexto.Cuestionarios.Add(cuestionario);
            }

            cuestionario.ModalidadTiempo = modelo.ModalidadTiempo;
            cuestionario.MaximoIntentos = modelo.MaximoIntentos;
            cuestionario.CriterioCalificacion = modelo.CriterioCalificacion;
            cuestionario.MostrarResultadosAlFinalizar = modelo.MostrarResultadosAlFinalizar;
            cuestionario.MostrarRespuestasCorrectas = modelo.MostrarRespuestasCorrectas;

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion<int>.Correcto(cuestionario.Id, "La configuración fue guardada.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<CrearPregunta?> PrepararPreguntaAsync(int cuestionarioId)
    {
        var datos = await contexto.Cuestionarios
            .AsNoTracking()
            .Where(x => x.Id == cuestionarioId && x.Tarea.Estado == EstadoTarea.Borrador)
            .Select(x => new
            {
                x.Id,
                Titulo = x.Tarea.Titulo,
                AsignacionId = x.Tarea.AsignacionId,
                SiguienteOrden = x.Preguntas.Count + 1
            })
            .FirstOrDefaultAsync();

        if (datos is null)
            return null;

        if (!await acceso.PuedeGestionarAsignacionVigenteAsync(datos.AsignacionId))
            return null;

        return new CrearPregunta
        {
            CuestionarioId = datos.Id,
            Cuestionario = datos.Titulo,
            Orden = datos.SiguienteOrden
        };
    }

    public async Task<ResultadoOperacion> AgregarPreguntaAsync(CrearPregunta modelo)
    {
        var cuestionario = await contexto.Cuestionarios
            .Include(x => x.Tarea)
            .FirstOrDefaultAsync(x =>
                x.Id == modelo.CuestionarioId &&
                x.Tarea.Estado == EstadoTarea.Borrador);

        if (cuestionario is null)
            return ResultadoOperacion.Error("El cuestionario no está disponible para edición.");

        if (!await acceso.PuedeGestionarAsignacionVigenteAsync(cuestionario.Tarea.AsignacionId))
            return ResultadoOperacion.Error("No tiene acceso al cuestionario.");

        if (await contexto.Preguntas.AnyAsync(x =>
            x.CuestionarioId == modelo.CuestionarioId && x.Orden == modelo.Orden))
        {
            return ResultadoOperacion.Validacion(nameof(modelo.Orden), "Ya existe una pregunta con ese orden.");
        }

        var opciones = Array.Empty<string>();
        var respuestasCorrectas = ParsearRespuestasCorrectas(modelo.RespuestaCorrecta);

        if (modelo.Tipo is TipoPregunta.SeleccionUnica or TipoPregunta.SeleccionMultiple or TipoPregunta.VerdaderoFalso)
        {
            opciones = modelo.Tipo == TipoPregunta.VerdaderoFalso
                ? new[] { "Verdadero", "Falso" }
                : (modelo.OpcionesSeparadas ?? string.Empty)
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (opciones.Length < 2)
                return ResultadoOperacion.Validacion(nameof(modelo.OpcionesSeparadas), "Ingrese al menos dos opciones.");

            if (respuestasCorrectas.Count == 0)
                return ResultadoOperacion.Validacion(nameof(modelo.RespuestaCorrecta), "Indique al menos una respuesta correcta.");

            if (modelo.Tipo != TipoPregunta.SeleccionMultiple && respuestasCorrectas.Count != 1)
            {
                return ResultadoOperacion.Validacion(
                    nameof(modelo.RespuestaCorrecta),
                    "Para este tipo de pregunta debe indicar una sola respuesta correcta.");
            }

            var faltantes = respuestasCorrectas
                .Where(correcta => !opciones.Any(opcion => Iguales(opcion, correcta)))
                .ToList();

            if (faltantes.Count > 0)
            {
                return ResultadoOperacion.Validacion(
                    nameof(modelo.RespuestaCorrecta),
                    "Cada respuesta correcta debe coincidir exactamente con una de las opciones.");
            }
        }
        else if (respuestasCorrectas.Count == 0)
        {
            return ResultadoOperacion.Validacion(nameof(modelo.RespuestaCorrecta), "Ingrese una respuesta aceptada.");
        }

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            var pregunta = new Pregunta
            {
                CuestionarioId = modelo.CuestionarioId,
                Orden = modelo.Orden,
                Tipo = modelo.Tipo,
                Enunciado = modelo.Enunciado.Trim(),
                Punteo = modelo.Punteo
            };

            contexto.Preguntas.Add(pregunta);
            await contexto.SaveChangesAsync();

            if (opciones.Length > 0)
            {
                for (var indice = 0; indice < opciones.Length; indice++)
                {
                    contexto.OpcionesPreguntas.Add(new OpcionPregunta
                    {
                        PreguntaId = pregunta.Id,
                        Orden = indice + 1,
                        Texto = opciones[indice],
                        EsCorrecta = respuestasCorrectas.Any(correcta => Iguales(opciones[indice], correcta))
                    });
                }
            }
            else
            {
                foreach (var correcta in respuestasCorrectas)
                {
                    contexto.RespuestasAceptadas.Add(new RespuestaAceptada
                    {
                        PreguntaId = pregunta.Id,
                        Texto = correcta
                    });
                }
            }

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion.Correcto("La pregunta fue agregada.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<ResolverCuestionario?> ObtenerParaResolverAsync(int id)
    {
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();
        if (!alumnoId.HasValue)
            return null;

        var inscripcionId = await contexto.Inscripciones.AsNoTracking()
            .Where(i =>
                i.AlumnoId == alumnoId.Value &&
                i.Estado == EstadoInscripcion.Activa &&
                i.Seccion.CicloEscolar.Activo &&
                contexto.Cuestionarios.Any(c =>
                    c.Id == id && c.Tarea.Asignacion.SeccionId == i.SeccionId))
            .Select(i => (int?)i.Id)
            .FirstOrDefaultAsync();
        if (!inscripcionId.HasValue)
            return null;

        var modelo = await contexto.Cuestionarios
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.Tarea.Estado == EstadoTarea.Publicada &&
                x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                (!x.Tarea.FechaDisponibilidad.HasValue || x.Tarea.FechaDisponibilidad <= DateTime.UtcNow) &&
                (!x.Tarea.FechaLimite.HasValue || x.Tarea.FechaLimite >= DateTime.UtcNow || x.Tarea.PermitirEntregaTardia) &&
                contexto.Inscripciones.Any(i =>
                    i.AlumnoId == alumnoId.Value &&
                    i.SeccionId == x.Tarea.Asignacion.SeccionId &&
                    i.Estado == EstadoInscripcion.Activa &&
                    i.Seccion.CicloEscolar.Activo))
            .Select(x => new ResolverCuestionario
            {
                CuestionarioId = x.Id,
                Titulo = x.Tarea.Titulo,
                MaximoIntentos = x.MaximoIntentos,
                IntentosUsados = x.Intentos.Count(i =>
                    i.AlumnoId == alumnoId.Value &&
                    (i.InscripcionId == inscripcionId.Value || !i.InscripcionId.HasValue) &&
                    i.Estado == EstadoIntentoCuestionario.Finalizado),
                Respuestas = x.Preguntas
                    .OrderBy(p => p.Orden)
                    .Select(p => new RespuestaCuestionario
                    {
                        PreguntaId = p.Id,
                        Orden = p.Orden,
                        Tipo = p.Tipo,
                        Enunciado = p.Enunciado,
                        Punteo = p.Punteo,
                        Opciones = p.Opciones
                            .OrderBy(o => o.Orden)
                            .Select(o => new OpcionCuestionario
                            {
                                Id = o.Id,
                                Texto = o.Texto
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (modelo is not null && modelo.MaximoIntentos > 0 && modelo.IntentosUsados >= modelo.MaximoIntentos)
            return null;

        return modelo;
    }

    public async Task<ResultadoOperacion<ResultadoIntento>> ResolverAsync(ResolverCuestionario modelo)
    {
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();
        if (!alumnoId.HasValue)
            return ResultadoOperacion<ResultadoIntento>.Error("Solo un alumno puede resolver el cuestionario.");

        var cuestionario = await contexto.Cuestionarios
            .Include(x => x.Tarea)
            .Include(x => x.Preguntas)
                .ThenInclude(p => p.Opciones)
            .Include(x => x.Preguntas)
                .ThenInclude(p => p.RespuestasAceptadas)
            .FirstOrDefaultAsync(x =>
                x.Id == modelo.CuestionarioId &&
                x.Tarea.Estado == EstadoTarea.Publicada &&
                x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo);

        var ahora = DateTime.UtcNow;
        if (cuestionario is null ||
            !await acceso.AlumnoPerteneceAsignacionAsync(cuestionario.Tarea.AsignacionId) ||
            (cuestionario.Tarea.FechaDisponibilidad.HasValue &&
             cuestionario.Tarea.FechaDisponibilidad > ahora) ||
            (cuestionario.Tarea.FechaLimite.HasValue &&
             cuestionario.Tarea.FechaLimite < ahora &&
             !cuestionario.Tarea.PermitirEntregaTardia))
            return ResultadoOperacion<ResultadoIntento>.Error("El cuestionario no está disponible.");

        var inscripcionId = await contexto.Inscripciones.AsNoTracking()
            .Where(i =>
                i.AlumnoId == alumnoId.Value &&
                i.Estado == EstadoInscripcion.Activa &&
                contexto.Asignaciones.Any(a =>
                    a.Id == cuestionario.Tarea.AsignacionId &&
                    a.SeccionId == i.SeccionId &&
                    a.Seccion.CicloEscolarId == i.CicloEscolarId &&
                    a.Seccion.CicloEscolar.Activo))
            .Select(i => (int?)i.Id)
            .FirstOrDefaultAsync();
        if (!inscripcionId.HasValue)
            return ResultadoOperacion<ResultadoIntento>.Error("El cuestionario no está disponible.");

        var intentosPrevios = await contexto.IntentosCuestionarios
            .AsNoTracking()
            .Where(x =>
                x.CuestionarioId == cuestionario.Id &&
                x.AlumnoId == alumnoId.Value &&
                (x.InscripcionId == inscripcionId.Value || !x.InscripcionId.HasValue) &&
                x.Estado == EstadoIntentoCuestionario.Finalizado)
            .Select(x => x.Calificacion ?? 0)
            .ToListAsync();

        if (cuestionario.MaximoIntentos > 0 && intentosPrevios.Count >= cuestionario.MaximoIntentos)
            return ResultadoOperacion<ResultadoIntento>.Error("Ya alcanzó el máximo de intentos.");

        var respuestas = modelo.Respuestas
            .GroupBy(x => x.PreguntaId)
            .ToDictionary(x => x.Key, x => x.First());

        var puntajeObtenido = 0m;
        var puntajeTotal = cuestionario.Preguntas.Sum(x => x.Punteo);

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            var intento = new IntentoCuestionario
            {
                CuestionarioId = cuestionario.Id,
                AlumnoId = alumnoId.Value,
                InscripcionId = inscripcionId.Value,
                NumeroIntento = intentosPrevios.Count + 1,
                Estado = EstadoIntentoCuestionario.EnCurso,
                FechaInicio = DateTime.UtcNow
            };

            contexto.IntentosCuestionarios.Add(intento);
            await contexto.SaveChangesAsync();

            foreach (var pregunta in cuestionario.Preguntas.OrderBy(x => x.Orden))
            {
                respuestas.TryGetValue(pregunta.Id, out var respuestaModelo);
                var seleccionadas = ObtenerOpcionesSeleccionadas(respuestaModelo);

                var respuesta = new RespuestaAlumno
                {
                    IntentoCuestionarioId = intento.Id,
                    PreguntaId = pregunta.Id,
                    TextoRespuesta = Normalizar(respuestaModelo?.Texto),
                    FechaPresentacion = DateTime.UtcNow,
                    FechaRespuesta = DateTime.UtcNow
                };

                var correcta = Evaluar(pregunta, respuestaModelo?.Texto, seleccionadas);
                respuesta.PunteoObtenido = correcta ? pregunta.Punteo : 0;
                puntajeObtenido += respuesta.PunteoObtenido ?? 0;

                contexto.RespuestasAlumnos.Add(respuesta);
                await contexto.SaveChangesAsync();

                foreach (var opcionId in seleccionadas.Distinct())
                {
                    if (!pregunta.Opciones.Any(x => x.Id == opcionId))
                        continue;

                    contexto.RespuestasAlumnosOpciones.Add(new RespuestaAlumnoOpcion
                    {
                        RespuestaAlumnoId = respuesta.Id,
                        PreguntaId = pregunta.Id,
                        OpcionPreguntaId = opcionId
                    });
                }
            }

            var notaIntento = puntajeTotal > 0
                ? Math.Round(puntajeObtenido / puntajeTotal * 100, 2)
                : 0;

            intento.Estado = EstadoIntentoCuestionario.Finalizado;
            intento.FechaFinalizacion = DateTime.UtcNow;
            intento.Calificacion = notaIntento;
            intento.FechaCalificacion = DateTime.UtcNow;

            var notas = intentosPrevios.Append(notaIntento).ToList();
            var notaAplicada = cuestionario.CriterioCalificacion switch
            {
                CriterioCalificacionIntentos.UltimoIntento => notaIntento,
                CriterioCalificacionIntentos.Promedio => Math.Round(notas.Average(), 2),
                _ => notas.Max()
            };

            var entrega = await contexto.Entregas
                .Where(x =>
                    x.TareaId == cuestionario.TareaId &&
                    x.AlumnoId == alumnoId.Value &&
                    (x.InscripcionId == inscripcionId.Value || !x.InscripcionId.HasValue))
                .OrderByDescending(x => x.InscripcionId == inscripcionId.Value)
                .ThenByDescending(x => x.NumeroEnvio)
                .FirstOrDefaultAsync();

            if (entrega is not null)
            {
                entrega.FechaEntrega ??= DateTime.UtcNow;
                entrega.Estado = EstadoEntrega.Calificada;
                entrega.Calificacion = Math.Round(notaAplicada / 100 * cuestionario.Tarea.PunteoMaximo, 2);
                entrega.FechaCalificacion = DateTime.UtcNow;
            }

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion<ResultadoIntento>.Correcto(
                new ResultadoIntento
                {
                    Calificacion = notaIntento,
                    CalificacionAplicada = notaAplicada,
                    PunteoObtenido = puntajeObtenido,
                    PunteoTotal = puntajeTotal,
                    MostrarResultados = cuestionario.MostrarResultadosAlFinalizar,
                    MostrarRespuestasCorrectas = cuestionario.MostrarRespuestasCorrectas
                },
                "Cuestionario finalizado.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private static bool Evaluar(Pregunta pregunta, string? texto, IReadOnlyCollection<int> seleccionadas)
    {
        if (pregunta.Tipo == TipoPregunta.TextoCorto)
        {
            return pregunta.RespuestasAceptadas.Any(x => Iguales(x.Texto, texto));
        }

        var correctas = pregunta.Opciones
            .Where(x => x.EsCorrecta)
            .Select(x => x.Id)
            .OrderBy(x => x)
            .ToArray();

        var marcadas = seleccionadas
            .Where(id => pregunta.Opciones.Any(x => x.Id == id))
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        return correctas.SequenceEqual(marcadas);
    }

    private static IReadOnlyCollection<int> ObtenerOpcionesSeleccionadas(RespuestaCuestionario? respuesta)
    {
        if (respuesta is null)
            return Array.Empty<int>();

        var opciones = respuesta.OpcionIds
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (respuesta.OpcionId.HasValue && !opciones.Contains(respuesta.OpcionId.Value))
            opciones.Add(respuesta.OpcionId.Value);

        return opciones;
    }

    private static List<string> ParsearRespuestasCorrectas(string? valor) =>
        (valor ?? string.Empty)
            .Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool Iguales(string? izquierda, string? derecha) =>
        string.Equals(Normalizar(izquierda), Normalizar(derecha), StringComparison.OrdinalIgnoreCase);

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
