namespace Content.Shared.GameplayMetrics;

public abstract class SharedBasicGameplayMetricsSystem : EntitySystem
{
    public abstract void RecordMetric(string name, Dictionary<string, object?> metricData, ExtraInfo extraInfo = ExtraInfo.Basic);

    // This exists so people don't start putting in like "null" or "" for if the entity prototype doesn't exist.
    public string? GetEntProtoIdOrNull(EntityUid uid)
    {
        return MetaData(uid).EntityPrototype?.ID;
    }
}

// Don't add stuff like the gamemode type, current map etc... - those should be its own metric, and you should match
// on the round number to combine them.
[Flags]
public enum ExtraInfo
{
    None = 0,

    RoundNumber = 1 << 0,
    GameTime = 1 << 1,

    Basic = RoundNumber | GameTime,
}
