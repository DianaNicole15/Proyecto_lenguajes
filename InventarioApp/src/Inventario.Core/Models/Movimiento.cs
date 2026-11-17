namespace Inventario.Core.Models;

/// <summary>Una entrada o salida de mercancía.</summary>
public class Movimiento
{
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string Codigo { get; set; } = "";
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }

    /// <summary>Existencia que había justo antes del movimiento (columna "Había" del reporte).</summary>
    public int ExistenciaPrevia { get; set; }
    public string Motivo { get; set; } = "";

    public Movimiento Clonar() => (Movimiento)MemberwiseClone();
}
