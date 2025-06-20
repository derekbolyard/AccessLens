namespace AccessLensApi.Features.Checkout.Models
{
    public record CheckoutRequest(string Email, Guid ScanId);
}
