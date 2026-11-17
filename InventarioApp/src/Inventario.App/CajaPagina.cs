using System.ComponentModel;
using Inventario.Core.Models;
using Inventario.Core.Services;

namespace Inventario.App;

/// <summary>Caja/venta: se arma el carrito por código o por nombre y "Cobrar" descuenta el inventario.</summary>
public class CajaPagina : PaginaBase
{
    private class LineaCarrito
    {
        public string Codigo { get; init; } = "";
        public string Producto { get; init; } = "";
        public decimal Precio { get; init; }
        public int Cantidad { get; set; }
        public int Existencia { get; init; }
        public string Categoria { get; init; } = "";
    }

    private readonly BindingList<LineaCarrito> _carrito = new();
    private readonly DataGridView _grid = Estilo.CrearGrid();
    private readonly TextBox _txtCodigo = new() { TextAlign = HorizontalAlignment.Left, PlaceholderText = "Añadir por codigo" };
    private readonly TextBox _txtNombre = new() { TextAlign = HorizontalAlignment.Left, PlaceholderText = "Buscar producto con nombre" };
    private readonly Label _lblTotal = new();

    public CajaPagina(InventarioService inventario, ReporteService reportes) : base(inventario, reportes)
    {
        // --- Código y búsqueda ---
        var superior = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.White };
        superior.Controls.Add(Estilo.Etiqueta("Código del producto:", 7, 13, 12f));
        superior.Controls.Add(new Label
        {
            Size = new Size(34, 32)
        });
        superior.Controls.Add(Estilo.Caja(_txtCodigo, 232, 12, 410, 32));
        superior.Controls.Add(new Label
        {
            Text = "🔎",
            Font = new Font("Segoe UI Emoji", 11f),
            BackColor = Estilo.GrisClaro,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(24, 56),
            Size = new Size(34, 32)
        });
        superior.Controls.Add(Estilo.Caja(_txtNombre, 58, 56, 584, 32));

        _txtCodigo.KeyDown += (s, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            var codigo = _txtCodigo.Text.Trim();
            _txtCodigo.Clear();
            if (codigo.Length > 0) AgregarPorCodigo(codigo);
        };
        _txtNombre.KeyDown += (s, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            var texto = _txtNombre.Text.Trim();
            _txtNombre.Clear();
            if (texto.Length > 0) AgregarPorNombre(texto);
        };

