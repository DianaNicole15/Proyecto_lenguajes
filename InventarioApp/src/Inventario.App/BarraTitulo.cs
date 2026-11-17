using System.Runtime.InteropServices;

namespace Inventario.App;

/// <summary>Barra de título del diseño: texto a la izquierda y botón X vino a la derecha. Sirve para arrastrar la ventana.</summary>
public class BarraTitulo : Panel
{
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr ventana, int mensaje, int w, int l);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 2;

    public BarraTitulo(Form formulario, string titulo, Color fondo, int alto, float tamano = 11f, bool negrita = true)
    {
        Dock = DockStyle.Top;
        Height = alto;
        BackColor = fondo;

        var etiqueta = new Label
        {
            Text = titulo,
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", tamano, negrita ? FontStyle.Bold : FontStyle.Regular)
        };
        var cerrar = new Button
        {
            Text = "X",
            Dock = DockStyle.Right,
            Width = 56,
            BackColor = Estilo.Vino,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            TabStop = false,
            UseVisualStyleBackColor = false
        };
        cerrar.FlatAppearance.BorderSize = 0;
        cerrar.Click += (s, e) => formulario.Close();

        Controls.Add(etiqueta);
        Controls.Add(cerrar);

        MouseDown += (s, e) => Arrastrar(formulario, e);
        etiqueta.MouseDown += (s, e) => Arrastrar(formulario, e);
    }

    private static void Arrastrar(Form formulario, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessage(formulario.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
    }
}
