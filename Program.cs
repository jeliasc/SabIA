using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using System.Globalization;
using System.Net;
using Proyecto_Final.Data;
using Proyecto_Final.Data.Seeders;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Seguridad.Filtros;
using Proyecto_Final.Servicios.Correo;
using Proyecto_Final.Servicios.Usuarios;
using Proyecto_Final.Servicios.Autenticacion;
using Proyecto_Final.Servicios.GestionRoles;
using Proyecto_Final.Servicios.GestionPermisos;
using Proyecto_Final.Servicios.GestionResolucionesTraslados;
using Proyecto_Final.Servicios.Academico;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.Servicios.GestionDocentes;
using Proyecto_Final.Servicios.GestionAlumnos;
using Proyecto_Final.Servicios.GestionEncargados;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.Servicios.GestionPeriodos;
using Proyecto_Final.Servicios.GestionGrados;
using Proyecto_Final.Servicios.GestionSecciones;
using Proyecto_Final.Servicios.GestionCursos;
using Proyecto_Final.Servicios.GestionAsignaciones;
using Proyecto_Final.Servicios.GestionInscripciones;
using Proyecto_Final.Servicios.GestionUnidades;
using Proyecto_Final.Servicios.GestionMateriales;
using Proyecto_Final.Servicios.GestionPlanificaciones;
using Proyecto_Final.Servicios.GestionTareas;
using Proyecto_Final.Servicios.GestionEntregas;
using Proyecto_Final.Servicios.GestionComentariosAcademicos;
using Proyecto_Final.Servicios.GestionCuestionarios;
using Proyecto_Final.Servicios.GestionDocumentos;
using Proyecto_Final.Servicios.GestionNotificaciones;
using Proyecto_Final.Servicios.GestionCalificaciones;
using Proyecto_Final.Servicios.InteligenciaArtificial;
using Proyecto_Final.Servicios.Dashboard;
using Proyecto_Final.Servicios.ExploradorAcademico;
using Proyecto_Final.Servicios.Auditoria;

var builder = WebApplication.CreateBuilder(args);

var rutaClavesProteccion = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(rutaClavesProteccion))
{
    Directory.CreateDirectory(rutaClavesProteccion);
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(rutaClavesProteccion));
}

// CONFIGURAR CABECERAS REENVIADAS (PROXY INVERSO)
var configuracionProxySeguro = builder.Configuration.GetSection("ProxySeguro");
var confiarEnProxyRender = configuracionProxySeguro.GetValue<bool>("ConfiarEnProxyRender");
var ejecutandoseEnRender = string.Equals(
    Environment.GetEnvironmentVariable("RENDER"),
    "true",
    StringComparison.OrdinalIgnoreCase);

if (confiarEnProxyRender && !ejecutandoseEnRender)
{
    throw new InvalidOperationException(
        "ProxySeguro:ConfiarEnProxyRender solo puede habilitarse dentro de Render.");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    if (confiarEnProxyRender)
    {
        // Render no publica direcciones estaticas para sus proxies. Dejar ambas
        // colecciones vacias confia en cualquier par directo y solo es seguro
        // cuando el puerto de la aplicacion esta aislado por la red de Render.
        return;
    }

    // Comportamiento seguro por defecto: solo loopback y los origenes
    // autorizados expresamente en la configuracion.
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);

    foreach (var entrada in configuracionProxySeguro
        .GetSection("ProxiesConfiables")
        .GetChildren())
    {
        var valor = entrada.Value;
        if (string.IsNullOrWhiteSpace(valor))
        {
            continue;
        }

        if (!IPAddress.TryParse(valor, out var proxy))
        {
            throw new InvalidOperationException(
                $"La direccion '{valor}' de ProxySeguro:ProxiesConfiables no es valida.");
        }

        options.KnownProxies.Add(proxy);
    }

    foreach (var entrada in configuracionProxySeguro
        .GetSection("RedesConfiables")
        .GetChildren())
    {
        var valor = entrada.Value;
        if (string.IsNullOrWhiteSpace(valor))
        {
            continue;
        }

        if (!System.Net.IPNetwork.TryParse(valor, out var red))
        {
            throw new InvalidOperationException(
                $"La red '{valor}' de ProxySeguro:RedesConfiables no es valida.");
        }

        options.KnownIPNetworks.Add(red);
    }
});

