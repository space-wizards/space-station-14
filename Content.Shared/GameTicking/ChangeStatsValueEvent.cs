using Robust.Shared.Serialization;

namespace Content.Shared.GameTicking;

/// <summary>
/// Event that raised to transfer a certain int to a certain statistic prototype
/// </summary>
[ByRefEvent]
public sealed class ChangeStatsValueEvent(string key, int amount) : EntityEventArgs
{
    public string Key = key;
    public int Amount = amount;
}
