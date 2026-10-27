using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionCuestionarios;
using Proyecto_Final.Servicios.GestionTareas;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Tareas;

namespace Proyecto_Final.Tests;

public sealed class TareaServicioTests
{
    [Fact]
    public async Task Cerrar_ConservarPendientes_NoRegistraCerosAdministrativos()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var creada = await servicio.CrearAsync(Modelo(TipoTarea.Archivo),
            TestContext.Current.CancellationToken);
        Assert.True((await servicio.PublicarAsync(creada.Datos)).Exitoso);

        var previo = await servicio.ObtenerParaCerrarAsync(creada.Datos);
        var resultado = await servicio.CerrarAsync(new CerrarTarea { Id = creada.Datos });

        Assert.NotNull(previo);
        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(EstadoTarea.Cerrada, (await contexto.Tareas.FindAsync(
            [creada.Datos], TestContext.Current.CancellationToken))!.Estado);
        Assert.All(await contexto.Entregas.Where(x => x.TareaId == creada.Datos)
            .ToListAsync(TestContext.Current.CancellationToken),
            entrega => Assert.Null(entrega.Calificacion));
    }

    [Fact]
    public async Task Cerrar_CalificarSinActividad_RegistraCerosAutomaticosYAuditoria()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var creada = await servicio.CrearAsync(Modelo(TipoTarea.Archivo),
            TestContext.Current.CancellationToken);
        Assert.True((await servicio.PublicarAsync(creada.Datos)).Exitoso);

        var resultado = await servicio.CerrarAsync(new CerrarTarea
        {
            Id = creada.Datos, CalificarSinActividad = true
        });

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var entregas = await contexto.Entregas.Where(x => x.TareaId == creada.Datos)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.NotEmpty(entregas);
        Assert.All(entregas, entrega =>
        {
            Assert.Equal(0, entrega.Calificacion);
            Assert.Null(entrega.FechaEntrega);
            Assert.Equal(OrigenCalificacionAdministrativa.AutomaticaAlCerrar,
                entrega.OrigenCalificacionAdministrativa);
        });
        Assert.Equal(entregas.Count, await contexto.HistorialesCalificaciones.CountAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CrearCuestionario_CreaBorradorYConfiguracionEnElMismoFlujo()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var archivos = new ArchivosPrueba();
        var acceso = new AccesoPrueba();
        var servicio = new TareaServicio(contexto, archivos, acceso);

        var resultado = await servicio.CrearAsync(
            Modelo(TipoTarea.Cuestionario),
            TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.True(resultado.Datos > 0);
        var tarea = await contexto.Tareas
            .Include(x => x.Cuestionario)
            .Include(x => x.Archivos)
            .SingleAsync(x => x.Id == resultado.Datos, TestContext.Current.CancellationToken);
        Assert.Equal(EstadoTarea.Borrador, tarea.Estado);
        Assert.NotNull(tarea.Cuestionario);
        Assert.Empty(tarea.Archivos);
        Assert.Equal(0, archivos.Guardados);

        var configuracion = await new CuestionarioServicio(contexto, acceso)
            .ObtenerConfiguracionAsync(tarea.Id);
        Assert.NotNull(configuracion);
        Assert.Equal(tarea.Id, configuracion!.TareaId);
    }

    [Fact]
    public async Task CrearCuestionario_MismoToken_NoDuplicaTareaNiCuestionario()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var modelo = Modelo(TipoTarea.Cuestionario);

        var primero = await servicio.CrearAsync(modelo, TestContext.Current.CancellationToken);
        var segundo = await servicio.CrearAsync(modelo, TestContext.Current.CancellationToken);

        Assert.True(primero.Exitoso, primero.Mensaje);
        Assert.True(segundo.Exitoso, segundo.Mensaje);
        Assert.Equal(primero.Datos, segundo.Datos);
        Assert.Equal(1, await contexto.Tareas.CountAsync(
            x => x.GrupoVersionId == modelo.TokenCreacion,
            TestContext.Current.CancellationToken));
        Assert.Equal(1, await contexto.Cuestionarios.CountAsync(
            x => x.TareaId == primero.Datos,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CrearCuestionario_ConAdjunto_RechazaSinProcesarArchivo()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var archivos = new ArchivosPrueba();
        var servicio = new TareaServicio(contexto, archivos, new AccesoPrueba());
        var modelo = Modelo(TipoTarea.Cuestionario);
        modelo.Archivo = ArchivoFormulario();

        var resultado = await servicio.CrearAsync(modelo, TestContext.Current.CancellationToken);

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(modelo.Archivo), resultado.Errores.Keys);
        Assert.Equal(0, archivos.Guardados);
        Assert.False(await contexto.Tareas.AnyAsync(
            x => x.GrupoVersionId == modelo.TokenCreacion,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CrearEntregaArchivo_ConservaAdjuntoYBorrador()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var archivos = new ArchivosPrueba();
        var servicio = new TareaServicio(contexto, archivos, new AccesoPrueba());
        var modelo = Modelo(TipoTarea.Archivo);
        modelo.Archivo = ArchivoFormulario();

        var resultado = await servicio.CrearAsync(modelo, TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(1, archivos.Guardados);
        var tarea = await contexto.Tareas
            .Include(x => x.Archivos)
            .SingleAsync(x => x.Id == resultado.Datos, TestContext.Current.CancellationToken);
        Assert.Equal(TipoTarea.Archivo, tarea.Tipo);
        Assert.Equal(EstadoTarea.Borrador, tarea.Estado);
        Assert.Single(tarea.Archivos);
        Assert.Null(await contexto.Cuestionarios.SingleOrDefaultAsync(
            x => x.TareaId == tarea.Id,
            TestContext.Current.CancellationToken));
        Assert.Equal(CriterioCalificacionIntentos.MejorNota,
            tarea.CriterioCalificacionEntrega);
    }

    [Fact]
    public async Task CrearEntregaArchivo_ConVariosIntentos_PersisteCriterioSeleccionado()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var modelo = Modelo(TipoTarea.Archivo);
        modelo.MaximoIntentosEntrega = 3;
        modelo.CriterioCalificacionEntrega = CriterioCalificacionIntentos.Promedio;

        var resultado = await servicio.CrearAsync(modelo, TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var tarea = await contexto.Tareas.SingleAsync(x => x.Id == resultado.Datos,
            TestContext.Current.CancellationToken);
        Assert.Equal(3, tarea.MaximoIntentosEntrega);
        Assert.Equal(CriterioCalificacionIntentos.Promedio,
            tarea.CriterioCalificacionEntrega);
    }

    [Fact]
    public async Task PublicarCuestionarioIncompleto_PermaneceEnBorrador()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var creado = await servicio.CrearAsync(
            Modelo(TipoTarea.Cuestionario),
            TestContext.Current.CancellationToken);

        var resultado = await servicio.PublicarAsync(creado.Datos);

        Assert.False(resultado.Exitoso);
        Assert.Contains("configuración", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            EstadoTarea.Borrador,
            (await contexto.Tareas.FindAsync([creado.Datos], TestContext.Current.CancellationToken))!.Estado);
    }

    [Fact]
    public async Task PublicarCuestionarioConfigurado_PermitePublicacion()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var creado = await servicio.CrearAsync(
            Modelo(TipoTarea.Cuestionario),
            TestContext.Current.CancellationToken);
        var cuestionario = await contexto.Cuestionarios.SingleAsync(
            x => x.TareaId == creado.Datos,
            TestContext.Current.CancellationToken);
        cuestionario.Preguntas.Add(new Pregunta
        {
            Orden = 1,
            Tipo = TipoPregunta.SeleccionUnica,
            Enunciado = "Pregunta válida",
            Punteo = 10,
            Opciones =
            [
                new OpcionPregunta { Orden = 1, Texto = "Correcta", EsCorrecta = true },
                new OpcionPregunta { Orden = 2, Texto = "Incorrecta" }
            ]
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var resultado = await servicio.PublicarAsync(creado.Datos);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(
            EstadoTarea.Publicada,
            (await contexto.Tareas.FindAsync([creado.Datos], TestContext.Current.CancellationToken))!.Estado);
    }

    [Fact]
    public async Task Crear_AsignacionNoAutorizada_ConservaValidacionExistente()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba(false));
        var modelo = Modelo(TipoTarea.Archivo);

        var resultado = await servicio.CrearAsync(modelo, TestContext.Current.CancellationToken);

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(modelo.AsignacionId), resultado.Errores.Keys);
    }

    [Fact]
    public async Task Reabrir_AlcanceManipulado_RechazaSinRegistrarAutorizacion()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { }, sinCuestionario: true);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Tipo = TipoTarea.Archivo;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());

        var resultado = await servicio.ReabrirAsync(new ReabrirTarea
        {
            Id = tarea.Id,
            TokenSolicitud = Guid.NewGuid(),
            Alcance = (AlcanceReaperturaTarea)999,
            Motivo = "Motivo válido de prueba.",
            FechaLimite = DateTime.Now.AddDays(1)
        });

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(ReabrirTarea.Alcance), resultado.Errores.Keys);
        Assert.Empty(await contexto.ReaperturasTareas.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Reabrir_MotivoSoloEspacios_RechazaSinRegistrarAutorizacion()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { }, sinCuestionario: true);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Tipo = TipoTarea.Archivo;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());

        var resultado = await servicio.ReabrirAsync(new ReabrirTarea
        {
            Id = tarea.Id,
            TokenSolicitud = Guid.NewGuid(),
            Motivo = "   \t   ",
            FechaLimite = DateTime.Now.AddDays(1)
        });

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(ReabrirTarea.Motivo), resultado.Errores.Keys);
        Assert.Empty(await contexto.ReaperturasTareas.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Reabrir_SoloPendientes_ExtiendeSoloLaEntregaPendiente()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { }, sinCuestionario: true);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Tipo = TipoTarea.Archivo;
        var alumno = await contexto.Alumnos.SingleAsync(TestContext.Current.CancellationToken);
        var inscripcion = await contexto.Inscripciones.SingleAsync(TestContext.Current.CancellationToken);
        contexto.Entregas.AddRange(
            new Entrega { Tarea = tarea, Alumno = alumno, Inscripcion = inscripcion, NumeroEnvio = 1, Estado = EstadoEntrega.Enviada, FechaEntrega = DateTime.UtcNow },
            new Entrega { Tarea = tarea, Alumno = alumno, Inscripcion = inscripcion, NumeroEnvio = 2, Estado = EstadoEntrega.Pendiente });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var limite = DateTime.Now.AddDays(1);

        var resultado = await servicio.ReabrirAsync(new ReabrirTarea
        {
            Id = tarea.Id, TokenSolicitud = Guid.NewGuid(), FechaLimite = limite,
            Motivo = "Extensión para pendientes.", Alcance = AlcanceReaperturaTarea.SoloPendientes
        });

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var entregas = await contexto.Entregas.OrderBy(x => x.NumeroEnvio).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, entregas.Count);
        Assert.Null(entregas[0].FechaLimiteIndividual);
        Assert.NotNull(entregas[1].FechaLimiteIndividual);
        var beneficiado = await contexto.ReaperturasTareasAlumnos.SingleAsync(TestContext.Current.CancellationToken);
        Assert.False(beneficiado.OportunidadAdicional);
    }

    [Fact]
    public async Task Reabrir_Todos_CreaOportunidadAdicionalSinAlterarEntregaHistorica()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { }, sinCuestionario: true);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Tipo = TipoTarea.Archivo;
        var alumno = await contexto.Alumnos.SingleAsync(TestContext.Current.CancellationToken);
        var inscripcion = await contexto.Inscripciones.SingleAsync(TestContext.Current.CancellationToken);
        contexto.Entregas.Add(new Entrega
        {
            Tarea = tarea, Alumno = alumno, Inscripcion = inscripcion,
            NumeroEnvio = 1, Estado = EstadoEntrega.Calificada,
            FechaEntrega = DateTime.UtcNow.AddDays(-1), Calificacion = 9
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());

        var resultado = await servicio.ReabrirAsync(new ReabrirTarea
        {
            Id = tarea.Id, TokenSolicitud = Guid.NewGuid(), FechaLimite = DateTime.Now.AddDays(1),
            Motivo = "Nueva oportunidad general.", Alcance = AlcanceReaperturaTarea.Todos
        });

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var entregas = await contexto.Entregas.OrderBy(x => x.NumeroEnvio).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, entregas.Count);
        Assert.Equal(EstadoEntrega.Calificada, entregas[0].Estado);
        Assert.Equal(9, entregas[0].Calificacion);
        Assert.Equal(EstadoEntrega.Pendiente, entregas[1].Estado);
        Assert.True((await contexto.ReaperturasEntregas.SingleAsync(TestContext.Current.CancellationToken)).AutorizaOportunidadAdicional);
    }

    [Fact]
    public async Task Historial_TareaCerradaConResponsableVigente_PermiteAbrirReapertura()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { }, sinCuestionario: true);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Tipo = TipoTarea.Archivo;
        tarea.Estado = EstadoTarea.Cerrada;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());

        var fila = Assert.Single(await servicio.ObtenerHistorialAsync());

        Assert.True(fila.PuedeReabrirse);
        Assert.NotNull(await servicio.ObtenerParaReabrirAsync(tarea.Id));
    }

    [Fact]
    public async Task Historial_TareaCerradaSinGestionVigente_OcultaReapertura()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { }, sinCuestionario: true);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Tipo = TipoTarea.Archivo;
        tarea.Estado = EstadoTarea.Cerrada;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba(false));

        var fila = Assert.Single(await servicio.ObtenerHistorialAsync());

        Assert.False(fila.PuedeReabrirse);
        Assert.Null(await servicio.ObtenerParaReabrirAsync(tarea.Id));
    }

    [Fact]
    public async Task Reabrir_TareaCerrada_LaDevuelveAPublicadaSinEliminarHistorial()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { }, sinCuestionario: true);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Tipo = TipoTarea.Archivo;
        tarea.Estado = EstadoTarea.Cerrada;
        var alumno = await contexto.Alumnos.SingleAsync(TestContext.Current.CancellationToken);
        var inscripcion = await contexto.Inscripciones.SingleAsync(TestContext.Current.CancellationToken);
        contexto.Entregas.Add(new Entrega
        {
            Tarea = tarea, Alumno = alumno, Inscripcion = inscripcion,
            NumeroEnvio = 1, Estado = EstadoEntrega.Calificada,
            FechaEntrega = DateTime.UtcNow.AddDays(-1), Calificacion = 8
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());

        var resultado = await servicio.ReabrirAsync(new ReabrirTarea
        {
            Id = tarea.Id, TokenSolicitud = Guid.NewGuid(), FechaLimite = DateTime.Now.AddDays(1),
            Motivo = "Reapertura de actividad cerrada.", Alcance = AlcanceReaperturaTarea.Todos
        });

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var entregas = await contexto.Entregas.OrderBy(x => x.NumeroEnvio)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(EstadoTarea.Publicada,
            (await contexto.Tareas.FindAsync([tarea.Id], TestContext.Current.CancellationToken))!.Estado);
        Assert.Equal(2, entregas.Count);
        Assert.Equal(EstadoEntrega.Calificada, entregas[0].Estado);
        Assert.Equal(8, entregas[0].Calificacion);
        Assert.Single(await contexto.ReaperturasTareas.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Editar_PublicadaSinRegistros_ConservaEstadoPublicado()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { }, sinCuestionario: true);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Tipo = TipoTarea.Archivo;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var modelo = await servicio.ObtenerEditarAsync(tarea.Id);
        Assert.NotNull(modelo);
        modelo!.Titulo = "Título corregido";
        modelo.Instrucciones = "Instrucciones corregidas";

        var resultado = await servicio.EditarAsync(modelo, TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var actual = await contexto.Tareas.FindAsync([tarea.Id], TestContext.Current.CancellationToken);
        Assert.Equal(EstadoTarea.Publicada, actual!.Estado);
        Assert.Equal("Título corregido", actual.Titulo);
    }

    [Fact]
    public async Task Editar_PublicadaConEntrega_PermiteContenidoSinAlterarEntrega()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { }, sinCuestionario: true);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Tipo = TipoTarea.Archivo;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var alumno = await contexto.Alumnos.SingleAsync(TestContext.Current.CancellationToken);
        var inscripcion = await contexto.Inscripciones.SingleAsync(TestContext.Current.CancellationToken);
        contexto.Entregas.Add(new Entrega
        {
            Tarea = tarea, Alumno = alumno, Inscripcion = inscripcion,
            NumeroEnvio = 1, Estado = EstadoEntrega.Calificada,
            FechaEntrega = DateTime.UtcNow, Calificacion = 9
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var modelo = await servicio.ObtenerEditarAsync(tarea.Id);
        Assert.NotNull(modelo);
        modelo!.Instrucciones = "Aclaración posterior";

        var resultado = await servicio.EditarAsync(modelo, TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var entrega = await contexto.Entregas.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(EstadoEntrega.Calificada, entrega.Estado);
        Assert.Equal(9, entrega.Calificacion);
        Assert.Equal("Aclaración posterior", (await contexto.Tareas.FindAsync([tarea.Id], TestContext.Current.CancellationToken))!.Instrucciones);
    }

    [Fact]
    public async Task Editar_PublicadaConEntrega_BloqueaCambioEstructural()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { }, sinCuestionario: true);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Tipo = TipoTarea.Archivo;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var alumno = await contexto.Alumnos.SingleAsync(TestContext.Current.CancellationToken);
        var inscripcion = await contexto.Inscripciones.SingleAsync(TestContext.Current.CancellationToken);
        contexto.Entregas.Add(new Entrega
        {
            Tarea = tarea, Alumno = alumno, Inscripcion = inscripcion,
            NumeroEnvio = 1, Estado = EstadoEntrega.Enviada, FechaEntrega = DateTime.UtcNow
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var modelo = await servicio.ObtenerEditarAsync(tarea.Id);
        Assert.NotNull(modelo);
        modelo!.PunteoMaximo += 1;

        var resultado = await servicio.EditarAsync(modelo, TestContext.Current.CancellationToken);

        Assert.False(resultado.Exitoso);
        Assert.Equal(tarea.PunteoMaximo,
            (await contexto.Tareas.FindAsync([tarea.Id], TestContext.Current.CancellationToken))!.PunteoMaximo);
    }

    [Theory]
    [InlineData(TipoTarea.Archivo)]
    [InlineData(TipoTarea.Cuestionario)]
    public async Task Editar_TipoInmutable_RechazaPostManipulado(TipoTarea tipoOriginal)
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var tarea = new Tarea
        {
            AsignacionId = 1,
            Tipo = tipoOriginal,
            Titulo = "Actividad existente",
            Estado = EstadoTarea.Borrador,
            PunteoMaximo = 10,
            FechaCreacion = DateTime.UtcNow,
            CreadoPorUsuarioId = "docente",
            GrupoVersionId = Guid.NewGuid()
        };
        contexto.Tareas.Add(tarea);
        if (tipoOriginal == TipoTarea.Cuestionario)
            contexto.Cuestionarios.Add(new Cuestionario { Tarea = tarea });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var modelo = await servicio.ObtenerEditarAsync(tarea.Id);
        Assert.NotNull(modelo);
        modelo!.Tipo = tipoOriginal == TipoTarea.Archivo
            ? TipoTarea.Cuestionario
            : TipoTarea.Archivo;

        var resultado = await servicio.EditarAsync(modelo, TestContext.Current.CancellationToken);

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(modelo.Tipo), resultado.Errores.Keys);
        var persistida = await contexto.Tareas.FindAsync([tarea.Id], TestContext.Current.CancellationToken);
        Assert.Equal(tipoOriginal, persistida!.Tipo);
        Assert.Equal(tipoOriginal == TipoTarea.Cuestionario, await contexto.Cuestionarios.AnyAsync(
            x => x.TareaId == tarea.Id, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(TipoTarea.Archivo)]
    [InlineData(TipoTarea.Cuestionario)]
    public async Task Editar_TipoInmutable_ConservaCamposEditablesDelTipoOriginal(TipoTarea tipo)
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var tarea = new Tarea
        {
            AsignacionId = 1,
            Tipo = tipo,
            Titulo = "Actividad existente",
            Estado = EstadoTarea.Borrador,
            PunteoMaximo = 10,
            FechaCreacion = DateTime.UtcNow,
            CreadoPorUsuarioId = "docente",
            GrupoVersionId = Guid.NewGuid()
        };
        contexto.Tareas.Add(tarea);
        if (tipo == TipoTarea.Cuestionario)
            contexto.Cuestionarios.Add(new Cuestionario { Tarea = tarea });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var modelo = await servicio.ObtenerEditarAsync(tarea.Id);
        Assert.NotNull(modelo);
        modelo!.Titulo = "Título actualizado";
        modelo.Instrucciones = "Instrucciones actualizadas";

        var resultado = await servicio.EditarAsync(modelo, TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var persistida = await contexto.Tareas.FindAsync([tarea.Id], TestContext.Current.CancellationToken);
        Assert.Equal(tipo, persistida!.Tipo);
        Assert.Equal("Título actualizado", persistida.Titulo);
        Assert.Equal("Instrucciones actualizadas", persistida.Instrucciones);
    }

    private static CrearTarea Modelo(TipoTarea tipo) => new()
    {
        AsignacionId = 1,
        Tipo = tipo,
        TokenCreacion = Guid.NewGuid(),
        Titulo = tipo == TipoTarea.Cuestionario ? "Cuestionario nuevo" : "Entrega nueva",
        PunteoMaximo = 10,
        FechaDisponibilidad = new DateTime(2026, 10, 15, 8, 0, 0, DateTimeKind.Unspecified),
        FechaLimite = new DateTime(2026, 10, 16, 8, 0, 0, DateTimeKind.Unspecified)
    };

    private static FormFile ArchivoFormulario()
    {
        var contenido = new MemoryStream("contenido"u8.ToArray());
        return new FormFile(contenido, 0, contenido.Length, "Archivo", "guia.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    private static Contexto CrearContexto() => new(
        new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static Task SembrarEscenarioAsync(Contexto contexto) =>
        CuestionarioServicioTests.SembrarAsync(
            contexto,
            _ => { },
            EstadoTarea.Borrador,
            sinCuestionario: true);

    private sealed class AccesoPrueba(bool autorizado = true) : IAccesoAcademicoServicio
    {
        public string? UsuarioId => "docente";
        public bool EsOperadorInstitucional => false;
        public bool TienePermiso(string permiso) => true;
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() =>
            Task.FromResult(new ContextoAcademicoUsuario(1, null));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult<int?>(1);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult<int?>(null);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) => Task.FromResult(autorizado);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(autorizado);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) => Task.FromResult(autorizado);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(autorizado);
    }

    private sealed class ArchivosPrueba : IArchivoFisicoServicio
    {
        public int Guardados { get; private set; }

        public Task<ResultadoOperacion<ArchivoGuardado>> GuardarAsync(
            IFormFile archivo,
            string categoria,
            CancellationToken cancellationToken = default)
        {
            Guardados++;
            return Task.FromResult(ResultadoOperacion<ArchivoGuardado>.Correcto(new ArchivoGuardado
            {
                NombreOriginal = archivo.FileName,
                ClaveAlmacenamiento = $"pruebas/{Guid.NewGuid():N}",
                TipoMime = archivo.ContentType,
                TamanoBytes = archivo.Length,
                HashSha256 = "hash-prueba"
            }));
        }

        public Task<Stream?> AbrirLecturaAsync(
            string claveAlmacenamiento,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);

        public Task EliminarAsync(
            string claveAlmacenamiento,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
