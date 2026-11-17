using System.ComponentModel;
using System.Globalization;
using Inventario.Core.Models;
using Inventario.Core.Services;

namespace Inventario.App;

/// <summary>Administrar: Registrar, Modificar, Eliminar y Departamentos, como en el diseño.</summary>
public class AdministrarPagina : PaginaBase
{
    private enum Vista { Registrar, Modificar, Eliminar, Departamentos }
    private record FilaDepartamento(string Icono, string Departamento);

    private Vista _vista = Vista.Registrar;
    private bool _editando;

    private readonly Label _banner = new();

    // Formulario (Registrar y, después de pedir el código, Modificar)
    private readonly Panel _panelFormulario = new();
    private readonly TextBox _txtCodigo = new();
    private readonly TextBox _txtNombre = new();
    private readonly TextBox _txtPrecio = new();
    private readonly ComboBox _cmbDepartamento = Estilo.Combo();
    private readonly NumericUpDown _numExistencia = new();
    private readonly NumericUpDown _numMinimo = new();

    // Cuadro gris "Modificar producto" / "Eliminar producto"
    private readonly Panel _panelAccion = new();
    private readonly Label _lblAccion = new();
    private readonly TextBox _txtCodigoAccion = new();

    // Departamentos
    private readonly Panel _panelDepartamentos = new();
    private readonly TextBox _txtBuscarDepto = new() { PlaceholderText = "Buscar.." };
    private readonly DataGridView _gridDeptos = Estilo.CrearGrid();
    private readonly TextBox _txtNuevoDepto = new();

    public AdministrarPagina(InventarioService inventario, ReporteService reportes) : base(inventario, reportes)
    {
        // --- Botones: cuatro del mismo tamaño y separados parejo ---
        var botones = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Color.White };
        var acciones = new (string Texto, Vista Vista)[]
        {
            ("📖 Registrar", Vista.Registrar),
            ("📝 Modificar", Vista.Modificar),
            ("🗑 Eliminar", Vista.Eliminar),
            ("🗂 Departame", Vista.Departamentos)
        };
        for (int i = 0; i < acciones.Length; i++)
        {
            var vista = acciones[i].Vista;
            var boton = Estilo.Boton(acciones[i].Texto, (s, e) => Mostrar(vista));
            boton.SetBounds(24 + i * 174, 12, 150, 40);
            botones.Controls.Add(boton);
        }

        // --- Banner azul ---
        _banner.Dock = DockStyle.Top;
        _banner.Height = 34;
        _banner.BackColor = Estilo.AzulBanner;
        _banner.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        _banner.TextAlign = ContentAlignment.MiddleLeft;
        _banner.Padding = new Padding(14, 0, 0, 0);

        var cuerpo = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        cuerpo.Controls.Add(ConstruirFormulario());
        cuerpo.Controls.Add(ConstruirPanelAccion());
        cuerpo.Controls.Add(ConstruirDepartamentos());

        Controls.Add(cuerpo);
        Controls.Add(_banner);
        Controls.Add(botones);