builder.Services.AddScoped<AuditoriaSaveChangesInterceptor>();
builder.Services.AddDbContext<Contexto>((serviceProvider, options) =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("ConexionPrincipal"))
        .AddInterceptors(serviceProvider.GetRequiredService<AuditoriaSaveChangesInterceptor>())
);


builder.Services.AddIdentity<Usuario, Rol>(options =>
    {
        // Requisitos de contraseña
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        // Bloqueo por intentos fallidos
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        // El correo no puede repetirse
        options.User.RequireUniqueEmail = true;

        // No exigimos confirmación por correo por ahora
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<Contexto>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<ForzarCambioContrasenaFiltro>();
builder.Services.AddScoped<AuditoriaRechazosFiltro>();
builder.Services.AddScoped<BloquearEscrituraCicloHistoricoFiltro>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login/Login";
    options.LogoutPath = "/Login/CerrarSesion";
    options.AccessDeniedPath = "/Login/AccesoDenegado";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Events.OnRedirectToLogin = contexto =>
    {
        if (EsNavegacionHtml(contexto.HttpContext))
        {
            contexto.Response.Redirect(contexto.RedirectUri);
        }
        else
        {
            contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }

        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = contexto =>
    {
        // La redirección predeterminada transforma un 403 en 302 y evita que
        // la capa de estados presente la respuesta institucional. Mantener el
        // código también protege a clientes AJAX y API de recibir HTML.
        contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Permisos.Dashboard.CoordinacionVer, politica =>
        politica.RequireAuthenticatedUser()
            .RequireAssertion(contexto =>
                AutorizacionDashboardCoordinacion.PuedeVer(contexto.User)));
    options.AddPolicy(Politicas.RelevarAsignacion, politica =>
        politica.RequireAssertion(contexto =>
            contexto.User.IsInRole(Roles.Superusuario) ||
            contexto.User.HasClaim(TiposClaims.Permiso, Permisos.Asignaciones.Relevar) ||
            (contexto.User.HasClaim(TiposClaims.Permiso, Permisos.Asignaciones.RelevarExcepcional) &&
             AutorizacionDashboardCoordinacion.PuedeVer(contexto.User))));
    options.AddPolicy(Permisos.Asignaciones.RelevarExcepcional, politica =>
        politica.RequireAssertion(contexto =>
            contexto.User.IsInRole(Roles.Superusuario) ||
            (contexto.User.HasClaim(TiposClaims.Permiso, Permisos.Asignaciones.RelevarExcepcional) &&
             AutorizacionDashboardCoordinacion.PuedeVer(contexto.User))));
    options.AddPolicy(Permisos.Ciclos.Cerrar, politica =>
        politica.RequireAssertion(contexto =>
            contexto.User.IsInRole(Roles.Superusuario) ||
            (contexto.User.HasClaim(TiposClaims.Permiso, Permisos.Ciclos.Cerrar) &&
             contexto.User.HasClaim(TiposClaims.Permiso, Permisos.Planificaciones.Revisar))));
    options.AddPolicy(Permisos.Ciclos.Reabrir, politica =>
        politica.RequireAssertion(contexto =>
            contexto.User.IsInRole(Roles.Superusuario) ||
            (contexto.User.HasClaim(TiposClaims.Permiso, Permisos.Ciclos.Reabrir) &&
             contexto.User.HasClaim(TiposClaims.Permiso, Permisos.Planificaciones.Revisar))));

    options.AddPolicy(
        Politicas.AuditoriaSuperusuario,
        politica => politica.RequireRole(Roles.Superusuario));
    options.AddPolicy(Politicas.ComentariosAcademicos,
        politica => politica.RequireAuthenticatedUser());

    // EXIGIR AUTENTICACIÓN EN TODO EL SISTEMA
    // excepto en las acciones marcadas con AllowAnonymous.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSingleton<
    IAuthorizationPolicyProvider,
    ProveedorPoliticasPermisos
>();

builder.Services.AddControllersWithViews(options =>
{
    // APLICAR VALIDACIÓN DE CONTRASEÑA OBLIGATORIA
    // A TODOS LOS CONTROLADORES DEL SISTEMA.
    options.Filters.AddService<ForzarCambioContrasenaFiltro>();
    options.Filters.AddService<AuditoriaRechazosFiltro>();
    options.Filters.AddService<BloquearEscrituraCicloHistoricoFiltro>();

    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor(
        (valor, campo) => $"El valor '{valor}' no es válido para {campo}.");
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(
        campo => $"Debe proporcionar un valor para {campo}.");
    options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(
        () => "Debe proporcionar un valor.");
    options.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor(
        valor => $"El valor '{valor}' no es válido.");
    options.ModelBindingMessageProvider.SetNonPropertyUnknownValueIsInvalidAccessor(
        () => "El valor no es válido.");
    options.ModelBindingMessageProvider.SetUnknownValueIsInvalidAccessor(
        valor => $"El valor '{valor}' no es válido.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(
        valor => $"El valor '{valor}' no es válido.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
        campo => $"{campo} debe ser un número.");
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
        _ => "El campo es obligatorio.");
});

var culturaGuatemala = new CultureInfo("es-GT");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(culturaGuatemala);
    options.SupportedCultures = [culturaGuatemala];
    options.SupportedUICultures = [culturaGuatemala];
});

