using Microsoft.Extensions.Options;

namespace HomeWizardLogger.Configurations;

public record HomeWizardSettings
{
    public const string SectionName = "HomeWizard";
    
    public required string Address { get; set; }
    
    public required string Token { get; set; }

    public string? UniqueIdOverride { get; set; }
}

public class HomeWizardSettingsValidation : IValidateOptions<HomeWizardSettings>
{
    public ValidateOptionsResult Validate(string? name, HomeWizardSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Address))
        {
            return ValidateOptionsResult.Fail("Address must be provided.");
        }
        
        if (string.IsNullOrWhiteSpace(settings.Token))
        {
            return ValidateOptionsResult.Fail("Token must be provided.");
        }

        if (settings.UniqueIdOverride != null && string.IsNullOrWhiteSpace(settings.UniqueIdOverride))
        {
            return ValidateOptionsResult.Fail("UniqueIdOverride must be omitted or contain a non-whitespace ID.");
        }
        
        return ValidateOptionsResult.Success;
    }
}
