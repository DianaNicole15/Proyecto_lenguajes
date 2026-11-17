using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Inventario.Core.Exceptions;
using Inventario.Core.Models;
using Inventario.Core.Validators;

namespace Inventario.Core.Services;

public enum CriterioOrden { Nombre, Precio, Existencia, Categoria }

public record ResultadoProcesamiento(int Exitosos, IReadOnlyList<string> Errores, TimeSpan Duracion);

public record ResultadoImportacion(int Importados, IReadOnlyList<string> Errores);

public record LineaVenta(string Codigo, int Cantidad);

public record ResultadoVenta(decimal Total, IReadOnlyList<Producto> ProductosPorAgotarse);

/// <summary>
/// Lógica del inventario. Usa un diccionario (búsqueda por código) y una lista (historial).
/// Todas las operaciones están protegidas con un candado para poder usarse en paralelo.
/// </summary>
public class InventarioService
{
    private readonly Dictionary<string, Producto> _productos = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Movimiento> _historial = new();
    private readonly object _candado = new();
    private readonly List<string> _departamentos = new(Categorias.Validas);

    /// <summary>Se dispara cuando una salida deja un producto en bajo inventario.</summary>
    public event Action<Producto>? BajoInventarioDetectado;

    // ---------- Productos ----------

    public void RegistrarProducto(Producto producto)
    {
        ValidadorProducto.Validar(producto, CategoriasValidas());
        lock (_candado)
        {
            if (_productos.ContainsKey(producto.Codigo))
                throw new ProductoDuplicadoException(producto.Codigo);
            _productos[producto.Codigo] = producto.Clonar();
        }
    }

    /// <summary>
    /// Modifica nombre, categoría, precio y stock mínimo. La existencia NO se cambia aquí:
    /// solo cambia con entradas y salidas, para que el historial siempre cuadre.
    /// </summary>
    public void ModificarProducto(Producto cambios)
    {
        ValidadorProducto.Validar(cambios, CategoriasValidas());
        lock (_candado)
        {
            var actual = Obtener(cambios.Codigo);
            actual.Nombre = cambios.Nombre;
            actual.Categoria = cambios.Categoria;
            actual.Precio = cambios.Precio;
            actual.StockMinimo = cambios.StockMinimo;
        }
    }

    public void EliminarProducto(string codigo)
    {
        lock (_candado)
        {
            var producto = Obtener(codigo);
            _productos.Remove(producto.Codigo);
        }
    }

    public Producto ConsultarPorCodigo(string codigo)
    {
        lock (_candado)
        {
            return Obtener(codigo).Clonar();
        }
    }

    public List<Producto> Listar()
    {
        lock (_candado)
        {
            return _productos.Values.Select(p => p.Clonar()).ToList();
        }
    }

    /// <summary>Busca por nombre o categoría (ignora mayúsculas y acentos). Texto vacío = todos.</summary>
    public List<Producto> Buscar(string? texto)
    {
        var t = (texto ?? "").Trim();
        return Listar()
            .Where(p => Contiene(p.Nombre, t) || Contiene(p.Categoria, t))
            .ToList();
    }

    /// <summary>Ordena usando una función selectora (función de orden superior).</summary>
    public static IEnumerable<Producto> Ordenar(IEnumerable<Producto> productos, CriterioOrden criterio, bool ascendente)
    {
        Func<Producto, IComparable> clave = SelectorDe(criterio);
        return ascendente ? productos.OrderBy(clave) : productos.OrderByDescending(clave);
    }

    public ResultadoImportacion ImportarProductos(IEnumerable<Producto> productos)
    {
        int importados = 0, numero = 0;
        var errores = new List<string>();
        foreach (var producto in productos)
        {
            numero++;
            try
            {
                ValidadorProducto.Validar(producto, CategoriasValidas());
                lock (_candado)
                {
                    _productos[producto.Codigo] = producto.Clonar(); // si ya existe, se reemplaza
                }
                importados++;
            }
            catch (InventarioException ex)
            {
                errores.Add($"Registro {numero}: {ex.Message}");
            }
        }
        return new ResultadoImportacion(importados, errores);
    }

