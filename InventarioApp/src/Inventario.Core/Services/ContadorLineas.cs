namespace Inventario.Core.Services;

/// <summary>Cuenta líneas de código (sin líneas vacías ni comentarios de una línea).</summary>
public static class ContadorLineas
{
    /// <summary>Busca InventarioApp.sln hacia arriba y cuenta los .cs de src. Null si no lo encuentra.</summary>
    public static int? ContarDesdeSolucion()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "InventarioApp.sln")))
            carpeta = carpeta.Parent;

        return carpeta is null ? null : Contar(Path.Combine(carpeta.FullName, "src"));
    }

    public static int Contar(string carpeta)
    {
        var sep = Path.DirectorySeparatorChar;
        return Directory.EnumerateFiles(carpeta, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{sep}obj{sep}") && !f.Contains($"{sep}bin{sep}"))
            .SelectMany(f => File.ReadLines(f))
            .Count(linea =>
            {
                var t = linea.Trim();
                return t.Length > 0 && !t.StartsWith("//");
            });
    }
}
