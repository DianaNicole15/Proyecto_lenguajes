using Inventario.Core.Exceptions;
using Inventario.Core.Models;

namespace Inventario.Core.Validators;

public static class ValidadorProducto
{
    /// <summary>
    /// Valida el producto. Además limpia espacios y normaliza la categoría
    /// (por eso modifica el objeto recibido).
    /// </summary>
    public static void Validar(Producto producto, IEnumerable<string>? departamentos = null)
    {
        if (producto is null)
            throw new ValidacionException("El producto no puede ser nulo.");

        producto.Codigo = (producto.Codigo ?? "").Trim();
        producto.Nombre = (producto.Nombre ?? "").Trim();

        if (producto.Codigo.Length == 0)
            throw new ValidacionException("El código es obligatorio.");
        if (producto.Codigo.Length > 20 || producto.Codigo.Contains(' '))
            throw new ValidacionException("El código no puede tener espacios ni más de 20 caracteres.");
        if (producto.Nombre.Length == 0)
            throw new ValidacionException("El nombre es obligatorio.");

        var permitidos = departamentos?.ToList() ?? Categorias.Validas.ToList();
        var categoria = Categorias.Normalizar(producto.Categoria, permitidos);
        if (categoria is null)
            throw new ValidacionException(
                $"Departamento inválido. Use uno de: {string.Join(", ", permitidos)}.");
        producto.Categoria = categoria;

        if (producto.Precio <= 0)
            throw new ValidacionException("El precio debe ser mayor a 0.");
        if (producto.Existencia < 0)
            throw new ValidacionException("La existencia no puede ser negativa.");
        if (producto.StockMinimo < 0)
            throw new ValidacionException("El stock mínimo no puede ser negativo.");
    }
}