    // ---------- Departamentos ----------

    public List<string> Departamentos()
    {
        lock (_candado)
        {
            return _departamentos.ToList();
        }
    }

    /// <summary>Agrega un departamento nuevo (además de los 4 oficiales).</summary>
    public void AgregarDepartamento(string nombre)
    {
        nombre = (nombre ?? "").Trim();
        if (nombre.Length == 0)
            throw new ValidacionException("El nombre del departamento es obligatorio.");
        if (nombre.Length > 40)
            throw new ValidacionException("El nombre del departamento no puede tener más de 40 caracteres.");

        lock (_candado)
        {
            if (Categorias.Normalizar(nombre, _departamentos) is not null)
                throw new ValidacionException("Ese departamento ya existe.");
            _departamentos.Add(nombre);
        }
    }

    private List<string> CategoriasValidas() => Departamentos();

    // ---------- Movimientos ----------

    public void RegistrarEntrada(string codigo, int cantidad, string motivo = "") =>
        RegistrarMovimiento(new Movimiento { Codigo = codigo, Tipo = TipoMovimiento.Entrada, Cantidad = cantidad, Motivo = motivo });

    public void RegistrarSalida(string codigo, int cantidad, string motivo = "") =>
        RegistrarMovimiento(new Movimiento { Codigo = codigo, Tipo = TipoMovimiento.Salida, Cantidad = cantidad, Motivo = motivo });

    /// <summary>
    /// Deja la existencia en el valor indicado registrando la diferencia como entrada o salida
    /// (así el historial siempre cuadra).
    /// </summary>
    public void AjustarExistencia(string codigo, int nuevaExistencia, string motivo = "Ajuste de inventario")
    {
        if (nuevaExistencia < 0)
            throw new ValidacionException("La existencia no puede ser negativa.");

        int diferencia;
        lock (_candado)
        {
            diferencia = nuevaExistencia - Obtener(codigo).Existencia;
        }

        if (diferencia > 0) RegistrarEntrada(codigo, diferencia, motivo);
        else if (diferencia < 0) RegistrarSalida(codigo, -diferencia, motivo);
    }

    /// <summary>Aplica un movimiento y actualiza la existencia. Impide salidas mayores a la existencia.</summary>
    public void RegistrarMovimiento(Movimiento movimiento)
    {
        ValidadorMovimiento.Validar(movimiento);
        Producto? alerta = null;

        lock (_candado)
        {
            var producto = Obtener(movimiento.Codigo);

            if (movimiento.Tipo == TipoMovimiento.Salida && movimiento.Cantidad > producto.Existencia)
                throw new StockInsuficienteException(producto.Codigo, producto.Existencia, movimiento.Cantidad);

            int previa = producto.Existencia;

            producto.Existencia += movimiento.Tipo == TipoMovimiento.Entrada
                ? movimiento.Cantidad
                : -movimiento.Cantidad;

            var registro = movimiento.Clonar();
            registro.Codigo = producto.Codigo;
            registro.ExistenciaPrevia = previa;
            _historial.Add(registro);

            if (movimiento.Tipo == TipoMovimiento.Salida && producto.BajoInventario)
                alerta = producto.Clonar();
        }

        if (alerta is not null)
            BajoInventarioDetectado?.Invoke(alerta);
    }

