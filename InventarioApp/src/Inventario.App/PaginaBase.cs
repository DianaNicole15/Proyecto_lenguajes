using Inventario.Core.Services;

namespace Inventario.App;

/// <summary>Base de cada pantalla: acceso a los servicios y aviso de que los datos cambiaron.</summary>
public abstract class PaginaBase : UserControl
{
    protected InventarioService Inv { get; }
    protected ReporteService Reportes { get; }

    /// <summary>Se dispara cuando la pantalla cambió datos y las demás deben refrescarse.</summary>
    public event Action? DatosCambiaron;

    protected PaginaBase(InventarioService inventario, ReporteService reportes)
    {
        Inv = inventario;
        Reportes = reportes;
        Dock = DockStyle.Fill;
        BackColor = Color.White;
    }

    protected void Notificar() => DatosCambiaron?.Invoke();

    public virtual void Refrescar() { }
}
