using Microsoft.Extensions.Options;

namespace HomeWizardLogger.Configurations;

public record TimescaleDbSettings
{
    public const string SectionName = "TimescaleDB";

    public bool Enabled { get; set; }
    public required string ConnectionString { get; set; }
}

public class TimescaleDbSettingsValidation : IValidateOptions<TimescaleDbSettings>
{
    public ValidateOptionsResult Validate(string? name, TimescaleDbSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            return ValidateOptionsResult.Fail("Connection string must be provided.");
        }
        
        return ValidateOptionsResult.Success;
    }
}
