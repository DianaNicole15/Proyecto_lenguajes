using Inventario.Core.Exceptions;
using Inventario.Core.Models;
using Inventario.Core.Services;
using Xunit;

namespace Inventario.Tests;

public class DepartamentoYAjusteTests
{
    private static Producto Nuevo(string categoria = "Papelería", int existencia = 10) =>
        new Producto { Codigo = "X1", Nombre = "Producto", Categoria = categoria, Precio = 10m, Existencia = existencia, StockMinimo = 2 };

    // ---------- Departamentos ----------

    [Fact]
    public void Departamentos_ArrancaConLasCuatroOficiales()
    {
        var servicio = new InventarioService();
        Assert.Equal(4, servicio.Departamentos().Count);
    }

    [Fact]
    public void AgregarDepartamento_PermiteRegistrarProductosEnEl()
    {
        var servicio = new InventarioService();
        Assert.Throws<ValidacionException>(() => servicio.RegistrarProducto(Nuevo("Juguetes")));

        servicio.AgregarDepartamento("Juguetes");
        servicio.RegistrarProducto(Nuevo("juguetes"));

        Assert.Equal("Juguetes", servicio.ConsultarPorCodigo("X1").Categoria);
    }

    [Fact]
    public void AgregarDepartamento_RechazaRepetidosYVacios()
    {
        var servicio = new InventarioService();
        Assert.Throws<ValidacionException>(() => servicio.AgregarDepartamento("ELECTRONICA")); // ya existe (sin acento)
        Assert.Throws<ValidacionException>(() => servicio.AgregarDepartamento("   "));
    }

    // ---------- Ajuste de existencia y columna "Había" ----------

    [Fact]
    public void AjustarExistencia_HaciaArribaRegistraUnaEntrada()
    {
        var servicio = new InventarioService();
        servicio.RegistrarProducto(Nuevo());

        servicio.AjustarExistencia("X1", 25);

        Assert.Equal(25, servicio.ConsultarPorCodigo("X1").Existencia);
        var movimiento = servicio.Historial("X1").Single();
        Assert.Equal(TipoMovimiento.Entrada, movimiento.Tipo);
        Assert.Equal(15, movimiento.Cantidad);
        Assert.Equal(10, movimiento.ExistenciaPrevia);
    }

    [Fact]
    public void AjustarExistencia_HaciaAbajoRegistraUnaSalida()
    {
        var servicio = new InventarioService();
        servicio.RegistrarProducto(Nuevo());

        servicio.AjustarExistencia("X1", 4);

        var movimiento = servicio.Historial("X1").Single();
        Assert.Equal(TipoMovimiento.Salida, movimiento.Tipo);
        Assert.Equal(6, movimiento.Cantidad);
    }

    [Fact]
    public void AjustarExistencia_SinCambioNoRegistraNada_YNegativoSeRechaza()
    {
        var servicio = new InventarioService();
        servicio.RegistrarProducto(Nuevo());

        servicio.AjustarExistencia("X1", 10);
        Assert.Empty(servicio.Historial());
        Assert.Throws<ValidacionException>(() => servicio.AjustarExistencia("X1", -1));
    }

    [Fact]
    public void Movimiento_GuardaLaExistenciaQueHabiaAntes()
    {
        var servicio = new InventarioService();
        servicio.RegistrarProducto(Nuevo(existencia: 10));

        servicio.RegistrarSalida("X1", 3);
        servicio.RegistrarEntrada("X1", 5);

        var historial = servicio.Historial("X1");
        Assert.Equal(10, historial[0].ExistenciaPrevia); // antes de la salida
        Assert.Equal(7, historial[1].ExistenciaPrevia);  // antes de la entrada
    }
}
