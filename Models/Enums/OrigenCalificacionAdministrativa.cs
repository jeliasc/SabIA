namespace Proyecto_Final.Models;

// Identifica una nota asentada sin que exista una actividad académica real.
// Se conserva incluso si el cero se rectifica, para no perder trazabilidad.
public enum OrigenCalificacionAdministrativa
{
    ManualSinEntrega = 1,
    AutomaticaAlCerrar = 2
}
