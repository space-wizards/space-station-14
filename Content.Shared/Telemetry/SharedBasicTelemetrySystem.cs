namespace Content.Shared.Telemetry;

public abstract class SharedBasicTelemetrySystem : EntitySystem
{
    public abstract void AddTelemetryData(string campaign, string metadata);
}


public static class Campaigns
{
    public const string MiningOre = "MiningOre";
    public const string CargoOrders = "CargoOrders";
    public const string StorageImplant = "StorageImplant";
}
