using AccessLensApi.Models;

namespace AccessLensApi.Features.Scans.Models
{
    public class ScanResponse
    {
        public Teaser Teaser { get; set; } = new Teaser();
        public int Score { get; set; }
    }
}
