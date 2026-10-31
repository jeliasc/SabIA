using System.Security.Claims;
using Proyecto_Final.ViewModels.ExploradorAcademico;

namespace Proyecto_Final.Servicios.ExploradorAcademico;

public interface IExploradorAcademicoServicio
{
    Task<ExploradorAcademicoIndice?> ObtenerIndiceAsync();
    Task<ExploradorGradoAcademico?> ObtenerGradoAsync(int gradoId);
    Task<ExploradorSeccionAcademico?> ObtenerSeccionAsync(int seccionId, ClaimsPrincipal usuario);
    Task<ExploradorCursoAcademico?> ObtenerCursoAsync(int asignacionId, ClaimsPrincipal usuario);
}
