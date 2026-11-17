using Inventario.Core.Exceptions;

namespace Inventario.App;

/// <summary>Colores y ayudas visuales tomadas del diseño de Figma del equipo.</summary>
public static class Estilo
{
    public static readonly Color Vino = ColorTranslator.FromHtml("#6B0F3A");
    public static readonly Color Pizarra = ColorTranslator.FromHtml("#577A8C");
    public static readonly Color AzulClaro = ColorTranslator.FromHtml("#BCC9D4");
    public static readonly Color AzulBanner = ColorTranslator.FromHtml("#C5D8E4");
    public static readonly Color Beige = ColorTranslator.FromHtml("#F3EEE9");
    public static readonly Color BeigeActivo = ColorTranslator.FromHtml("#E4D8CC");
    public static readonly Color Gris = ColorTranslator.FromHtml("#D9D9D9");
    public static readonly Color GrisClaro = ColorTranslator.FromHtml("#EBEBEB");
    public static readonly Color Marron = ColorTranslator.FromHtml("#948179");

    /// <summary>Tabla con el estilo del diseño: encabezado pizarra y cuerpo azul claro.</summary>
    public static DataGridView CrearGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = AzulClaro,
            BorderStyle = BorderStyle.None,
            GridColor = Color.White,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleVertical,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 38
        };
        grid.ColumnHeadersDefaultCellStyle.BackColor = Pizarra;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Pizarra;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
        grid.DefaultCellStyle.BackColor = AzulClaro;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 10f);
        grid.DefaultCellStyle.SelectionBackColor = Pizarra;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.RowTemplate.Height = 28;
        return grid;
    }

    public static DataGridViewTextBoxColumn Columna(
        DataGridView grid, string propiedad, string titulo, string? formato, int peso, bool centrar = false)
    {
        var columna = new DataGridViewTextBoxColumn
        {
            Name = propiedad,
            DataPropertyName = propiedad,
            HeaderText = titulo,
            FillWeight = peso,
            SortMode = DataGridViewColumnSortMode.Programmatic
        };
        if (formato is not null)
            columna.DefaultCellStyle.Format = formato;
        grid.Columns.Add(columna);

        if (centrar)
        {
            columna.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            columna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }
        return columna;
    }

    /// <summary>Botón plano; por defecto gris como los del diseño.</summary>
    public static Button Boton(string texto, EventHandler alHacerClic, Color? fondo = null, Color? letra = null, float tamano = 11f)
    {
        var boton = new Button
        {
            Text = texto,
            FlatStyle = FlatStyle.Flat,
            BackColor = fondo ?? Gris,
            ForeColor = letra ?? Color.Black,
            Font = new Font("Segoe UI", tamano, FontStyle.Bold),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        boton.FlatAppearance.BorderSize = 0;
        boton.Click += alHacerClic;
        return boton;
    }

    /// <summary>Envuelve una caja de texto en un recuadro de color para que todas midan lo mismo.</summary>
    public static Panel Caja(Control control, int x, int y, int ancho, int alto, Color? fondo = null)
    {
        var panel = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(ancho, alto),
            BackColor = fondo ?? Gris,
            Padding = new Padding(8, 6, 8, 0)
        };
        control.Dock = DockStyle.Fill;
        control.Font = new Font("Segoe UI", 11f);
        if (control is TextBox texto)
        {
            texto.BorderStyle = BorderStyle.None;
            texto.BackColor = panel.BackColor;
        }
        panel.Controls.Add(control);
        return panel;
    }

    public static Label Etiqueta(string texto, int x, int y, float tamano = 11f) =>
        new()
        {
            Text = texto,
            Font = new Font("Segoe UI", tamano, FontStyle.Bold),
            Location = new Point(x, y),
            AutoSize = true
        };

    /// <summary>Lista desplegable gris (como las del diseño).</summary>
    public static ComboBox Combo(bool editable = false) =>
        new()
        {
            DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = Gris,
            Font = new Font("Segoe UI", 10.5f)
        };

    /// <summary>Ejecuta una acción y muestra los errores con un aviso. Devuelve true si todo salió bien.</summary>
    public static bool Ejecutar(Action accion)
    {
        try
        {
            accion();
            return true;
        }
        catch (InventarioException ex)
        {
            Aviso.Advertencia(ex.Message);
        }
        catch (Exception ex)
        {
            Aviso.Advertencia($"Ocurrió un error inesperado:\n{ex.Message}");
        }
        return false;
    }
}
