using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Inventario.Core.Exceptions;
using Inventario.Core.Models;

namespace Inventario.Core.Data;

public static class ArchivoJson
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // deja los acentos legibles
        Converters = { new JsonStringEnumConverter() }
    };

    public static void GuardarProductos(string ruta, IEnumerable<Producto> productos) =>
        Guardar(ruta, productos.ToList());

    public static List<Producto> CargarProductos(string ruta) => Cargar<List<Producto>>(ruta);

    public static void GuardarMovimientos(string ruta, IEnumerable<Movimiento> movimientos) =>
        Guardar(ruta, movimientos.ToList());

    public static List<Movimiento> CargarMovimientos(string ruta) => Cargar<List<Movimiento>>(ruta);

    public static void Guardar<T>(string ruta, T datos)
    {
        var carpeta = Path.GetDirectoryName(Path.GetFullPath(ruta));
        if (!string.IsNullOrEmpty(carpeta))
            Directory.CreateDirectory(carpeta);
        File.WriteAllText(ruta, JsonSerializer.Serialize(datos, Opciones));
    }

    public static T Cargar<T>(string ruta) where T : class
    {
        try
        {
            var texto = File.ReadAllText(ruta);
            return JsonSerializer.Deserialize<T>(texto, Opciones)
                   ?? throw new InventarioException("El archivo JSON está vacío.");
        }
        catch (JsonException ex)
        {
            throw new InventarioException($"El archivo JSON no tiene el formato esperado: {ex.Message}");
        }
    }
}
