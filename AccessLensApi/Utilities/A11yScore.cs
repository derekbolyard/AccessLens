using System.Text.Json.Nodes;

namespace AccessLensApi.Utilities;

public static class A11yScore
{
    // tweak the weights here if you ever change your scoring model
    private static readonly Dictionary<string, int> ImpactWeight =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["critical"] = 5,
            ["serious"] = 3,
            ["moderate"] = 2,
            ["warning"] = 1,
            ["minor"] = 1,
            ["info"] = 1
        };

    /// <summary>
    /// Returns a 0-100 score from any JSON shape (A, D, or raw axe).
    /// </summary>
    public static int From(JsonNode root)
    {
        int penalty = 0;

        // -------- Shape A: array of { pageUrl, issues[] } -------------
        if (root is JsonArray arrShapeA)
        {
            foreach (var page in arrShapeA)
                AddIssues(page?["issues"] as JsonArray, ref penalty);
        }
        // -------- Shape D (our earlier scanner): { pages:[ {violations[]}, … ] } ----
        else if (root?["pages"] is JsonArray pagesShapeD)
        {
            foreach (var page in pagesShapeD)
                AddViolations(page?["violations"] as JsonArray, ref penalty);
        }
        // -------- Raw axe result: { violations:[ … ] } ----------------
        else
        {
            AddViolations(root?["violations"] as JsonArray, ref penalty);
        }

        return Math.Max(0, 100 - penalty);
    }

    /* ---------- helpers ------------------------------------------------------ */
    private static void AddIssues(JsonArray? issues, ref int penalty)
    {
        if (issues is null) return;
        foreach (var iss in issues)
        {
            string impact = (iss?["type"] ?? iss?["impact"])?.ToString() ?? "minor";
            penalty += ImpactWeight.TryGetValue(impact, out int w) ? w : 1;
        }
    }

    private static void AddViolations(JsonArray? vios, ref int penalty)
    {
        if (vios is null) return;
        foreach (var v in vios)
        {
            string impact = v?["impact"]?.ToString() ?? "minor";
            penalty += ImpactWeight.TryGetValue(impact, out int w) ? w : 1;
        }
    }
}
