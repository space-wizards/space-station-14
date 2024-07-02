using Content.Shared.Destructible.Thresholds;
using Content.Shared.Storage;

namespace Content.Server.GameTicking.Rules.Components;


/// <summary>
/// Given a list of EntitySpawnEntries, selects between MinRules & MaxRules gamerules to add to the round without duplicates.
/// </summary>
[RegisterComponent, Access(typeof(RandomRuleSystem))]
public sealed partial class RandomRuleComponent : Component
{
    /// <summary>
    /// The gamerules that get added at random.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> SelectableGameRules = new();

    /// <summary>
    /// The minimum and maximum gamerules that get added at a time.
    /// </summary>
    [DataField]
    public MinMax MinMaxRules = new(1, 1);
}
