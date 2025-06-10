namespace Content.Shared.Telemetry;

public abstract class SharedBasicTelemetrySystem : EntitySystem
{
    public abstract void AddTelemetryData(string campaign, string message);
}


public static class Campaigns
{
}
