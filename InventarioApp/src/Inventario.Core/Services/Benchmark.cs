using Inventario.Core.Models;

namespace Inventario.Core.Services;

/// <summary>
/// Compara el procesamiento secuencial contra el concurrente con datos generados de forma
/// determinista (misma semilla = mismos movimientos), para que el resultado sea repetible.
/// </summary>
public static class Benchmark
{
    private const int CantidadProductos = 100;

    public static InventarioService CrearServicioBase()
    {
        var servicio = new InventarioService();
        for (int i = 1; i <= CantidadProductos; i++)
        {
            servicio.RegistrarProducto(new Producto
            {
                Codigo = $"B{i:D4}",
                Nombre = $"Producto {i}",
                Categoria = Categorias.Validas[i % Categorias.Validas.Count],
                Precio = 10 + (i * 7 % 90),
                Existencia = 10_000_000,   // suficiente para que ninguna salida falle
                StockMinimo = 100
            });
        }
        return servicio;
    }

    public static List<Movimiento> GenerarMovimientos(int cantidad, int semilla = 42)
    {
        var azar = new Random(semilla);
        var lista = new List<Movimiento>(cantidad);
        for (int i = 0; i < cantidad; i++)
        {
            lista.Add(new Movimiento
            {
                Codigo = $"B{azar.Next(1, CantidadProductos + 1):D4}",
                Tipo = azar.Next(2) == 0 ? TipoMovimiento.Entrada : TipoMovimiento.Salida,
                Cantidad = azar.Next(1, 11),
                Motivo = "benchmark"
            });
        }
        return lista;
    }

    public static List<ResultadoBenchmark> Ejecutar(IEnumerable<int> volumenes)
    {
        Medir(2_000); // calentamiento: evita que la compilación JIT distorsione la primera medición
        return volumenes.Select(Medir).ToList();
    }

    private static ResultadoBenchmark Medir(int volumen)
    {
        var movimientos = GenerarMovimientos(volumen);
        var secuencial = CrearServicioBase();
        var concurrente = CrearServicioBase();

        var rs = secuencial.ProcesarMovimientos(movimientos, concurrente: false);
        var rc = concurrente.ProcesarMovimientos(movimientos, concurrente: true);

        bool iguales = secuencial.ValorTotal() == concurrente.ValorTotal()
                       && rs.Exitosos == rc.Exitosos
                       && rs.Errores.Count == rc.Errores.Count;

        return new ResultadoBenchmark(volumen, rs.Duracion.TotalMilliseconds, rc.Duracion.TotalMilliseconds, iguales);
    }
}
