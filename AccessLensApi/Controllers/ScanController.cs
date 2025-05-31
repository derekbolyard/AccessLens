using AccessLensApi.Models;
using AccessLensApi.Services;
using AccessLensApi.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace AccessLensApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScanController : ControllerBase
    {
        private readonly IA11yScanner _scanner;
        private readonly IPdfService _pdf;

        public ScanController(IA11yScanner scanner, IPdfService pdf)
        {
            _scanner = scanner;
            _pdf = pdf;
        }

        [HttpPost("starter")]
        public async Task<IActionResult> Starter([FromBody] ScanRequest req)
        {
            if (!Uri.IsWellFormedUriString(req.Url, UriKind.Absolute))
                return BadRequest("Invalid URL.");

            var siteName = "Test Site";

            var jsonArr = await _scanner.ScanFivePagesAsync(req.Url);
            var pdf = _pdf.GeneratePdf(siteName, jsonArr);

            //return Ok(new
            //{
            //    score = A11yScore.From(jsonArr),
            //    pdfUrl = pdf,
            //    raw = jsonArr
            //});

            return File(pdf, "application/pdf", "accesslens-report.pdf");
        }
    }
}
