namespace Inventario.App;

public static class Dialogos
{
    /// <summary>Muestra una lista y devuelve la posición elegida (o null si cancela).</summary>
    public static int? Elegir(string titulo, IReadOnlyList<string> opciones)
    {
        using var formulario = new Form
        {
            Text = titulo,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(440, 260),
            MaximizeBox = false,
            MinimizeBox = false
        };
        var lista = new ListBox { Left = 12, Top = 12, Width = 416, Height = 190 };
        lista.Items.AddRange(opciones.Cast<object>().ToArray());
        lista.SelectedIndex = 0;
        var btnAceptar = new Button { Text = "Agregar", Left = 252, Top = 214, Width = 85, DialogResult = DialogResult.OK };
        var btnCancelar = new Button { Text = "Cancelar", Left = 343, Top = 214, Width = 85, DialogResult = DialogResult.Cancel };
        lista.DoubleClick += (s, e) => formulario.DialogResult = DialogResult.OK;

        formulario.Controls.AddRange(new Control[] { lista, btnAceptar, btnCancelar });
        formulario.AcceptButton = btnAceptar;
        formulario.CancelButton = btnCancelar;

        return formulario.ShowDialog() == DialogResult.OK && lista.SelectedIndex >= 0 ? lista.SelectedIndex : null;
    }
}
