using Inventario.Core.Exceptions;
using Inventario.Core.Models;
using Inventario.Core.Services;
using Xunit;

namespace Inventario.Tests;

public class InventarioServiceTests
{
    private static Producto P(string codigo = "A1", string nombre = "Lápiz", string categoria = "Papelería",
                              decimal precio = 10m, int existencia = 5, int minimo = 2) =>
        new Producto { Codigo = codigo, Nombre = nombre, Categoria = categoria, Precio = precio, Existencia = existencia, StockMinimo = minimo };

    /// <summary>E1: 10 uds a $200 | P1: 0 uds a $50 (agotado) | L1: 4 uds a $25 (bajo inventario)</summary>
    private static InventarioService NuevoServicio()
    {
        var servicio = new InventarioService();
        servicio.RegistrarProducto(P("E1", "Mouse", "Electrónica", 200m, 10, 3));
        servicio.RegistrarProducto(P("P1", "Cuaderno", "Papelería", 50m, 0, 5));
        servicio.RegistrarProducto(P("L1", "Cloro", "Limpieza", 25m, 4, 5));
        return servicio;
    }

    // ---------- Casos comunes ----------

    [Fact]
    public void RegistrarProducto_PermiteConsultarloPorCodigo()
    {
        var servicio = NuevoServicio();
        var producto = servicio.ConsultarPorCodigo("E1");
        Assert.Equal("Mouse", producto.Nombre);
        Assert.Equal(10, producto.Existencia);
    }

    [Fact]
    public void RegistrarProducto_ConCodigoRepetido_LanzaExcepcion()
    {
        var servicio = NuevoServicio();
        Assert.Throws<ProductoDuplicadoException>(() => servicio.RegistrarProducto(P("E1")));
    }

    [Fact]
    public void Entrada_AumentaLaExistencia()
    {
        var servicio = NuevoServicio();
        servicio.RegistrarEntrada("E1", 5, "compra");
        Assert.Equal(15, servicio.ConsultarPorCodigo("E1").Existencia);
    }

    [Fact]
    public void Salida_DisminuyeLaExistencia()
    {
        var servicio = NuevoServicio();
        servicio.RegistrarSalida("E1", 4, "venta");
        Assert.Equal(6, servicio.ConsultarPorCodigo("E1").Existencia);
    }

    [Fact]
    public void Salida_MayorAlInventario_LanzaExcepcionYNoCambiaNada()
    {
        var servicio = NuevoServicio();
        Assert.Throws<StockInsuficienteException>(() => servicio.RegistrarSalida("L1", 5));
        Assert.Equal(4, servicio.ConsultarPorCodigo("L1").Existencia);
        Assert.Empty(servicio.Historial("L1"));
    }

