namespace HomeWizardLogger.Models;

public static class MeasurementTimestamp
{
    public static DateTime ResolveUtc(DateTime? timestamp, DateTimeOffset receivedAt, TimeZoneInfo timezone)
    {
        if (timestamp == null)
            return receivedAt.UtcDateTime;

        if (timestamp.Value.Kind != DateTimeKind.Unspecified)
            return timestamp.Value.ToUniversalTime();

        var local = timestamp.Value;
        if (timezone.IsInvalidTime(local))
            throw new ArgumentException($"Meter timestamp {local:O} does not exist in timezone {timezone.Id}.", nameof(timestamp));

        if (!timezone.IsAmbiguousTime(local))
            return TimeZoneInfo.ConvertTimeToUtc(local, timezone);

        // The live stream's receipt time distinguishes the two occurrences of the repeated hour.
        return timezone.GetAmbiguousTimeOffsets(local)
            .Select(offset => new DateTimeOffset(local, offset).UtcDateTime)
            .MinBy(utc => (utc - receivedAt.UtcDateTime).Duration());
    }
}
