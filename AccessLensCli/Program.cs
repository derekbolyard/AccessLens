using System.Text.Json;
using System.Text.RegularExpressions;
using AccessLensCli;

const int TOTAL_RULES_TESTED = 68;
var severityMap = new Dictionary<string, (string Label, int Rank)> {
    ["critical"] = ("Critical", 0),
    ["serious"]  = ("Serious", 1),
    ["error"]    = ("Serious", 1),
    ["major"]    = ("Major", 2),
    ["warning"]  = ("Moderate", 3),
    ["minor"]    = ("Minor", 4),
    ["info"]     = ("Info", 5)
};

string TrimHtml(string? s)
{
    if (string.IsNullOrEmpty(s)) return "";
    string t = Regex.Replace(s, @"\s+", " ").Trim();
    if (t.Length > 80) t = t.Substring(0,80) + "…";
    return t;
}

List<Issue> LoadIssues(JsonElement root)
{
    var issues = new List<Issue>();
    if (root.ValueKind == JsonValueKind.Array)
    {
        foreach (var page in root.EnumerateArray())
            LoadPage(page, issues);
    }
    else if (root.ValueKind == JsonValueKind.Object)
    {
        if (root.TryGetProperty("issues", out var iss))
        {
            LoadPage(root, issues);
        }
        else if (root.TryGetProperty("results", out var results))
        {
            foreach (var page in results.EnumerateObject())
            {
                var pageObj = new Dictionary<string, JsonElement> {
                    ["pageUrl"] = JsonDocument.Parse($"\"{page.Name}\"").RootElement,
                    ["issues"] = page.Value
                };
                LoadPage(JsonDocument.Parse(JsonSerializer.Serialize(pageObj)).RootElement, issues);
            }
        }
        else
        {
            throw new Exception("Unrecognised JSON format");
        }
    }
    else
    {
        throw new Exception("Unrecognised JSON format");
    }
    return issues;
}

void LoadPage(JsonElement page, List<Issue> issues)
{
    string pageUrl = page.TryGetProperty("pageUrl", out var p1) ? p1.GetString() ?? "unknown" :
                     page.TryGetProperty("url", out var p2) ? p2.GetString() ?? "unknown" :
                     "unknown";
    var issArray = page.GetProperty("issues");
    foreach (var iss in issArray.EnumerateArray())
    {
        string code = iss.GetProperty("code").GetString() ?? "";
        string rule = code.Split('.')[0];
        string message = iss.GetProperty("message").GetString() ?? "";
        string html = TrimHtml(iss.GetProperty("context").GetString() ?? "");
        string sevKey = iss.GetProperty("type").GetString()?.ToLower() ?? "";
        if (!severityMap.TryGetValue(sevKey, out var sev))
            sev = ("Other", 6);
        issues.Add(new Issue(rule, message, html, sev.Label, sev.Rank, pageUrl));
    }
}


List<Summary> Aggregate(List<Issue> issues)
{
    var groups = issues
        .GroupBy(i => new { i.Rule, i.IssueText, i.Severity, i.Rank })
        .Select(g => new Summary(g.Key.Rule, g.Key.IssueText, g.Key.Severity,
            g.Select(x => x.Page).Distinct().Count(), g.First().Page, g.Key.Rank))
        .OrderBy(s => s.Rank)
        .ThenByDescending(s => s.Pages)
        .ToList();
    return groups;
}

string SeverityCounts(IEnumerable<Summary> summaries)
{
    var order = new[]{"Critical","Serious","Major","Moderate","Minor","Info"};
    var counts = summaries.GroupBy(s=>s.Severity)
        .ToDictionary(g=>g.Key,g=>g.Count());
    return string.Join("  •  ", order.Where(o=>counts.ContainsKey(o)).Select(o=>$"{o}: {counts[o]}") );
}

void BuildPdf(string siteName, List<Summary> summaries, string outPath, int pageCount, int rulesPassed, int rulesFailed)
{
    var pdf = new PdfBuilder();
    var page1 = pdf.AddPage();
    double y = 780;
    page1.Text(70, y, 16, $"AccessGuard – Preliminary Accessibility Scan");
    y -= 25;
    page1.Text(70, y, 12, siteName);
    y -= 20;
    page1.Text(70, y, 10, $"Date: {DateTime.Today:yyyy-MM-dd}    |    Pages crawled: {pageCount}");
    y -= 30;
    page1.Text(70, y, 10, $"Rules tested: {TOTAL_RULES_TESTED}");
    y -= 15;
    page1.Text(70, y, 10, $"Rules passed: {rulesPassed}");
    y -= 15;
    page1.Text(70, y, 10, $"Rules failing: {rulesFailed}");
    y -= 20;
    page1.Text(70, y, 10, $"Severity breakdown: {SeverityCounts(summaries)}");
    y -= 25;
    page1.Text(70, y, 10, "Critical and Serious issues should be resolved first to reduce legal exposure.");

    var page2 = pdf.AddPage();
    double y2 = 800;
    var headers = new[]{"Issue","Rule","Severity","Pages","Example URL"};
    var widths = new[]{40,15,10,5,40};
    page2.Text(50, y2, 12, FormatRow(headers,widths));
    y2 -= 20;
    foreach (var row in summaries)
    {
        var values = new[]{row.Issue,row.Rule,row.Severity,row.Pages.ToString(),row.ExampleUrl};
        foreach(var line in WrapRow(values,widths))
        {
            if (y2 < 40) break; // avoid overflow
            page2.Text(50, y2, 10, line);
            y2 -= 12;
        }
        if (y2 < 40) break;
        y2 -= 4;
    }

    pdf.Save(outPath);
}

string FormatRow(string[] values, int[] widths)
{
    return string.Join(" | ", values.Select((v,i)=>v.PadRight(widths[i]).Substring(0,widths[i])));
}

IEnumerable<string> WrapRow(string[] values, int[] widths)
{
    var parts = values.Select((v,i)=>Wrap(v,widths[i])).ToArray();
    int lines = parts.Max(p=>p.Length);
    for(int line=0; line<lines; line++)
    {
        yield return string.Join(" | ", parts.Select((p,i)=> (line < p.Length ? p[line] : "").PadRight(widths[i])));
    }
}

string[] Wrap(string text, int width)
{
    if (string.IsNullOrEmpty(text)) return new[]{""};
    var list = new List<string>();
    for(int i=0;i<text.Length;i+=width)
        list.Add(text.Substring(i, Math.Min(width,text.Length-i)));
    return list.ToArray();
}

if (args.Length != 3)
{
    Console.WriteLine("Usage: AccessLensCli <Site name> results.json out.pdf");
    return;
}

string siteName = args[0];
string jsonPath = args[1];
string pdfPath = args[2];
var json = File.ReadAllText(jsonPath);
var doc = JsonDocument.Parse(json);
var issues = LoadIssues(doc.RootElement);
var summary = Aggregate(issues);
int pageCount = issues.Select(i => i.Page).Distinct().Count();
int uniqueFailedRules = summary.Select(s=>s.Rule).Distinct().Count();
int rulesPassed = TOTAL_RULES_TESTED - uniqueFailedRules;
BuildPdf(siteName, summary, pdfPath, pageCount, rulesPassed, uniqueFailedRules);
Console.WriteLine($"Report written to {pdfPath}");

record Issue(string Rule, string IssueText, string Html, string Severity, int Rank, string Page);
record Summary(string Rule, string Issue, string Severity, int Pages, string ExampleUrl, int Rank);
