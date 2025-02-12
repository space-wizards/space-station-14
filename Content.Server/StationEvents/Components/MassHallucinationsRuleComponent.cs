using Content.Server.StationEvents.Events;
using Robust.Shared.Audio;
using Robust.Shared.Collections;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(MassHallucinationsRule))]
public sealed partial class MassHallucinationsRuleComponent : Component
{
    /// <summary>
    /// The maximum time between incidents in seconds
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), ViewVariables(required: true), ViewVariables(VVAccess.ReadWrite)]
    public float MaxTimeBetweenIncidents;

    /// <summary>
    /// The minimum time between incidents in seconds
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), ViewVariables(required: true), ViewVariables(VVAccess.ReadWrite)]
    public float MinTimeBetweenIncidents;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), ViewVariables(required: true), ViewVariables(VVAccess.ReadWrite)]
    public float MaxSoundDistance;

    [DataField(required: true)]
    public SoundSpecifier Sounds = default!;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> AffectedEntities = new();
}
