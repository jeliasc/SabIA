using Proyecto_Final.Models;

namespace Proyecto_Final.Servicios.Auditoria;

public interface IAuditoriaServicio
{
    RegistroAuditoria CrearRegistro(
        string modulo, string accion, TipoEventoAuditoria tipo,
        ResultadoAuditoria resultado, string descripcion,
        string? entidad = null, string? entidadId = null,
        string? justificacion = null, string? valoresAnteriores = null,
        string? valoresNuevos = null, string? usuarioId = null);

    void AgregarATransaccion(RegistroAuditoria registro);
    Task RegistrarAsync(RegistroAuditoria registro);
    Task RegistrarIndependienteAsync(RegistroAuditoria registro);
}
