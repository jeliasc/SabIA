using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.GestionCuestionarios;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Cuestionarios;

namespace Proyecto_Final.Tests;

public sealed class CuestionarioServicioTests
{
    // Ids fijos del escenario
    internal const int CuestionarioId = 1;
    internal const int PreguntaUnica = 10;
    internal const int PreguntaDirecta = 20;
    internal const int PreguntaMultiple = 30;
    internal const int OpcionCorrectaUnica = 101;
    internal const int OpcionIncorrectaUnica = 102;
    internal const int OpcionMultipleX = 301;
    internal const int OpcionMultipleY = 302;
    internal const int OpcionMultipleZ = 303;

    // -----------------------------------------------------------------
    // Respuestas directas
    // -----------------------------------------------------------------

    [Theory]
    [InlineData("Árbol", "arbol")]
    [InlineData("árbol", "  ARBOL  ")]
    [InlineData("fotosíntesis", "Fotosintesis.")]
    [InlineData("Revolución de 1944", "revolucion   de 1944")]
    [InlineData("pingüino", "pinguino")]
    [InlineData("¿Guatemala?", "guatemala")]
    public void RespuestaDirecta_IgnoraMayusculasTildesEspaciosYPuntuacion(string aceptada, string respuesta)
    {
        Assert.True(ComparadorRespuestas.SonEquivalentes(aceptada, respuesta));
    }

    [Theory]
    [InlineData("año", "ano")]
    [InlineData("árbol", "árboles")]
    [InlineData("célula", "")]
    public void RespuestaDirecta_ConservaDiferenciasReales(string aceptada, string respuesta)
    {
        Assert.False(ComparadorRespuestas.SonEquivalentes(aceptada, respuesta));
    }

    [Fact]
    public void RespuestaDirecta_EnieCompuestaEsIgualAEnieNormal()
    {
        // "n" + tilde combinable (U+0303), como la escriben algunos teclados.
        Assert.True(ComparadorRespuestas.SonEquivalentes("Año", "An\u0303o"));
    }

    // -----------------------------------------------------------------
    // Configuración
    // -----------------------------------------------------------------