// CONFIGURAR SERVICIO DE CORREO
builder.Services.Configure<ConfiguracionCorreo>(
    builder.Configuration.GetSection("Correo")
);

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".SabIA.Ciclo";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

//INYECIÓN DE DEPENDENCIAS DE SERVICIOS DEL SISTEMA
builder.Services.AddScoped<IServicioCorreo, ServicioCorreo>();
builder.Services.AddScoped<IUsuarioServicio, UsuarioServicio>();
builder.Services.AddScoped<IAutenticacionServicio, AutenticacionServicio>();
builder.Services.AddScoped<IRolServicio, RolServicio>();
builder.Services.AddScoped<IPermisoServicio, PermisoServicio>();
builder.Services.AddScoped<ICuentaAcademicaServicio, CuentaAcademicaServicio>();
builder.Services.AddScoped<IEntregaPendienteServicio, EntregaPendienteServicio>();
builder.Services.AddScoped<IArchivoFisicoServicio, ArchivoFisicoServicio>();
builder.Services.AddScoped<IAccesoAcademicoServicio, AccesoAcademicoServicio>();
builder.Services.AddScoped<IDocenteServicio, DocenteServicio>();
builder.Services.AddScoped<IAlumnoServicio, AlumnoServicio>();
builder.Services.AddScoped<IEncargadoServicio, EncargadoServicio>();
builder.Services.AddScoped<ICicloServicio, CicloServicio>();
builder.Services.AddScoped<IContextoCicloEscolar, ContextoCicloEscolarServicio>();
builder.Services.AddScoped<IPeriodoServicio, PeriodoServicio>();
builder.Services.AddScoped<IGradoServicio, GradoServicio>();
builder.Services.AddScoped<ISeccionServicio, SeccionServicio>();
builder.Services.AddScoped<ICursoServicio, CursoServicio>();
builder.Services.AddScoped<IAsignacionServicio, AsignacionServicio>();
builder.Services.AddScoped<IDelegacionAcademicaServicio, DelegacionAcademicaServicio>();
builder.Services.AddScoped<IInscripcionServicio, InscripcionServicio>();
builder.Services.AddScoped<IResolucionTrasladoAcademicoServicio, ResolucionTrasladoAcademicoServicio>();
builder.Services.AddScoped<IPertenenciaAcademicaHistoricaServicio, PertenenciaAcademicaHistoricaServicio>();
builder.Services.AddScoped<IResultadoOficialTrasladoServicio, ResultadoOficialTrasladoServicio>();
builder.Services.AddScoped<IUnidadServicio, UnidadServicio>();
builder.Services.AddScoped<IMaterialServicio, MaterialServicio>();
builder.Services.AddScoped<IPlanificacionServicio, PlanificacionServicio>();
builder.Services.AddScoped<ITareaServicio, TareaServicio>();
builder.Services.AddScoped<IEntregaServicio, EntregaServicio>();
builder.Services.AddScoped<IComentarioAcademicoServicio, ComentarioAcademicoServicio>();
builder.Services.AddScoped<ICuestionarioServicio, CuestionarioServicio>();
builder.Services.AddHostedService<FinalizadorIntentosVencidos>();
builder.Services.AddScoped<IDocumentoServicio, DocumentoServicio>();
builder.Services.AddScoped<INotificacionServicio, NotificacionServicio>();
builder.Services.AddScoped<ICalificacionServicio, CalificacionServicio>();
builder.Services.AddScoped<IDashboardServicio, DashboardServicio>();
builder.Services.AddScoped<IExploradorAcademicoServicio, ExploradorAcademicoServicio>();
builder.Services.AddScoped<IAuditoriaServicio, AuditoriaServicio>();
builder.Services.AddScoped<IAuditoriaConsultaServicio, AuditoriaConsultaServicio>();
builder.Services.Configure<ConfiguracionIa>(builder.Configuration.GetSection("InteligenciaArtificial"));
builder.Services.AddHttpClient<IIaServicio, IaServicio>((serviceProvider, client) =>
{
    var configuracion = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ConfiguracionIa>>().Value;
    client.BaseAddress = new Uri(configuracion.UrlBase.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromMinutes(5);
});

var app = builder.Build();

// ACTIVAR CABECERAS REENVIADAS AL INICIO DEL PIPELINE
app.UseForwardedHeaders();

var loggerArchivos = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("InicializacionArchivos");

InicializadorArchivos.Inicializar(
    app.Environment,
    loggerArchivos
);

using (var ambito = app.Services.CreateScope())
{
    var servicios = ambito.ServiceProvider;
    var loggerMigraciones = servicios.GetRequiredService<ILoggerFactory>()
        .CreateLogger("InicializacionMigraciones");
    // La ausencia de la opción conserva el comportamiento histórico.
    var aplicarMigracionesAlInicio = app.Configuration
        .GetValue<bool?>("Migraciones:AplicarAlInicio") ?? true;

    // 1. Aplicar migraciones automáticamente al arrancar cuando se configure.
    var context = servicios.GetRequiredService<Contexto>();
    if (aplicarMigracionesAlInicio)
    {
        loggerMigraciones.LogInformation("Las migraciones automáticas están habilitadas al iniciar.");
        await context.Database.MigrateAsync();
    }
    else
    {
        loggerMigraciones.LogInformation("Las migraciones automáticas están deshabilitadas al iniciar.");
    }

    // 2. Ejecutar seeders después de que las tablas ya existen
    await SeguridadSeeders.InicializarAsync(
        servicios,
        app.Configuration
    );
}

// Los errores HTML siempre se presentan con una vista institucional, incluso
// en Development. El middleware conserva la excepción en registros y los
// diagnósticos técnicos, sin enviarla al navegador del usuario final.
app.UseWhen(EsNavegacionHtml, rama =>
    rama.UseExceptionHandler("/Home/Error"));

// AJAX, fetch y consumidores no HTML mantienen un formato de problema y su
// estado HTTP. No se reejecutan como una vista HTML.
app.UseWhen(contexto => !EsNavegacionHtml(contexto), rama =>
    rama.UseExceptionHandler(errores => errores.Run(async contexto =>
    {
        contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
        contexto.Response.ContentType = "application/problem+json";
        await contexto.Response.WriteAsJsonAsync(new
        {
            status = StatusCodes.Status500InternalServerError,
            title = "No fue posible completar la solicitud.",
            detail = "Ocurrió un error inesperado. Intente nuevamente más tarde."
        });
    })));

if (!app.Environment.IsDevelopment())
{
    // HSTS obliga al navegador a utilizar HTTPS en ambientes de producción.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestLocalization();
app.UseRouting();
app.UseSession();
app.UseAuthentication();

// Solo las solicitudes HTML reciben una pantalla segura. AJAX, fetch y API
// conservan su código y formato de respuesta original.
app.UseWhen(EsNavegacionHtml, rama =>
    rama.UseStatusCodePagesWithReExecute("/Home/Estado/{0}"));

app.UseAuthorization();

// PERMITIR ARCHIVOS ESTÁTICOS SIN AUTENTICACIÓN
app.MapStaticAssets()
    .AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapFallbackToController("NoEncontrado", "Home")
    .AllowAnonymous();


app.Run();

static bool EsNavegacionHtml(HttpContext contexto) =>
    !string.Equals(contexto.Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) &&
    contexto.Request.Headers.Accept.Any(valor =>
        valor?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
