using Content.Shared.Storage;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(RandomRuleSystem))]
public sealed partial class RandomRuleComponent : Component
{
    /// <summary>
    /// The gamerules that get added at random.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> SelectableGameRules = new();

    /// <summary>
    /// The maximum gamerules that get added at a time.
    /// </summary>
    [DataField]
    public int MaxRules = 1;

    /// <summary>
    /// The minimum gamerules that get added at a time.
    /// </summary>
    [DataField]
    public int MinRules = 1;
}
