namespace Inventario.Core.Exceptions;

/// <summary>Base de todos los errores propios del sistema.</summary>
public class InventarioException : Exception
{
    public InventarioException(string mensaje) : base(mensaje) { }
}

public class ValidacionException : InventarioException
{
    public ValidacionException(string mensaje) : base(mensaje) { }
}

public class ProductoNoEncontradoException : InventarioException
{
    public ProductoNoEncontradoException(string codigo)
        : base($"No existe un producto con el código '{codigo}'.") { }
}

public class ProductoDuplicadoException : InventarioException
{
    public ProductoDuplicadoException(string codigo)
        : base($"Ya existe un producto con el código '{codigo}'.") { }
}

public class StockInsuficienteException : InventarioException
{
    public StockInsuficienteException(string codigo, int disponible, int solicitado)
        : base($"Stock insuficiente para '{codigo}': disponible {disponible}, solicitado {solicitado}.") { }
}
