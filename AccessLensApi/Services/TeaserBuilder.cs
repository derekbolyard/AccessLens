using Microsoft.Playwright;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text.Json.Nodes;


namespace AccessLensApi.Services;

public static class TeaserBuilder
{
    private static readonly FontFamily Sans;

    static TeaserBuilder()
    {
        // Try to find Arial; if not present, fall back to the first system font.
        var arial = SystemFonts.Families
            .FirstOrDefault(f => f.Name.Equals("Arial", StringComparison.OrdinalIgnoreCase));

        Sans = arial.Equals(default(FontFamily))
            ? SystemFonts.Families.First()
            : arial;
    }

    /// <summary>
    /// Builds a 600 × 320 teaser PNG. Pass ONLY the violations for the first page.
    /// </summary>
    public static async Task<byte[]> BuildAsync(
        IPage page,
        JsonArray violations,
        int score,
        int criticalCount,
        int seriousCount)
    {
        /* 1 ────── Screenshot ─────────────────────────────────────────────── */
        byte[] screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true,
            Type = ScreenshotType.Png
        });

        using var thumb = Image.Load<Rgba32>(screenshot)
                               .Clone(i => i.Resize(300, 320));        // fit left half

        using var canvas = new Image<Rgba32>(600, 320);
        canvas.Mutate(c =>
        {
            /* 1️⃣  ─ thumbnail (keep aspect ratio, center) ─────────────── */
            const int thumbBoxW = 300;
            const int thumbBoxH = 320;

            // resize with Max mode → no stretching
            using var fit = thumb.Clone(i => i.Resize(new ResizeOptions
            {
                Size = new Size(thumbBoxW, thumbBoxH),
                Mode = ResizeMode.Max,
                Sampler = KnownResamplers.Lanczos3
            }));

            // center it vertically if tall aspect ratio
            int offsetY = (thumbBoxH - fit.Height) / 2;
            c.DrawImage(fit, new Point(0, offsetY), 1f);

            /* 2️⃣  ─ red filled circles (visible) ─────────────────────── */
            var fillBrush = Brushes.Solid(Color.FromRgba(255, 0, 0, 110));
            var borderPen = Pens.Solid(Color.Red, 2);

            for (int i = 0; i < Math.Min(criticalCount, 3); i++)
            {
                var center = new PointF(28 + i * 46, offsetY + 28);
                var circle = new EllipsePolygon(center, 18);

                c.Fill(fillBrush, circle);   // translucent fill
                c.Draw(borderPen, circle);   // crisp outline
                c.DrawText(
                    (i + 1).ToString(),
                    Sans.CreateFont(14, FontStyle.Bold),
                    Color.White,
                    center - new PointF(6, 10));
            }

            /* 3️⃣  ─ right-hand score bar (unchanged except moved down) ─ */
            var barX = thumbBoxW;
            c.Fill(Color.FromRgb(27, 38, 54), new RectangleF(barX, 0, 300, 320));

            var bigFont = Sans.CreateFont(46, FontStyle.Bold);
            var smallFont = Sans.CreateFont(20);

            c.DrawText(score.ToString(), bigFont, Color.White, new PointF(barX + 40, 40));
            c.DrawText("/100", Sans.CreateFont(16), Color.White, new PointF(barX + 120, 55));

            c.DrawText($"{criticalCount} Critical", smallFont, Color.OrangeRed, new PointF(barX + 40, 140));
            c.DrawText($"{seriousCount} Serious", smallFont, Color.Gold, new PointF(barX + 40, 180));

            c.DrawText("Access Lens", Sans.CreateFont(14), Color.LightGray, new PointF(barX + 40, 280));
        });


        using var ms = new MemoryStream();
        canvas.SaveAsPng(ms);
        return ms.ToArray();            // ready to upload / return in API
    }
}
