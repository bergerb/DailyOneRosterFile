using FluentValidation;

namespace DailyOneRosterFile.Api.Models;

public class UploadFileValidator : AbstractValidator<UploadFileDto>
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public UploadFileValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("No file uploaded.");

        When(x => x.File is not null, () =>
        {
            RuleFor(x => x.File!.FileName)
                .NotEmpty().WithMessage("No file uploaded.")
                .Must(name => name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Only .zip files are accepted.");

            RuleFor(x => x.File!.Length)
                .GreaterThan(0).WithMessage("No file uploaded.")
                .LessThanOrEqualTo(MaxFileSize).WithMessage("File size must not exceed 5MB.");
        });
    }
}
