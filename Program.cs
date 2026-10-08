using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
using Proyecto_Final.Servicios.GestionCuestionarios;
using Proyecto_Final.Servicios.GestionDocumentos;
using Proyecto_Final.Servicios.GestionNotificaciones;
using Proyecto_Final.Servicios.GestionCalificaciones;
using Proyecto_Final.Servicios.InteligenciaArtificial;
using Proyecto_Final.Servicios.Dashboard;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<Contexto>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("ConexionPrincipal")
    )
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

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login/Login";
    options.LogoutPath = "/Login/CerrarSesion";
    options.AccessDeniedPath = "/Login/AccesoDenegado";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthorization(options =>
{
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
});

// CONFIGURAR SERVICIO DE CORREO
builder.Services.Configure<ConfiguracionCorreo>(
    builder.Configuration.GetSection("Correo")
);

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

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
builder.Services.AddScoped<IPeriodoServicio, PeriodoServicio>();
builder.Services.AddScoped<IGradoServicio, GradoServicio>();
builder.Services.AddScoped<ISeccionServicio, SeccionServicio>();
builder.Services.AddScoped<ICursoServicio, CursoServicio>();
builder.Services.AddScoped<IAsignacionServicio, AsignacionServicio>();
builder.Services.AddScoped<IInscripcionServicio, InscripcionServicio>();
builder.Services.AddScoped<IUnidadServicio, UnidadServicio>();
builder.Services.AddScoped<IMaterialServicio, MaterialServicio>();
builder.Services.AddScoped<IPlanificacionServicio, PlanificacionServicio>();
builder.Services.AddScoped<ITareaServicio, TareaServicio>();
builder.Services.AddScoped<IEntregaServicio, EntregaServicio>();
builder.Services.AddScoped<ICuestionarioServicio, CuestionarioServicio>();
builder.Services.AddScoped<IDocumentoServicio, DocumentoServicio>();
builder.Services.AddScoped<INotificacionServicio, NotificacionServicio>();
builder.Services.AddScoped<ICalificacionServicio, CalificacionServicio>();
builder.Services.AddScoped<IDashboardServicio, DashboardServicio>();
builder.Services.Configure<ConfiguracionIa>(builder.Configuration.GetSection("InteligenciaArtificial"));
builder.Services.AddHttpClient<IIaServicio, IaServicio>((serviceProvider, client) =>
{
    var configuracion = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ConfiguracionIa>>().Value;
    client.BaseAddress = new Uri(configuracion.UrlBase.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromMinutes(5);
});

var app = builder.Build();

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

    await SeguridadSeeders.InicializarAsync(
        servicios,
        app.Configuration
    );
}

// CONFIGURAR CANAL DE SOLICITUDES HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS obliga al navegador a utilizar HTTPS en ambientes de producción.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// PERMITIR ARCHIVOS ESTÁTICOS SIN AUTENTICACIÓN
app.MapStaticAssets()
    .AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
