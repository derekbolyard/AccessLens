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

            var json = await _scanner.ScanFivePagesAsync(req.Url);

            // credit/payment checks already done inside scanner or middleware
            int score = A11yScore.From(json["pages"]![0]!);      // or pass upfront
            string pdfUrl = await _pdf.GenerateAndUploadPdf(req.Url, json["pages"]![0]!);

            return Ok(new
            {
                score,
                pdfUrl,
                teaserUrl = (string?)json["teaserUrl"] ?? ""
            });

            //return File(pdf, "application/pdf", "accesslens-report.pdf");
        }
    }
}
