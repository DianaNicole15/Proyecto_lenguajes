using Inventario.Core.Models;

namespace Inventario.Core.Services;

public record ResumenInventario(
    int TotalProductos,
    int UnidadesTotales,
    decimal ValorTotal,
    int Agotados,
    int BajoInventario,
    IReadOnlyDictionary<string, int> ProductosPorCategoria);

/// <summary>Reportes construidos con LINQ y expresiones lambda.</summary>
public class ReporteService
{
    private readonly InventarioService _inventario;

    public ReporteService(InventarioService inventario)
    {
        _inventario = inventario;
    }

    public List<Producto> Disponibles() =>
        _inventario.Listar().Where(p => p.Existencia > 0).OrderBy(p => p.Nombre).ToList();

    public List<Producto> Agotados() =>
        _inventario.Listar().Where(p => p.Existencia == 0).OrderBy(p => p.Nombre).ToList();

    public List<Producto> ConBajoInventario() =>
        _inventario.Listar().Where(p => p.BajoInventario).OrderBy(p => p.Existencia).ToList();

    public List<Producto> DeMayorValor(int cuantos = 5) =>
        _inventario.Listar().OrderByDescending(p => p.ValorTotal).Take(cuantos).ToList();

    public decimal ValorTotal() => _inventario.ValorTotal();

    /// <summary>Resumen estadístico del inventario.</summary>
    public ResumenInventario Resumen()
    {
        var productos = _inventario.Listar();
        return new ResumenInventario(
            productos.Count,
            productos.Sum(p => p.Existencia),
            productos.Sum(p => p.ValorTotal),
            productos.Count(p => p.Existencia == 0),
            productos.Count(p => p.BajoInventario),
            productos.GroupBy(p => p.Categoria).ToDictionary(g => g.Key, g => g.Count()));
    }
}