    [Fact]
    public void Movimiento_DeProductoInexistente_LanzaExcepcion()
    {
        var servicio = NuevoServicio();
        Assert.Throws<ProductoNoEncontradoException>(() => servicio.RegistrarEntrada("NOEXISTE", 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Movimiento_ConCantidadInvalida_LanzaValidacion(int cantidad)
    {
        var servicio = NuevoServicio();
        Assert.Throws<ValidacionException>(() => servicio.RegistrarEntrada("E1", cantidad));
    }

    [Fact]
    public void Validacion_RechazaDatosInvalidos()
    {
        var servicio = new InventarioService();
        Assert.Throws<ValidacionException>(() => servicio.RegistrarProducto(P(codigo: "  ")));
        Assert.Throws<ValidacionException>(() => servicio.RegistrarProducto(P(precio: -5m)));
        Assert.Throws<ValidacionException>(() => servicio.RegistrarProducto(P(existencia: -1)));
        Assert.Throws<ValidacionException>(() => servicio.RegistrarProducto(P(categoria: "Juguetes")));
    }

    [Fact]
    public void Modificar_CambiaDatosPeroNoLaExistencia()
    {
        var servicio = NuevoServicio();
        servicio.ModificarProducto(P("E1", "Mouse gamer", "Electrónica", 350m, 999, 4));
        var producto = servicio.ConsultarPorCodigo("E1");
        Assert.Equal("Mouse gamer", producto.Nombre);
        Assert.Equal(350m, producto.Precio);
        Assert.Equal(10, producto.Existencia); // la existencia solo cambia con movimientos
    }

    [Fact]
    public void Eliminar_QuitaElProducto()
    {
        var servicio = NuevoServicio();
        servicio.EliminarProducto("P1");
        Assert.Throws<ProductoNoEncontradoException>(() => servicio.ConsultarPorCodigo("P1"));
    }

    // ---------- Búsqueda, orden y reportes ----------

    [Fact]
    public void Buscar_PorNombreOCategoria_IgnoraMayusculasYAcentos()
    {
        var servicio = NuevoServicio();
        Assert.Single(servicio.Buscar("mouse"));
        Assert.Equal("P1", servicio.Buscar("papeleria").Single().Codigo);
        Assert.Equal(3, servicio.Buscar("").Count);
    }

    [Fact]
    public void Ordenar_PorPrecioAscendenteYDescendente()
    {
        var servicio = NuevoServicio();
        var asc = InventarioService.Ordenar(servicio.Listar(), CriterioOrden.Precio, true).Select(p => p.Codigo);
        var desc = InventarioService.Ordenar(servicio.Listar(), CriterioOrden.Precio, false).Select(p => p.Codigo);
        Assert.Equal(new[] { "L1", "P1", "E1" }, asc);
        Assert.Equal(new[] { "E1", "P1", "L1" }, desc);
    }

    [Fact]
    public void ValorTotal_EsLaSumaDePrecioPorExistencia()
    {
        var servicio = NuevoServicio();
        Assert.Equal(2100m, servicio.ValorTotal()); // 200*10 + 50*0 + 25*4
    }

    [Fact]
    public void Reportes_ClasificanLosProductos()
    {
        var reportes = new ReporteService(NuevoServicio());
        Assert.Equal(new[] { "L1", "E1" }, reportes.Disponibles().Select(p => p.Codigo).OrderByDescending(c => c));
        Assert.Equal("P1", reportes.Agotados().Single().Codigo);
        Assert.Equal(2, reportes.ConBajoInventario().Count); // P1 (0<=5) y L1 (4<=5)
        Assert.Equal("E1", reportes.DeMayorValor(1).Single().Codigo);
    }

    [Fact]
    public void Salida_QueDejaBajoInventario_DisparaLaAlerta()
    {
        var servicio = NuevoServicio();
        Producto? alertado = null;
        servicio.BajoInventarioDetectado += p => alertado = p;

        servicio.RegistrarSalida("E1", 8); // 10 - 8 = 2 <= 3

        Assert.NotNull(alertado);
        Assert.Equal("E1", alertado!.Codigo);
    }

    // ---------- Historial y procesamiento por lote ----------

    [Fact]
    public void Historial_RegistraYFiltraPorCodigo()
    {
        var servicio = NuevoServicio();
        servicio.RegistrarEntrada("E1", 2);
        servicio.RegistrarSalida("E1", 1);
        servicio.RegistrarEntrada("L1", 1);
        Assert.Equal(2, servicio.Historial("E1").Count);
        Assert.Equal(3, servicio.Historial().Count);
    }

    [Fact]
    public void ProcesarMovimientos_ReportaErroresSinDetenerElResto()
    {
        var servicio = NuevoServicio();
        var movimientos = new List<Movimiento>
        {
            new() { Codigo = "E1", Tipo = TipoMovimiento.Entrada, Cantidad = 5 },
            new() { Codigo = "L1", Tipo = TipoMovimiento.Salida, Cantidad = 100 }, // falla
            new() { Codigo = "X9", Tipo = TipoMovimiento.Entrada, Cantidad = 1 }   // falla
        };
        var resultado = servicio.ProcesarMovimientos(movimientos, concurrente: false);
        Assert.Equal(1, resultado.Exitosos);
        Assert.Equal(2, resultado.Errores.Count);
    }

    [Fact]
    public void Secuencial_Y_Concurrente_DanElMismoResultado()
    {
        var movimientos = Benchmark.GenerarMovimientos(5_000);
        var a = Benchmark.CrearServicioBase();
        var b = Benchmark.CrearServicioBase();

        var ra = a.ProcesarMovimientos(movimientos, concurrente: false);
        var rb = b.ProcesarMovimientos(movimientos, concurrente: true);

        Assert.Equal(ra.Exitosos, rb.Exitosos);
        Assert.Equal(a.ValorTotal(), b.ValorTotal());
        Assert.Equal(a.Historial().Count, b.Historial().Count);
    }
}
