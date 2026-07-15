using DailyOneRosterFile.Api.Models.Validation;

namespace DailyOneRosterFile.Api.Interfaces;

public interface IOneRosterValidator
{
    ValidationResult Validate(Stream zipStream, CancellationToken cancellationToken = default);
}
