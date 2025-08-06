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
    /// When the toggled faction will be added.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan TimeFactionChange = TimeSpan.Zero;

    /// <summary>
    /// When the toggled faction will be removed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan TimeFactionChangeBack = TimeSpan.Zero;

    /// <summary>
    /// How long to wait between the faction toggling on.
    /// </summary>
    [DataField]
    public TimeSpan TimeUntilFactionChange = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Random extra seconds of waiting up to:.
    /// </summary>
    [DataField]
    public int RandomBonusTimeUntilFactionChange = 0;

    /// <summary>
    /// How long to remain as the new faction.
    /// </summary>
    [DataField]
    public TimeSpan TimeAsFaction = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Random extra seconds as new faction up to:.
    /// </summary>
    [DataField]
    public int RandomBonusTimeAsFaction = 0;

    /// <summary>
    /// The faction to be toggled on and off.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<NpcFactionPrototype> Faction = "Mouse"; //string.Empty;

    [DataField]
    public bool HasChangedOnce = false;
}
