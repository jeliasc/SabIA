using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.GestionAsignaciones;
using Proyecto_Final.Servicios.GestionCuestionarios;
using Proyecto_Final.Servicios.GestionEntregas;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Entregas;
using Microsoft.AspNetCore.Http;

namespace Proyecto_Final.Tests;

public sealed class EntregaServicioTests
{
    [Fact]
    public async Task EntregarAsync_ConDosIntentos_CreaPendienteSiguienteSinModificarElPrimero()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { }, sinCuestionario: true);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Tipo = TipoTarea.Archivo;
        tarea.MaximoIntentosEntrega = 2;
        var alumno = await contexto.Alumnos.SingleAsync(TestContext.Current.CancellationToken);
        var inscripcion = await contexto.Inscripciones.SingleAsync(TestContext.Current.CancellationToken);
        contexto.Entregas.Add(new Entrega
        {
            Tarea = tarea,
            Alumno = alumno,
            Inscripcion = inscripcion,
            NumeroEnvio = 1,
            Estado = EstadoEntrega.Pendiente
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var entregaId = await contexto.Entregas.Select(x => x.Id).SingleAsync(TestContext.Current.CancellationToken);

        var servicio = new EntregaServicio(
            contexto, new ArchivosPrueba(), new CuestionarioServicioTests.AccesoPrueba(1));
        var resultado = await servicio.EntregarAsync(new EntregarTarea
        {
            Id = entregaId,
            Archivo = ArchivoFormulario()
        }, TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var intentos = await contexto.Entregas.OrderBy(x => x.NumeroEnvio)
            .Include(x => x.Archivos).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Collection(intentos,
            primero =>
            {
                Assert.Equal(EstadoEntrega.Enviada, primero.Estado);
                Assert.NotNull(primero.FechaEntrega);
                Assert.Single(primero.Archivos);
            },
            segundo =>
            {
                Assert.Equal(2, segundo.NumeroEnvio);
                Assert.Equal(EstadoEntrega.Pendiente, segundo.Estado);
                Assert.Null(segundo.FechaEntrega);
            });
    }

    [Fact]
    public async Task EntregaArchivo_CerradaSinEntregaTardia_BloqueaGetYPostAunqueExistaIntentoPendiente()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(contexto);
        var entrega = await contexto.Entregas.SingleAsync(TestContext.Current.CancellationToken);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        entrega.Estado = EstadoEntrega.Pendiente;
        entrega.FechaEntrega = null;
        tarea.Tipo = TipoTarea.Archivo;
        tarea.Estado = EstadoTarea.Cerrada;
        tarea.PermitirEntregaTardia = false;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = new EntregaServicio(
            contexto, new ArchivosPrueba(), new CuestionarioServicioTests.AccesoPrueba(1));

        Assert.Null(await servicio.ObtenerParaEntregarAsync(entregaId));
        var resultado = await servicio.EntregarAsync(new EntregarTarea
        {
            Id = entregaId,
            Archivo = ArchivoFormulario()
        }, TestContext.Current.CancellationToken);

        Assert.False(resultado.Exitoso);
        Assert.Null((await contexto.Entregas.FindAsync(
            [entregaId], TestContext.Current.CancellationToken))!.FechaEntrega);
    }

    [Fact]
    public async Task EntregaArchivo_CerradaConEntregaTardia_HabilitaSoloElIntentoPendiente()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(contexto);
        var entrega = await contexto.Entregas.SingleAsync(TestContext.Current.CancellationToken);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        entrega.Estado = EstadoEntrega.Pendiente;
        entrega.FechaEntrega = null;
        tarea.Tipo = TipoTarea.Archivo;
        tarea.Estado = EstadoTarea.Cerrada;
        tarea.PermitirEntregaTardia = true;
        tarea.FechaLimite = DateTime.UtcNow.AddMinutes(-1);
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = new EntregaServicio(
            contexto, new ArchivosPrueba(), new CuestionarioServicioTests.AccesoPrueba(1));

        Assert.NotNull(await servicio.ObtenerParaEntregarAsync(entregaId));
        var resultado = await servicio.EntregarAsync(new EntregarTarea
        {
            Id = entregaId,
            Archivo = ArchivoFormulario()
        }, TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(EstadoEntrega.Tardia, (await contexto.Entregas.FindAsync(
            [entregaId], TestContext.Current.CancellationToken))!.Estado);
    }

    [Fact]
    public async Task EntregaArchivo_CambioACerradaDespuesDelGet_BloqueaElPost()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(contexto);
        var entrega = await contexto.Entregas.SingleAsync(TestContext.Current.CancellationToken);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        entrega.Estado = EstadoEntrega.Pendiente;
        entrega.FechaEntrega = null;
        tarea.Tipo = TipoTarea.Archivo;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = new EntregaServicio(
            contexto, new ArchivosPrueba(), new CuestionarioServicioTests.AccesoPrueba(1));
        Assert.NotNull(await servicio.ObtenerParaEntregarAsync(entregaId));

        tarea.Estado = EstadoTarea.Cerrada;
        tarea.PermitirEntregaTardia = false;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var resultado = await servicio.EntregarAsync(new EntregarTarea
        {
            Id = entregaId,
            Archivo = ArchivoFormulario()
        }, TestContext.Current.CancellationToken);

        Assert.False(resultado.Exitoso);
    }

    [Fact]
    public async Task CalificarAsync_EntregaValida_NoEvaluaNavegacionesNulas()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(contexto);
        var servicio = CrearServicio(contexto, responsable: true);

        var resultado = await servicio.CalificarAsync(new CalificarEntrega
        {
            Id = entregaId,
            Calificacion = 8,
            Retroalimentacion = "Buen trabajo."
        });

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var entrega = await contexto.Entregas.SingleAsync(
            x => x.Id == entregaId,
            TestContext.Current.CancellationToken);
        Assert.Equal(8, entrega.Calificacion);
        Assert.Equal(EstadoEntrega.Calificada, entrega.Estado);
    }

    [Fact]
    public async Task CalificarAsync_NotaMayorAlPunteoMaximo_RechazaSinModificarLaEntrega()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(contexto);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.PunteoMaximo = 100;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = CrearServicio(contexto, responsable: true);

        var resultado = await servicio.CalificarAsync(new CalificarEntrega
        {
            Id = entregaId,
            Calificacion = 101,
            Retroalimentacion = "No debe persistirse."
        });

        Assert.False(resultado.Exitoso);
        Assert.Equal("La calificación debe estar entre 0 y 100.00.",
            resultado.Errores[nameof(CalificarEntrega.Calificacion)]);
        var entrega = await contexto.Entregas.SingleAsync(x => x.Id == entregaId,
            TestContext.Current.CancellationToken);
        Assert.Null(entrega.Calificacion);
        Assert.Null(entrega.Retroalimentacion);
        Assert.Equal(EstadoEntrega.Enviada, entrega.Estado);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(100d)]
    [InlineData(99.50d)]
    public async Task CalificarAsync_LimitesYDecimalValidos_SeGuardan(decimal calificacion)
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(contexto);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.PunteoMaximo = 100;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = CrearServicio(contexto, responsable: true);

        var resultado = await servicio.CalificarAsync(new CalificarEntrega
        {
            Id = entregaId,
            Calificacion = calificacion
        });

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(calificacion, (await contexto.Entregas.SingleAsync(
            x => x.Id == entregaId, TestContext.Current.CancellationToken)).Calificacion);
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(100.01d)]
    public async Task CalificarAsync_FueraDeRango_NoSeGuarda(decimal calificacion)
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(contexto);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.PunteoMaximo = 100;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = CrearServicio(contexto, responsable: true);

        var resultado = await servicio.CalificarAsync(new CalificarEntrega
        {
            Id = entregaId,
            Calificacion = calificacion
        });

        Assert.False(resultado.Exitoso);
        Assert.Null((await contexto.Entregas.SingleAsync(
            x => x.Id == entregaId, TestContext.Current.CancellationToken)).Calificacion);
    }

    [Fact]
    public async Task CalificarCero_ArchivoSinEntrega_RegistraCeroSinFabricarEnvio()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarPendienteSinEntregaAsync(contexto, TipoTarea.Archivo);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Estado = EstadoTarea.Cerrada;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = CrearServicio(contexto, responsable: true);

        var disponible = await servicio.ObtenerParaCalificarCeroAsync(entregaId);
        var sinConfirmar = await servicio.CalificarCeroAsync(new CalificarCeroEntrega
        {
            Id = entregaId
        });
        var resultado = await servicio.CalificarCeroAsync(new CalificarCeroEntrega
        {
            Id = entregaId,
            Confirmado = true
        });

        Assert.NotNull(disponible);
        Assert.False(sinConfirmar.Exitoso);
        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var entrega = await contexto.Entregas.SingleAsync(x => x.Id == entregaId,
            TestContext.Current.CancellationToken);
        Assert.Equal(EstadoEntrega.Calificada, entrega.Estado);
        Assert.Equal(0, entrega.Calificacion);
        Assert.Null(entrega.FechaEntrega);
        Assert.NotNull(entrega.FechaCalificacion);
        Assert.False(string.IsNullOrWhiteSpace(entrega.CalificadoPorUsuarioId));
        Assert.Equal(OrigenCalificacionAdministrativa.ManualSinEntrega,
            entrega.OrigenCalificacionAdministrativa);
        Assert.Empty(entrega.Archivos);
        Assert.Single(await contexto.HistorialesCalificaciones.ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.Null(await servicio.ObtenerParaCalificarCeroAsync(entregaId));
    }

    [Fact]
    public async Task RectificarCeroAdministrativo_ConservaAuditoriaYSinHabilitarEntrega()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarPendienteSinEntregaAsync(contexto, TipoTarea.Archivo);
        var servicio = CrearServicio(contexto, responsable: true);
        Assert.True((await servicio.CalificarCeroAsync(new CalificarCeroEntrega
        {
            Id = entregaId, Confirmado = true
        })).Exitoso);

        var disponible = await servicio.ObtenerParaRectificarCeroAsync(entregaId);
        var resultado = await servicio.RectificarCeroAsync(new RectificarCeroAdministrativo
        {
            Id = entregaId,
            Motivo = "Se registró cero al alumno equivocado por error docente."
        });

        Assert.NotNull(disponible);
        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var entrega = await contexto.Entregas.SingleAsync(x => x.Id == entregaId,
            TestContext.Current.CancellationToken);
        Assert.Null(entrega.Calificacion);
        Assert.Equal(EstadoEntrega.CeroAdministrativoRectificado, entrega.Estado);
        Assert.Equal(OrigenCalificacionAdministrativa.ManualSinEntrega,
            entrega.OrigenCalificacionAdministrativa);
        Assert.NotNull(entrega.FechaRectificacionAdministrativa);
        Assert.False(string.IsNullOrWhiteSpace(entrega.MotivoRectificacionAdministrativa));
        Assert.Null(await servicio.ObtenerParaEntregarAsync(entregaId));
        Assert.Equal(2, await contexto.HistorialesCalificaciones.CountAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CalificarCero_CuestionarioSinIntentos_RegistraCeroSinCrearIntento()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarPendienteSinEntregaAsync(contexto, TipoTarea.Cuestionario);
        var servicio = CrearServicio(contexto, responsable: true);

        var resultado = await servicio.CalificarCeroAsync(new CalificarCeroEntrega
        {
            Id = entregaId,
            Confirmado = true
        });

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        var entrega = await contexto.Entregas.SingleAsync(x => x.Id == entregaId,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, entrega.Calificacion);
        Assert.Null(entrega.FechaEntrega);
        Assert.Equal(EstadoEntrega.Calificada, entrega.Estado);
        Assert.Empty(await contexto.IntentosCuestionarios.ToListAsync(
            TestContext.Current.CancellationToken));

        var reapertura = await servicio.ReabrirAsync(new ReabrirEntrega
        {
            Id = entregaId,
            FechaLimite = DateTime.Now.AddDays(1),
            Motivo = "Nueva oportunidad de cuestionario después del cero."
        });
        Assert.True(reapertura.Exitoso, reapertura.Mensaje);
        var alumno = new CuestionarioServicio(contexto,
            new CuestionarioServicioTests.AccesoPrueba(1));
        var nuevoIntento = await alumno.IniciarAsync(CuestionarioServicioTests.CuestionarioId);
        Assert.True(nuevoIntento.Exitoso, nuevoIntento.Mensaje);
        Assert.Equal(0, (await contexto.Entregas.FindAsync(
            [entregaId], TestContext.Current.CancellationToken))!.Calificacion);
    }

    [Fact]
    public async Task CalificarCero_RechazaEvaluacionPreviaYDocenteNoAutorizado()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarPendienteSinEntregaAsync(contexto, TipoTarea.Archivo);
        var entrega = await contexto.Entregas.SingleAsync(x => x.Id == entregaId,
            TestContext.Current.CancellationToken);
        contexto.Entregas.Add(new Entrega
        {
            TareaId = entrega.TareaId,
            AlumnoId = entrega.AlumnoId,
            InscripcionId = entrega.InscripcionId,
            NumeroEnvio = 2,
            Estado = EstadoEntrega.Calificada,
            FechaEntrega = DateTime.UtcNow.AddMinutes(-1),
            Calificacion = 8
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var autorizado = CrearServicio(contexto, responsable: true);
        var ajeno = CrearServicio(contexto, responsable: false);
        Assert.Null(await autorizado.ObtenerParaCalificarCeroAsync(entregaId));
        Assert.False((await autorizado.CalificarCeroAsync(new CalificarCeroEntrega
        {
            Id = entregaId,
            Confirmado = true
        })).Exitoso);
        Assert.False((await ajeno.CalificarCeroAsync(new CalificarCeroEntrega
        {
            Id = entregaId,
            Confirmado = true
        })).Exitoso);
    }

    [Fact]
    public async Task CalificarCero_ReaperturaYNuevaEntrega_ConservanAuditoriaDelCero()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarPendienteSinEntregaAsync(contexto, TipoTarea.Archivo);
        var docente = CrearServicio(contexto, responsable: true);
        Assert.True((await docente.CalificarCeroAsync(new CalificarCeroEntrega
        {
            Id = entregaId,
            Confirmado = true
        })).Exitoso);

        var reapertura = await docente.ReabrirAsync(new ReabrirEntrega
        {
            Id = entregaId,
            FechaLimite = DateTime.Now.AddDays(1),
            Motivo = "Nueva oportunidad después de la calificación con cero."
        });
        Assert.True(reapertura.Exitoso, reapertura.Mensaje);

        var nuevoId = await contexto.Entregas
            .Where(x => x.Id != entregaId)
            .Select(x => x.Id)
            .SingleAsync(TestContext.Current.CancellationToken);
        var alumno = new EntregaServicio(contexto, new ArchivosPrueba(),
            new CuestionarioServicioTests.AccesoPrueba(1));
        var envio = await alumno.EntregarAsync(new EntregarTarea
        {
            Id = nuevoId,
            Archivo = ArchivoFormulario()
        }, TestContext.Current.CancellationToken);

        Assert.True(envio.Exitoso, envio.Mensaje);
        var intentos = await contexto.Entregas.OrderBy(x => x.NumeroEnvio)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, intentos[0].Calificacion);
        Assert.NotNull(intentos[0].FechaCalificacion);
        Assert.Null(intentos[0].FechaEntrega);
        Assert.NotNull(intentos[1].FechaEntrega);
    }

    [Fact]
    public async Task CalificarAsync_EntregaInexistente_DevuelveError()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(
            contexto,
            _ => { },
            sinCuestionario: true);
        var servicio = CrearServicio(contexto, responsable: true);

        var resultado = await servicio.CalificarAsync(new CalificarEntrega
        {
            Id = 999,
            Calificacion = 8
        });

        Assert.False(resultado.Exitoso);
    }

    [Fact]
    public async Task CalificarAsync_InscripcionNoVigente_RechazaSinModificarEntrega()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(
            contexto,
            EstadoInscripcion.Retirada);
        var servicio = CrearServicio(contexto, responsable: true);

        Assert.Null(await servicio.ObtenerParaCalificarAsync(entregaId));

        var resultado = await servicio.CalificarAsync(new CalificarEntrega
        {
            Id = entregaId,
            Calificacion = 8
        });

        Assert.False(resultado.Exitoso);
        var entrega = await contexto.Entregas.AsNoTracking().SingleAsync(
            x => x.Id == entregaId,
            TestContext.Current.CancellationToken);
        Assert.Null(entrega.Calificacion);
        Assert.Equal(EstadoEntrega.Enviada, entrega.Estado);
    }

    [Fact]
    public async Task CalificarAsync_DocenteNoAutorizado_RechazaSinModificarEntrega()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(contexto);
        var servicio = CrearServicio(contexto, responsable: false);

        var resultado = await servicio.CalificarAsync(new CalificarEntrega
        {
            Id = entregaId,
            Calificacion = 8
        });

        Assert.False(resultado.Exitoso);
        var entrega = await contexto.Entregas.AsNoTracking().SingleAsync(
            x => x.Id == entregaId,
            TestContext.Current.CancellationToken);
        Assert.Null(entrega.Calificacion);
        Assert.Equal(EstadoEntrega.Enviada, entrega.Estado);
    }

    [Fact]
    public async Task CalificarAsync_DelegacionVigente_ConservaAccesoDelegado()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(contexto);
        var servicio = CrearServicio(
            contexto,
            responsable: false,
            delegacion: new DelegacionPrueba(true));

        var resultado = await servicio.CalificarAsync(new CalificarEntrega
        {
            Id = entregaId,
            Calificacion = 9
        });

        Assert.True(resultado.Exitoso, resultado.Mensaje);
    }

    [Fact]
    public async Task DescargarArchivoTareaAsync_AlumnoNoPuedeAbrirAdjuntoDeBorrador()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { }, sinCuestionario: true);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Estado = EstadoTarea.Borrador;
        var archivo = new Archivo
        {
            NombreOriginal = "guia-privada.pdf",
            ClaveAlmacenamiento = "tareas/privada.pdf",
            TipoMime = "application/pdf",
            TamanoBytes = 1,
            HashSha256 = "hash",
            SubidoPorUsuarioId = "docente",
            FechaCreacion = DateTime.UtcNow
        };
        contexto.Archivos.Add(archivo);
        contexto.TareasArchivos.Add(new TareaArchivo { Tarea = tarea, Archivo = archivo });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = new EntregaServicio(
            contexto,
            new ArchivosLecturaPrueba(),
            new AlumnoAccesoPrueba(pertenece: true));

        Assert.Null(await servicio.DescargarArchivoTareaAsync(archivo.Id));
    }

    [Fact]
    public async Task DetalleEntrega_DelegacionSinOperacionDeEntrega_NoConcedeAcceso()
    {
        await using var contexto = CrearContexto();
        var entregaId = await SembrarEntregaAsync(contexto);
        var servicio = CrearServicio(
            contexto,
            responsable: false,
            delegacion: new DelegacionPrueba(true, OperacionDelegacionAcademica.GestionarEvaluaciones));

        Assert.Null(await servicio.ObtenerDetalleAsync(entregaId));
    }

    [Fact]
    public async Task Listado_UsuarioSinPerfilNiAutorizacionInstitucional_NoRecibeEntregas()
    {
        await using var contexto = CrearContexto();
        await SembrarEntregaAsync(contexto);
        var servicio = new EntregaServicio(contexto, null!, new SinPerfilAccesoPrueba());

        Assert.Empty(await servicio.ObtenerTodasAsync());
    }

    [Fact]
    public async Task Listado_DosIntentosMismaActividad_DevuelveUnaFilaConHistorialCompleto()
    {
        await using var contexto = CrearContexto();
        var primeraId = await SembrarEntregaAsync(contexto);
        var primera = await contexto.Entregas.FindAsync(
            [primeraId], TestContext.Current.CancellationToken);
        contexto.Entregas.Add(new Entrega
        {
            TareaId = primera!.TareaId,
            AlumnoId = primera.AlumnoId,
            InscripcionId = primera.InscripcionId,
            NumeroEnvio = 2,
            Estado = EstadoEntrega.Pendiente
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = CrearServicio(contexto, responsable: true);
        var listado = await servicio.ObtenerTodasAsync();

        var fila = Assert.Single(listado);
        // El segundo registro sigue pendiente: no consume un intento ordinario.
        Assert.Equal(1, fila.CantidadIntentos);
        Assert.Equal(2, fila.NumeroEnvio);
        Assert.Equal(primeraId, fila.EntregaCalificableId);
        Assert.True(fila.PuedeCalificarse);

        var detalle = await servicio.ObtenerDetalleAsync(fila.Id);
        Assert.NotNull(detalle);
        Assert.Equal(2, detalle!.Intentos.Count);
        Assert.Equal(fila.CantidadIntentos, detalle.IntentosOrdinariosUsados);
        Assert.Equal(2, detalle.Intentos[0].NumeroEnvio);
        Assert.True(detalle.TieneIntentoPendienteEvaluacion);
    }

    [Fact]
    public async Task Listado_ReaperturaConPendientePosterior_ConservaCalificacionDelIntentoEnviado()
    {
        await using var contexto = CrearContexto();
        var enviadaId = await SembrarEntregaAsync(contexto);
        var enviada = await contexto.Entregas.FindAsync(
            [enviadaId], TestContext.Current.CancellationToken);
        contexto.Entregas.Add(new Entrega
        {
            TareaId = enviada!.TareaId,
            AlumnoId = enviada.AlumnoId,
            InscripcionId = enviada.InscripcionId,
            NumeroEnvio = 2,
            Estado = EstadoEntrega.Pendiente
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = CrearServicio(contexto, responsable: true);

        var fila = Assert.Single(await servicio.ObtenerTodasAsync());

        Assert.True(fila.PuedeCalificarse);
        Assert.Equal(enviadaId, fila.EntregaCalificableId);
        Assert.NotNull(await servicio.ObtenerParaCalificarAsync(enviadaId));
    }

    [Fact]
    public async Task ListadoYDetalle_IntentoCalificadoConPendientePosterior_ConservanCalificacionYFechaHistoricas()
    {
        await using var contexto = CrearContexto();
        var enviadaId = await SembrarEntregaAsync(contexto);
        var enviada = await contexto.Entregas.FindAsync(
            [enviadaId], TestContext.Current.CancellationToken);
        enviada!.Estado = EstadoEntrega.Calificada;
        enviada.Calificacion = 100;
        var fechaEnviada = enviada.FechaEntrega;
        contexto.Entregas.Add(new Entrega
        {
            TareaId = enviada.TareaId,
            AlumnoId = enviada.AlumnoId,
            InscripcionId = enviada.InscripcionId,
            NumeroEnvio = 2,
            Estado = EstadoEntrega.Pendiente
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = CrearServicio(contexto, responsable: true);
        var fila = Assert.Single(await servicio.ObtenerTodasAsync());
        var detalle = await servicio.ObtenerDetalleAsync(fila.Id);

        Assert.Equal(fechaEnviada, fila.FechaEntrega);
        Assert.Equal(100, fila.Calificacion);
        Assert.False(fila.TieneIntentoPendienteEvaluacion);
        Assert.True(fila.TieneCalificacionPrevia);
        Assert.Equal("Nuevo intento pendiente", fila.EstadoIntento);
        Assert.NotNull(detalle);
        Assert.Equal(fila.FechaEntrega, detalle!.Intentos
            .Where(x => x.FechaEntrega.HasValue).Max(x => x.FechaEntrega));
        Assert.Equal(fila.Calificacion, detalle.CalificacionEfectiva);
        Assert.True(detalle.TieneCalificacionPrevia);
        Assert.Equal("Nuevo intento pendiente", detalle.EstadoIntentoActual);
    }

    [Fact]
    public async Task ListadoYDetalle_EnvioSinCalificarConNotaPrevia_MuestranEvaluacionPendiente()
    {
        await using var contexto = CrearContexto();
        var primerIntentoId = await SembrarEntregaAsync(contexto);
        var primerIntento = await contexto.Entregas.FindAsync(
            [primerIntentoId], TestContext.Current.CancellationToken);
        primerIntento!.Estado = EstadoEntrega.Calificada;
        primerIntento.Calificacion = 90;

        var segundoIntento = new Entrega
        {
            TareaId = primerIntento.TareaId,
            AlumnoId = primerIntento.AlumnoId,
            InscripcionId = primerIntento.InscripcionId,
            NumeroEnvio = 2,
            Estado = EstadoEntrega.Enviada,
            FechaEntrega = DateTime.UtcNow.AddMinutes(1)
        };
        contexto.Entregas.Add(segundoIntento);
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = CrearServicio(contexto, responsable: true);
        var fila = Assert.Single(await servicio.ObtenerTodasAsync());
        var detalle = await servicio.ObtenerDetalleAsync(fila.Id);

        Assert.Equal(90, fila.Calificacion);
        Assert.True(fila.TieneIntentoPendienteEvaluacion);
        Assert.True(fila.TieneCalificacionPrevia);
        Assert.Equal("Evaluación pendiente", fila.EstadoIntento);
        Assert.Equal(segundoIntento.Id, fila.Id);
        Assert.NotNull(detalle);
        Assert.Equal(fila.Calificacion, detalle!.CalificacionEfectiva);
        Assert.True(detalle.TieneIntentoPendienteEvaluacion);
        Assert.True(detalle.TieneCalificacionPrevia);
        Assert.Equal("Evaluación pendiente", detalle.EstadoIntentoActual);
    }

    [Fact]
    public async Task ListadoYDetalle_VariosIntentosCalificados_UsanLaCalificacionMasReciente()
    {
        await using var contexto = CrearContexto();
        var primeroId = await SembrarEntregaAsync(contexto);
        var primero = await contexto.Entregas.FindAsync(
            [primeroId], TestContext.Current.CancellationToken);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Tipo = TipoTarea.Archivo;
        primero!.Estado = EstadoEntrega.Calificada;
        primero.Calificacion = 100;
        var segundo = new Entrega
        {
            TareaId = primero.TareaId,
            AlumnoId = primero.AlumnoId,
            InscripcionId = primero.InscripcionId,
            NumeroEnvio = 2,
            Estado = EstadoEntrega.Calificada,
            FechaEntrega = DateTime.UtcNow,
            Calificacion = 90
        };
        contexto.Entregas.Add(segundo);
        tarea.CriterioCalificacionEntrega = CriterioCalificacionIntentos.MejorNota;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = CrearServicio(contexto, responsable: true);
        var fila = Assert.Single(await servicio.ObtenerTodasAsync());
        var detalle = await servicio.ObtenerDetalleAsync(fila.Id);

        Assert.Equal(100, fila.Calificacion);
        Assert.Equal(segundo.FechaEntrega, fila.FechaEntrega);
        Assert.False(fila.TieneCalificacionPrevia);
        Assert.Equal("Calificada", fila.EstadoIntento);
        Assert.NotNull(detalle);
        Assert.Equal(fila.Calificacion, detalle!.CalificacionEfectiva);

        tarea.CriterioCalificacionEntrega = CriterioCalificacionIntentos.UltimoIntento;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        fila = Assert.Single(await servicio.ObtenerTodasAsync());
        detalle = await servicio.ObtenerDetalleAsync(fila.Id);
        Assert.Equal(90, fila.Calificacion);
        Assert.Equal(fila.Calificacion, detalle!.CalificacionEfectiva);

        tarea.CriterioCalificacionEntrega = CriterioCalificacionIntentos.Promedio;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        fila = Assert.Single(await servicio.ObtenerTodasAsync());
        detalle = await servicio.ObtenerDetalleAsync(fila.Id);
        Assert.Equal(95, fila.Calificacion);
        Assert.Equal(fila.Calificacion, detalle!.CalificacionEfectiva);
    }

    [Fact]
    public async Task ListadoYDetalle_SinEntregaEnviada_MantienenFechaYCalificacionNulas()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { });
        var servicio = CrearServicio(contexto, responsable: true);

        var fila = Assert.Single(await servicio.ObtenerTodasAsync());
        var detalle = await servicio.ObtenerDetalleAsync(fila.Id);

        Assert.Null(fila.FechaEntrega);
        Assert.Null(fila.Calificacion);
        Assert.Null(fila.EntregaCalificableId);
        Assert.False(fila.TieneCalificacionPrevia);
        Assert.Equal("Pendiente", fila.EstadoIntento);
        Assert.NotNull(detalle);
        Assert.Null(detalle!.CalificacionEfectiva);
        Assert.Equal("Pendiente", detalle.EstadoIntentoActual);
    }

    [Fact]
    public async Task ListadoYDetalle_HistorialFiltrado_UsanElMismoResumenDeIntentos()
    {
        await using var contexto = CrearContexto();
        var primerId = await SembrarEntregaAsync(contexto);
        var primero = await contexto.Entregas.FindAsync(
            [primerId], TestContext.Current.CancellationToken);
        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        var limite = DateTime.UtcNow.AddDays(1);
        primero!.Estado = EstadoEntrega.Calificada;
        primero.Calificacion = 88;

        // Los dos envÃ­os anteriores quedan fuera del filtro visual al cerrar
        // la tarea; la reapertura individual pendiente mantiene la actividad
        // visible. El contador debe seguir viendo los tres envÃ­os ordinarios.
        contexto.Entregas.AddRange(
            new Entrega
            {
                TareaId = primero!.TareaId,
                AlumnoId = primero.AlumnoId,
                InscripcionId = primero.InscripcionId,
                NumeroEnvio = 2,
                Estado = EstadoEntrega.Enviada,
                FechaEntrega = DateTime.UtcNow.AddHours(-2)
            },
            new Entrega
            {
                TareaId = primero.TareaId,
                AlumnoId = primero.AlumnoId,
                InscripcionId = primero.InscripcionId,
                NumeroEnvio = 3,
                Estado = EstadoEntrega.Enviada,
                FechaEntrega = DateTime.UtcNow.AddHours(-1)
            });
        var extraordinaria = new Entrega
        {
            TareaId = primero.TareaId,
            AlumnoId = primero.AlumnoId,
            InscripcionId = primero.InscripcionId,
            NumeroEnvio = 4,
            Estado = EstadoEntrega.Pendiente,
            FechaLimiteIndividual = limite
        };
        contexto.Entregas.Add(extraordinaria);
        tarea.Estado = EstadoTarea.Cerrada;
        contexto.ReaperturasEntregas.Add(new ReaperturaEntrega
        {
            Entrega = extraordinaria,
            FechaReapertura = DateTime.UtcNow,
            FechaLimite = limite,
            AutorizaOportunidadAdicional = true,
            Motivo = "Oportunidad extraordinaria de prueba.",
            ReabiertaPorUsuarioId = "docente"
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = CrearServicio(contexto, responsable: true);
        Assert.Empty(await servicio.ObtenerTodasAsync());
        var fila = Assert.Single(await servicio.ObtenerHistorialAsync());
        var detalle = await servicio.ObtenerDetalleAsync(fila.Id);

        Assert.Equal(3, fila.CantidadIntentos);
        Assert.Equal(1, fila.OportunidadesAdicionales);
        Assert.Equal(0, fila.OportunidadesAdicionalesConsumidas);
        Assert.Equal(1, fila.OportunidadesAdicionalesVigentes);
        Assert.Equal(88, fila.Calificacion);
        Assert.NotNull(fila.FechaEntrega);
        Assert.NotNull(detalle);
        Assert.Equal(fila.CantidadIntentos, detalle!.IntentosOrdinariosUsados);
        Assert.Equal(fila.OportunidadesAdicionales,
            detalle.OportunidadesAdicionales);
        Assert.Equal(fila.OportunidadesAdicionalesVigentes,
            detalle.OportunidadesAdicionalesVigentes);
        Assert.Equal(fila.Calificacion, detalle.CalificacionEfectiva);
    }

    [Fact]
    public async Task ListadoYDetalle_OportunidadExtraordinariaEnviada_NoConsumeCupoOrdinario()
    {
        await using var contexto = CrearContexto();
        var primerId = await SembrarEntregaAsync(contexto);
        var primero = await contexto.Entregas.FindAsync(
            [primerId], TestContext.Current.CancellationToken);
        var extraordinaria = new Entrega
        {
            TareaId = primero!.TareaId,
            AlumnoId = primero.AlumnoId,
            InscripcionId = primero.InscripcionId,
            NumeroEnvio = 2,
            Estado = EstadoEntrega.Enviada,
            FechaEntrega = DateTime.UtcNow
        };
        contexto.Entregas.Add(extraordinaria);
        contexto.ReaperturasEntregas.Add(new ReaperturaEntrega
        {
            Entrega = extraordinaria,
            FechaReapertura = DateTime.UtcNow.AddHours(-1),
            FechaLimite = DateTime.UtcNow.AddDays(1),
            AutorizaOportunidadAdicional = true,
            Motivo = "Oportunidad extraordinaria consumida.",
            ReabiertaPorUsuarioId = "docente"
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = CrearServicio(contexto, responsable: true);
        var fila = Assert.Single(await servicio.ObtenerTodasAsync());
        var detalle = await servicio.ObtenerDetalleAsync(fila.Id);

        Assert.Equal(1, fila.CantidadIntentos);
        Assert.Equal(1, fila.OportunidadesAdicionalesConsumidas);
        Assert.NotNull(detalle);
        Assert.Equal(1, detalle!.IntentosOrdinariosUsados);
        Assert.Equal(1, detalle.OportunidadesAdicionalesConsumidas);
    }

    [Fact]
    public async Task ListadoYDetalle_Cuestionario_SeparaIntentosOrdinariosYExtraordinarios()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto,
            cuestionario => cuestionario.MaximoIntentos = 2);
        var cuestionario = await contexto.Cuestionarios.SingleAsync(
            TestContext.Current.CancellationToken);
        var entrega = await contexto.Entregas.SingleAsync(
            TestContext.Current.CancellationToken);
        var limite = DateTime.UtcNow.AddDays(1);
        var entregaExtraordinaria = new Entrega
        {
            TareaId = entrega.TareaId,
            AlumnoId = entrega.AlumnoId,
            InscripcionId = entrega.InscripcionId,
            NumeroEnvio = 2,
            Estado = EstadoEntrega.Pendiente,
            FechaLimiteIndividual = limite
        };
        contexto.Entregas.Add(entregaExtraordinaria);
        var reapertura = new ReaperturaEntrega
        {
            Entrega = entregaExtraordinaria,
            FechaReapertura = DateTime.UtcNow,
            FechaLimite = limite,
            AutorizaOportunidadAdicional = true,
            Motivo = "AutorizaciÃ³n extraordinaria de cuestionario.",
            ReabiertaPorUsuarioId = "docente"
        };
        contexto.ReaperturasEntregas.Add(reapertura);
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        contexto.IntentosCuestionarios.AddRange(
            new IntentoCuestionario
            {
                CuestionarioId = cuestionario.Id,
                AlumnoId = entrega.AlumnoId,
                InscripcionId = entrega.InscripcionId,
                NumeroIntento = 1,
                Estado = EstadoIntentoCuestionario.Finalizado,
                FechaInicio = DateTime.UtcNow.AddHours(-3),
                FechaFinalizacion = DateTime.UtcNow.AddHours(-2),
                OrdenPreguntas = "1"
            },
            new IntentoCuestionario
            {
                CuestionarioId = cuestionario.Id,
                AlumnoId = entrega.AlumnoId,
                InscripcionId = entrega.InscripcionId,
                NumeroIntento = 2,
                Estado = EstadoIntentoCuestionario.Expirado,
                FechaInicio = DateTime.UtcNow.AddHours(-2),
                FechaFinalizacion = DateTime.UtcNow.AddHours(-1),
                OrdenPreguntas = "1"
            },
            new IntentoCuestionario
            {
                CuestionarioId = cuestionario.Id,
                AlumnoId = entrega.AlumnoId,
                InscripcionId = entrega.InscripcionId,
                ReaperturaEntregaId = reapertura.Id,
                NumeroIntento = 3,
                Estado = EstadoIntentoCuestionario.EnCurso,
                FechaInicio = DateTime.UtcNow,
                OrdenPreguntas = "1"
            });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = CrearServicio(contexto, responsable: true);
        var fila = Assert.Single(await servicio.ObtenerTodasAsync());
        var detalle = await servicio.ObtenerDetalleAsync(fila.Id);

        Assert.Equal(2, fila.CantidadIntentos);
        Assert.Equal(1, fila.OportunidadesAdicionalesConsumidas);
        Assert.NotNull(detalle);
        Assert.Equal(2, detalle!.IntentosOrdinariosUsados);
        Assert.Equal(1, detalle.OportunidadesAdicionalesConsumidas);
    }

    [Fact]
    public async Task ObtenerParaCalificarAsync_SoloOfreceIntentosEnviadosDelMismoContexto()
    {
        await using var contexto = CrearContexto();
        var primerIntentoId = await SembrarEntregaAsync(contexto);
        var primerIntento = await contexto.Entregas.FindAsync(
            [primerIntentoId], TestContext.Current.CancellationToken);

        var segundoIntento = new Entrega
        {
            TareaId = primerIntento!.TareaId,
            AlumnoId = primerIntento.AlumnoId,
            InscripcionId = primerIntento.InscripcionId,
            NumeroEnvio = 2,
            Estado = EstadoEntrega.Enviada,
            FechaEntrega = DateTime.UtcNow
        };
        contexto.Entregas.Add(segundoIntento);
        contexto.Entregas.Add(new Entrega
        {
            TareaId = primerIntento.TareaId,
            AlumnoId = primerIntento.AlumnoId,
            InscripcionId = primerIntento.InscripcionId,
            NumeroEnvio = 3,
            Estado = EstadoEntrega.Pendiente
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var servicio = CrearServicio(contexto, responsable: true);
        var modelo = await servicio.ObtenerParaCalificarAsync(primerIntentoId);

        Assert.NotNull(modelo);
        Assert.Collection(modelo!.Intentos,
            intento =>
            {
                Assert.Equal(segundoIntento.Id, intento.Id);
                Assert.Equal(EstadoEntrega.Enviada, intento.Estado);
            },
            intento =>
            {
                Assert.Equal(primerIntentoId, intento.Id);
                Assert.Equal(EstadoEntrega.Enviada, intento.Estado);
            });
    }

    private static EntregaServicio CrearServicio(
        Contexto contexto,
        bool responsable,
        IDelegacionAcademicaServicio? delegacion = null) =>
        new(contexto, null!, new AccesoPrueba(responsable), delegacion);

    private static Contexto CrearContexto() => new(
        new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static async Task<int> SembrarPendienteSinEntregaAsync(
        Contexto contexto,
        TipoTarea tipo)
    {
        await CuestionarioServicioTests.SembrarAsync(
            contexto,
            _ => { },
            sinCuestionario: tipo == TipoTarea.Archivo);

        var tarea = await contexto.Tareas.SingleAsync(TestContext.Current.CancellationToken);
        tarea.Tipo = tipo;
        if (tipo == TipoTarea.Archivo)
        {
            tarea.MaximoIntentosEntrega = 1;
            var alumno = await contexto.Alumnos.SingleAsync(TestContext.Current.CancellationToken);
            var inscripcion = await contexto.Inscripciones.SingleAsync(
                TestContext.Current.CancellationToken);
            contexto.Entregas.Add(new Entrega
            {
                Tarea = tarea,
                Alumno = alumno,
                Inscripcion = inscripcion,
                NumeroEnvio = 1,
                Estado = EstadoEntrega.Pendiente
            });
        }

        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        contexto.ChangeTracker.Clear();
        return await contexto.Entregas.Select(x => x.Id).SingleAsync(
            TestContext.Current.CancellationToken);
    }

    private static async Task<int> SembrarEntregaAsync(
        Contexto contexto,
        EstadoInscripcion estadoInscripcion = EstadoInscripcion.Activa)
    {
        await CuestionarioServicioTests.SembrarAsync(
            contexto,
            _ => { },
            sinCuestionario: true);

        var tarea = await contexto.Tareas.SingleAsync(
            TestContext.Current.CancellationToken);
        var alumno = await contexto.Alumnos.SingleAsync(
            TestContext.Current.CancellationToken);
        var inscripcion = await contexto.Inscripciones.SingleAsync(
            TestContext.Current.CancellationToken);
        inscripcion.Estado = estadoInscripcion;

        var entrega = new Entrega
        {
            Tarea = tarea,
            Alumno = alumno,
            Inscripcion = inscripcion,
            NumeroEnvio = 1,
            Estado = EstadoEntrega.Enviada,
            FechaEntrega = DateTime.UtcNow
        };
        contexto.Entregas.Add(entrega);
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        contexto.ChangeTracker.Clear();
        return entrega.Id;
    }

    private sealed class AccesoPrueba(bool responsable) : IAccesoAcademicoServicio
    {
        public string? UsuarioId => "docente";
        public bool EsOperadorInstitucional => false;
        public bool TienePermiso(string permiso) => true;
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() =>
            Task.FromResult(new ContextoAcademicoUsuario(1, null));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult<int?>(1);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult<int?>(null);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) =>
            Task.FromResult(responsable);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) =>
            Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) =>
            Task.FromResult(responsable);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) =>
            Task.FromResult(responsable);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) =>
            Task.FromResult(responsable);
    }

    private sealed class DelegacionPrueba(
        bool autorizada,
        OperacionDelegacionAcademica operacionAutorizada = OperacionDelegacionAcademica.CalificarEntregas) : IDelegacionAcademicaServicio
    {
        public Task<bool> PuedeActuarAsync(
            int asignacionId,
            int docenteId,
            OperacionDelegacionAcademica operacion) =>
            Task.FromResult(
                autorizada &&
                operacion == operacionAutorizada);
    }

    private sealed class AlumnoAccesoPrueba(bool pertenece) : IAccesoAcademicoServicio
    {
        public string? UsuarioId => "alumno";
        public bool EsOperadorInstitucional => false;
        public bool TienePermiso(string permiso) => true;
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() =>
            Task.FromResult(new ContextoAcademicoUsuario(null, 1));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult<int?>(null);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult<int?>(1);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) => Task.FromResult(pertenece);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(pertenece);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) => Task.FromResult(pertenece);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(false);
    }

    private sealed class SinPerfilAccesoPrueba : IAccesoAcademicoServicio
    {
        public string? UsuarioId => "externo";
        public bool EsOperadorInstitucional => false;
        public bool TienePermiso(string permiso) => true;
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() =>
            Task.FromResult(new ContextoAcademicoUsuario(null, null));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult<int?>(null);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult<int?>(null);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(false);
    }

    private static IFormFile ArchivoFormulario()
    {
        var contenido = new MemoryStream("contenido"u8.ToArray());
        return new FormFile(contenido, 0, contenido.Length, "Archivo", "entrega.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    private sealed class ArchivosPrueba : IArchivoFisicoServicio
    {
        public Task<ResultadoOperacion<ArchivoGuardado>> GuardarAsync(
            IFormFile archivo, string categoria, CancellationToken cancellationToken = default) =>
            Task.FromResult(ResultadoOperacion<ArchivoGuardado>.Correcto(new ArchivoGuardado
            {
                NombreOriginal = archivo.FileName,
                ClaveAlmacenamiento = "pruebas/entrega.pdf",
                TipoMime = archivo.ContentType,
                TamanoBytes = archivo.Length,
                HashSha256 = "hash"
            }));

        public Task<Stream?> AbrirLecturaAsync(string claveAlmacenamiento,
            CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
        public Task EliminarAsync(string claveAlmacenamiento,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ArchivosLecturaPrueba : IArchivoFisicoServicio
    {
        public Task<ResultadoOperacion<ArchivoGuardado>> GuardarAsync(IFormFile archivo, string categoria,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream?> AbrirLecturaAsync(string claveAlmacenamiento,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(new MemoryStream("contenido"u8.ToArray()));
        public Task EliminarAsync(string claveAlmacenamiento,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
