using Inventario.Core.Data;
using Inventario.Core.Services;

namespace Inventario.App;

/// <summary>Ventana principal: barra de título, barra de navegación beige y las cuatro pantallas del diseño.</summary>
public class MainForm : Form
{
    private readonly InventarioService _inventario = new();
    private readonly ReporteService _reportes;
    private readonly List<(Button Boton, PaginaBase Pagina)> _paginas = new();
    private readonly Panel _contenido = new() { Dock = DockStyle.Fill, BackColor = Color.White };

    public MainForm(string usuario)
    {
        Text = $"Punto de venta (sesión: {usuario})";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1004, 650);
        BackColor = Color.White;
        Paint += (s, e) => e.Graphics.DrawRectangle(Pens.Silver, 0, 0, Width - 1, Height - 1);

        _reportes = new ReporteService(_inventario);

        var navegacion = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Estilo.Beige };
        Agregar(navegacion, "🛒 Caja/venta", new CajaPagina(_inventario, _reportes));
        Agregar(navegacion, "📝 Administrar", new AdministrarPagina(_inventario, _reportes));
        Agregar(navegacion, "📊 Historial", new HistorialPagina(_inventario, _reportes));
        Agregar(navegacion, "📚 Inventario", new InventarioPagina(_inventario, _reportes));

        Controls.Add(_contenido);
        Controls.Add(navegacion);
        Controls.Add(new BarraTitulo(this, "Punto de venta", Color.White, 34, 12f));

        CargarDatosIniciales();
        RefrescarTodo();
        Mostrar(0);
    }

    /// <summary>Botones de navegación del mismo tamaño y con la misma separación.</summary>
    private void Agregar(Panel navegacion, string texto, PaginaBase pagina)
    {
        int indice = _paginas.Count;
        var boton = Estilo.Boton(texto, (s, e) => Mostrar(indice), Estilo.Beige, tamano: 9.0f);
        boton.FlatAppearance.BorderSize = 1;
        boton.FlatAppearance.BorderColor = Color.DimGray;
        boton.SetBounds(24 + indice * 160, 7, 134, 34);
        navegacion.Controls.Add(boton);

        pagina.Visible = false;
        pagina.DatosCambiaron += RefrescarTodo;
        _contenido.Controls.Add(pagina);
        _paginas.Add((boton, pagina));
    }

    private void Mostrar(int indice)
    {
        for (int i = 0; i < _paginas.Count; i++)
        {
            _paginas[i].Pagina.Visible = i == indice;
            _paginas[i].Boton.BackColor = i == indice ? Estilo.BeigeActivo : Estilo.Beige;
        }
        _paginas[indice].Pagina.BringToFront();
        _paginas[indice].Pagina.Refrescar();
    }

    private void RefrescarTodo()
    {
        foreach (var (_, pagina) in _paginas)
            pagina.Refrescar();
    }

    /// <summary>Al abrir se cargan los productos de ejemplo compartidos con el equipo (data/productos.json).</summary>
    private void CargarDatosIniciales()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "data", "productos.json");
        if (!File.Exists(ruta)) return;
        Estilo.Ejecutar(() => _inventario.ImportarProductos(ArchivoJson.CargarProductos(ruta)));
    }
}
