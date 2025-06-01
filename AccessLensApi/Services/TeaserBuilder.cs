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
            /* 2 ────── Paste thumbnail ─────────────────────────────────── */
            c.DrawImage(thumb, new Point(0, 0), 1f);

            /* 3 ────── Draw THREE red circles as "Critical markers" ────── */
            var pen = Pens.Solid(Color.FromRgba(255, 0, 0, 180), 3);
            for (int i = 0; i < Math.Min(criticalCount, 3); i++)
            {
                var center = new PointF(30 + i * 45, 30);
                var circle = new EllipsePolygon(center, 18);
                c.Draw(pen, circle);
                c.DrawText(
                    (i + 1).ToString(),
                    Sans.CreateFont(14, FontStyle.Bold),
                    Color.White,
                    center - new PointF(6, 10));  // crude centre
            }

            /* 4 ────── Right-hand score bar ────────────────────────────── */
            var barX = 300;   // start of right half
            c.Fill(Color.FromRgb(27, 38, 54), new RectangleF(barX, 0, 300, 320));

            var bigFont = Sans.CreateFont(46, FontStyle.Bold);
            var smallFont = Sans.CreateFont(20);

            // Score
            c.DrawText(score.ToString(), bigFont, Color.White, new PointF(barX + 40, 40));
            c.DrawText("/100", Sans.CreateFont(16), Color.White, new PointF(barX + 40 + 80, 55));

            // Counts
            c.DrawText($"{criticalCount} Critical", smallFont, Color.OrangeRed, new PointF(barX + 40, 140));
            c.DrawText($"{seriousCount} Serious", smallFont, Color.Gold, new PointF(barX + 40, 180));

            // Logo
            c.DrawText("Access Lens", Sans.CreateFont(14), Color.LightGray, new PointF(barX + 40, 280));
        });

        using var ms = new MemoryStream();
        canvas.SaveAsPng(ms);
        return ms.ToArray();            // ready to upload / return in API
    }
}
