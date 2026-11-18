using System.Text.Json.Serialization;

namespace Inventario.Core.Models;

/// <summary>Un producto del inventario.</summary>
public class Producto
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";
    public decimal Precio { get; set; }
    public int Existencia { get; set; }
    public int StockMinimo { get; set; }

    // Propiedades calculadas (no se guardan en los archivos).
    [JsonIgnore] public decimal ValorTotal => Precio * Existencia;

    /// <summary>Regla compartida con el equipo: bajo inventario = existencia &lt;= stock mínimo.</summary>
    [JsonIgnore] public bool BajoInventario => Existencia <= StockMinimo;

    [JsonIgnore]
    public string Estado => Existencia == 0 ? "Agotado" : BajoInventario ? "Bajo inventario" : "OK";

    public Producto Clonar() => (Producto)MemberwiseClone();
}
