using System.ComponentModel;
using Inventario.Core.Models;
using Inventario.Core.Services;

namespace Inventario.App;

/// <summary>Inventario: buscador y tabla de productos (Código, Nombre, Precio, Inventario, Inv. Mínimo, Departamento).</summary>
public class InventarioPagina : PaginaBase
{
    private readonly DataGridView _grid = Estilo.CrearGrid();
    private readonly TextBox _txtBuscar = new() { TextAlign = HorizontalAlignment.Left, PlaceholderText = "Buscar por nombre" };
    private CriterioOrden _criterio = CriterioOrden.Nombre;
    private bool _ascendente = true;

    public InventarioPagina(InventarioService inventario, ReporteService reportes) : base(inventario, reportes)
    {
        var banner = new Label
        {
            Text = "Inventario",
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Estilo.AzulBanner,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0)
        };

        var buscador = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.White };
        buscador.Controls.Add(new Label
        {
            Text = "🔎",
            Font = new Font("Segoe UI Emoji", 11f),
            BackColor = Estilo.GrisClaro,
            TextAlign = ContentAlignment.MiddleCenter,
            Bounds = new Rectangle(14, 16, 40, 34)
        });
        buscador.Controls.Add(Estilo.Caja(_txtBuscar, 54, 16, 420, 34));
        _txtBuscar.TextChanged += (s, e) => Refrescar();

        Estilo.Columna(_grid, "Codigo", "Código", null, 87);
        Estilo.Columna(_grid, "Nombre", "Nombre del producto", null, 226);
        Estilo.Columna(_grid, "Precio", "Precio", "C2", 137, centrar: true);
        Estilo.Columna(_grid, "Existencia", "Inventario", "N0", 143, centrar: true);
        var minimo = Estilo.Columna(_grid, "StockMinimo", "Inv. Mínimo", "N0", 155, centrar: true);
        minimo.DefaultCellStyle.BackColor = Estilo.AzulBanner;
        Estilo.Columna(_grid, "Categoria", "Departamento", null, 203, centrar: true);
        _grid.ColumnHeaderMouseClick += Ordenar;

        var contenedor = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 0, 14, 14), BackColor = Color.White };
        contenedor.Controls.Add(_grid);

        Controls.Add(contenedor);
        Controls.Add(buscador);
        Controls.Add(banner);
        Refrescar();
    }

    public override void Refrescar()
    {
        var lista = InventarioService.Ordenar(Inv.Buscar(_txtBuscar.Text), _criterio, _ascendente).ToList();
        _grid.DataSource = new BindingList<Producto>(lista);
    }

    /// <summary>Clic en un encabezado ordena por esa columna (precio, existencia, departamento o nombre).</summary>
    private void Ordenar(object? sender, DataGridViewCellMouseEventArgs e)
    {
        var nombre = _grid.Columns[e.ColumnIndex].Name;
        if (!Enum.TryParse<CriterioOrden>(nombre, out var criterio)) return;

        _ascendente = criterio == _criterio ? !_ascendente : true;
        _criterio = criterio;
        Refrescar();
    }
}
