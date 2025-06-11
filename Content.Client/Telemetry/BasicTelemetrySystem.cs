using Content.Shared.Telemetry;

namespace Content.Client.Telemetry;

public sealed class BasicTelemetrySystem : SharedBasicTelemetrySystem
{
    public override void AddTelemetryData(string campaign, string metadata)
    {
        // no op
    }
}
