using System.Drawing.Printing;

namespace Inventario.App;

/// <summary>Botón "Imprimir" de los reportes: abre la vista previa y de ahí se manda a la impresora.</summary>
public static class Impresion
{
    public static void Imprimir(string titulo, string[] encabezados, IReadOnlyList<string[]> filas, string? resumen = null)
    {
        int siguiente = 0;
        using var documento = new PrintDocument { DocumentName = titulo };
        documento.BeginPrint += (s, e) => siguiente = 0;
        documento.PrintPage += (s, e) =>
        {
            var g = e.Graphics!;
            var area = e.MarginBounds;
            using var fuenteTitulo = new Font("Segoe UI", 16f, FontStyle.Bold);
            using var fuenteTexto = new Font("Segoe UI", 9.5f);
            using var fuenteNegrita = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            using var fondoEncabezado = new SolidBrush(Estilo.Pizarra);
            using var formato = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

            int y = area.Top;
            g.DrawString(titulo, fuenteTitulo, Brushes.Black, area.Left, y);
            y += 36;
            if (!string.IsNullOrEmpty(resumen))
            {
                g.DrawString(resumen, fuenteNegrita, Brushes.Black, area.Left, y);
                y += 28;
            }

            float ancho = (float)area.Width / encabezados.Length;
            g.FillRectangle(fondoEncabezado, area.Left, y, area.Width, 26);
            for (int c = 0; c < encabezados.Length; c++)
                g.DrawString(encabezados[c], fuenteNegrita, Brushes.White, new RectangleF(area.Left + c * ancho + 4, y + 4, ancho - 8, 22), formato);
            y += 28;

            while (siguiente < filas.Count && y + 24 <= area.Bottom)
            {
                for (int c = 0; c < encabezados.Length; c++)
                    g.DrawString(filas[siguiente][c], fuenteTexto, Brushes.Black, new RectangleF(area.Left + c * ancho + 4, y + 2, ancho - 8, 22), formato);
                g.DrawLine(Pens.LightGray, area.Left, y + 24, area.Right, y + 24);
                y += 24;
                siguiente++;
            }
            e.HasMorePages = siguiente < filas.Count;
        };

        using var vista = new PrintPreviewDialog { Document = documento, Width = 900, Height = 700 };
        vista.ShowDialog();
    }
}
