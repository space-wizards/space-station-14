using Content.Shared.Trigger.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Trigger.Components;

/// <summary>
/// After being triggered applies the specified components and runs triggers again.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TwoStageTriggerComponent : Component
{
    /// <summary>
    /// The keys that will activate the timer and add the given components (first stage).
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> KeysIn = new() { TriggerSystem.DefaultTriggerKey };

    /// <summary>
    /// The key that will trigger once the timer is finished (second stage).
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? KeyOut = "stageTwo";

    /// <summary>
    /// How long it takes for the second stage to be triggered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan TriggerDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// This list of components that will be added on the first trigger.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// The time at which the second stage will trigger.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextTriggerTime;

    /// <summary>
    /// Has this entity been triggered already?
    /// Used to prevent the components from being added multiple times.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Triggered = false;

    /// <summary>
    /// The entity that activated this trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? User;
}
