using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Timing;

/// <summary>
/// Timer that creates a cooldown each time an object is activated/used
/// </summary>
/// <remarks>
/// Currently it only supports a single delay per entity, this means that for things that have two delay interactions they will share one timer, so this can cause issues. For example, the bible has a delay when opening the storage UI and when applying it's interaction effect, and they share the same delay.
/// </remarks>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(UseDelaySystem))]
public sealed partial class UseDelayComponent : Component
{
    /// <summary>
    /// When the delay starts.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan DelayStartTime;

    /// <summary>
    /// When the delay ends.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan DelayEndTime;

    /// <summary>
    /// Default delay time
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);
}
