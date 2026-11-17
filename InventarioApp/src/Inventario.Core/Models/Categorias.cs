using System.Globalization;

namespace Inventario.Core.Models;

/// <summary>Categorías oficiales acordadas por el equipo (las mismas en los tres lenguajes).</summary>
public static class Categorias
{
    public static readonly IReadOnlyList<string> Validas =
        new[] { "Electrónica", "Papelería", "Alimentos", "Limpieza" };

    /// <summary>Devuelve la categoría oficial (ignorando mayúsculas y acentos) o null si no existe.</summary>
    public static string? Normalizar(string? categoria) => Normalizar(categoria, Validas);

    /// <summary>Igual que el anterior, pero contra una lista de departamentos dada.</summary>
    public static string? Normalizar(string? categoria, IEnumerable<string> permitidas)
    {
        var texto = (categoria ?? "").Trim();
        return permitidas.FirstOrDefault(v => Comparar(v, texto));
    }

    private static bool Comparar(string a, string b) =>
        CultureInfo.InvariantCulture.CompareInfo.Compare(
            a, b, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;
}
