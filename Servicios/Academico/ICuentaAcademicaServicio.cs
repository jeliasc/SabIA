using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Personas;
using Proyecto_Final.ViewModels.Usuarios;

namespace Proyecto_Final.Servicios.Academico;

public interface ICuentaAcademicaServicio
{
    Task<ResultadoOperacion<CuentaAcademicaCreada>> CrearAsync(
        FormularioPersona modelo,
        string rol);

    Task<ResultadoOperacion> ActualizarAsync(
        Usuario usuario,
        FormularioPersona modelo);

    Task EnviarCredencialesAsync(
        Usuario usuario,
        string contrasenaTemporal);

    ResultadoContrasenaTemporal CrearResultadoCredenciales(
        Usuario usuario,
        string contrasenaTemporal);
}

public sealed class CuentaAcademicaCreada
{
    public Usuario Usuario { get; init; } = null!;
    public string ContrasenaTemporal { get; init; } = string.Empty;
}