    /// <summary>
    /// Cobra un carrito: todo o nada. Si una línea supera la existencia no se descuenta nada.
    /// Cada línea queda en el historial como una salida.
    /// </summary>
    public ResultadoVenta RealizarVenta(IEnumerable<LineaVenta> lineas, string motivo = "Venta")
    {
        var agrupadas = lineas
            .GroupBy(l => (l.Codigo ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new LineaVenta(g.Key, g.Sum(l => l.Cantidad)))
            .ToList();

        if (agrupadas.Count == 0)
            throw new ValidacionException("La venta no tiene productos.");
        foreach (var linea in agrupadas)
            ValidadorMovimiento.Validar(new Movimiento { Codigo = linea.Codigo, Cantidad = linea.Cantidad });

        decimal total = 0;
        var alertas = new List<Producto>();

        lock (_candado)
        {
            foreach (var linea in agrupadas) // primero se revisa todo...
            {
                var producto = Obtener(linea.Codigo);
                if (linea.Cantidad > producto.Existencia)
                    throw new StockInsuficienteException(producto.Codigo, producto.Existencia, linea.Cantidad);
            }
            foreach (var linea in agrupadas) // ...y después se aplica
            {
                var producto = Obtener(linea.Codigo);
                int previa = producto.Existencia;
                producto.Existencia -= linea.Cantidad;
                total += producto.Precio * linea.Cantidad;
                _historial.Add(new Movimiento
                {
                    Codigo = producto.Codigo,
                    Tipo = TipoMovimiento.Salida,
                    Cantidad = linea.Cantidad,
                    ExistenciaPrevia = previa,
                    Motivo = motivo
                });
                if (producto.BajoInventario)
                    alertas.Add(producto.Clonar());
            }
        }

        foreach (var alerta in alertas)
            BajoInventarioDetectado?.Invoke(alerta);

        return new ResultadoVenta(total, alertas);
    }

    /// <summary>
    /// Procesa varios movimientos, de forma secuencial o concurrente (Parallel.ForEach).
    /// Los movimientos con error no detienen a los demás: se reportan al final.
    /// </summary>
    public ResultadoProcesamiento ProcesarMovimientos(IReadOnlyList<Movimiento> movimientos, bool concurrente)
    {
        var errores = new ConcurrentBag<string>();
        int exitosos = 0;

        void Aplicar(Movimiento m)
        {
            try
            {
                RegistrarMovimiento(m);
                Interlocked.Increment(ref exitosos);
            }
            catch (InventarioException ex)
            {
                errores.Add($"{m.Codigo} ({m.Tipo} {m.Cantidad}): {ex.Message}");
            }
        }

        var reloj = Stopwatch.StartNew();
        if (concurrente)
            Parallel.ForEach(movimientos, m => Aplicar(m));
        else
            foreach (var m in movimientos)
                Aplicar(m);
        reloj.Stop();

        return new ResultadoProcesamiento(exitosos, errores.OrderBy(e => e).ToList(), reloj.Elapsed);
    }

    public List<Movimiento> Historial(string? codigo = null)
    {
        var filtro = (codigo ?? "").Trim();
        lock (_candado)
        {
            return _historial
                .Where(m => filtro.Length == 0 || m.Codigo.Equals(filtro, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.Fecha)
                .Select(m => m.Clonar())
                .ToList();
        }
    }

    public decimal ValorTotal()
    {
        lock (_candado)
        {
            return _productos.Values.Sum(p => p.ValorTotal);
        }
    }

    // ---------- Auxiliares ----------

    private Producto Obtener(string? codigo)
    {
        var clave = (codigo ?? "").Trim();
        return _productos.TryGetValue(clave, out var producto)
            ? producto
            : throw new ProductoNoEncontradoException(clave);
    }

    private static bool Contiene(string texto, string buscado) =>
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(
            texto, buscado, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    private static Func<Producto, IComparable> SelectorDe(CriterioOrden criterio)
    {
        switch (criterio)
        {
            case CriterioOrden.Precio: return p => p.Precio;
            case CriterioOrden.Existencia: return p => p.Existencia;
            case CriterioOrden.Categoria: return p => p.Categoria;
            default: return p => p.Nombre;
        }
    }
}
