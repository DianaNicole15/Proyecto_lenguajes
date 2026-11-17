using System.Drawing.Drawing2D;

namespace Inventario.App;

public enum TipoAviso { Advertencia, Exito }

/// <summary>Atajos para mostrar los avisos del diseño.</summary>
public static class Aviso
{
    public static void Advertencia(string mensaje) => Mostrar(mensaje, TipoAviso.Advertencia);
    public static void Exito(string mensaje) => Mostrar(mensaje, TipoAviso.Exito);

    public static bool Confirmar(string mensaje) =>
        MessageBox.Show(mensaje, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    private static void Mostrar(string mensaje, TipoAviso tipo)
    {
        using var aviso = new AvisoForm(mensaje, tipo);
        aviso.ShowDialog(Form.ActiveForm);
    }
}

/// <summary>Ventana "Aviso": barra blanca con X vino, franja gris, icono a la izquierda y texto centrado.</summary>
public class AvisoForm : Form
{
    public AvisoForm(string mensaje, TipoAviso tipo)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        KeyPreview = true;
        BackColor = Color.White;
        ClientSize = new Size(400, 206);
        Paint += (s, e) => e.Graphics.DrawRectangle(Pens.Silver, 0, 0, Width - 1, Height - 1);
        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape) Close();
        };

        var icono = new IconoAviso(tipo) { Location = new Point(34, 96) };
        var texto = new Label
        {
            Text = mensaje,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Location = new Point(124, 66),
            Size = new Size(262, 128),
            TextAlign = ContentAlignment.MiddleCenter
        };
        var franja = new Panel { Dock = DockStyle.Top, Height = 26, BackColor = Estilo.Gris };

        Controls.Add(icono);
        Controls.Add(texto);
        Controls.Add(franja);
        Controls.Add(new BarraTitulo(this, "Aviso", Color.White, 30, 11f, negrita: false));
    }

    /// <summary>Triángulo amarillo con "!" o cuadro verde con palomita, dibujados a mano.</summary>
    private class IconoAviso : Control
    {
        private readonly TipoAviso _tipo;

        public IconoAviso(TipoAviso tipo)
        {
            _tipo = tipo;
            Size = new Size(76, 76);
            DoubleBuffered = true;
            BackColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_tipo == TipoAviso.Advertencia)
            {
                using var amarillo = new SolidBrush(Color.FromArgb(255, 238, 0));
                g.FillPolygon(amarillo, new[] { new PointF(38, 4), new PointF(73, 68), new PointF(3, 68) });
                using var fuente = new Font("Segoe UI", 28f, FontStyle.Bold);
                using var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("!", fuente, Brushes.Black, new RectangleF(0, 24, 76, 46), formato);
            }
            else
            {
                using var camino = CuadroRedondeado(new Rectangle(4, 4, 68, 68), 16);
                using var verde = new SolidBrush(Color.FromArgb(46, 176, 62));
                g.FillPath(verde, camino);
                using var pluma = new Pen(Color.White, 8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                g.DrawLines(pluma, new[] { new PointF(19, 40), new PointF(33, 54), new PointF(58, 22) });
            }
        }

        private static GraphicsPath CuadroRedondeado(Rectangle r, int radio)
        {
            int d = radio * 2;
            var camino = new GraphicsPath();
            camino.AddArc(r.X, r.Y, d, d, 180, 90);
            camino.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            camino.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            camino.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            camino.CloseFigure();
            return camino;
        }
    }
}
