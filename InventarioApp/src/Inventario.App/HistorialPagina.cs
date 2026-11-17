using System.ComponentModel;
using System.Globalization;
using Inventario.Core.Models;
using Inventario.Core.Services;

namespace Inventario.App;

/// <summary>Historial: Productos bajos en inventario, Reporte de inventario y Reporte de movimientos.</summary>
public class HistorialPagina : PaginaBase
{
    private enum Sub { Bajos, Inventario, Movimientos }
    private record FilaMovimiento(string Codigo, string Nombre, int Habia, string Tipo, int Cantidad, string Departamento);

    private const string Todos = "Todos";
    private bool _cargando;

    private readonly Label _banner = new();
    private readonly Panel _panelBajos;
    private readonly Panel _panelInventario;
    private readonly Panel _panelMovimientos;

    // Productos bajos en inventario
    private readonly ComboBox _diaBajos = Estilo.Combo();
    private readonly ComboBox _buscarBajos = Estilo.Combo(editable: true);
    private readonly DataGridView _gridBajos = Estilo.CrearGrid();

    // Reporte de inventario
    private readonly ComboBox _deptoInventario = Estilo.Combo();
    private readonly Label _lblCosto = new() { AutoSize = true, Font = new Font("Segoe UI", 14f, FontStyle.Bold) };
    private readonly Label _lblCantidad = new() { AutoSize = true, Font = new Font("Segoe UI", 14f, FontStyle.Bold) };
    private readonly DataGridView _gridInventario = Estilo.CrearGrid();
    private List<Producto> _filasInventario = new();

    // Reporte de movimientos
    private readonly ComboBox _diaMov = Estilo.Combo();
    private readonly ComboBox _tipoMov = Estilo.Combo();
    private readonly ComboBox _buscarMov = Estilo.Combo(editable: true);
    private readonly ComboBox _deptoMov = Estilo.Combo();
    private readonly DataGridView _gridMov = Estilo.CrearGrid();
    private List<FilaMovimiento> _filasMov = new();

    public HistorialPagina(InventarioService inventario, ReporteService reportes) : base(inventario, reportes)
    {
        var botones = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.White };
        var titulos = new[] { "Productos bajos en inventario", "Reporte de inventario", "Reporte de movimientos" };
        for (int i = 0; i < titulos.Length; i++)
        {
            var sub = (Sub)i;
            var boton = Estilo.Boton(titulos[i], (s, e) => Mostrar(sub), tamano: 10f);
            boton.SetBounds(24 + i * 264, 12, 240, 36);
            botones.Controls.Add(boton);
        }

        _banner.Dock = DockStyle.Top;
        _banner.Height = 34;
        _banner.BackColor = Estilo.AzulBanner;
        _banner.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        _banner.TextAlign = ContentAlignment.MiddleLeft;
        _banner.Padding = new Padding(14, 0, 0, 0);

        _tipoMov.Items.AddRange(new object[] { Todos, "Entrada", "Salida" });
        _tipoMov.SelectedIndex = 0;

        _panelBajos = ConstruirBajos();
        _panelInventario = ConstruirInventario();
        _panelMovimientos = ConstruirMovimientos();

        var cuerpo = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        cuerpo.Controls.Add(_panelBajos);
        cuerpo.Controls.Add(_panelInventario);
        cuerpo.Controls.Add(_panelMovimientos);

        Controls.Add(cuerpo);
        Controls.Add(_banner);
        Controls.Add(botones);

        foreach (var combo in new[] { _diaBajos, _diaMov, _tipoMov, _deptoInventario, _deptoMov })
            combo.SelectedIndexChanged += (s, e) => { if (!_cargando) RefrescarReportes(); };
        foreach (var combo in new[] { _buscarBajos, _buscarMov })
            combo.TextChanged += (s, e) => { if (!_cargando) RefrescarReportes(); };

