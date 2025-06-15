namespace Content.Shared.GameplayMetrics;

public abstract class SharedBasicGameplayMetricsSystem : EntitySystem
{
    public abstract void RecordMetric(string campaign, string metadata);
}

public static class Campaigns
{
    public const string MiningOre = "MiningOre";
    public const string CargoOrders = "CargoOrders";
    public const string StorageImplant = "StorageImplant";
}
