using System.Globalization;

namespace Inventario.Core.Services;

public static class Texto
{
    /// <summary>¿El texto contiene lo buscado? Ignora mayúsculas y acentos. Búsqueda vacía = siempre sí.</summary>
    public static bool Contiene(string? texto, string? buscado) =>
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(
            texto ?? "", buscado ?? "", CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
}
