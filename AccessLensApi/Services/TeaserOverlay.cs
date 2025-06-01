using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AccessLensApi.Services;

public static class TeaserOverlay
{
    private static readonly FontFamily Sans;

    static TeaserOverlay()
    {
        // Try to find Arial; if not present, fall back to the first system font.
        var arial = SystemFonts.Families
            .FirstOrDefault(f => f.Name.Equals("Arial", StringComparison.OrdinalIgnoreCase));

        Sans = arial.Equals(default(FontFamily))
            ? SystemFonts.Families.First()
            : arial;
    }

    public static byte[] AddOverlay(
        byte[] basePng,
        int score,
        int critical,
        int serious,
        int moderate)
    {
        const int CanvasW = 600;
        const int CanvasH = 320;
        const int ThumbW = 300;

        using var thumb = Image.Load<Rgba32>(basePng)
            .Clone(i => i.Resize(new ResizeOptions
            {
                Size = new Size(ThumbW, CanvasH),
                Mode = ResizeMode.Crop
            }));

        using var canvas = new Image<Rgba32>(CanvasW, CanvasH);

        canvas.Mutate(c =>
        {
            /* paste thumb, centred */
            int offsetY = (CanvasH - thumb.Height) / 2;
            c.DrawImage(thumb, new Point(0, offsetY), 1f);

            DrawCircles(critical, serious, moderate, offsetY, c, thumb);

            /* right-hand info bar */
            int barX = ThumbW;
            c.Fill(Color.FromRgb(27, 38, 54), new RectangleF(barX, 0, CanvasW - barX, CanvasH));

            c.DrawText(score.ToString(), Sans.CreateFont(46, FontStyle.Bold), Color.White, new PointF(barX + 40, 40));
            c.DrawText("/100", Sans.CreateFont(16), Color.White, new PointF(barX + 120, 55));

            c.DrawText($"{critical} Critical", Sans.CreateFont(20), Color.OrangeRed, new PointF(barX + 40, 120));
            c.DrawText($"{serious} Serious", Sans.CreateFont(20), Color.Gold, new PointF(barX + 40, 160));
            c.DrawText($"{moderate} Moderate", Sans.CreateFont(20), Color.CornflowerBlue,
            new PointF(barX + 40, 200));

            c.DrawText("Access Lens", Sans.CreateFont(14), Color.LightGray, new PointF(barX + 40, 280));
        });

        using var ms = new MemoryStream();
        canvas.SaveAsPng(ms);
        return ms.ToArray();
    }

    private static void DrawCircles(int critCount, int seriCount, int moderCount, int offsetY, IImageProcessingContext c, Image<Rgba32> thumb)
    {
        int markers;          // how many circles to draw
        Color markerColor;    // their fill / stroke colour

        if (critCount > 0)
        {
            markers = Math.Min(critCount, 3);
            markerColor = Color.Red;                // Critical
        }
        else if (seriCount > 0)
        {
            markers = Math.Min(seriCount, 3);
            markerColor = Color.Gold;               // Serious
        }
        else if (moderCount > 0)
        {
            markers = Math.Min(moderCount, 3);
            markerColor = Color.CornflowerBlue;      // Moderate
        }
        else
        {
            markers = 0;                        // nothing to mark
            markerColor = Color.Transparent;
        }

        /* -----------------------------------------------------------
           STEP 2 – If something to mark, set up paints & fonts
           ----------------------------------------------------------- */
        if (markers > 0)
        {
            var fill = Brushes.Solid(markerColor.WithAlpha(0.45f));   // translucent
            var border = Pens.Solid(markerColor, 2);
            var font = Sans.CreateFont(14, FontStyle.Bold);

            /* -------------------------------------------------------
               STEP 3 – Draw circles #1 … #markers
               ------------------------------------------------------- */
            for (int i = 0; i < markers; i++)
            {
                // lay them out horizontally 46 px apart
                var center = new PointF(28 + i * 46, offsetY + MathF.Min(thumb.Height, 280) * 0.15f);

                var circle = new EllipsePolygon(center, 18);
                c.Fill(fill, circle);                 // shaded interior
                c.Draw(border, circle);               // outline

                // white number in the middle
                c.DrawText((i + 1).ToString(), font, Color.White,
                           center - new PointF(6, 10));
            }
        }
    }
}
