using Robust.Shared.GameStates;

namespace Content.Shared.Timing;

/// <summary>
/// Timer that creates a cooldown each time an object is activated/used
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class UseDelayComponent : Component
{
    [AutoNetworkedField]
    public TimeSpan LastUseTime;

    [AutoNetworkedField]
    public TimeSpan? DelayEndTime;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     Stores remaining delay pausing (and eventually, serialization).
    /// </summary>
    [DataField]
    public TimeSpan? RemainingDelay;

    public bool ActiveDelay => DelayEndTime != null;
}
