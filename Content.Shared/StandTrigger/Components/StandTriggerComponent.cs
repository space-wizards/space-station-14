using Content.Shared.StandTrigger.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.StandTrigger.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause, AutoGenerateComponentState(true)]
[Access(typeof(StandTriggerSystem))]
public sealed partial class StandTriggerComponent : Component
{
    /// <summary>
    /// If any entities occupy the blacklist on the same tile then trigger won't work.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// If this is true, will still trigger on entities that are in air / weightless.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreWeightless;

    /// <summary>
    /// The next time this component will trigger.
    /// </summary>
    [AutoPausedField, AutoNetworkedField]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    /// <summary>
    /// The interval between trigger updates.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
}
