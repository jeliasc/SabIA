namespace Proyecto_Final.Servicios.Academico;

public interface IEntregaPendienteServicio
{
    Task CrearParaTareasDisponiblesAsync(
        int alumnoId,
        int seccionId,
        CancellationToken cancellationToken = default
    );
}
