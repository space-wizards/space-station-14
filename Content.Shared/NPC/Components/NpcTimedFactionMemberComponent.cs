using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.NPC.Components;

/// <summary>
/// On randomized timer, adds the wearer to a faction.
/// On a second timer, removes the wearer from said faction.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, Access(typeof(NpcTimedFactionSystem))]
public sealed partial class NpcTimedFactionComponent : Component
{
    /// <summary>
    /// How long to wait before changing the faction
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan TimeFactionChange = TimeSpan.Zero;

    /// <summary>
    /// How long to remain as the new faction
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan TimeFactionChangeBack = TimeSpan.Zero;


    [DataField]
    public TimeSpan TimeUntilFactionChange = TimeSpan.FromSeconds(300);

    [DataField]
    public TimeSpan TimeAsFaction = TimeSpan.FromSeconds(30);

    [DataField(required: true)]
    public ProtoId<NpcFactionPrototype> Faction = string.Empty;
}



// Wait X seconds
// Swap faction to B
// Wait Y seconds
// Swap faction back to A