        Refrescar();
        Mostrar(Sub.Bajos);
    }

    // =====================================================================
    //  Construcción de cada reporte
    // =====================================================================

    private static Panel Contenedor(DataGridView grid, Panel? extraAbajo = null)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 10), BackColor = Color.White };
        panel.Controls.Add(grid);
        return panel;
    }

    private Panel ConstruirBajos()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

        var filtros = new Panel { Dock = DockStyle.Top, Height = 104, BackColor = Color.White };
        filtros.Controls.Add(Estilo.Etiqueta("del día:", 36, 20));
        _diaBajos.SetBounds(150, 16, 300, 30);
        filtros.Controls.Add(_diaBajos);
        filtros.Controls.Add(Estilo.Etiqueta("Buscar por nombre o categoría", 36, 62));
        _buscarBajos.SetBounds(340, 58, 300, 30);
        filtros.Controls.Add(_buscarBajos);

        Estilo.Columna(_gridBajos, "Codigo", "Código", null, 70, centrar: true);
        Estilo.Columna(_gridBajos, "Nombre", "Nombre del producto", null, 200);
        Estilo.Columna(_gridBajos, "Precio", "Precio", "C2", 100, centrar: true);
        Estilo.Columna(_gridBajos, "Existencia", "Inventario", "N0", 100, centrar: true);
        Estilo.Columna(_gridBajos, "StockMinimo", "Inv. Mínimo", "N0", 110, centrar: true).DefaultCellStyle.BackColor = Estilo.AzulBanner;
        Estilo.Columna(_gridBajos, "Categoria", "Departamento", null, 140, centrar: true);

        panel.Controls.Add(Contenedor(_gridBajos));
        panel.Controls.Add(filtros);
        return panel;
    }

    private Panel ConstruirInventario()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

        var superior = new Panel { Dock = DockStyle.Top, Height = 132, BackColor = Color.White };
        superior.Controls.Add(Estilo.Etiqueta("Costo de inventario", 36, 14));
        _lblCosto.Location = new Point(36, 42);
        superior.Controls.Add(_lblCosto);
        superior.Controls.Add(Estilo.Etiqueta("Cantidad de productos en Inventario", 340, 14));
        _lblCantidad.Location = new Point(340, 42);
        superior.Controls.Add(_lblCantidad);

        superior.Controls.Add(Estilo.Etiqueta("Departamento", 36, 88));
        _deptoInventario.SetBounds(180, 84, 300, 30);
        superior.Controls.Add(_deptoInventario);
        var imprimir = Estilo.Boton("🖨 Imprimir", (s, e) => ImprimirInventario());
        imprimir.SetBounds(510, 80, 150, 38);
        superior.Controls.Add(imprimir);

        Estilo.Columna(_gridInventario, "Codigo", "Código", null, 70, centrar: true);
        Estilo.Columna(_gridInventario, "Nombre", "Nombre del producto", null, 220);
        Estilo.Columna(_gridInventario, "Precio", "Precio", "C2", 100, centrar: true);
        Estilo.Columna(_gridInventario, "Existencia", "Existencia", "N0", 100, centrar: true);
        Estilo.Columna(_gridInventario, "StockMinimo", "Inv. Mínimo", "N0", 110, centrar: true);

        panel.Controls.Add(Contenedor(_gridInventario));
        panel.Controls.Add(superior);
        return panel;
    }

    private Panel ConstruirMovimientos()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

        var filtros = new Panel { Dock = DockStyle.Top, Height = 150, BackColor = Color.White };
        filtros.Controls.Add(Estilo.Etiqueta("Del día", 36, 20));
        _diaMov.SetBounds(270, 16, 280, 30);
        filtros.Controls.Add(_diaMov);
        filtros.Controls.Add(Estilo.Etiqueta("Movimientos", 600, 20));
        _tipoMov.SetBounds(730, 16, 230, 30);
        filtros.Controls.Add(_tipoMov);

        filtros.Controls.Add(Estilo.Etiqueta("Buscar Nombre o Categoría", 36, 62));
        _buscarMov.SetBounds(270, 58, 280, 30);
        filtros.Controls.Add(_buscarMov);

        filtros.Controls.Add(Estilo.Etiqueta("Departamento", 36, 104));
        _deptoMov.SetBounds(270, 100, 280, 30);
        filtros.Controls.Add(_deptoMov);

        Estilo.Columna(_gridMov, "Codigo", "Código", null, 70, centrar: true);
        Estilo.Columna(_gridMov, "Nombre", "Nombre del producto", null, 220);
        Estilo.Columna(_gridMov, "Habia", "Había", "N0", 80, centrar: true);
        Estilo.Columna(_gridMov, "Tipo", "Tipo", null, 90, centrar: true);
        Estilo.Columna(_gridMov, "Cantidad", "Cantidad", "N0", 90, centrar: true);
        Estilo.Columna(_gridMov, "Departamento", "Departamento", null, 140, centrar: true);

        var pie = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.White };
        var imprimir = Estilo.Boton("🖨 Imprimir", (s, e) => ImprimirMovimientos());
        imprimir.Size = new Size(150, 38);
        imprimir.Location = new Point(1004 - 150 - 24, 10);
        imprimir.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        pie.Controls.Add(imprimir);

        panel.Controls.Add(Contenedor(_gridMov));
        panel.Controls.Add(pie);
        panel.Controls.Add(filtros);
        return panel;
    }

    // =====================================================================
    //  Datos
    // =====================================================================

    private void Mostrar(Sub sub)
    {
        _panelBajos.Visible = sub == Sub.Bajos;
        _panelInventario.Visible = sub == Sub.Inventario;
        _panelMovimientos.Visible = sub == Sub.Movimientos;
        _banner.Text = sub switch
        {
            Sub.Bajos => "Productos Bajos en inventario",
            Sub.Inventario => "Reporte de inventario",
            _ => "Reporte de movimientos"
        };
    }

    public override void Refrescar()
    {
        _cargando = true;
        CargarDias(_diaBajos);
        CargarDias(_diaMov);
        CargarDepartamentos(_deptoInventario);
        CargarDepartamentos(_deptoMov);
        CargarBusqueda(_buscarBajos);
        CargarBusqueda(_buscarMov);
        _cargando = false;
        RefrescarReportes();
    }

    private void CargarDias(ComboBox combo)
    {
        var elegido = combo.SelectedItem as string;
        var dias = Inv.Historial()
            .Select(m => m.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Distinct()
            .OrderByDescending(d => d)
            .Cast<object>()
            .ToArray();
        combo.Items.Clear();
        combo.Items.Add(Todos);
        combo.Items.AddRange(dias);
        combo.SelectedItem = elegido is not null && combo.Items.Contains(elegido) ? elegido : Todos;
    }

    private void CargarDepartamentos(ComboBox combo)
    {
        var elegido = combo.SelectedItem as string;
        combo.Items.Clear();
        combo.Items.Add(Todos);
        combo.Items.AddRange(Inv.Departamentos().Cast<object>().ToArray());
        combo.SelectedItem = elegido is not null && combo.Items.Contains(elegido) ? elegido : Todos;
    }

    private void CargarBusqueda(ComboBox combo)
    {
        var texto = combo.Text;
        var sugerencias = Inv.Listar().Select(p => p.Nombre).Concat(Inv.Departamentos())
            .Distinct().OrderBy(t => t).Cast<object>().ToArray();
        combo.Items.Clear();
        combo.Items.AddRange(sugerencias);
        combo.Text = texto;
    }

    private static DateTime? DiaElegido(ComboBox combo) =>
        combo.SelectedItem is string texto
        && DateTime.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia)
            ? dia.Date
            : null;

    private static string? DepartamentoElegido(ComboBox combo) =>
        combo.SelectedItem is string texto && texto != Todos ? texto : null;

    private void RefrescarReportes()
    {
        RefrescarBajos();
        RefrescarInventario();
        RefrescarMovimientos();
    }

    private void RefrescarBajos()
    {
        var dia = DiaElegido(_diaBajos);
        var texto = _buscarBajos.Text.Trim();

        // "del día": productos que tuvieron movimientos ese día
        HashSet<string>? delDia = dia is null
            ? null
            : Inv.Historial().Where(m => m.Fecha.Date == dia.Value.Date)
                .Select(m => m.Codigo).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var lista = Reportes.ConBajoInventario()
            .Where(p => delDia is null || delDia.Contains(p.Codigo))
            .Where(p => texto.Length == 0 || Texto.Contiene(p.Nombre, texto) || Texto.Contiene(p.Categoria, texto))
            .ToList();
        _gridBajos.DataSource = new BindingList<Producto>(lista);
    }

    private void RefrescarInventario()
    {
        var departamento = DepartamentoElegido(_deptoInventario);
        _filasInventario = Inv.Listar()
            .Where(p => departamento is null || p.Categoria == departamento)
            .OrderBy(p => p.Codigo)
            .ToList();
        _gridInventario.DataSource = new BindingList<Producto>(_filasInventario);

        _lblCosto.Text = _filasInventario.Sum(p => p.ValorTotal).ToString("C2");
        _lblCantidad.Text = _filasInventario.Sum(p => p.Existencia).ToString("N0");
    }

    private void RefrescarMovimientos()
    {
        var dia = DiaElegido(_diaMov);
        var departamento = DepartamentoElegido(_deptoMov);
        var texto = _buscarMov.Text.Trim();
        var tipo = _tipoMov.SelectedItem as string;
        var productos = Inv.Listar().ToDictionary(p => p.Codigo, StringComparer.OrdinalIgnoreCase);

        _filasMov = Inv.Historial()
            .AsEnumerable().Reverse()
            .Where(m => dia is null || m.Fecha.Date == dia.Value.Date)
            .Where(m => tipo is null || tipo == Todos || m.Tipo.ToString() == tipo)
            .Select(m =>
            {
                productos.TryGetValue(m.Codigo, out var p);
                return new FilaMovimiento(m.Codigo, p?.Nombre ?? "(producto eliminado)", m.ExistenciaPrevia,
                    m.Tipo.ToString(), m.Cantidad, p?.Categoria ?? "");
            })
            .Where(f => departamento is null || f.Departamento == departamento)
            .Where(f => texto.Length == 0 || Texto.Contiene(f.Nombre, texto) || Texto.Contiene(f.Departamento, texto))
            .ToList();
        _gridMov.DataSource = new BindingList<FilaMovimiento>(_filasMov);
    }

    // =====================================================================
    //  Imprimir
    // =====================================================================

    private void ImprimirInventario() =>
        Impresion.Imprimir(
            "Reporte de inventario",
            new[] { "Código", "Nombre del producto", "Precio", "Existencia", "Inv. Mínimo" },
            _filasInventario.Select(p => new[] { p.Codigo, p.Nombre, p.Precio.ToString("C2"), p.Existencia.ToString("N0"), p.StockMinimo.ToString("N0") }).ToList(),
            $"Costo de inventario: {_lblCosto.Text}     Cantidad de productos en inventario: {_lblCantidad.Text}");

    private void ImprimirMovimientos() =>
        Impresion.Imprimir(
            "Reporte de movimientos",
            new[] { "Código", "Nombre del producto", "Había", "Tipo", "Cantidad", "Departamento" },
            _filasMov.Select(f => new[] { f.Codigo, f.Nombre, f.Habia.ToString("N0"), f.Tipo, f.Cantidad.ToString("N0"), f.Departamento }).ToList());
}
