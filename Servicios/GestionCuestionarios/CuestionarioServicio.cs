using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionAsignaciones;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.Cuestionarios;

namespace Proyecto_Final.Servicios.GestionCuestionarios;

public sealed class CuestionarioServicio(
    Contexto contexto,
    IAccesoAcademicoServicio acceso,
    IDelegacionAcademicaServicio? delegaciones = null) : ICuestionarioServicio
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
        var ahora = DateTime.UtcNow;

        if (docenteId.HasValue)
        {
            consulta = consulta.Where(x =>
                (historial
                    ? (x.Tarea.Asignacion.DocenteId == docenteId.Value ||
                       contexto.DelegacionesAcademicas.Any(d =>
                          d.AsignacionId == x.Tarea.AsignacionId &&
                          d.DocenteId == docenteId.Value && d.Activa &&
                          d.FechaInicioUtc <= ahora &&
                          (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                          (d.Operaciones & OperacionDelegacionAcademica.RevisarCuestionarios) ==
                              OperacionDelegacionAcademica.RevisarCuestionarios)) &&
                      (x.Tarea.Estado == EstadoTarea.Cerrada ||
                       x.Tarea.Asignacion.Estado != EstadoRegistro.Activo ||
                       !x.Tarea.Asignacion.Seccion.CicloEscolar.Activo)
                    : ((x.Tarea.Asignacion.DocenteId == docenteId.Value &&
                        x.Tarea.Asignacion.Estado == EstadoRegistro.Activo) ||
                       contexto.DelegacionesAcademicas.Any(d =>
                          d.AsignacionId == x.Tarea.AsignacionId &&
                          d.DocenteId == docenteId.Value && d.Activa &&
                          d.FechaInicioUtc <= ahora &&
                          (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                          (d.Operaciones & OperacionDelegacionAcademica.RevisarCuestionarios) ==
                              OperacionDelegacionAcademica.RevisarCuestionarios)) &&
                      x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                      x.Tarea.Estado != EstadoTarea.Cerrada));
        }
        else if (alumnoId.HasValue)
        {
            consulta = historial
                ? consulta.Where(x =>
                    x.Tarea.Estado != EstadoTarea.Borrador &&
                    contexto.Inscripciones.Any(i =>
                        i.AlumnoId == alumnoId.Value &&
                        i.SeccionId == x.Tarea.Asignacion.SeccionId &&
                        i.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId) &&
                    (x.Tarea.Estado == EstadoTarea.Cerrada ||
                     !(x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                       x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                       contexto.Inscripciones.Any(i =>
                           i.AlumnoId == alumnoId.Value &&
                           i.SeccionId == x.Tarea.Asignacion.SeccionId &&
                           i.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                           i.Estado == EstadoInscripcion.Activa))))
                : consulta.Where(x =>
                    (x.Tarea.Asignacion.Estado == EstadoRegistro.Activo ||
                     contexto.DelegacionesAcademicas.Any(d =>
                        d.AsignacionId == x.Tarea.AsignacionId && d.Activa &&
                        d.FechaInicioUtc <= ahora &&
                        (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                        (d.Operaciones & OperacionDelegacionAcademica.ContinuarActividadesAlumnos) ==
                            OperacionDelegacionAcademica.ContinuarActividadesAlumnos)) &&
                    x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                    x.Tarea.Estado == EstadoTarea.Publicada &&
                    (!x.Tarea.FechaDisponibilidad.HasValue || x.Tarea.FechaDisponibilidad <= ahora) &&
                    contexto.Inscripciones.Any(i =>
                    i.AlumnoId == alumnoId.Value &&
                    i.SeccionId == x.Tarea.Asignacion.SeccionId &&
                    i.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId &&
                    i.Estado == EstadoInscripcion.Activa &&
                    i.Seccion.CicloEscolar.Activo));
        }
        else if (acceso.EsOperadorInstitucional)
        {
            consulta = consulta.Where(x => historial
                ? x.Tarea.Estado == EstadoTarea.Cerrada ||
                  x.Tarea.Asignacion.Estado != EstadoRegistro.Activo ||
                  !x.Tarea.Asignacion.Seccion.CicloEscolar.Activo
                : x.Tarea.Estado != EstadoTarea.Cerrada &&
                  x.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                  x.Tarea.Asignacion.Seccion.CicloEscolar.Activo);
        }
        else
        {
            consulta = consulta.Where(_ => false);
        }

        var puedeVerResultados = !alumnoId.HasValue;

        return await consulta
            .OrderByDescending(x => x.Id)
            .Select(x => new CuestionarioLista
            {
                PuedeVerResultados = puedeVerResultados,
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
            // En cuestionarios nuevos las claves permanecen privadas hasta que
            // el docente las habilite expresamente en la configuración.
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
                (x.Tarea.Asignacion.Estado == EstadoRegistro.Activo ||
                 contexto.DelegacionesAcademicas.Any(d =>
                    d.AsignacionId == x.Tarea.AsignacionId && d.Activa &&
                    d.FechaInicioUtc <= ahora &&
                    (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                    (d.Operaciones & OperacionDelegacionAcademica.ContinuarActividadesAlumnos) ==
                        OperacionDelegacionAcademica.ContinuarActividadesAlumnos)) &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                (!x.Tarea.FechaDisponibilidad.HasValue || x.Tarea.FechaDisponibilidad <= ahora))
            .Select(x => new
            {
                Inicio = new InicioCuestionario
                {
                    CuestionarioId = x.Id,
                    TareaId = x.TareaId,
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

        var disponibilidad = await ObtenerDisponibilidadAlumnoAsync(
            modelo.Inicio.TareaId, alumnoId.Value, inscripcionId.Value, ahora);
        if (!disponibilidad.Disponible)
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
        inicio.OportunidadAutorizada = await contexto.ReaperturasEntregas
            .AsNoTracking()
            .AnyAsync(x => x.Entrega.TareaId == inicio.TareaId &&
                x.Entrega.AlumnoId == alumnoId.Value &&
                x.Entrega.InscripcionId == inscripcionId.Value &&
                x.AutorizaOportunidadAdicional && x.FechaLimite >= ahora &&
                !contexto.IntentosCuestionarios.Any(i => i.ReaperturaEntregaId == x.Id));

        // Fuera del plazo y sin entrega tardía solo se puede consultar el resultado.
        inicio.PlazoVencido = false;

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
                (x.Tarea.Asignacion.Estado == EstadoRegistro.Activo ||
                 contexto.DelegacionesAcademicas.Any(d =>
                    d.AsignacionId == x.Tarea.AsignacionId && d.Activa &&
                    d.FechaInicioUtc <= ahora &&
                    (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                    (d.Operaciones & OperacionDelegacionAcademica.ContinuarActividadesAlumnos) ==
                        OperacionDelegacionAcademica.ContinuarActividadesAlumnos)) &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo);

        if (cuestionario is null ||
            !await acceso.AlumnoPerteneceAsignacionAsync(cuestionario.Tarea.AsignacionId) ||
            (cuestionario.Tarea.FechaDisponibilidad.HasValue && cuestionario.Tarea.FechaDisponibilidad > ahora))
        {
            return ResultadoOperacion<int>.Error("El cuestionario no está disponible.");
        }

        var disponibilidad = await ObtenerDisponibilidadAlumnoAsync(
            cuestionario.TareaId, alumnoId.Value, inscripcionId.Value, ahora);
        if (!disponibilidad.Disponible)
            return ResultadoOperacion<int>.Error("El plazo para resolver el cuestionario venció o no tiene una reapertura autorizada.");

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

        var autorizacion = await contexto.ReaperturasEntregas
            .AsNoTracking()
            .Where(x => x.Entrega.TareaId == cuestionario.TareaId &&
                x.Entrega.AlumnoId == alumnoId.Value &&
                x.Entrega.InscripcionId == inscripcionId.Value &&
                x.AutorizaOportunidadAdicional && x.FechaLimite >= ahora &&
                !contexto.IntentosCuestionarios.Any(i => i.ReaperturaEntregaId == x.Id))
            .OrderBy(x => x.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();

        if (cuestionario.MaximoIntentos > 0 && terminados >= cuestionario.MaximoIntentos &&
            !autorizacion.HasValue)
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
            FechaLimite = CalcularFechaLimite(cuestionario, ahora, disponibilidad.FechaLimite),
            ReaperturaEntregaId = autorizacion,
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
        if (!await IntentoDisponibleAsync(intento, ahora) || Vencido(intento, ahora))
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
        if (!await IntentoDisponibleAsync(intento, ahora) || Vencido(intento, ahora))
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
        if (!await IntentoDisponibleAsync(intento, ahora) || Vencido(intento, ahora))
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
        if (!await IntentoDisponibleAsync(intento, ahora) || Vencido(intento, ahora))
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
                Correcta = EsCorrecta(pregunta, registro),
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
                 x.Cuestionario.Tarea.Estado != EstadoTarea.Publicada ||
                 contexto.Entregas.Any(entrega =>
                    entrega.TareaId == x.Cuestionario.TareaId &&
                    entrega.AlumnoId == x.AlumnoId &&
                    entrega.InscripcionId == x.InscripcionId &&
                    entrega.FechaLimiteIndividual.HasValue &&
                    entrega.FechaLimiteIndividual.Value < limite &&
                    !contexto.Entregas.Any(posterior =>
                        posterior.TareaId == entrega.TareaId &&
                        posterior.AlumnoId == entrega.AlumnoId &&
                        posterior.InscripcionId == entrega.InscripcionId &&
                        posterior.NumeroEnvio > entrega.NumeroEnvio)) ||
                 contexto.ReaperturasTareas.Any(reapertura =>
                    reapertura.TareaId == x.Cuestionario.TareaId &&
                    reapertura.FechaLimite < limite &&
                    !contexto.ReaperturasTareas.Any(posterior =>
                        posterior.TareaId == reapertura.TareaId &&
                        (posterior.FechaReapertura > reapertura.FechaReapertura ||
                         (posterior.FechaReapertura == reapertura.FechaReapertura &&
                          posterior.Id > reapertura.Id))))))
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

            if (!Vencido(intento, ahora) && await IntentoDisponibleAsync(intento, ahora))
                continue;

            await FinalizarInternoAsync(intento, EstadoIntentoCuestionario.Expirado, ahora);
            contexto.ChangeTracker.Clear();
            finalizados++;
        }

        return finalizados;
    }

    // =====================================================================
    // REVISIÓN DEL DOCENTE
    // =====================================================================

    public async Task<ResultadosCuestionario?> ObtenerResultadosAsync(int id)
    {
        var datos = await contexto.Cuestionarios
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.TareaId,
                x.Tarea.AsignacionId,
                x.Tarea.Titulo,
                x.Tarea.Estado,
                x.Tarea.PunteoMaximo,
                Curso = x.Tarea.Asignacion.Curso.Nombre,
                GradoSeccion = x.Tarea.Asignacion.Seccion.Grado.Nombre + " " + x.Tarea.Asignacion.Seccion.Nombre,
                x.MaximoIntentos,
                x.CriterioCalificacion
            })
            .FirstOrDefaultAsync();

        if (datos is null || !await PuedeRevisarAsync(datos.AsignacionId))
            return null;

        var preguntas = await contexto.Preguntas
            .AsNoTracking()
            .Include(x => x.Opciones)
            .Include(x => x.RespuestasAceptadas)
            .Where(x => x.CuestionarioId == id)
            .OrderBy(x => x.Orden)
            .ToListAsync();

        var entregas = await contexto.Entregas
            .AsNoTracking()
            .Where(x => x.TareaId == datos.TareaId)
            .Select(x => new
            {
                x.AlumnoId,
                x.NumeroEnvio,
                x.Calificacion,
                Nombre = x.Alumno.Usuario.PrimerApellido + ", " + x.Alumno.Usuario.PrimerNombre,
                Codigo = x.Alumno.CodigoPersonal
            })
            .ToListAsync();

        var intentos = await contexto.IntentosCuestionarios
            .AsNoTracking()
            .Where(x => x.CuestionarioId == id)
            .Select(x => new
            {
                x.Id,
                x.AlumnoId,
                x.NumeroIntento,
                x.Estado,
                x.Calificacion,
                x.FechaInicio,
                x.FechaFinalizacion,
                Nombre = x.Alumno.Usuario.PrimerApellido + ", " + x.Alumno.Usuario.PrimerNombre,
                Codigo = x.Alumno.CodigoPersonal
            })
            .ToListAsync();

        // Una fila por alumno: los que tienen entrega de la tarea y los que
        // tengan intentos aunque ya no estén inscritos en la sección.
        var alumnos = entregas
            .GroupBy(x => x.AlumnoId)
            .Select(g => g.OrderByDescending(x => x.NumeroEnvio).First())
            .Select(x => new { x.AlumnoId, x.Nombre, x.Codigo, Nota = x.Calificacion })
            .ToList();

        alumnos.AddRange(intentos
            .Where(i => alumnos.All(a => a.AlumnoId != i.AlumnoId))
            .GroupBy(i => i.AlumnoId)
            .Select(g => new { AlumnoId = g.Key, g.First().Nombre, g.First().Codigo, Nota = (decimal?)null }));

        var modelo = new ResultadosCuestionario
        {
            CuestionarioId = datos.Id,
            Titulo = datos.Titulo,
            Curso = datos.Curso,
            GradoSeccion = datos.GradoSeccion,
            EstadoTarea = datos.Estado,
            PunteoMaximo = datos.PunteoMaximo,
            MaximoIntentos = datos.MaximoIntentos,
            CriterioCalificacion = datos.CriterioCalificacion,
            TotalAlumnos = alumnos.Count
        };

        foreach (var alumno in alumnos.OrderBy(x => x.Nombre))
        {
            var propios = intentos
                .Where(x => x.AlumnoId == alumno.AlumnoId)
                .OrderBy(x => x.NumeroIntento)
                .ToList();

            var ultimo = propios.LastOrDefault();
            var estado = ultimo?.Estado switch
            {
                null => "Sin iniciar",
                EstadoIntentoCuestionario.EnCurso => "En curso",
                EstadoIntentoCuestionario.Expirado => "Terminado por tiempo",
                EstadoIntentoCuestionario.Anulado => "Anulado",
                _ => "Finalizado"
            };

            modelo.Alumnos.Add(new ResultadoAlumnoCuestionario
            {
                AlumnoId = alumno.AlumnoId,
                Alumno = alumno.Nombre,
                CodigoPersonal = alumno.Codigo ?? string.Empty,
                Estado = estado,
                NotaEntrega = alumno.Nota,
                Intentos = propios.Select(x => new IntentoResumen
                {
                    IntentoId = x.Id,
                    Numero = x.NumeroIntento,
                    Estado = x.Estado,
                    Calificacion = x.Calificacion,
                    Fecha = x.FechaFinalizacion ?? x.FechaInicio
                }).ToList()
            });
        }

        // Estadísticas: el intento que define la nota de cada alumno
        // (el mejor con el criterio "mejor nota"; el último en los demás).
        var ultimosTerminados = intentos
            .Where(x => x.Estado is EstadoIntentoCuestionario.Finalizado or EstadoIntentoCuestionario.Expirado)
            .GroupBy(x => x.AlumnoId)
            .Select(g => datos.CriterioCalificacion == CriterioCalificacionIntentos.MejorNota
                ? g.OrderByDescending(x => x.Calificacion ?? 0).ThenByDescending(x => x.NumeroIntento).First()
                : g.OrderByDescending(x => x.NumeroIntento).First())
            .ToList();

        modelo.AlumnosConIntento = ultimosTerminados.Count;
        modelo.PromedioPorcentaje = ultimosTerminados.Count == 0
            ? null
            : Math.Round(ultimosTerminados.Average(x => x.Calificacion ?? 0), 2);

        var idsIntentos = ultimosTerminados.Select(x => x.Id).ToList();
        var respuestas = await contexto.RespuestasAlumnos
            .AsNoTracking()
            .Include(x => x.OpcionesSeleccionadas)
            .Where(x => idsIntentos.Contains(x.IntentoCuestionarioId))
            .ToListAsync();

        var numero = 1;
        foreach (var pregunta in preguntas)
        {
            var propias = idsIntentos
                .Select(intentoId => respuestas.FirstOrDefault(r =>
                    r.IntentoCuestionarioId == intentoId && r.PreguntaId == pregunta.Id))
                .ToList();

            modelo.Preguntas.Add(new EstadisticaPregunta
            {
                Numero = numero++,
                Enunciado = pregunta.Enunciado,
                Tipo = pregunta.Tipo,
                Evaluados = propias.Count,
                SinResponder = propias.Count(r => SinResponder(pregunta, r)),
                Correctas = propias.Count(r => EsCorrecta(pregunta, r))
            });
        }

        return modelo;
    }

    public async Task<RevisionIntento?> ObtenerRevisionIntentoAsync(int intentoId)
    {
        var intento = await contexto.IntentosCuestionarios
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Alumno).ThenInclude(x => x.Usuario)
            .Include(x => x.Cuestionario).ThenInclude(x => x.Tarea)
            .Include(x => x.Cuestionario).ThenInclude(x => x.Preguntas).ThenInclude(x => x.Opciones)
            .Include(x => x.Cuestionario).ThenInclude(x => x.Preguntas).ThenInclude(x => x.RespuestasAceptadas)
            .Include(x => x.Respuestas).ThenInclude(x => x.OpcionesSeleccionadas)
            .Include(x => x.Respuestas).ThenInclude(x => x.CalificadoPorUsuario)
            .FirstOrDefaultAsync(x => x.Id == intentoId);

        if (intento is null || !await PuedeRevisarAsync(intento.Cuestionario.Tarea.AsignacionId))
            return null;

        var cuestionario = intento.Cuestionario;
        var terminado = intento.Estado is EstadoIntentoCuestionario.Finalizado or EstadoIntentoCuestionario.Expirado;

        var notaEntrega = await contexto.Entregas
            .AsNoTracking()
            .Where(x =>
                x.TareaId == cuestionario.TareaId &&
                x.AlumnoId == intento.AlumnoId &&
                (x.InscripcionId == intento.InscripcionId || !x.InscripcionId.HasValue))
            .OrderByDescending(x => x.NumeroEnvio)
            .Select(x => x.Calificacion)
            .FirstOrDefaultAsync();

        var modelo = new RevisionIntento
        {
            IntentoId = intento.Id,
            CuestionarioId = cuestionario.Id,
            Titulo = cuestionario.Tarea.Titulo,
            Alumno = intento.Alumno.Usuario.PrimerNombre + " " + intento.Alumno.Usuario.PrimerApellido,
            CodigoPersonal = intento.Alumno.CodigoPersonal ?? string.Empty,
            NumeroIntento = intento.NumeroIntento,
            Estado = intento.Estado,
            FechaInicio = intento.FechaInicio,
            FechaFinalizacion = intento.FechaFinalizacion,
            Calificacion = intento.Calificacion ?? 0,
            PunteoObtenido = intento.Respuestas.Sum(x => x.PunteoObtenido ?? 0),
            PunteoTotal = cuestionario.Preguntas.Sum(x => x.Punteo),
            NotaEntrega = notaEntrega,
            PunteoMaximoTarea = cuestionario.Tarea.PunteoMaximo,
            PuedeAjustar = terminado && await PuedeAjustarAsync(cuestionario.Tarea.AsignacionId)
        };

        var orden = LeerOrden(intento.OrdenPreguntas);
        if (orden.Count == 0)
            orden = cuestionario.Preguntas.OrderBy(x => x.Orden).Select(x => x.Id).ToList();

        var numero = 1;
        foreach (var preguntaId in orden)
        {
            var pregunta = cuestionario.Preguntas.FirstOrDefault(x => x.Id == preguntaId);
            if (pregunta is null)
                continue;

            var registro = intento.Respuestas.FirstOrDefault(x => x.PreguntaId == preguntaId);
            modelo.Detalle.Add(new DetalleRevision
            {
                RespuestaId = registro?.Id,
                Numero = numero++,
                Tipo = pregunta.Tipo,
                Enunciado = pregunta.Enunciado,
                RespuestaAlumno = DescribirRespuestaAlumno(pregunta, registro),
                RespuestaCorrecta = DescribirRespuestaCorrecta(pregunta),
                SinResponder = SinResponder(pregunta, registro),
                Correcta = terminado ? EsCorrecta(pregunta, registro) : Evaluar(pregunta, registro),
                PunteoObtenido = registro?.PunteoObtenido ?? 0,
                Punteo = pregunta.Punteo,
                Retroalimentacion = pregunta.Retroalimentacion,
                AjusteManual = registro?.CalificadoPorUsuarioId is not null,
                AjustadoPor = registro?.CalificadoPorUsuario is { } usuario
                    ? usuario.PrimerNombre + " " + usuario.PrimerApellido
                    : null,
                FechaAjuste = registro?.CalificadoPorUsuarioId is not null ? registro.FechaCalificacion : null
            });
        }

        return modelo;
    }

    // Acepta como correcta (o revierte) una respuesta directa que la
    // comparación automática no reconoció, y recalcula el intento y la entrega.
    public async Task<ResultadoOperacion<int>> AjustarRespuestaDirectaAsync(int respuestaId, bool aceptar)
    {
        var registro = await contexto.RespuestasAlumnos
            .Include(x => x.Pregunta).ThenInclude(x => x.RespuestasAceptadas)
            .Include(x => x.IntentoCuestionario).ThenInclude(x => x.Cuestionario).ThenInclude(x => x.Tarea)
            .Include(x => x.IntentoCuestionario).ThenInclude(x => x.Cuestionario).ThenInclude(x => x.Preguntas)
            .FirstOrDefaultAsync(x => x.Id == respuestaId);

        if (registro is null)
            return ResultadoOperacion<int>.Error("La respuesta no existe.");

        var intento = registro.IntentoCuestionario;
        var cuestionario = intento.Cuestionario;

        if (!await PuedeAjustarAsync(cuestionario.Tarea.AsignacionId))
            return ResultadoOperacion<int>.Error("No tiene permiso para ajustar respuestas de este cuestionario.");

        if (intento.Estado is not (EstadoIntentoCuestionario.Finalizado or EstadoIntentoCuestionario.Expirado))
            return ResultadoOperacion<int>.Error("Solo se pueden ajustar intentos terminados.");

        if (registro.Pregunta.Tipo != TipoPregunta.TextoCorto)
            return ResultadoOperacion<int>.Error("Solo se ajustan preguntas de respuesta directa.");

        if (string.IsNullOrWhiteSpace(registro.TextoRespuesta))
            return ResultadoOperacion<int>.Error("La pregunta no tiene respuesta del alumno.");

        var ahora = DateTime.UtcNow;
        string mensaje;

        if (aceptar)
        {
            if (registro.PunteoObtenido >= registro.Pregunta.Punteo)
                return ResultadoOperacion<int>.Correcto(intento.Id, "La respuesta ya estaba calificada como correcta.");

            registro.PunteoObtenido = registro.Pregunta.Punteo;
            registro.CalificadoPorUsuarioId = acceso.UsuarioId;
            registro.FechaCalificacion = ahora;
            registro.RequiereRevisionManual = false;
            mensaje = "La respuesta fue aceptada como correcta y la nota se recalculó.";
        }
        else
        {
            if (registro.CalificadoPorUsuarioId is null)
                return ResultadoOperacion<int>.Error("La respuesta no tiene un ajuste manual que revertir.");

            registro.PunteoObtenido = Evaluar(registro.Pregunta, registro) ? registro.Pregunta.Punteo : 0;
            registro.CalificadoPorUsuarioId = null;
            registro.FechaCalificacion = ahora;
            mensaje = "Se revirtió el ajuste y la nota se recalculó.";
        }

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            await contexto.SaveChangesAsync();

            var orden = LeerOrden(intento.OrdenPreguntas);
            var preguntas = orden.Count > 0
                ? cuestionario.Preguntas.Where(x => orden.Contains(x.Id)).ToList()
                : cuestionario.Preguntas.ToList();

            var total = preguntas.Sum(x => x.Punteo);
            var obtenido = await contexto.RespuestasAlumnos
                .Where(x => x.IntentoCuestionarioId == intento.Id)
                .SumAsync(x => x.PunteoObtenido ?? 0);

            intento.Calificacion = total > 0 ? Math.Round(obtenido / total * 100, 2) : 0;
            intento.FechaCalificacion = ahora;
            await contexto.SaveChangesAsync();

            await ActualizarEntregaAsync(intento, cuestionario, ahora);
            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }

        return ResultadoOperacion<int>.Correcto(intento.Id, mensaje);
    }

    // Desde Entregas: dirige al alumno a su resultado autorizado o al revisor
    // al intento que define la nota, sin mezclar ambos permisos.
    public async Task<AccesoRespuestasEntrega?> ObtenerRespuestasDeEntregaAsync(int entregaId)
    {
        var entrega = await contexto.Entregas
            .AsNoTracking()
            .Where(x => x.Id == entregaId && x.Tarea.Cuestionario != null)
            .Select(x => new
            {
                x.AlumnoId,
                x.InscripcionId,
                x.Tarea.AsignacionId,
                CuestionarioId = x.Tarea.Cuestionario!.Id,
                x.Tarea.Cuestionario.CriterioCalificacion,
                x.Tarea.Cuestionario.MostrarRespuestasCorrectas
            })
            .FirstOrDefaultAsync();

        if (entrega is null)
            return null;

        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var esAlumno = perfil.AlumnoId.HasValue;
        if (esAlumno)
        {
            if (perfil.AlumnoId != entrega.AlumnoId ||
                !await acceso.PuedeConsultarAsignacionHistoricaAsync(entrega.AsignacionId))
            {
                return null;
            }

            if (!entrega.MostrarRespuestasCorrectas)
            {
                return new AccesoRespuestasEntrega
                {
                    CuestionarioId = entrega.CuestionarioId,
                    EsAlumno = true,
                    RespuestasRestringidas = true
                };
            }
        }
        else if (!await PuedeRevisarAsync(entrega.AsignacionId))
        {
            return null;
        }

        var intentos = await contexto.IntentosCuestionarios
            .AsNoTracking()
            .Where(x =>
                x.CuestionarioId == entrega.CuestionarioId &&
                x.AlumnoId == entrega.AlumnoId &&
                (x.InscripcionId == entrega.InscripcionId || !x.InscripcionId.HasValue) &&
                x.Estado != EstadoIntentoCuestionario.Anulado)
            .Select(x => new { x.Id, x.NumeroIntento, x.Estado, x.Calificacion })
            .ToListAsync();

        // El intento que define la nota; si ninguno terminó, el que está en curso.
        var terminados = intentos
            .Where(x => x.Estado is EstadoIntentoCuestionario.Finalizado or EstadoIntentoCuestionario.Expirado)
            .ToList();

        var elegido = terminados.Count == 0
            ? intentos.OrderByDescending(x => x.NumeroIntento).FirstOrDefault()
            : entrega.CriterioCalificacion == CriterioCalificacionIntentos.MejorNota
                ? terminados.OrderByDescending(x => x.Calificacion ?? 0).ThenByDescending(x => x.NumeroIntento).First()
                : terminados.OrderByDescending(x => x.NumeroIntento).First();

        return new AccesoRespuestasEntrega
        {
            IntentoId = elegido?.Id,
            CuestionarioId = entrega.CuestionarioId,
            EsAlumno = esAlumno
        };
    }

    // Docente de la asignación (también ciclos anteriores) o coordinación.
    private async Task<bool> PuedeRevisarAsync(int asignacionId)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        if (perfil.AlumnoId.HasValue && !perfil.DocenteId.HasValue)
            return false;

        if (perfil.DocenteId.HasValue)
        {
            if (await acceso.PuedeConsultarAsignacionHistoricaAsync(asignacionId))
                return true;

            return delegaciones != null && await delegaciones.PuedeActuarAsync(
                asignacionId,
                perfil.DocenteId.Value,
                OperacionDelegacionAcademica.RevisarCuestionarios);
        }

        return acceso.EsOperadorInstitucional &&
            acceso.TienePermiso(Seguridad.Permisos.Cuestionarios.Ver) &&
            await acceso.PuedeConsultarAsignacionHistoricaAsync(asignacionId);
    }

    // Solo en asignaciones vigentes: los ciclos cerrados quedan de consulta.
    private async Task<bool> PuedeAjustarAsync(int asignacionId)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        if (!perfil.DocenteId.HasValue)
            return false;

        if (!acceso.TienePermiso(Seguridad.Permisos.Entregas.Calificar))
            return false;

        return await acceso.EsDocenteDeAsignacionAsync(asignacionId);
    }

    // =====================================================================
    // IMPORTACIÓN DESDE IA
    // =====================================================================

    // Tareas de cuestionario en borrador donde el usuario puede agregar preguntas.
    public async Task<List<OpcionSeleccion>> ObtenerTareasParaImportarAsync()
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var consulta = contexto.Tareas
            .AsNoTracking()
            .Where(x =>
                x.Tipo == TipoTarea.Cuestionario &&
                x.Estado == EstadoTarea.Borrador &&
                x.Asignacion.Estado == EstadoRegistro.Activo &&
                x.Asignacion.Seccion.CicloEscolar.Activo);

        if (perfil.DocenteId.HasValue)
            consulta = consulta.Where(x => x.Asignacion.DocenteId == perfil.DocenteId.Value);
        else if (!acceso.EsOperadorInstitucional)
            return [];

        return await consulta
            .OrderByDescending(x => x.Id)
            .Select(x => new OpcionSeleccion
            {
                Id = x.Id,
                Texto = x.Titulo + " · " + x.Asignacion.Curso.Nombre + " · " +
                        x.Asignacion.Seccion.Grado.Nombre + " " + x.Asignacion.Seccion.Nombre
            })
            .ToListAsync();
    }

    // Agrega al cuestionario de la tarea las preguntas revisadas por el
    // docente. La tarea sigue en borrador: el docente decide cuándo publicar.
    public async Task<ResultadoOperacion<int>> ImportarPreguntasAsync(ImportarCuestionarioIa modelo)
    {
        if (!modelo.TareaId.HasValue)
            return ResultadoOperacion<int>.Validacion(nameof(modelo.TareaId), "Seleccione la tarea de cuestionario.");

        var tarea = await contexto.Tareas.FirstOrDefaultAsync(x =>
            x.Id == modelo.TareaId.Value &&
            x.Tipo == TipoTarea.Cuestionario &&
            x.Estado == EstadoTarea.Borrador);

        if (tarea is null)
            return ResultadoOperacion<int>.Validacion(nameof(modelo.TareaId), "La tarea no existe o ya fue publicada.");

        if (!await acceso.PuedeGestionarAsignacionVigenteAsync(tarea.AsignacionId))
            return ResultadoOperacion<int>.Error("No tiene acceso a esa tarea.");

        var seleccionadas = modelo.Preguntas
            .Select((pregunta, indice) => (pregunta, indice))
            .Where(x => x.pregunta.Incluir)
            .ToList();

        if (seleccionadas.Count == 0)
            return ResultadoOperacion<int>.Error("Marque al menos una pregunta para importar.");

        // Validación de cada pregunta marcada.
        var errores = new Dictionary<string, string>();
        var preparadas = new List<(PreguntaImportadaIa Datos, string[] Opciones, List<string> Correctas)>();

        foreach (var (pregunta, indice) in seleccionadas)
        {
            var clave = $"Preguntas[{indice}]";
            var opciones = pregunta.Tipo == TipoPregunta.VerdaderoFalso
                ? ["Verdadero", "Falso"]
                : Lineas(pregunta.Opciones);
            var correctas = Lineas(pregunta.Correctas).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (string.IsNullOrWhiteSpace(pregunta.Enunciado))
                errores[$"{clave}.Enunciado"] = "Escriba la pregunta.";

            if (pregunta.Punteo <= 0)
                errores[$"{clave}.Punteo"] = "El punteo debe ser mayor que cero.";

            if (pregunta.Tipo == TipoPregunta.TextoCorto)
            {
                if (correctas.Count == 0)
                    errores[$"{clave}.Correctas"] = "Escriba al menos una respuesta aceptada.";
                opciones = [];
            }
            else
            {
                if (opciones.Length < 2)
                    errores[$"{clave}.Opciones"] = "Escriba al menos dos opciones, una por línea.";
                else if (correctas.Count == 0)
                    errores[$"{clave}.Correctas"] = "Indique la respuesta correcta.";
                else if (pregunta.Tipo != TipoPregunta.SeleccionMultiple && correctas.Count != 1)
                    errores[$"{clave}.Correctas"] = "Este tipo de pregunta lleva una sola respuesta correcta.";
                else if (correctas.Any(c => !opciones.Any(o => CoincideOpcion(o, c))))
                    errores[$"{clave}.Correctas"] = "Cada respuesta correcta debe ser igual a una de las opciones.";
            }

            preparadas.Add((pregunta, opciones, correctas));
        }

        if (errores.Count > 0)
            return ResultadoOperacion<int>.Validacion(errores);

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            var cuestionario = await contexto.Cuestionarios
                .FirstOrDefaultAsync(x => x.TareaId == tarea.Id);

            if (cuestionario is null)
            {
                cuestionario = new Cuestionario { TareaId = tarea.Id };
                contexto.Cuestionarios.Add(cuestionario);
            }

            if (cuestionario.ModalidadTiempo == ModalidadTiempoCuestionario.TiempoPorPregunta &&
                !cuestionario.TiempoPorPreguntaSegundos.HasValue)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<int>.Error(
                    "El cuestionario usa tiempo por pregunta sin un tiempo predeterminado. Configúrelo antes de importar.");
            }

            cuestionario.Origen = OrigenCuestionario.InteligenciaArtificial;
            await contexto.SaveChangesAsync();

            var orden = await contexto.Preguntas
                .Where(x => x.CuestionarioId == cuestionario.Id)
                .Select(x => (int?)x.Orden)
                .MaxAsync() ?? 0;

            foreach (var (datos, opciones, correctas) in preparadas)
            {
                var pregunta = new Pregunta
                {
                    CuestionarioId = cuestionario.Id,
                    Orden = ++orden,
                    Tipo = datos.Tipo,
                    Enunciado = datos.Enunciado.Trim(),
                    Punteo = datos.Punteo,
                    Retroalimentacion = string.IsNullOrWhiteSpace(datos.Retroalimentacion)
                        ? null
                        : datos.Retroalimentacion.Trim()
                };

                for (var i = 0; i < opciones.Length; i++)
                {
                    pregunta.Opciones.Add(new OpcionPregunta
                    {
                        Orden = i + 1,
                        Texto = opciones[i],
                        EsCorrecta = correctas.Any(c => CoincideOpcion(opciones[i], c))
                    });
                }

                if (datos.Tipo == TipoPregunta.TextoCorto)
                {
                    foreach (var correcta in correctas)
                        pregunta.RespuestasAceptadas.Add(new RespuestaAceptada { Texto = correcta });
                }

                contexto.Preguntas.Add(pregunta);
            }

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion<int>.Correcto(
                cuestionario.Id,
                $"Se importaron {preparadas.Count} pregunta(s). Revise el cuestionario y publíquelo cuando esté listo.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private static string[] Lineas(string? valor) =>
        (valor ?? string.Empty)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .ToArray();

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

            await contexto.SaveChangesAsync();
            await ActualizarEntregaAsync(intento, cuestionario, ahora);

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    // Recalcula la calificación de la entrega con todos los intentos terminados
    // del alumno, según el criterio del cuestionario. Se usa al finalizar un
    // intento y cuando el docente ajusta una respuesta.
    private async Task ActualizarEntregaAsync(
        IntentoCuestionario intento,
        Cuestionario cuestionario,
        DateTime ahora)
    {
        var notas = await contexto.IntentosCuestionarios
            .AsNoTracking()
            .Where(x =>
                x.CuestionarioId == intento.CuestionarioId &&
                x.AlumnoId == intento.AlumnoId &&
                (x.InscripcionId == intento.InscripcionId || !x.InscripcionId.HasValue) &&
                (x.Estado == EstadoIntentoCuestionario.Finalizado ||
                 x.Estado == EstadoIntentoCuestionario.Expirado))
            .OrderBy(x => x.NumeroIntento)
            .Select(x => x.Calificacion ?? 0)
            .ToListAsync();

        if (notas.Count == 0)
            return;

        var notaAplicada = AplicarCriterio(cuestionario.CriterioCalificacion, notas);

        var entrega = await contexto.Entregas
            .Where(x =>
                x.TareaId == cuestionario.TareaId &&
                x.AlumnoId == intento.AlumnoId &&
                (x.InscripcionId == intento.InscripcionId || !x.InscripcionId.HasValue))
            .OrderByDescending(x => x.InscripcionId == intento.InscripcionId)
            .ThenByDescending(x => x.NumeroEnvio)
            .FirstOrDefaultAsync();

        if (entrega is null)
            return;

        entrega.FechaEntrega ??= ahora;
        entrega.Estado = EstadoEntrega.Calificada;
        entrega.Calificacion = Math.Round(notaAplicada / 100 * cuestionario.Tarea.PunteoMaximo, 2);
        entrega.FechaCalificacion = ahora;
        await contexto.SaveChangesAsync();
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

    // La reapertura general no cambia por sí sola el acceso de todos: se
    // consulta el alumno alcanzado y el plazo individual que quedó auditado.
    private async Task<DisponibilidadAlumno> ObtenerDisponibilidadAlumnoAsync(
        int tareaId, int alumnoId, int inscripcionId, DateTime ahora)
    {
        var entrega = await contexto.Entregas.AsNoTracking()
            .Where(x => x.TareaId == tareaId && x.AlumnoId == alumnoId &&
                x.InscripcionId == inscripcionId)
            .OrderByDescending(x => x.NumeroEnvio)
            .Select(x => new
            {
                x.FechaLimiteIndividual,
                TieneReaperturaVigente = x.Reaperturas.Any(r => r.FechaLimite >= ahora)
            })
            .FirstOrDefaultAsync();

        // El plazo individual es la autorización efectiva de la entrega más
        // reciente. Cuando vence no se debe volver al plazo general, pues eso
        // reviviría una autorización individual ya agotada.
        if (entrega?.FechaLimiteIndividual is DateTime limiteIndividual)
            return new DisponibilidadAlumno(
                limiteIndividual >= ahora && entrega.TieneReaperturaVigente,
                limiteIndividual);

        var general = await contexto.ReaperturasTareas.AsNoTracking()
            .Where(x => x.TareaId == tareaId)
            .OrderByDescending(x => x.FechaReapertura)
            .Select(x => new { x.Id, x.FechaLimite })
            .FirstOrDefaultAsync();

        if (general is not null)
        {
            if (general.FechaLimite < ahora)
                return new DisponibilidadAlumno(false, general.FechaLimite);

            var beneficiado = await contexto.ReaperturasTareasAlumnos.AsNoTracking()
                .AnyAsync(x => x.ReaperturaTareaId == general.Id && x.AlumnoId == alumnoId);
            if (!beneficiado)
                return new DisponibilidadAlumno(false, general.FechaLimite);

            return new DisponibilidadAlumno(true, general.FechaLimite);
        }

        var tarea = await contexto.Tareas.AsNoTracking().Where(x => x.Id == tareaId)
            .Select(x => new { x.Estado, x.FechaDisponibilidad, x.FechaLimite, x.PermitirEntregaTardia })
            .FirstOrDefaultAsync();
        if (tarea is null || tarea.Estado == EstadoTarea.Borrador)
            return new DisponibilidadAlumno(false, tarea?.FechaLimite);

        if (tarea.Estado == EstadoTarea.Cerrada)
            return new DisponibilidadAlumno(tarea.PermitirEntregaTardia, tarea.FechaLimite);

        if (tarea.Estado != EstadoTarea.Publicada ||
            (tarea.FechaDisponibilidad.HasValue && tarea.FechaDisponibilidad > ahora) ||
            (tarea.FechaLimite.HasValue && tarea.FechaLimite < ahora && !tarea.PermitirEntregaTardia))
            return new DisponibilidadAlumno(false, tarea?.FechaLimite);

        return new DisponibilidadAlumno(true, tarea.FechaLimite);
    }

    private sealed record DisponibilidadAlumno(bool Disponible, DateTime? FechaLimite);

    public static DateTime? CalcularFechaLimite(
        Cuestionario cuestionario, DateTime inicio, DateTime? fechaLimiteActividad = null)
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
        var fechaActividad = fechaLimiteActividad ?? tarea.FechaLimite;
        if (fechaActividad.HasValue && !tarea.PermitirEntregaTardia &&
            (!limite.HasValue || fechaActividad.Value < limite.Value))
        {
            limite = fechaActividad.Value;
        }

        return limite;
    }

    public static int? TiempoDePregunta(Cuestionario cuestionario, Pregunta pregunta) =>
        cuestionario.ModalidadTiempo == ModalidadTiempoCuestionario.TiempoPorPregunta
            ? pregunta.TiempoLimiteSegundos ?? cuestionario.TiempoPorPreguntaSegundos
            : null;

    private static bool Vencido(IntentoCuestionario intento, DateTime ahora) =>
        intento.FechaLimite.HasValue && ahora > intento.FechaLimite.Value + Tolerancia;

    private async Task<bool> IntentoDisponibleAsync(IntentoCuestionario intento, DateTime ahora)
    {
        if (!await TareaDisponibleAsync(intento.Cuestionario.Tarea))
            return false;

        var disponibilidad = await ObtenerDisponibilidadAlumnoAsync(
            intento.Cuestionario.TareaId, intento.AlumnoId, intento.InscripcionId ?? 0, ahora);
        return disponibilidad.Disponible;
    }

    private async Task<bool> TareaDisponibleAsync(Tarea tarea)
    {
        if (!tarea.Asignacion.Seccion.CicloEscolar.Activo)
            return false;

        if (tarea.Asignacion.Estado == EstadoRegistro.Activo)
            return true;

        var ahora = DateTime.UtcNow;
        return await contexto.DelegacionesAcademicas.AsNoTracking().AnyAsync(d =>
            d.AsignacionId == tarea.AsignacionId && d.Activa &&
            d.FechaInicioUtc <= ahora &&
            (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
            (d.Operaciones & OperacionDelegacionAcademica.ContinuarActividadesAlumnos) ==
                OperacionDelegacionAcademica.ContinuarActividadesAlumnos);
    }

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

    // Correcta según el punteo registrado, que incluye los ajustes del docente.
    private static bool EsCorrecta(Pregunta pregunta, RespuestaAlumno? registro) =>
        registro?.PunteoObtenido is decimal punteo && pregunta.Punteo > 0 && punteo >= pregunta.Punteo;

    private static bool SinResponder(Pregunta pregunta, RespuestaAlumno? registro) =>
        registro is null ||
        (pregunta.Tipo == TipoPregunta.TextoCorto
            ? string.IsNullOrWhiteSpace(registro.TextoRespuesta)
            : registro.OpcionesSeleccionadas.Count == 0);

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
