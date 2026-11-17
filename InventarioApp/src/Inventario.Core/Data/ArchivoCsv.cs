using System.Globalization;
using System.Text;
using Inventario.Core.Exceptions;
using Inventario.Core.Models;

namespace Inventario.Core.Data;

/// <summary>CSV con encabezado: codigo,nombre,categoria,precio,existencia,stockMinimo</summary>
public static class ArchivoCsv
{
    private const string Encabezado = "codigo,nombre,categoria,precio,existencia,stockMinimo";
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public static void GuardarProductos(string ruta, IEnumerable<Producto> productos)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Encabezado);
        foreach (var p in productos)
        {
            sb.AppendLine(string.Join(",",
                Escapar(p.Codigo),
                Escapar(p.Nombre),
                Escapar(p.Categoria),
                p.Precio.ToString("0.00", Cultura),
                p.Existencia.ToString(Cultura),
                p.StockMinimo.ToString(Cultura)));
        }
        File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(true)); // con BOM para que Excel lea los acentos
    }

    public static List<Producto> CargarProductos(string ruta)
    {
        var lineas = File.ReadAllLines(ruta, Encoding.UTF8);
        if (lineas.Length == 0)
            throw new InventarioException("El archivo CSV está vacío.");

        var productos = new List<Producto>();
        for (int i = 1; i < lineas.Length; i++) // la línea 0 es el encabezado
        {
            if (string.IsNullOrWhiteSpace(lineas[i])) continue;

            var c = Dividir(lineas[i]);
            if (c.Count != 6)
                throw new InventarioException($"Línea {i + 1}: se esperaban 6 columnas y hay {c.Count}.");

            if (!decimal.TryParse(c[3], NumberStyles.Number, Cultura, out var precio)
                || !int.TryParse(c[4], NumberStyles.Integer, Cultura, out var existencia)
                || !int.TryParse(c[5], NumberStyles.Integer, Cultura, out var minimo))
                throw new InventarioException($"Línea {i + 1}: precio, existencia o stock mínimo no son números válidos.");

            productos.Add(new Producto
            {
                Codigo = c[0],
                Nombre = c[1],
                Categoria = c[2],
                Precio = precio,
                Existencia = existencia,
                StockMinimo = minimo
            });
        }
        return productos;
    }

    private static string Escapar(string texto) =>
        texto.Contains(',') || texto.Contains('"')
            ? "\"" + texto.Replace("\"", "\"\"") + "\""
            : texto;

    public static void GuardarTabla(string ruta, string[] encabezados, IEnumerable<string[]> filas)
{
    var sb = new StringBuilder();
    sb.AppendLine(string.Join(",", encabezados.Select(Escapar)));
    foreach (var fila in filas)
        sb.AppendLine(string.Join(",", fila.Select(Escapar)));
    File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(true));
}

    private static List<string> Dividir(string linea)
    {
        var campos = new List<string>();
        var actual = new StringBuilder();
        bool entreComillas = false;

        for (int i = 0; i < linea.Length; i++)
        {
            char c = linea[i];
            if (entreComillas)
            {
                if (c == '"')
                {
                    if (i + 1 < linea.Length && linea[i + 1] == '"') { actual.Append('"'); i++; }
                    else entreComillas = false;
                }
                else actual.Append(c);
            }
            else if (c == '"') entreComillas = true;
            else if (c == ',') { campos.Add(actual.ToString()); actual.Clear(); }
            else actual.Append(c);
        }
        campos.Add(actual.ToString());
        return campos;
    }
}
