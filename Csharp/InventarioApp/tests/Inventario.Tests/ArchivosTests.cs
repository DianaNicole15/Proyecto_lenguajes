using Inventario.Core.Data;
using Inventario.Core.Exceptions;
using Inventario.Core.Models;
using Xunit;

namespace Inventario.Tests;

public class ArchivosTests
{
    private static List<Producto> Ejemplo() => new()
    {
        new Producto { Codigo = "A1", Nombre = "Café, molido", Categoria = "Alimentos", Precio = 145.5m, Existencia = 5, StockMinimo = 10 },
        new Producto { Codigo = "A2", Nombre = "Tornillo 1/2\"", Categoria = "Limpieza", Precio = 3m, Existencia = 100, StockMinimo = 20 }
    };

    private static void ConArchivoTemporal(string extension, Action<string> prueba)
    {
        var ruta = Path.Combine(Path.GetTempPath(), $"inv_{Guid.NewGuid():N}{extension}");
        try { prueba(ruta); }
        finally { if (File.Exists(ruta)) File.Delete(ruta); }
    }

    [Fact]
    public void Json_GuardarYCargar_ConservaLosDatos()
    {
        ConArchivoTemporal(".json", ruta =>
        {
            ArchivoJson.GuardarProductos(ruta, Ejemplo());
            var cargados = ArchivoJson.CargarProductos(ruta);
            Assert.Equal(2, cargados.Count);
            Assert.Equal("Café, molido", cargados[0].Nombre);
            Assert.Equal(145.5m, cargados[0].Precio);
        });
    }

    [Fact]
    public void Csv_GuardarYCargar_ManejaComasYComillas()
    {
        ConArchivoTemporal(".csv", ruta =>
        {
            ArchivoCsv.GuardarProductos(ruta, Ejemplo());
            var cargados = ArchivoCsv.CargarProductos(ruta);
            Assert.Equal(2, cargados.Count);
            Assert.Equal("Café, molido", cargados[0].Nombre);
            Assert.Equal("Tornillo 1/2\"", cargados[1].Nombre);
            Assert.Equal(145.5m, cargados[0].Precio);
        });
    }

    [Fact]
    public void Json_Invalido_LanzaExcepcionDelSistema()
    {
        ConArchivoTemporal(".json", ruta =>
        {
            File.WriteAllText(ruta, "esto no es json");
            Assert.Throws<InventarioException>(() => ArchivoJson.CargarProductos(ruta));
        });
    }

    [Fact]
    public void Csv_ConNumeroInvalido_LanzaExcepcionDelSistema()
    {
        ConArchivoTemporal(".csv", ruta =>
        {
            File.WriteAllText(ruta, "codigo,nombre,categoria,precio,existencia,stockMinimo\nA1,Lápiz,Papelería,abc,5,2\n");
            Assert.Throws<InventarioException>(() => ArchivoCsv.CargarProductos(ruta));
        });
    }
}
