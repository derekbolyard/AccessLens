using Microsoft.Playwright;
using System.Text.Json.Nodes;

namespace AccessLensApi.Services;

public static class ScreenshotHelper
{
    /// <summary>
    /// Capture a teaser screenshot. If the first critical violation exists,
    /// the image is cropped around that element (with padding), otherwise the current viewport is captured.
    /// Returns (pngBytes, didZoom).
    /// </summary>
    public static async Task<(byte[] png, bool zoomed)> CaptureAsync(
        IPage page,
        JsonArray violations)
    {
        // 1) Prepare default options: no Clip => full-viewport
        var options = new PageScreenshotOptions
        {
            Type = ScreenshotType.Png,
            FullPage = false
        };

        bool zoomed = false;

        /* ── Step A: Find the selector of the first critical node, if any ───────────────────── */
        string? selector = violations
            .FirstOrDefault(v => v?["impact"]?.ToString() == "critical")?
            ["nodes"]?[0]?["target"]?[0]?.ToString();

        if (!string.IsNullOrEmpty(selector))
        {
            // 2) Ask the browser for the bounding box of that element
            string? rectJson = await page.EvaluateAsync<string?>(@"
                sel => {
                  const el = document.querySelector(sel);
                  if (!el) return null;
                  const r = el.getBoundingClientRect();
                  return JSON.stringify({ x: r.x, y: r.y, w: r.width, h: r.height });
                }", selector);

            if (!string.IsNullOrEmpty(rectJson))
            {
                var rect = (JsonObject)JsonNode.Parse(rectJson)!;

                // 3) Extract values and apply padding
                const int pad = 40;
                int rawX = (int)rect["x"]!.GetValue<double>();
                int rawY = (int)rect["y"]!.GetValue<double>();
                int rawW = (int)rect["w"]!.GetValue<double>();
                int rawH = (int)rect["h"]!.GetValue<double>();

                int clipX = Math.Max(rawX - pad, 0);
                int clipY = Math.Max(rawY - pad, 0);
                int clipW = rawW + pad * 2;
                int clipH = rawH + pad * 2;

                // 4) Read the viewport size from the page
                //    (If ViewportSize is null, we fall back to evaluating window.innerWidth/Height.)
                int viewportWidth, viewportHeight;
                if (page.ViewportSize is not null)
                {
                    viewportWidth = page.ViewportSize.Width;
                    viewportHeight = page.ViewportSize.Height;
                }
                else
                {
                    // fallback: ask the browser for innerWidth/innerHeight
                    var dims = await page.EvaluateAsync<JsonObject>(@"
                        () => ({ 
                            width: window.innerWidth, 
                            height: window.innerHeight 
                        })
                    ");
                    viewportWidth = dims["width"]!.GetValue<int>();
                    viewportHeight = dims["height"]!.GetValue<int>();
                }

                // 5) Clamp the clip so it never exceeds page bounds
                //    (If clipX+clipW goes beyond viewport, shorten clipW, same for height.)
                if (clipX + clipW > viewportWidth)
                    clipW = viewportWidth - clipX;
                if (clipY + clipH > viewportHeight)
                    clipH = viewportHeight - clipY;

                // --- 5½)  Decide if crop would be “too zoomed” -----------------------------
                // element % of viewport area
                double elementAreaPct = (rawW * rawH) / (double)(viewportWidth * viewportHeight);

                // element width/height % of viewport
                double elementWpct = rawW / (double)viewportWidth;
                double elementHpct = rawH / (double)viewportHeight;

                // thresholds: tweak to taste
                const double MIN_AREA_PCT = 0.25;   // 25 % of viewport area
                const double MIN_DIM_PCT = 0.50;   // 50 % of width OR height

                bool cropWouldBeTiny = elementAreaPct < MIN_AREA_PCT &&
                                       elementWpct < MIN_DIM_PCT &&
                                       elementHpct < MIN_DIM_PCT;

                if (cropWouldBeTiny)
                {
                    // force full-viewport – ignore selector
                    selector = null;
                }

                // 6) Ensure the final clipped box is still valid
                bool isValidClip = clipW > 0 && clipH > 0
                                   && clipX >= 0 && clipY >= 0
                                   && clipX + clipW <= viewportWidth
                                   && clipY + clipH <= viewportHeight;

                if (isValidClip && selector != null)
                {
                    options.Clip = new()
                    {
                        X = clipX,
                        Y = clipY,
                        Width = clipW,
                        Height = clipH
                    };
                    zoomed = true;
                }
                // If isValidClip is false, we’ll simply leave `options.Clip == null`
                // and zoomed remains false → fallback to viewport screenshot.
            }
        }

        /* ── Step B: Take the screenshot (either clipped or full-viewport) ─────────────────── */
        byte[] png = await page.ScreenshotAsync(options);
        return (png, zoomed);
    }
}
