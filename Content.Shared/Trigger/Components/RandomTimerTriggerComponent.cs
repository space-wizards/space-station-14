using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components;

/// <summary>
/// This is used for randomizing a <see cref="TimerTriggerComponent"/> on MapInit.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RandomTimerTriggerComponent : Component
{
    /// <summary>
    /// The minimum random trigger time.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Min;

    /// <summary>
    /// The maximum random trigger time.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Max;
}
