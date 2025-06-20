using Content.Server.StationEvents.Events;
using Robust.Shared.Audio;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(MassHallucinationsRule))]
public sealed partial class MassHallucinationsRuleComponent : Component
{
    /// <summary>
    /// The maximum time between incidents.
    /// </summary>
    [DataField]
    public TimeSpan MaxTimeBetweenIncidents = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The minimum time between incidents.
    /// </summary>
    [DataField]
    public TimeSpan MinTimeBetweenIncidents = TimeSpan.FromSeconds(30);

    [DataField]
    public float MaxSoundDistance;

    [DataField(required: true)]
    public SoundSpecifier Sounds;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> AffectedEntities = [];
}
