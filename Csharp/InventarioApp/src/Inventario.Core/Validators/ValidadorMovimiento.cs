using Inventario.Core.Exceptions;
using Inventario.Core.Models;

namespace Inventario.Core.Validators;

public static class ValidadorMovimiento
{
    public const int CantidadMaxima = 1_000_000;

    public static void Validar(Movimiento movimiento)
    {
        if (movimiento is null)
            throw new ValidacionException("El movimiento no puede ser nulo.");
        if (string.IsNullOrWhiteSpace(movimiento.Codigo))
            throw new ValidacionException("El movimiento necesita un código de producto.");
        if (movimiento.Cantidad <= 0)
            throw new ValidacionException("La cantidad debe ser mayor a 0.");
        if (movimiento.Cantidad > CantidadMaxima)
            throw new ValidacionException($"La cantidad no puede superar {CantidadMaxima:N0}.");
    }
}
