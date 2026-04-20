using System.Runtime.InteropServices;

namespace HomeWizardLogger;

public static class Const
{
    public const string UserAgent = "HomeWizardLogger/1.0";

    public static readonly object Headers = new
    {
        User_Agent = UserAgent,
        Accept = "application/json"
    };
    
    private const string TimezoneWindows = "FLE Standard Time";

    private const string TimezoneLinux = "Europe/Helsinki";

    public static readonly string TimezoneId =
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? TimezoneLinux : TimezoneWindows;
}