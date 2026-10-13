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
    // Margen para la latencia de red y el envío automático al vencer el tiempo.
    public const int ToleranciaSegundos = 10;

    private static readonly TimeSpan Tolerancia = TimeSpan.FromSeconds(ToleranciaSegundos);

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

    // =====================================================================
    // CONFIGURACIÓN (DOCENTE)
    // =====================================================================

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
            TiempoGeneralMinutos = cuestionario?.TiempoGeneralSegundos is int segundos
                ? (int)Math.Ceiling(segundos / 60d)
                : null,
            TiempoPorPreguntaSegundos = cuestionario?.TiempoPorPreguntaSegundos,
            PermitirNavegacion = cuestionario?.PermitirNavegacion ?? false,
            OrdenAleatorio = cuestionario?.OrdenAleatorio ?? false,
            MaximoIntentos = cuestionario?.MaximoIntentos ?? 1,
            CriterioCalificacion = cuestionario?.CriterioCalificacion ?? CriterioCalificacionIntentos.MejorNota,
            MostrarResultadosAlFinalizar = cuestionario?.MostrarResultadosAlFinalizar ?? true,
            MostrarRespuestasCorrectas = cuestionario?.MostrarRespuestasCorrectas ?? false
        };
    }

    public async Task<ResultadoOperacion<int>> GuardarConfiguracionAsync(ConfigurarCuestionario modelo)
    {
        switch (modelo.ModalidadTiempo)
        {
            case ModalidadTiempoCuestionario.TiempoGeneral when !modelo.TiempoGeneralMinutos.HasValue:
                return ResultadoOperacion<int>.Validacion(
                    nameof(modelo.TiempoGeneralMinutos),
                    "Indique el tiempo total del cuestionario en minutos.");
            case ModalidadTiempoCuestionario.TiempoPorPregunta when !modelo.TiempoPorPreguntaSegundos.HasValue:
                return ResultadoOperacion<int>.Validacion(
                    nameof(modelo.TiempoPorPreguntaSegundos),
                    "Indique el tiempo predeterminado por pregunta en segundos.");
        }

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
            cuestionario.TiempoGeneralSegundos =
                modelo.ModalidadTiempo == ModalidadTiempoCuestionario.TiempoGeneral
                    ? modelo.TiempoGeneralMinutos!.Value * 60
                    : null;
            cuestionario.TiempoPorPreguntaSegundos =
                modelo.ModalidadTiempo == ModalidadTiempoCuestionario.TiempoPorPregunta
                    ? modelo.TiempoPorPreguntaSegundos
                    : null;

            // Con tiempo por pregunta no se puede regresar: cada pregunta
            // se cierra cuando vence su tiempo.
            cuestionario.PermitirNavegacion =
                modelo.PermitirNavegacion &&
                modelo.ModalidadTiempo != ModalidadTiempoCuestionario.TiempoPorPregunta;

            cuestionario.OrdenAleatorio = modelo.OrdenAleatorio;
            cuestionario.MaximoIntentos = modelo.MaximoIntentos;
            cuestionario.CriterioCalificacion = modelo.CriterioCalificacion;
            cuestionario.MostrarResultadosAlFinalizar = modelo.MostrarResultadosAlFinalizar;
            cuestionario.MostrarRespuestasCorrectas = modelo.MostrarRespuestasCorrectas;

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            var mensaje = modelo.PermitirNavegacion && !cuestionario.PermitirNavegacion
                ? "La configuración fue guardada. La navegación libre se desactivó porque el tiempo es por pregunta."
                : "La configuración fue guardada.";

            return ResultadoOperacion<int>.Correcto(cuestionario.Id, mensaje);
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
                x.ModalidadTiempo,
                x.TiempoPorPreguntaSegundos,
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
            UsaTiempoPorPregunta = datos.ModalidadTiempo == ModalidadTiempoCuestionario.TiempoPorPregunta,
            TiempoPredeterminadoSegundos = datos.TiempoPorPreguntaSegundos,
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
                .Where(correcta => !opciones.Any(opcion => CoincideOpcion(opcion, correcta)))
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

        if (cuestionario.ModalidadTiempo == ModalidadTiempoCuestionario.TiempoPorPregunta &&
            !modelo.TiempoLimiteSegundos.HasValue &&
            !cuestionario.TiempoPorPreguntaSegundos.HasValue)
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.TiempoLimiteSegundos),
                "Indique el tiempo de la pregunta.");
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
                Punteo = modelo.Punteo,
                TiempoLimiteSegundos =
                    cuestionario.ModalidadTiempo == ModalidadTiempoCuestionario.TiempoPorPregunta
                        ? modelo.TiempoLimiteSegundos
                        : null,
                Retroalimentacion = string.IsNullOrWhiteSpace(modelo.Retroalimentacion)
                    ? null
                    : modelo.Retroalimentacion.Trim()
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
                        EsCorrecta = respuestasCorrectas.Any(correcta => CoincideOpcion(opciones[indice], correcta))
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

    // =====================================================================
    // RESOLUCIÓN (ALUMNO)
    // =====================================================================

    public async Task<InicioCuestionario?> ObtenerInicioAsync(int id)
    {
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();
        if (!alumnoId.HasValue)
            return null;

        var inscripcionId = await ObtenerInscripcionVigenteAsync(id, alumnoId.Value);
        if (!inscripcionId.HasValue)
            return null;

        var ahora = DateTime.UtcNow;
        var modelo = await contexto.Cuestionarios
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.Tarea.Estado == EstadoTarea.Publicada &&
                x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                (!x.Tarea.FechaDisponibilidad.HasValue || x.Tarea.FechaDisponibilidad <= ahora))
            .Select(x => new
            {
                Inicio = new InicioCuestionario
                {
                    CuestionarioId = x.Id,
                    Titulo = x.Tarea.Titulo,
                    Instrucciones = x.Tarea.Instrucciones,
                    CantidadPreguntas = x.Preguntas.Count,
                    MaximoIntentos = x.MaximoIntentos,
                    ModalidadTiempo = x.ModalidadTiempo,
                    TiempoGeneralSegundos = x.TiempoGeneralSegundos,
                    TiempoPorPreguntaSegundos = x.TiempoPorPreguntaSegundos,
                    PermitirNavegacion = x.PermitirNavegacion
                },
                x.Tarea.FechaLimite,
                x.Tarea.PermitirEntregaTardia
            })
            .FirstOrDefaultAsync();

        if (modelo is null)
            return null;

        // Si quedó un intento abierto cuyo tiempo ya venció, se cierra antes
        // de mostrar la pantalla para que el conteo de intentos sea correcto.
        var enCurso = await ConsultaIntentosCompletos()
            .FirstOrDefaultAsync(x =>
                x.CuestionarioId == id &&
                x.AlumnoId == alumnoId.Value &&
                x.InscripcionId == inscripcionId.Value &&
                x.Estado == EstadoIntentoCuestionario.EnCurso);

        if (enCurso is not null && Vencido(enCurso, ahora))
        {
            await FinalizarInternoAsync(enCurso, EstadoIntentoCuestionario.Expirado, ahora);
            enCurso = null;
        }

        var inicio = modelo.Inicio;
        inicio.IntentoEnCursoId = enCurso?.Id;

        var terminados = await contexto.IntentosCuestionarios
            .AsNoTracking()
            .Where(x =>
                x.CuestionarioId == id &&
                x.AlumnoId == alumnoId.Value &&
                (x.InscripcionId == inscripcionId.Value || !x.InscripcionId.HasValue) &&
                (x.Estado == EstadoIntentoCuestionario.Finalizado ||
                 x.Estado == EstadoIntentoCuestionario.Expirado))
            .OrderByDescending(x => x.NumeroIntento)
            .Select(x => x.Id)
            .ToListAsync();

        inicio.IntentosUsados = terminados.Count;
        inicio.UltimoIntentoId = terminados.Count > 0 ? terminados[0] : null;

        // Fuera del plazo y sin entrega tardía solo se puede consultar el resultado.
        inicio.PlazoVencido = modelo.FechaLimite.HasValue &&
            modelo.FechaLimite < ahora &&
            !modelo.PermitirEntregaTardia;

        return inicio;
    }

    public async Task<ResultadoOperacion<int>> IniciarAsync(int id)
    {
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();
        if (!alumnoId.HasValue)
            return ResultadoOperacion<int>.Error("Solo un alumno puede resolver el cuestionario.");

        var inscripcionId = await ObtenerInscripcionVigenteAsync(id, alumnoId.Value);
        if (!inscripcionId.HasValue)
            return ResultadoOperacion<int>.Error("El cuestionario no está disponible.");

        var ahora = DateTime.UtcNow;
        var cuestionario = await contexto.Cuestionarios
            .Include(x => x.Tarea)
            .Include(x => x.Preguntas)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Tarea.Estado == EstadoTarea.Publicada &&
                x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo);

        if (cuestionario is null ||
            !await acceso.AlumnoPerteneceAsignacionAsync(cuestionario.Tarea.AsignacionId) ||
            (cuestionario.Tarea.FechaDisponibilidad.HasValue && cuestionario.Tarea.FechaDisponibilidad > ahora))
        {
            return ResultadoOperacion<int>.Error("El cuestionario no está disponible.");
        }

        var enCurso = await ConsultaIntentosCompletos()
            .FirstOrDefaultAsync(x =>
                x.CuestionarioId == id &&
                x.AlumnoId == alumnoId.Value &&
                x.InscripcionId == inscripcionId.Value &&
                x.Estado == EstadoIntentoCuestionario.EnCurso);

        if (enCurso is not null)
        {
            if (!Vencido(enCurso, ahora))
                return ResultadoOperacion<int>.Correcto(enCurso.Id, "Continúe con su intento.");

            await FinalizarInternoAsync(enCurso, EstadoIntentoCuestionario.Expirado, ahora);
        }

        if (cuestionario.Tarea.FechaLimite.HasValue &&
            cuestionario.Tarea.FechaLimite < ahora &&
            !cuestionario.Tarea.PermitirEntregaTardia)
        {
            return ResultadoOperacion<int>.Error("El plazo para resolver el cuestionario ya venció.");
        }

        if (cuestionario.Preguntas.Count == 0)
            return ResultadoOperacion<int>.Error("El cuestionario no tiene preguntas.");

        var intentosPrevios = await contexto.IntentosCuestionarios
            .Where(x =>
                x.CuestionarioId == id &&
                x.AlumnoId == alumnoId.Value &&
                (x.InscripcionId == inscripcionId.Value || !x.InscripcionId.HasValue))
            .Select(x => new { x.Estado, x.NumeroIntento, x.InscripcionId })
            .ToListAsync();

        var terminados = intentosPrevios.Count(x =>
            x.Estado is EstadoIntentoCuestionario.Finalizado or EstadoIntentoCuestionario.Expirado);

        if (cuestionario.MaximoIntentos > 0 && terminados >= cuestionario.MaximoIntentos)
            return ResultadoOperacion<int>.Error("Ya alcanzó el máximo de intentos.");

        var orden = cuestionario.Preguntas
            .OrderBy(x => x.Orden)
            .Select(x => x.Id)
            .ToArray();

        if (cuestionario.OrdenAleatorio)
            Random.Shared.Shuffle(orden);

        var intento = new IntentoCuestionario
        {
            CuestionarioId = cuestionario.Id,
            AlumnoId = alumnoId.Value,
            InscripcionId = inscripcionId.Value,
            NumeroIntento = intentosPrevios
                .Where(x => x.InscripcionId == inscripcionId.Value)
                .Select(x => x.NumeroIntento)
                .DefaultIfEmpty(0)
                .Max() + 1,
            Estado = EstadoIntentoCuestionario.EnCurso,
            FechaInicio = ahora,
            FechaLimite = CalcularFechaLimite(cuestionario, ahora),
            OrdenPreguntas = string.Join(',', orden),
            PreguntaActual = 0
        };

        contexto.IntentosCuestionarios.Add(intento);

        try
        {
            await contexto.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Otra pestaña inició el intento al mismo tiempo: se continúa con ese.
            contexto.Entry(intento).State = EntityState.Detached;
            var existente = await contexto.IntentosCuestionarios
                .AsNoTracking()
                .Where(x =>
                    x.CuestionarioId == id &&
                    x.InscripcionId == inscripcionId.Value &&
                    x.Estado == EstadoIntentoCuestionario.EnCurso)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            if (existente.HasValue)
                return ResultadoOperacion<int>.Correcto(existente.Value, "Continúe con su intento.");

            throw;
        }

        return ResultadoOperacion<int>.Correcto(intento.Id, "Intento iniciado.");
    }

    public async Task<VistaIntento> ObtenerIntentoAsync(int intentoId)
    {
        var intento = await CargarIntentoAlumnoAsync(intentoId);
        if (intento is null)
            return VistaIntento.NoEncontrado();

        if (intento.Estado != EstadoIntentoCuestionario.EnCurso)
            return VistaIntento.Terminado(intento.Id);

        var ahora = DateTime.UtcNow;
        if (!TareaDisponible(intento.Cuestionario.Tarea) || Vencido(intento, ahora))
        {
            await FinalizarInternoAsync(intento, EstadoIntentoCuestionario.Expirado, ahora);
            return VistaIntento.Terminado(intento.Id);
        }

        var cuestionario = intento.Cuestionario;
        var orden = LeerOrden(intento.OrdenPreguntas);
        var modelo = new IntentoEnCurso
        {
            IntentoId = intento.Id,
            CuestionarioId = cuestionario.Id,
            Titulo = cuestionario.Tarea.Titulo,
            PermitirNavegacion = cuestionario.PermitirNavegacion,
            TotalPreguntas = orden.Count,
            SegundosRestantes = intento.FechaLimite.HasValue
                ? SegundosHasta(intento.FechaLimite.Value, ahora)
                : null
        };

        if (cuestionario.PermitirNavegacion)
        {
            var numero = 1;
            foreach (var preguntaId in orden)
            {
                var pregunta = cuestionario.Preguntas.FirstOrDefault(x => x.Id == preguntaId);
                if (pregunta is null)
                    continue;

                modelo.Preguntas.Add(CrearPreguntaIntento(
                    pregunta,
                    numero++,
                    intento.Respuestas.FirstOrDefault(x => x.PreguntaId == preguntaId)));
            }

            return VistaIntento.EnCurso(modelo);
        }

        var actual = await PrepararPreguntaActualAsync(intento, orden, ahora);
        if (actual is null)
        {
            await FinalizarInternoAsync(intento, EstadoIntentoCuestionario.Finalizado, ahora);
            return VistaIntento.Terminado(intento.Id);
        }

        var respuestaActual = intento.Respuestas.First(x => x.PreguntaId == actual.Id);
        var tiempo = TiempoDePregunta(cuestionario, actual);
        if (tiempo.HasValue)
        {
            modelo.SegundosRestantesPregunta = SegundosHasta(
                respuestaActual.FechaPresentacion.AddSeconds(tiempo.Value),
                ahora);
        }

        modelo.Preguntas.Add(CrearPreguntaIntento(actual, intento.PreguntaActual + 1, respuestaActual));
        return VistaIntento.EnCurso(modelo);
    }

    public async Task<ResultadoOperacion<bool>> GuardarRespuestaAsync(
        int intentoId,
        RespuestaCuestionario respuesta)
    {
        var intento = await CargarIntentoAlumnoAsync(intentoId);
        if (intento is null)
            return ResultadoOperacion<bool>.Error("El intento no está disponible.");

        if (intento.Estado != EstadoIntentoCuestionario.EnCurso)
            return ResultadoOperacion<bool>.Correcto(true, "El intento ya fue finalizado.");

        var ahora = DateTime.UtcNow;
        if (!TareaDisponible(intento.Cuestionario.Tarea) || Vencido(intento, ahora))
        {
            await FinalizarInternoAsync(intento, EstadoIntentoCuestionario.Expirado, ahora);
            return ResultadoOperacion<bool>.Correcto(true, "El tiempo del cuestionario terminó.");
        }

        if (!intento.Cuestionario.PermitirNavegacion)
            return ResultadoOperacion<bool>.Error("Este cuestionario se responde pregunta por pregunta.");

        var pregunta = PreguntaDelIntento(intento, respuesta.PreguntaId);
        if (pregunta is null)
            return ResultadoOperacion<bool>.Error("La pregunta no pertenece al intento.");

        AplicarRespuesta(ObtenerOCrearRespuesta(intento, pregunta.Id, ahora), pregunta, respuesta, ahora);
        await contexto.SaveChangesAsync();

        return ResultadoOperacion<bool>.Correcto(false, "Respuesta guardada.");
    }

    public async Task<ResultadoOperacion<bool>> ResponderAsync(ResponderPregunta modelo)
    {
        var intento = await CargarIntentoAlumnoAsync(modelo.IntentoId);
        if (intento is null)
            return ResultadoOperacion<bool>.Error("El intento no está disponible.");

        if (intento.Estado != EstadoIntentoCuestionario.EnCurso)
            return ResultadoOperacion<bool>.Correcto(true, "El intento ya fue finalizado.");

        var ahora = DateTime.UtcNow;
        if (!TareaDisponible(intento.Cuestionario.Tarea) || Vencido(intento, ahora))
        {
            await FinalizarInternoAsync(intento, EstadoIntentoCuestionario.Expirado, ahora);
            return ResultadoOperacion<bool>.Correcto(true, "El tiempo del cuestionario terminó.");
        }

        if (intento.Cuestionario.PermitirNavegacion)
            return ResultadoOperacion<bool>.Error("Este cuestionario permite navegación libre.");

        var orden = LeerOrden(intento.OrdenPreguntas);
        if (intento.PreguntaActual >= orden.Count)
        {
            await FinalizarInternoAsync(intento, EstadoIntentoCuestionario.Finalizado, ahora);
            return ResultadoOperacion<bool>.Correcto(true, "Cuestionario finalizado.");
        }

        // Solo se acepta la respuesta de la pregunta en curso. Una respuesta a
        // una pregunta anterior (por ejemplo, con el botón Atrás) se ignora.
        var actualId = orden[intento.PreguntaActual];
        if (modelo.Respuesta.PreguntaId != actualId)
            return ResultadoOperacion<bool>.Correcto(false, "Ya no es posible modificar esa pregunta.");

        var pregunta = PreguntaDelIntento(intento, actualId);
        var mensaje = "Respuesta registrada.";

        if (pregunta is not null)
        {
            var registro = ObtenerOCrearRespuesta(intento, actualId, ahora);
            var tiempo = TiempoDePregunta(intento.Cuestionario, pregunta);
            var aTiempo = !tiempo.HasValue ||
                ahora <= registro.FechaPresentacion.AddSeconds(tiempo.Value) + Tolerancia;

            if (aTiempo)
                AplicarRespuesta(registro, pregunta, modelo.Respuesta, ahora);
            else
                mensaje = "El tiempo de la pregunta había terminado y la respuesta no se registró.";
        }

        intento.PreguntaActual++;
        await contexto.SaveChangesAsync();

        if (intento.PreguntaActual >= orden.Count)
        {
            await FinalizarInternoAsync(intento, EstadoIntentoCuestionario.Finalizado, ahora);
            return ResultadoOperacion<bool>.Correcto(true, "Cuestionario finalizado.");
        }

        return ResultadoOperacion<bool>.Correcto(false, mensaje);
    }

    public async Task<ResultadoOperacion<int>> FinalizarAsync(FinalizarIntento modelo)
    {
        var intento = await CargarIntentoAlumnoAsync(modelo.IntentoId);
        if (intento is null)
            return ResultadoOperacion<int>.Error("El intento no está disponible.");

        if (intento.Estado != EstadoIntentoCuestionario.EnCurso)
            return ResultadoOperacion<int>.Correcto(intento.Id, "El intento ya fue finalizado.");

        var ahora = DateTime.UtcNow;
        if (!TareaDisponible(intento.Cuestionario.Tarea) || Vencido(intento, ahora))
        {
            await FinalizarInternoAsync(intento, EstadoIntentoCuestionario.Expirado, ahora);
            return ResultadoOperacion<int>.Correcto(
                intento.Id,
                "El tiempo había terminado. Se calificaron las respuestas guardadas hasta ese momento.");
        }

        // En modo navegable se guardan las respuestas del formulario final.
        // En modo secuencial las respuestas ya se registraron una por una y las
        // preguntas que falten se califican con cero.
        if (intento.Cuestionario.PermitirNavegacion)
        {
            foreach (var respuesta in modelo.Respuestas.GroupBy(x => x.PreguntaId).Select(x => x.Last()))
            {
                var pregunta = PreguntaDelIntento(intento, respuesta.PreguntaId);
                if (pregunta is null)
                    continue;

                AplicarRespuesta(ObtenerOCrearRespuesta(intento, pregunta.Id, ahora), pregunta, respuesta, ahora);
            }

            await contexto.SaveChangesAsync();
        }

        await FinalizarInternoAsync(intento, EstadoIntentoCuestionario.Finalizado, ahora);
        return ResultadoOperacion<int>.Correcto(intento.Id, "Cuestionario finalizado.");
    }

    public async Task<ResultadoIntento?> ObtenerResultadoAsync(int intentoId)
    {
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();
        if (!alumnoId.HasValue)
            return null;

        var intento = await contexto.IntentosCuestionarios
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Cuestionario).ThenInclude(x => x.Tarea)
            .Include(x => x.Cuestionario).ThenInclude(x => x.Preguntas).ThenInclude(x => x.Opciones)
            .Include(x => x.Cuestionario).ThenInclude(x => x.Preguntas).ThenInclude(x => x.RespuestasAceptadas)
            .Include(x => x.Respuestas).ThenInclude(x => x.OpcionesSeleccionadas)
            .FirstOrDefaultAsync(x =>
                x.Id == intentoId &&
                x.AlumnoId == alumnoId.Value &&
                (x.Estado == EstadoIntentoCuestionario.Finalizado ||
                 x.Estado == EstadoIntentoCuestionario.Expirado));

        if (intento is null)
            return null;

        var cuestionario = intento.Cuestionario;
        var notas = await contexto.IntentosCuestionarios
            .AsNoTracking()
            .Where(x =>
                x.CuestionarioId == intento.CuestionarioId &&
                x.AlumnoId == intento.AlumnoId &&
                (x.InscripcionId == intento.InscripcionId || !x.InscripcionId.HasValue) &&
                (x.Estado == EstadoIntentoCuestionario.Finalizado ||
                 x.Estado == EstadoIntentoCuestionario.Expirado) &&
                x.NumeroIntento <= intento.NumeroIntento)
            .OrderBy(x => x.NumeroIntento)
            .Select(x => x.Calificacion ?? 0)
            .ToListAsync();

        var resultado = new ResultadoIntento
        {
            IntentoId = intento.Id,
            CuestionarioId = cuestionario.Id,
            Titulo = cuestionario.Tarea.Titulo,
            Expirado = intento.Estado == EstadoIntentoCuestionario.Expirado,
            Calificacion = intento.Calificacion ?? 0,
            PunteoObtenido = intento.Respuestas.Sum(x => x.PunteoObtenido ?? 0),
            PunteoTotal = cuestionario.Preguntas.Sum(x => x.Punteo),
            CalificacionAplicada = AplicarCriterio(cuestionario.CriterioCalificacion, notas),
            MostrarResultados = cuestionario.MostrarResultadosAlFinalizar,
            MostrarRespuestasCorrectas = cuestionario.MostrarRespuestasCorrectas
        };

        if (!cuestionario.MostrarRespuestasCorrectas)
            return resultado;

        var numero = 1;
        foreach (var preguntaId in LeerOrden(intento.OrdenPreguntas))
        {
            var pregunta = cuestionario.Preguntas.FirstOrDefault(x => x.Id == preguntaId);
            if (pregunta is null)
                continue;

            var registro = intento.Respuestas.FirstOrDefault(x => x.PreguntaId == preguntaId);
            resultado.Detalle.Add(new DetalleRespuestaIntento
            {
                Numero = numero++,
                Enunciado = pregunta.Enunciado,
                RespuestaAlumno = DescribirRespuestaAlumno(pregunta, registro),
                RespuestaCorrecta = DescribirRespuestaCorrecta(pregunta),
                Correcta = Evaluar(pregunta, registro),
                PunteoObtenido = registro?.PunteoObtenido ?? 0,
                Punteo = pregunta.Punteo,
                Retroalimentacion = pregunta.Retroalimentacion
            });
        }

        return resultado;
    }

    // Cierra los intentos cuyo tiempo venció sin que el alumno los terminara
    // (cerró el navegador, perdió conexión, etc.). Lo ejecuta un servicio en
    // segundo plano, sin usuario en sesión.
    public async Task<int> FinalizarVencidosAsync(DateTime ahora, CancellationToken cancellationToken = default)
    {
        var limite = ahora - Tolerancia;
        var ids = await contexto.IntentosCuestionarios
            .AsNoTracking()
            .Where(x =>
                x.Estado == EstadoIntentoCuestionario.EnCurso &&
                x.Cuestionario.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                ((x.FechaLimite.HasValue && x.FechaLimite < limite) ||
                 x.Cuestionario.Tarea.Estado != EstadoTarea.Publicada))
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .Take(200)
            .ToListAsync(cancellationToken);

        var finalizados = 0;
        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var intento = await ConsultaIntentosCompletos()
                .FirstOrDefaultAsync(x => x.Id == id && x.Estado == EstadoIntentoCuestionario.EnCurso, cancellationToken);

            if (intento is null)
                continue;

            await FinalizarInternoAsync(intento, EstadoIntentoCuestionario.Expirado, ahora);
            contexto.ChangeTracker.Clear();
            finalizados++;
        }

        return finalizados;
    }

    // =====================================================================
    // APOYO
    // =====================================================================

    private async Task<int?> ObtenerInscripcionVigenteAsync(int cuestionarioId, int alumnoId) =>
        await contexto.Inscripciones.AsNoTracking()
            .Where(i =>
                i.AlumnoId == alumnoId &&
                i.Estado == EstadoInscripcion.Activa &&
                i.Seccion.CicloEscolar.Activo &&
                contexto.Cuestionarios.Any(c =>
                    c.Id == cuestionarioId &&
                    c.Tarea.Asignacion.SeccionId == i.SeccionId &&
                    c.Tarea.Asignacion.Seccion.CicloEscolarId == i.CicloEscolarId))
            .Select(i => (int?)i.Id)
            .FirstOrDefaultAsync();

    private IQueryable<IntentoCuestionario> ConsultaIntentosCompletos() =>
        contexto.IntentosCuestionarios
            .AsSplitQuery()
            .Include(x => x.Cuestionario).ThenInclude(x => x.Tarea)
                .ThenInclude(x => x.Asignacion).ThenInclude(x => x.Seccion).ThenInclude(x => x.CicloEscolar)
            .Include(x => x.Cuestionario).ThenInclude(x => x.Preguntas).ThenInclude(x => x.Opciones)
            .Include(x => x.Cuestionario).ThenInclude(x => x.Preguntas).ThenInclude(x => x.RespuestasAceptadas)
            .Include(x => x.Respuestas).ThenInclude(x => x.OpcionesSeleccionadas);

    private async Task<IntentoCuestionario?> CargarIntentoAlumnoAsync(int intentoId)
    {
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();
        if (!alumnoId.HasValue)
            return null;

        return await ConsultaIntentosCompletos()
            .FirstOrDefaultAsync(x => x.Id == intentoId && x.AlumnoId == alumnoId.Value);
    }

    // Deja lista la pregunta en curso del modo secuencial. Registra el momento
    // en que se presenta (para medir su tiempo) y salta las preguntas cuyo
    // tiempo ya terminó mientras el alumno no estaba en la página.
    private async Task<Pregunta?> PrepararPreguntaActualAsync(
        IntentoCuestionario intento,
        IReadOnlyList<int> orden,
        DateTime ahora)
    {
        var huboCambios = false;

        while (intento.PreguntaActual < orden.Count)
        {
            var pregunta = PreguntaDelIntento(intento, orden[intento.PreguntaActual]);
            if (pregunta is null)
            {
                intento.PreguntaActual++;
                huboCambios = true;
                continue;
            }

            var registro = intento.Respuestas.FirstOrDefault(x => x.PreguntaId == pregunta.Id);
            if (registro is null)
            {
                ObtenerOCrearRespuesta(intento, pregunta.Id, ahora);
                await contexto.SaveChangesAsync();
                return pregunta;
            }

            var tiempo = TiempoDePregunta(intento.Cuestionario, pregunta);
            if (tiempo.HasValue && ahora > registro.FechaPresentacion.AddSeconds(tiempo.Value) + Tolerancia)
            {
                intento.PreguntaActual++;
                huboCambios = true;
                continue;
            }

            if (huboCambios)
                await contexto.SaveChangesAsync();

            return pregunta;
        }

        if (huboCambios)
            await contexto.SaveChangesAsync();

        return null;
    }

    private async Task FinalizarInternoAsync(
        IntentoCuestionario intento,
        EstadoIntentoCuestionario estadoFinal,
        DateTime ahora)
    {
        if (intento.Estado != EstadoIntentoCuestionario.EnCurso)
            return;

        var cuestionario = intento.Cuestionario;

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            var puntajeObtenido = 0m;
            var puntajeTotal = 0m;

            foreach (var preguntaId in LeerOrden(intento.OrdenPreguntas))
            {
                var pregunta = PreguntaDelIntento(intento, preguntaId);
                if (pregunta is null)
                    continue;

                // Las preguntas sin responder quedan registradas con cero
                // para que el docente vea el intento completo.
                var registro = intento.Respuestas.FirstOrDefault(x => x.PreguntaId == preguntaId)
                    ?? ObtenerOCrearRespuesta(intento, preguntaId, intento.FechaInicio);

                registro.PunteoObtenido = Evaluar(pregunta, registro) ? pregunta.Punteo : 0;
                registro.FechaCalificacion = ahora;

                puntajeObtenido += registro.PunteoObtenido.Value;
                puntajeTotal += pregunta.Punteo;
            }

            var notaIntento = puntajeTotal > 0
                ? Math.Round(puntajeObtenido / puntajeTotal * 100, 2)
                : 0;

            intento.Estado = estadoFinal;
            intento.FechaFinalizacion = ahora;
            intento.Calificacion = notaIntento;
            intento.FechaCalificacion = ahora;

            var notasPrevias = await contexto.IntentosCuestionarios
                .AsNoTracking()
                .Where(x =>
                    x.Id != intento.Id &&
                    x.CuestionarioId == intento.CuestionarioId &&
                    x.AlumnoId == intento.AlumnoId &&
                    (x.InscripcionId == intento.InscripcionId || !x.InscripcionId.HasValue) &&
                    (x.Estado == EstadoIntentoCuestionario.Finalizado ||
                     x.Estado == EstadoIntentoCuestionario.Expirado))
                .OrderBy(x => x.NumeroIntento)
                .Select(x => x.Calificacion ?? 0)
                .ToListAsync();

            var notaAplicada = AplicarCriterio(
                cuestionario.CriterioCalificacion,
                notasPrevias.Append(notaIntento).ToList());

            var entrega = await contexto.Entregas
                .Where(x =>
                    x.TareaId == cuestionario.TareaId &&
                    x.AlumnoId == intento.AlumnoId &&
                    (x.InscripcionId == intento.InscripcionId || !x.InscripcionId.HasValue))
                .OrderByDescending(x => x.InscripcionId == intento.InscripcionId)
                .ThenByDescending(x => x.NumeroEnvio)
                .FirstOrDefaultAsync();

            if (entrega is not null)
            {
                entrega.FechaEntrega ??= ahora;
                entrega.Estado = EstadoEntrega.Calificada;
                entrega.Calificacion = Math.Round(notaAplicada / 100 * cuestionario.Tarea.PunteoMaximo, 2);
                entrega.FechaCalificacion = ahora;
            }

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private RespuestaAlumno ObtenerOCrearRespuesta(IntentoCuestionario intento, int preguntaId, DateTime presentacion)
    {
        var registro = intento.Respuestas.FirstOrDefault(x => x.PreguntaId == preguntaId);
        if (registro is not null)
            return registro;

        registro = new RespuestaAlumno
        {
            IntentoCuestionarioId = intento.Id,
            PreguntaId = preguntaId,
            FechaPresentacion = presentacion
        };

        intento.Respuestas.Add(registro);
        contexto.RespuestasAlumnos.Add(registro);
        return registro;
    }

    private void AplicarRespuesta(
        RespuestaAlumno registro,
        Pregunta pregunta,
        RespuestaCuestionario respuesta,
        DateTime ahora)
    {
        registro.FechaRespuesta = ahora;

        if (pregunta.Tipo == TipoPregunta.TextoCorto)
        {
            var texto = respuesta.Texto?.Trim();
            registro.TextoRespuesta = string.IsNullOrEmpty(texto)
                ? null
                : texto.Length > 1000 ? texto[..1000] : texto;
            return;
        }

        IEnumerable<int> solicitadas = pregunta.Tipo == TipoPregunta.SeleccionMultiple
            ? respuesta.OpcionIds
            : respuesta.OpcionId.HasValue
                ? [respuesta.OpcionId.Value]
                : respuesta.OpcionIds.Take(1);

        var seleccion = solicitadas
            .Where(id => pregunta.Opciones.Any(x => x.Id == id))
            .ToHashSet();

        foreach (var anterior in registro.OpcionesSeleccionadas
                     .Where(x => !seleccion.Contains(x.OpcionPreguntaId))
                     .ToList())
        {
            registro.OpcionesSeleccionadas.Remove(anterior);
            contexto.RespuestasAlumnosOpciones.Remove(anterior);
        }

        foreach (var opcionId in seleccion.Where(id =>
                     registro.OpcionesSeleccionadas.All(x => x.OpcionPreguntaId != id)))
        {
            registro.OpcionesSeleccionadas.Add(new RespuestaAlumnoOpcion
            {
                PreguntaId = pregunta.Id,
                OpcionPreguntaId = opcionId
            });
        }
    }

    private static PreguntaIntento CrearPreguntaIntento(Pregunta pregunta, int numero, RespuestaAlumno? registro) =>
        new()
        {
            PreguntaId = pregunta.Id,
            Numero = numero,
            Tipo = pregunta.Tipo,
            Enunciado = pregunta.Enunciado,
            Punteo = pregunta.Punteo,
            Texto = registro?.TextoRespuesta,
            OpcionIds = registro?.OpcionesSeleccionadas.Select(x => x.OpcionPreguntaId).ToList() ?? [],
            Opciones = pregunta.Opciones
                .OrderBy(x => x.Orden)
                .Select(x => new OpcionCuestionario { Id = x.Id, Texto = x.Texto })
                .ToList()
        };

    private static Pregunta? PreguntaDelIntento(IntentoCuestionario intento, int preguntaId) =>
        LeerOrden(intento.OrdenPreguntas).Contains(preguntaId)
            ? intento.Cuestionario.Preguntas.FirstOrDefault(x => x.Id == preguntaId)
            : null;

    public static DateTime? CalcularFechaLimite(Cuestionario cuestionario, DateTime inicio)
    {
        DateTime? limite = cuestionario.ModalidadTiempo switch
        {
            ModalidadTiempoCuestionario.TiempoGeneral when cuestionario.TiempoGeneralSegundos > 0 =>
                inicio.AddSeconds(cuestionario.TiempoGeneralSegundos.Value),
            // Con tiempo por pregunta, el límite total es la suma de los tiempos
            // más la tolerancia de cada pregunta. Cada pregunta se controla aparte.
            ModalidadTiempoCuestionario.TiempoPorPregunta =>
                inicio.AddSeconds(cuestionario.Preguntas.Sum(p =>
                    (TiempoDePregunta(cuestionario, p) ?? 0) + ToleranciaSegundos)),
            _ => null
        };

        var tarea = cuestionario.Tarea;
        if (tarea.FechaLimite.HasValue && !tarea.PermitirEntregaTardia &&
            (!limite.HasValue || tarea.FechaLimite.Value < limite.Value))
        {
            limite = tarea.FechaLimite.Value;
        }

        return limite;
    }

    public static int? TiempoDePregunta(Cuestionario cuestionario, Pregunta pregunta) =>
        cuestionario.ModalidadTiempo == ModalidadTiempoCuestionario.TiempoPorPregunta
            ? pregunta.TiempoLimiteSegundos ?? cuestionario.TiempoPorPreguntaSegundos
            : null;

    private static bool Vencido(IntentoCuestionario intento, DateTime ahora) =>
        intento.FechaLimite.HasValue && ahora > intento.FechaLimite.Value + Tolerancia;

    private static bool TareaDisponible(Tarea tarea) =>
        tarea.Estado == EstadoTarea.Publicada &&
        tarea.Asignacion.Estado == EstadoRegistro.Activo &&
        tarea.Asignacion.Seccion.CicloEscolar.Activo;

    private static int SegundosHasta(DateTime limite, DateTime ahora) =>
        Math.Max(0, (int)Math.Ceiling((limite - ahora).TotalSeconds));

    private static List<int> LeerOrden(string? orden) =>
        (orden ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var id) ? id : 0)
            .Where(x => x > 0)
            .ToList();

    private static decimal AplicarCriterio(CriterioCalificacionIntentos criterio, IReadOnlyList<decimal> notas)
    {
        if (notas.Count == 0)
            return 0;

        return criterio switch
        {
            CriterioCalificacionIntentos.UltimoIntento => notas[^1],
            CriterioCalificacionIntentos.Promedio => Math.Round(notas.Average(), 2),
            _ => notas.Max()
        };
    }

    public static bool Evaluar(Pregunta pregunta, RespuestaAlumno? registro)
    {
        if (registro is null)
            return false;

        if (pregunta.Tipo == TipoPregunta.TextoCorto)
        {
            return pregunta.RespuestasAceptadas.Any(x =>
                ComparadorRespuestas.SonEquivalentes(x.Texto, registro.TextoRespuesta));
        }

        var correctas = pregunta.Opciones
            .Where(x => x.EsCorrecta)
            .Select(x => x.Id)
            .OrderBy(x => x)
            .ToArray();

        var marcadas = registro.OpcionesSeleccionadas
            .Select(x => x.OpcionPreguntaId)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        return correctas.Length > 0 && correctas.SequenceEqual(marcadas);
    }

    private static string DescribirRespuestaAlumno(Pregunta pregunta, RespuestaAlumno? registro)
    {
        if (registro is null)
            return "Sin respuesta";

        if (pregunta.Tipo == TipoPregunta.TextoCorto)
            return string.IsNullOrWhiteSpace(registro.TextoRespuesta) ? "Sin respuesta" : registro.TextoRespuesta;

        var textos = pregunta.Opciones
            .Where(x => registro.OpcionesSeleccionadas.Any(o => o.OpcionPreguntaId == x.Id))
            .OrderBy(x => x.Orden)
            .Select(x => x.Texto)
            .ToList();

        return textos.Count == 0 ? "Sin respuesta" : string.Join(", ", textos);
    }

    private static string DescribirRespuestaCorrecta(Pregunta pregunta) =>
        pregunta.Tipo == TipoPregunta.TextoCorto
            ? string.Join(" / ", pregunta.RespuestasAceptadas.Select(x => x.Texto))
            : string.Join(", ", pregunta.Opciones.Where(x => x.EsCorrecta).OrderBy(x => x.Orden).Select(x => x.Texto));

    private static List<string> ParsearRespuestasCorrectas(string? valor) =>
        (valor ?? string.Empty)
            .Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    // Las opciones de selección se comparan sin distinguir mayúsculas, pero sí
    // tildes, para no confundir opciones como "papa" y "papá".
    private static bool CoincideOpcion(string? opcion, string? correcta) =>
        string.Equals(opcion?.Trim(), correcta?.Trim(), StringComparison.OrdinalIgnoreCase);
}

public sealed class VistaIntento
{
    public bool Existe { get; private init; }

    public bool Finalizado { get; private init; }

    public int IntentoId { get; private init; }

    public IntentoEnCurso? Modelo { get; private init; }

    public static VistaIntento NoEncontrado() => new();

    public static VistaIntento Terminado(int intentoId) =>
        new() { Existe = true, Finalizado = true, IntentoId = intentoId };

    public static VistaIntento EnCurso(IntentoEnCurso modelo) =>
        new() { Existe = true, IntentoId = modelo.IntentoId, Modelo = modelo };
}
