using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Oddball;

[RegisterComponent]
public sealed partial class OddballRuleComponent : Component
{
    /// <summary>
    /// The length of a round, in seconds.
    /// </summary>
    [DataField]
    public TimeSpan RoundDuration = TimeSpan.FromSeconds(300);

    /// <summary>
    /// The time when the round should end.
    /// </summary>
    [DataField]
    public TimeSpan RoundEnd;

    /// <summary>
    /// How long before the round restarts.
    /// </summary>
    [DataField]
    public TimeSpan RestartDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The user who is in the lead.
    /// </summary>
    [DataField]
    public NetUserId? Leader;

    /// <summary>
    /// The potential gear players can spawn with.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<StartingGearPrototype>> SpawnGear = [];
}
