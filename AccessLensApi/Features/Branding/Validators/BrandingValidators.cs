using FluentValidation;

namespace AccessLensApi.Features.Branding.Validators
{
    public class BrandingCreateRequestValidator : AbstractValidator<BrandingCreateRequest>
    {
        public BrandingCreateRequestValidator()
        {
            RuleFor(x => x.PrimaryColor)
                .NotEmpty()
                .Matches("^#[0-9a-fA-F]{6}$")
                .WithMessage("Primary color must be a valid hex color code (e.g., #4f46e5)");

            RuleFor(x => x.SecondaryColor)
                .NotEmpty()
                .Matches("^#[0-9a-fA-F]{6}$")
                .WithMessage("Secondary color must be a valid hex color code (e.g., #e0e7ff)");

            RuleFor(x => x.Logo)
                .Must(BeValidImage)
                .When(x => x.Logo != null)
                .WithMessage("Logo must be a valid image file (jpg, png, gif) and under 5MB");
        }

        private bool BeValidImage(IFormFile? file)
        {
            if (file == null) return true;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            return allowedExtensions.Contains(extension) && file.Length < 5 * 1024 * 1024; // 5MB
        }
    }

    public class BrandingUpdateRequestValidator : AbstractValidator<BrandingUpdateRequest>
    {
        public BrandingUpdateRequestValidator()
        {
            RuleFor(x => x.PrimaryColor)
                .NotEmpty()
                .Matches("^#[0-9a-fA-F]{6}$")
                .WithMessage("Primary color must be a valid hex color code (e.g., #4f46e5)");

            RuleFor(x => x.SecondaryColor)
                .NotEmpty()
                .Matches("^#[0-9a-fA-F]{6}$")
                .WithMessage("Secondary color must be a valid hex color code (e.g., #e0e7ff)");

            RuleFor(x => x.Logo)
                .Must(BeValidImage)
                .When(x => x.Logo != null)
                .WithMessage("Logo must be a valid image file (jpg, png, gif) and under 5MB");
        }

        private bool BeValidImage(IFormFile? file)
        {
            if (file == null) return true;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            return allowedExtensions.Contains(extension) && file.Length < 5 * 1024 * 1024; // 5MB
        }
    }
}
