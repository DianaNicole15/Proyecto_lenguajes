using Inventario.Core.Exceptions;
using Inventario.Core.Models;
using Inventario.Core.Services;
using Xunit;

namespace Inventario.Tests;

public class VentaTests
{
    /// <summary>E1: 10 uds a $200 (mínimo 3) | L1: 4 uds a $25 (mínimo 5)</summary>
    private static InventarioService NuevoServicio()
    {
        var servicio = new InventarioService();
        servicio.RegistrarProducto(new Producto { Codigo = "E1", Nombre = "Mouse", Categoria = "Electrónica", Precio = 200m, Existencia = 10, StockMinimo = 3 });
        servicio.RegistrarProducto(new Producto { Codigo = "L1", Nombre = "Cloro", Categoria = "Limpieza", Precio = 25m, Existencia = 4, StockMinimo = 5 });
        return servicio;
    }

    [Fact]
    public void RealizarVenta_DescuentaExistenciasYCalculaElTotal()
    {
        var servicio = NuevoServicio();
        var resultado = servicio.RealizarVenta(new[] { new LineaVenta("E1", 2), new LineaVenta("L1", 1) });

        Assert.Equal(425m, resultado.Total); // 2*200 + 1*25
        Assert.Equal(8, servicio.ConsultarPorCodigo("E1").Existencia);
        Assert.Equal(3, servicio.ConsultarPorCodigo("L1").Existencia);
        Assert.Equal(2, servicio.Historial().Count);
    }

    [Fact]
    public void RealizarVenta_ConUnaLineaSinStock_NoCambiaNada()
    {
        var servicio = NuevoServicio();
        Assert.Throws<StockInsuficienteException>(() =>
            servicio.RealizarVenta(new[] { new LineaVenta("E1", 1), new LineaVenta("L1", 5) }));

        Assert.Equal(10, servicio.ConsultarPorCodigo("E1").Existencia); // todo o nada
        Assert.Empty(servicio.Historial());
    }

    [Fact]
    public void RealizarVenta_SumaLasLineasRepetidasDelMismoProducto()
    {
        var servicio = NuevoServicio();
        Assert.Throws<StockInsuficienteException>(() =>
            servicio.RealizarVenta(new[] { new LineaVenta("E1", 6), new LineaVenta("E1", 5) })); // 11 > 10
    }

    [Fact]
    public void RealizarVenta_ReportaLosProductosPorAgotarse()
    {
        var servicio = NuevoServicio();
        var resultado = servicio.RealizarVenta(new[] { new LineaVenta("E1", 8) }); // quedan 2 <= 3
        Assert.Equal("E1", resultado.ProductosPorAgotarse.Single().Codigo);
    }

    [Fact]
    public void RealizarVenta_SinProductos_LanzaValidacion()
    {
        var servicio = NuevoServicio();
        Assert.Throws<ValidacionException>(() => servicio.RealizarVenta(Array.Empty<LineaVenta>()));
    }
}
