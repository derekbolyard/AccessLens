using AccessLensApi.Features.Reports.Models;

namespace AccessLensApi.Features.Scans.Models
{
    public class ScanResponse
    {
        public TeaserDto Teaser { get; set; } = new TeaserDto();
        public int Score { get; set; }
    }
}