        Refrescar();
        Mostrar(Vista.Registrar);
    }

    // =====================================================================
    //  Construcción
    // =====================================================================

    private Panel ConstruirFormulario()
    {
        _panelFormulario.SetBounds(24, 24, 540, 400);

        // Etiquetas a la izquierda y campos alineados en la misma columna
        Fila(_panelFormulario, "Código", 0, Estilo.Caja(_txtCodigo, 150, 0, 360, 32));
        Fila(_panelFormulario, "Nombre", 44, Estilo.Caja(_txtNombre, 150, 44, 360, 32));
        Fila(_panelFormulario, "Precio", 88, Estilo.Caja(_txtPrecio, 150, 88, 360, 32));

        _cmbDepartamento.SetBounds(150, 132, 360, 32);
        Fila(_panelFormulario, "Departamento", 132, _cmbDepartamento);

        // Separador "— Inventario —"
        _panelFormulario.Controls.Add(new Panel { BackColor = Estilo.AzulClaro, Bounds = new Rectangle(0, 199, 190, 6) });
        _panelFormulario.Controls.Add(new Label
        {
            Text = "Inventario",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Bounds = new Rectangle(190, 188, 160, 28)
        });
        _panelFormulario.Controls.Add(new Panel { BackColor = Estilo.AzulClaro, Bounds = new Rectangle(350, 199, 160, 6) });

        _numExistencia.Maximum = 100_000_000;
        _numMinimo.Maximum = 1_000_000;
        foreach (var numero in new[] { _numExistencia, _numMinimo })
        {
            numero.BackColor = Estilo.Gris;
            numero.BorderStyle = BorderStyle.None;
            numero.Font = new Font("Segoe UI", 11f);
        }
        _numExistencia.SetBounds(150, 236, 260, 28);
        Fila(_panelFormulario, "Cantidad actual", 236, _numExistencia);
        _numMinimo.SetBounds(150, 282, 260, 28);
        Fila(_panelFormulario, "Inv. mínimo", 282, _numMinimo);

        var guardar = Estilo.Boton("✅ Guardar producto", (s, e) => Guardar());
        guardar.SetBounds(0, 338, 240, 44);
        _panelFormulario.Controls.Add(guardar);
        return _panelFormulario;
    }

    private static void Fila(Panel panel, string etiqueta, int y, Control campo)
    {
        panel.Controls.Add(Estilo.Etiqueta(etiqueta, 0, y + 5));
        panel.Controls.Add(campo);
    }

    private Panel ConstruirPanelAccion()
    {
        _panelAccion.SetBounds(24, 24, 400, 196);
        _panelAccion.BackColor = Estilo.Gris;

        _lblAccion.SetBounds(14, 10, 370, 26);
        _lblAccion.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        _panelAccion.Controls.Add(_lblAccion);

        _panelAccion.Controls.Add(new Label
        {
            Text = "Código del producto",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Bounds = new Rectangle(0, 54, 400, 28)
        });
        _panelAccion.Controls.Add(Estilo.Caja(_txtCodigoAccion, 14, 92, 372, 32, Estilo.GrisClaro));
        _txtCodigoAccion.KeyDown += (s, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            Aceptar();
        };

        var aceptar = Estilo.Boton("✅ Aceptar", (s, e) => Aceptar(), Estilo.Beige);
        aceptar.SetBounds(105, 140, 190, 40);
        _panelAccion.Controls.Add(aceptar);
        return _panelAccion;
    }

    private Panel ConstruirDepartamentos()
    {
        _panelDepartamentos.SetBounds(24, 18, 760, 440);

        // Izquierda: buscador y lista
        _panelDepartamentos.Controls.Add(new Label
        {
            Text = "🔎",
            Font = new Font("Segoe UI Emoji", 11f),
            BackColor = Estilo.GrisClaro,
            TextAlign = ContentAlignment.MiddleCenter,
            Bounds = new Rectangle(0, 0, 34, 32)
        });
        _panelDepartamentos.Controls.Add(Estilo.Caja(_txtBuscarDepto, 34, 0, 336, 32, Estilo.GrisClaro));
        _txtBuscarDepto.TextChanged += (s, e) => RefrescarDepartamentos();

        _gridDeptos.Dock = DockStyle.None;
        _gridDeptos.Bounds = new Rectangle(0, 44, 370, 380);
        _gridDeptos.BackgroundColor = Color.White;
        _gridDeptos.RowTemplate.Height = 32;
        _gridDeptos.DefaultCellStyle.BackColor = Estilo.GrisClaro;
        _gridDeptos.DefaultCellStyle.ForeColor = Color.Black;
        _gridDeptos.AlternatingRowsDefaultCellStyle.BackColor = Estilo.Gris;
        _gridDeptos.GridColor = Color.Silver;
        Estilo.Columna(_gridDeptos, "Icono", "", null, 10, centrar: true);
        Estilo.Columna(_gridDeptos, "Departamento", "Departamentos", null, 90);
        _panelDepartamentos.Controls.Add(_gridDeptos);

        // Derecha: "Nuevo Departamento"
        _panelDepartamentos.Controls.Add(Estilo.Etiqueta("Nuevo Departamento", 420, 0, 12f));
        _panelDepartamentos.Controls.Add(Estilo.Etiqueta("Nombre", 420, 40, 11f));
        _txtNuevoDepto.BorderStyle = BorderStyle.FixedSingle;
        _txtNuevoDepto.Font = new Font("Segoe UI", 11f);
        _txtNuevoDepto.Bounds = new Rectangle(420, 68, 290, 30);
        _txtNuevoDepto.KeyDown += (s, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            GuardarDepartamento();
        };
        _panelDepartamentos.Controls.Add(_txtNuevoDepto);

        var guardar = Estilo.Boton("✅ Guardar", (s, e) => GuardarDepartamento(), tamano: 10f);
        guardar.SetBounds(420, 112, 136, 34);
        var cancelar = Estilo.Boton("❌ Cancelar", (s, e) => _txtNuevoDepto.Clear(), tamano: 10f);
        cancelar.SetBounds(574, 112, 136, 34);
        _panelDepartamentos.Controls.Add(guardar);
        _panelDepartamentos.Controls.Add(cancelar);
        return _panelDepartamentos;
    }

    // =====================================================================
    //  Vistas
    // =====================================================================

    private void Mostrar(Vista vista)
    {
        _vista = vista;
        _editando = false;
        LimpiarFormulario();
        _txtCodigoAccion.Clear();
        _txtNuevoDepto.Clear();
        _txtBuscarDepto.Clear();

        _panelFormulario.Visible = vista == Vista.Registrar;
        _panelAccion.Visible = vista == Vista.Modificar || vista == Vista.Eliminar;
        _panelDepartamentos.Visible = vista == Vista.Departamentos;
        _lblAccion.Text = vista == Vista.Eliminar ? "Eliminar producto" : "Modificar producto";
        _banner.Text = vista switch
        {
            Vista.Registrar => "Registrar",
            Vista.Modificar => "Modificar",
            Vista.Eliminar => "Eliminar",
            _ => "Departamentos"
        };
        RefrescarDepartamentos();
    }

    private void LimpiarFormulario()
    {
        _txtCodigo.Clear();
        _txtCodigo.ReadOnly = false;
        _txtNombre.Clear();
        _txtPrecio.Clear();
        _cmbDepartamento.SelectedIndex = _cmbDepartamento.Items.Count > 0 ? 0 : -1;
        _numExistencia.Value = 0;
        _numExistencia.Enabled = true;
        _numMinimo.Value = 0;
    }

    public override void Refrescar()
    {
        var actual = _cmbDepartamento.SelectedItem as string;
        _cmbDepartamento.Items.Clear();
        _cmbDepartamento.Items.AddRange(Inv.Departamentos().Cast<object>().ToArray());
        _cmbDepartamento.SelectedItem = actual;
        if (_cmbDepartamento.SelectedIndex < 0 && _cmbDepartamento.Items.Count > 0)
            _cmbDepartamento.SelectedIndex = 0;
        RefrescarDepartamentos();
    }

    private void RefrescarDepartamentos()
    {
        var filas = Inv.Departamentos()
            .Where(d => Texto.Contiene(d, _txtBuscarDepto.Text.Trim()))
            .OrderBy(d => d)
            .Select(d => new FilaDepartamento("📁", d))
            .ToList();
        _gridDeptos.DataSource = new BindingList<FilaDepartamento>(filas);
    }

    // =====================================================================
    //  Acciones
    // =====================================================================

    private static bool LeerPrecio(string texto, out decimal precio) =>
        decimal.TryParse(texto.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out precio)
        || decimal.TryParse(texto.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out precio);

    private void Guardar()
    {
        if (!LeerPrecio(_txtPrecio.Text, out var precio))
        {
            Aviso.Advertencia("El precio debe ser un número.");
            return;
        }

        var producto = new Producto
        {
            Codigo = _txtCodigo.Text,
            Nombre = _txtNombre.Text,
            Categoria = _cmbDepartamento.SelectedItem as string ?? "",
            Precio = precio,
            Existencia = (int)_numExistencia.Value,
            StockMinimo = (int)_numMinimo.Value
        };

        bool correcto = Estilo.Ejecutar(() =>
        {
            if (_editando)
            {
                Inv.ModificarProducto(producto);
                // Si cambió la cantidad actual, la diferencia queda en el historial como entrada o salida.
                Inv.AjustarExistencia(producto.Codigo, producto.Existencia);
            }
            else
            {
                Inv.RegistrarProducto(producto);
            }
        });
        if (!correcto) return;

        Aviso.Exito("¡Registro exitoso!");
        Notificar();
        Mostrar(_vista);
    }

    private void Aceptar()
    {
        var codigo = _txtCodigoAccion.Text.Trim();
        if (codigo.Length == 0)
        {
            Aviso.Advertencia("Escribe el código del producto.");
            return;
        }

        if (_vista == Vista.Modificar)
        {
            Estilo.Ejecutar(() => CargarParaModificar(Inv.ConsultarPorCodigo(codigo)));
            return;
        }

        Estilo.Ejecutar(() =>
        {
            var producto = Inv.ConsultarPorCodigo(codigo);
            if (!Aviso.Confirmar($"¿Eliminar «{producto.Nombre}» ({producto.Codigo})?")) return;
            Inv.EliminarProducto(producto.Codigo);
            Aviso.Exito("¡Registro exitoso!");
            Notificar();
            Mostrar(Vista.Eliminar);
        });
    }

    /// <summary>Modificar: después de pedir el código se muestra el mismo formulario de Registrar con los datos cargados.</summary>
    private void CargarParaModificar(Producto p)
    {
        _editando = true;
        _panelAccion.Visible = false;
        _panelFormulario.Visible = true;

        _txtCodigo.Text = p.Codigo;
        _txtCodigo.ReadOnly = true;
        _txtNombre.Text = p.Nombre;
        _txtPrecio.Text = p.Precio.ToString("0.00", CultureInfo.CurrentCulture);
        _cmbDepartamento.SelectedItem = p.Categoria;
        _numExistencia.Value = Math.Min(p.Existencia, _numExistencia.Maximum);
        _numMinimo.Value = Math.Min(p.StockMinimo, _numMinimo.Maximum);
    }

    private void GuardarDepartamento()
    {
        if (!Estilo.Ejecutar(() => Inv.AgregarDepartamento(_txtNuevoDepto.Text))) return;

        Aviso.Exito("¡Registro exitoso!");
        _txtNuevoDepto.Clear();
        Notificar();
    }
}