    [Fact]
    public async Task Configuracion_TiempoPorPreguntaDesactivaNavegacion()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, x => { }, estadoTarea: EstadoTarea.Borrador, sinCuestionario: true);
        var servicio = new CuestionarioServicio(contexto, new AccesoPrueba(alumnoId: null));

        var resultado = await servicio.GuardarConfiguracionAsync(new ConfigurarCuestionario
        {
            TareaId = 1,
            ModalidadTiempo = ModalidadTiempoCuestionario.TiempoPorPregunta,
            TiempoPorPreguntaSegundos = 30,
            PermitirNavegacion = true,
            OrdenAleatorio = true
        });

        Assert.True(resultado.Exitoso);
        var cuestionario = await contexto.Cuestionarios.SingleAsync(TestContext.Current.CancellationToken);
        Assert.False(cuestionario.PermitirNavegacion);
        Assert.True(cuestionario.OrdenAleatorio);
        Assert.Equal(30, cuestionario.TiempoPorPreguntaSegundos);
        Assert.Null(cuestionario.TiempoGeneralSegundos);
    }

    [Fact]
    public async Task Configuracion_TiempoGeneralExigeMinutos()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, x => { }, estadoTarea: EstadoTarea.Borrador, sinCuestionario: true);
        var servicio = new CuestionarioServicio(contexto, new AccesoPrueba(alumnoId: null));

        var resultado = await servicio.GuardarConfiguracionAsync(new ConfigurarCuestionario
        {
            TareaId = 1,
            ModalidadTiempo = ModalidadTiempoCuestionario.TiempoGeneral
        });

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(ConfigurarCuestionario.TiempoGeneralMinutos), resultado.Errores.Keys);
    }

    // -----------------------------------------------------------------
    // Modo navegable
    // -----------------------------------------------------------------

    [Fact]
    public async Task Navegable_GuardaCorrigeYCalificaConRespuestaDirectaSinTildes()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, c => c.PermitirNavegacion = true);
        var servicio = CrearServicioAlumno(contexto);

        var inicio = await servicio.IniciarAsync(CuestionarioId);
        Assert.True(inicio.Exitoso);
        var intentoId = inicio.Datos;

        // Responde mal la primera y luego regresa a corregirla.
        Assert.True((await servicio.GuardarRespuestaAsync(intentoId, Unica(OpcionIncorrectaUnica))).Exitoso);
        Assert.True((await servicio.GuardarRespuestaAsync(intentoId, Directa("ARBOL"))).Exitoso);
        Assert.True((await servicio.GuardarRespuestaAsync(intentoId, Unica(OpcionCorrectaUnica))).Exitoso);

        var final = await servicio.FinalizarAsync(new FinalizarIntento
        {
            IntentoId = intentoId,
            Respuestas = [Multiple(OpcionMultipleX, OpcionMultipleY)]
        });

        Assert.True(final.Exitoso);
        var intento = await contexto.IntentosCuestionarios.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(EstadoIntentoCuestionario.Finalizado, intento.Estado);
        Assert.Equal(100m, intento.Calificacion);

        var entrega = await contexto.Entregas.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(EstadoEntrega.Calificada, entrega.Estado);
        Assert.Equal(10m, entrega.Calificacion);

        // Solo queda la opción corregida en la primera pregunta.
        var opciones = await contexto.RespuestasAlumnosOpciones
            .Where(x => x.PreguntaId == PreguntaUnica)
            .Select(x => x.OpcionPreguntaId)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal([OpcionCorrectaUnica], opciones);
    }

    [Fact]
    public async Task Navegable_SeleccionMultipleIncompletaEsIncorrecta()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, c => c.PermitirNavegacion = true);
        var servicio = CrearServicioAlumno(contexto);
        var intentoId = (await servicio.IniciarAsync(CuestionarioId)).Datos;

        await servicio.FinalizarAsync(new FinalizarIntento
        {
            IntentoId = intentoId,
            Respuestas =
            [
                Unica(OpcionCorrectaUnica),
                Directa("árbol"),
                Multiple(OpcionMultipleX)
            ]
        });

        var intento = await contexto.IntentosCuestionarios.SingleAsync(TestContext.Current.CancellationToken);
        // 1 + 1 de 3 puntos posibles
        Assert.Equal(66.67m, intento.Calificacion);
    }

    [Fact]
    public async Task Navegable_TiempoGeneralVencidoIgnoraRespuestasTardias()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, c =>
        {
            c.PermitirNavegacion = true;
            c.ModalidadTiempo = ModalidadTiempoCuestionario.TiempoGeneral;
            c.TiempoGeneralSegundos = 300;
        });
        var servicio = CrearServicioAlumno(contexto);
        var intentoId = (await servicio.IniciarAsync(CuestionarioId)).Datos;

        await servicio.GuardarRespuestaAsync(intentoId, Unica(OpcionCorrectaUnica));

        // El tiempo terminó hace un minuto.
        var intento = await contexto.IntentosCuestionarios.SingleAsync(TestContext.Current.CancellationToken);
        intento.FechaLimite = DateTime.UtcNow.AddMinutes(-1);
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        contexto.ChangeTracker.Clear();

        var final = await servicio.FinalizarAsync(new FinalizarIntento
        {
            IntentoId = intentoId,
            Respuestas = [Directa("árbol"), Multiple(OpcionMultipleX, OpcionMultipleY)]
        });

        Assert.True(final.Exitoso);
        intento = await contexto.IntentosCuestionarios.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(EstadoIntentoCuestionario.Expirado, intento.Estado);
        // Solo cuenta la respuesta guardada antes del vencimiento: 1 de 3.
        Assert.Equal(33.33m, intento.Calificacion);
    }

    // -----------------------------------------------------------------
    // Modo secuencial
    // -----------------------------------------------------------------

    [Fact]
    public async Task Secuencial_NoPermiteRegresarAPreguntaAnterior()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, c => c.PermitirNavegacion = false);
        var servicio = CrearServicioAlumno(contexto);
        var intentoId = (await servicio.IniciarAsync(CuestionarioId)).Datos;

        var vista = await servicio.ObtenerIntentoAsync(intentoId);
        Assert.Equal(PreguntaUnica, Assert.Single(vista.Modelo!.Preguntas).PreguntaId);

        // Responde mal la primera.
        var paso1 = await servicio.ResponderAsync(new ResponderPregunta
        {
            IntentoId = intentoId,
            Respuesta = Unica(OpcionIncorrectaUnica)
        });
        Assert.False(paso1.Datos);

        // Intenta volver a responder la primera: se ignora.
        await servicio.ResponderAsync(new ResponderPregunta
        {
            IntentoId = intentoId,
            Respuesta = Unica(OpcionCorrectaUnica)
        });

        // El modo secuencial tampoco acepta el guardado del modo navegable.
        Assert.False((await servicio.GuardarRespuestaAsync(intentoId, Unica(OpcionCorrectaUnica))).Exitoso);

        vista = await servicio.ObtenerIntentoAsync(intentoId);
        Assert.Equal(PreguntaDirecta, Assert.Single(vista.Modelo!.Preguntas).PreguntaId);

        await servicio.ResponderAsync(new ResponderPregunta { IntentoId = intentoId, Respuesta = Directa("Árbol.") });
        var ultimo = await servicio.ResponderAsync(new ResponderPregunta
        {
            IntentoId = intentoId,
            Respuesta = Multiple(OpcionMultipleX, OpcionMultipleY)
        });

        Assert.True(ultimo.Datos);
        var intento = await contexto.IntentosCuestionarios.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(EstadoIntentoCuestionario.Finalizado, intento.Estado);
        Assert.Equal(66.67m, intento.Calificacion);
    }

    [Fact]
    public async Task Secuencial_RespuestaFueraDelTiempoDeLaPreguntaNoSeRegistra()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, c =>
        {
            c.ModalidadTiempo = ModalidadTiempoCuestionario.TiempoPorPregunta;
            c.TiempoPorPreguntaSegundos = 20;
        });
        var servicio = CrearServicioAlumno(contexto);
        var intentoId = (await servicio.IniciarAsync(CuestionarioId)).Datos;

        var vista = await servicio.ObtenerIntentoAsync(intentoId);
        Assert.InRange(vista.Modelo!.SegundosRestantesPregunta!.Value, 19, 20);

        // La pregunta se presentó hace dos minutos.
        var registro = await contexto.RespuestasAlumnos.SingleAsync(TestContext.Current.CancellationToken);
        registro.FechaPresentacion = DateTime.UtcNow.AddMinutes(-2);
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        contexto.ChangeTracker.Clear();

        var paso = await servicio.ResponderAsync(new ResponderPregunta
        {
            IntentoId = intentoId,
            Respuesta = Unica(OpcionCorrectaUnica)
        });

        Assert.Contains("no se registró", paso.Mensaje);
        Assert.False(await contexto.RespuestasAlumnosOpciones.AnyAsync(TestContext.Current.CancellationToken));
    }

    // -----------------------------------------------------------------
    // Intentos, orden y cierre automático
    // -----------------------------------------------------------------

    [Fact]
    public async Task Inicio_ReanudaIntentoEnCursoYRespetaMaximo()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, c =>
        {
            c.PermitirNavegacion = true;
            c.MaximoIntentos = 1;
        });
        var servicio = CrearServicioAlumno(contexto);

        var primero = await servicio.IniciarAsync(CuestionarioId);
        var segundo = await servicio.IniciarAsync(CuestionarioId);
        Assert.Equal(primero.Datos, segundo.Datos);

        await servicio.FinalizarAsync(new FinalizarIntento { IntentoId = primero.Datos });

        var tercero = await servicio.IniciarAsync(CuestionarioId);
        Assert.False(tercero.Exitoso);

        var inicio = await servicio.ObtenerInicioAsync(CuestionarioId);
        Assert.NotNull(inicio);
        Assert.False(inicio!.PuedeIniciar);
        Assert.Equal(primero.Datos, inicio.UltimoIntentoId);
    }

    [Fact]
    public async Task OrdenAleatorio_ConservaElOrdenDelIntento()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, c =>
        {
            c.PermitirNavegacion = true;
            c.OrdenAleatorio = true;
        });
        var servicio = CrearServicioAlumno(contexto);
        var intentoId = (await servicio.IniciarAsync(CuestionarioId)).Datos;

        var primera = (await servicio.ObtenerIntentoAsync(intentoId)).Modelo!.Preguntas.Select(x => x.PreguntaId).ToList();
        var segunda = (await servicio.ObtenerIntentoAsync(intentoId)).Modelo!.Preguntas.Select(x => x.PreguntaId).ToList();

        Assert.Equal(primera, segunda);
        Assert.Equal([PreguntaUnica, PreguntaDirecta, PreguntaMultiple], primera.Order().ToList());
    }

    [Fact]
    public async Task FinalizadorVencidos_CalificaIntentosAbandonados()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, c =>
        {
            c.PermitirNavegacion = true;
            c.ModalidadTiempo = ModalidadTiempoCuestionario.TiempoGeneral;
            c.TiempoGeneralSegundos = 60;
        });
        var servicio = CrearServicioAlumno(contexto);
        var intentoId = (await servicio.IniciarAsync(CuestionarioId)).Datos;
        await servicio.GuardarRespuestaAsync(intentoId, Directa("arbol"));
        contexto.ChangeTracker.Clear();

        // El servicio en segundo plano no tiene usuario en sesión.
        var segundoPlano = new CuestionarioServicio(contexto, new AccesoPrueba(alumnoId: null));
        Assert.Equal(0, await segundoPlano.FinalizarVencidosAsync(DateTime.UtcNow, TestContext.Current.CancellationToken));

        var cantidad = await segundoPlano.FinalizarVencidosAsync(
            DateTime.UtcNow.AddMinutes(5),
            TestContext.Current.CancellationToken);

        Assert.Equal(1, cantidad);
        var intento = await contexto.IntentosCuestionarios.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(EstadoIntentoCuestionario.Expirado, intento.Estado);
        Assert.Equal(33.33m, intento.Calificacion);
        Assert.Equal(3, await contexto.RespuestasAlumnos.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Resultado_MuestraRetroalimentacionSoloSiEstaHabilitada()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto, c =>
        {
            c.PermitirNavegacion = true;
            c.MostrarRespuestasCorrectas = true;
        });
        var servicio = CrearServicioAlumno(contexto);
        var intentoId = (await servicio.IniciarAsync(CuestionarioId)).Datos;
        await servicio.FinalizarAsync(new FinalizarIntento
        {
            IntentoId = intentoId,
            Respuestas = [Unica(OpcionIncorrectaUnica)]
        });

        var resultado = await servicio.ObtenerResultadoAsync(intentoId);

        Assert.NotNull(resultado);
        Assert.Equal(3, resultado!.Detalle.Count);
        var primera = resultado.Detalle.Single(x => x.Enunciado == "¿Cuál es correcta?");
        Assert.False(primera.Correcta);
        Assert.Equal("A", primera.RespuestaCorrecta);
        Assert.Equal("B", primera.RespuestaAlumno);
        Assert.Equal("Porque A es la definición.", primera.Retroalimentacion);
    }

    // -----------------------------------------------------------------
    // Escenario
    // -----------------------------------------------------------------

    internal static RespuestaCuestionario Unica(int opcionId) =>
        new() { PreguntaId = PreguntaUnica, OpcionId = opcionId };

    internal static RespuestaCuestionario Directa(string texto) =>
        new() { PreguntaId = PreguntaDirecta, Texto = texto };

    internal static RespuestaCuestionario Multiple(params int[] opciones) =>
        new() { PreguntaId = PreguntaMultiple, OpcionIds = [.. opciones] };

    private static Contexto CrearContexto() => new(
        new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    internal static CuestionarioServicio CrearServicioAlumno(Contexto contexto) =>
        new(contexto, new AccesoPrueba(alumnoId: 1));

    internal static async Task SembrarAsync(
        Contexto contexto,
        Action<Cuestionario> configurar,
        EstadoTarea estadoTarea = EstadoTarea.Publicada,
        bool sinCuestionario = false)
    {
        var usuarioDocente = Usuario("docente");
        var usuarioAlumno = Usuario("alumno");
        var ciclo = new CicloEscolar
        {
            Id = 1,
            Anio = 2026,
            FechaInicio = new DateOnly(2026, 1, 1),
            FechaFin = new DateOnly(2026, 12, 1),
            Activo = true,
            Estado = EstadoCicloEscolar.Activo
        };
        var grado = new Grado
        {
            Id = 1,
            Nivel = NivelEducativo.Diversificado,
            Nombre = "Cuarto",
            Carrera = "Bachillerato",
            Orden = 1
        };
        var seccion = new Seccion
        {
            Id = 1,
            Nombre = "A",
            CicloEscolar = ciclo,
            Grado = grado,
            Estado = EstadoRegistro.Activo
        };
        var curso = new Curso { Id = 1, Nombre = "Química", Grado = grado };
        var docente = new Docente { Id = 1, Usuario = usuarioDocente, Carnet = "D-1" };
        var alumno = new Alumno { Id = 1, Usuario = usuarioAlumno, CodigoPersonal = "A-1" };
        var asignacion = new Asignacion
        {
            Id = 1,
            Seccion = seccion,
            Curso = curso,
            GradoId = grado.Id,
            Docente = docente,
            Estado = EstadoRegistro.Activo
        };
        var inscripcion = new Inscripcion
        {
            Id = 1,
            Alumno = alumno,
            Seccion = seccion,
            CicloEscolarId = ciclo.Id,
            Fecha = new DateOnly(2026, 1, 10),
            Estado = EstadoInscripcion.Activa
        };
        var tarea = new Tarea
        {
            Id = 1,
            Asignacion = asignacion,
            Tipo = TipoTarea.Cuestionario,
            Titulo = "Cuestionario de prueba",
            Estado = estadoTarea,
            PunteoMaximo = 10,
            FechaCreacion = DateTime.UtcNow,
            FechaLimite = DateTime.UtcNow.AddDays(7),
            CreadoPorUsuario = usuarioDocente,
            GrupoVersionId = Guid.NewGuid()
        };

        contexto.AddRange(
            usuarioDocente, usuarioAlumno, ciclo, grado, seccion, curso,
            docente, alumno, asignacion, inscripcion, tarea);

        if (!sinCuestionario)
        {
            var cuestionario = new Cuestionario
            {
                Id = CuestionarioId,
                Tarea = tarea,
                MaximoIntentos = 0,
                MostrarResultadosAlFinalizar = true
            };
            configurar(cuestionario);

            cuestionario.Preguntas.Add(new Pregunta
            {
                Id = PreguntaUnica,
                Orden = 1,
                Tipo = TipoPregunta.SeleccionUnica,
                Enunciado = "¿Cuál es correcta?",
                Punteo = 1,
                Retroalimentacion = "Porque A es la definición.",
                Opciones =
                [
                    new OpcionPregunta { Id = OpcionCorrectaUnica, Orden = 1, Texto = "A", EsCorrecta = true },
                    new OpcionPregunta { Id = OpcionIncorrectaUnica, Orden = 2, Texto = "B" }
                ]
            });
            cuestionario.Preguntas.Add(new Pregunta
            {
                Id = PreguntaDirecta,
                Orden = 2,
                Tipo = TipoPregunta.TextoCorto,
                Enunciado = "Escriba la palabra",
                Punteo = 1,
                RespuestasAceptadas = [new RespuestaAceptada { Texto = "Árbol" }]
            });
            cuestionario.Preguntas.Add(new Pregunta
            {
                Id = PreguntaMultiple,
                Orden = 3,
                Tipo = TipoPregunta.SeleccionMultiple,
                Enunciado = "Marque las correctas",
                Punteo = 1,
                Opciones =
                [
                    new OpcionPregunta { Id = OpcionMultipleX, Orden = 1, Texto = "X", EsCorrecta = true },
                    new OpcionPregunta { Id = OpcionMultipleY, Orden = 2, Texto = "Y", EsCorrecta = true },
                    new OpcionPregunta { Id = OpcionMultipleZ, Orden = 3, Texto = "Z" }
                ]
            });

            contexto.Add(cuestionario);
            contexto.Add(new Entrega
            {
                Tarea = tarea,
                Alumno = alumno,
                Inscripcion = inscripcion,
                Estado = EstadoEntrega.Pendiente
            });
        }

        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        contexto.ChangeTracker.Clear();
    }

    internal static Usuario Usuario(string id) => new()
    {
        Id = id,
        UserName = id,
        NormalizedUserName = id.ToUpperInvariant(),
        PrimerNombre = id,
        PrimerApellido = "Prueba",
        Activo = true
    };

    internal sealed class AccesoPrueba(int? alumnoId) : IAccesoAcademicoServicio
    {
        public string? UsuarioId => alumnoId.HasValue ? "alumno" : null;
        public bool EsOperadorInstitucional => false;
        public bool TienePermiso(string permiso) => true;
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() =>
            Task.FromResult(new ContextoAcademicoUsuario(null, alumnoId));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult<int?>(null);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult(alumnoId);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) => Task.FromResult(alumnoId.HasValue);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(true);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) => Task.FromResult(true);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(true);
    }
}
