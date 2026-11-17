namespace Inventario.Core.Models;

public record ResultadoBenchmark(int Volumen, double SecuencialMs, double ConcurrenteMs, bool ResultadosIguales);

/// <summary>Formato del archivo que el equipo puede juntar para la visualización comparativa.</summary>
public class ResultadosLenguaje
{
    public string Lenguaje { get; set; } = "C#";
    public string Plataforma { get; set; } = ".NET 8";
    public int? LineasDeCodigoAprox { get; set; }
    public List<ResultadoBenchmark> Benchmarks { get; set; } = new();
}