        // --- Tabla: Código | Producto | Precio | Cantidad | Existencia | Categoría ---
        _grid.ReadOnly = false;
        Estilo.Columna(_grid, "Codigo", "Código", null, 81).ReadOnly = true;
        Estilo.Columna(_grid, "Producto", "Producto", null, 480).ReadOnly = true;
        Estilo.Columna(_grid, "Precio", "Precio", "C2", 102, centrar: true).ReadOnly = true;
        Estilo.Columna(_grid, "Cantidad", "Cantidad", "N0", 119, centrar: true);   // la única editable
        Estilo.Columna(_grid, "Existencia", "Existencia", "N0", 116, centrar: true).ReadOnly = true;
        Estilo.Columna(_grid, "Categoria", "Categoría", null, 106, centrar: true).ReadOnly = true;
        _grid.DataSource = _carrito;
        _grid.CellValidating += ValidarCantidad;
        _grid.CellEndEdit += CantidadEditada;
        _grid.DataError += (s, e) => e.ThrowException = false;
        _grid.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Delete) QuitarSeleccionado();
        };

        // --- Cobrar y total ---
        var inferior = new Panel { Dock = DockStyle.Bottom, Height = 104, BackColor = Color.White };
        var cobrar = Estilo.Boton("Cobrar", (s, e) => Cobrar(), tamano: 17f);
        cobrar.Size = new Size(146, 46);
        _lblTotal.Size = new Size(206, 74);
        _lblTotal.Font = new Font("Segoe UI", 28f, FontStyle.Bold);
        _lblTotal.TextAlign = ContentAlignment.MiddleCenter;
        _lblTotal.BorderStyle = BorderStyle.FixedSingle;
        inferior.Controls.Add(cobrar);
        inferior.Controls.Add(_lblTotal);
        inferior.Resize += (s, e) =>
        {
            _lblTotal.Location = new Point(inferior.Width - _lblTotal.Width - 20, 14);
            cobrar.Location = new Point(_lblTotal.Left - cobrar.Width - 26, 28);
        };

        Controls.Add(_grid);
        Controls.Add(superior);
        Controls.Add(inferior);

        VisibleChanged += (s, e) => { if (Visible) _txtCodigo.Focus(); };
        ActualizarTotal();
    }

    // ---------- Agregar productos ----------

    private void AgregarPorCodigo(string codigo)
    {
        Estilo.Ejecutar(() => AgregarAlCarrito(Inv.ConsultarPorCodigo(codigo)));
        _txtCodigo.Focus();
    }

    private void AgregarPorNombre(string texto)
    {
        var encontrados = Inv.Buscar(texto);
        if (encontrados.Count == 0)
        {
            Aviso.Advertencia($"No se encontró ningún producto con «{texto}».");
            return;
        }

        var elegido = encontrados[0];
        if (encontrados.Count > 1)
        {
            var opciones = encontrados.Select(p => $"{p.Codigo} — {p.Nombre} ({p.Existencia} en existencia)").ToList();
            var indice = Dialogos.Elegir("Varios productos coinciden", opciones);
            if (indice is null) return;
            elegido = encontrados[indice.Value];
        }
        AgregarAlCarrito(elegido);
    }

    private void AgregarAlCarrito(Producto producto)
    {
        if (producto.Existencia == 0)
        {
            Aviso.Advertencia($"«{producto.Nombre}» está agotado.");
            return;
        }

        var existente = _carrito.FirstOrDefault(l => l.Codigo.Equals(producto.Codigo, StringComparison.OrdinalIgnoreCase));
        if (existente is null)
        {
            _carrito.Add(new LineaCarrito
            {
                Codigo = producto.Codigo,
                Producto = producto.Nombre,
                Precio = producto.Precio,
                Cantidad = 1,
                Existencia = producto.Existencia,
                Categoria = producto.Categoria
            });
        }
        else
        {
            if (existente.Cantidad + 1 > existente.Existencia)
            {
                Aviso.Advertencia("¡Alerta cantidad mayor a la existente!");
                return;
            }
            existente.Cantidad++;
            _carrito.ResetItem(_carrito.IndexOf(existente));
        }
        ActualizarTotal();
    }

    // ---------- Edición de la cantidad ----------

    private void ValidarCantidad(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        if (_grid.Columns[e.ColumnIndex].Name != "Cantidad") return;

        var fila = _grid.Rows[e.RowIndex];
        if (!int.TryParse(e.FormattedValue?.ToString(), out var cantidad) || cantidad < 1)
        {
            e.Cancel = true;
            fila.ErrorText = "La cantidad debe ser un número entero mayor a 0.";
        }
        else
        {
            fila.ErrorText = "";
        }
    }

    private void CantidadEditada(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _carrito.Count) return;

        var linea = _carrito[e.RowIndex];
        if (linea.Cantidad > linea.Existencia)
        {
            linea.Cantidad = linea.Existencia;
            _carrito.ResetItem(e.RowIndex);
            Aviso.Advertencia("¡Alerta cantidad mayor a la existente!");
        }
        ActualizarTotal();
    }

    private void QuitarSeleccionado()
    {
        if (_grid.CurrentRow?.DataBoundItem is LineaCarrito linea)
        {
            _carrito.Remove(linea);
            ActualizarTotal();
        }
    }

    private void ActualizarTotal() =>
        _lblTotal.Text = _carrito.Sum(l => l.Precio * l.Cantidad).ToString("C2");

    // ---------- Cobrar ----------

    private void Cobrar()
    {
        if (_carrito.Count == 0)
        {
            Aviso.Advertencia("Agrega productos antes de cobrar.");
            return;
        }

        ResultadoVenta? resultado = null;
        var lineas = _carrito.Select(l => new LineaVenta(l.Codigo, l.Cantidad)).ToList();
        if (!Estilo.Ejecutar(() => resultado = Inv.RealizarVenta(lineas))) return;

        _carrito.Clear();
        ActualizarTotal();
        Notificar();

        Aviso.Exito("¡Registro exitoso!");
        if (resultado!.ProductosPorAgotarse.Count > 0)
        {
            var nombres = string.Join("\n", resultado.ProductosPorAgotarse.Select(p => p.Nombre));
            Aviso.Advertencia("¡Alerta Producto por agotarse!\n" + nombres);
        }
        _txtCodigo.Focus();
    }
}
