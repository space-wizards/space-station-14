using Robust.Shared.GameStates;

namespace Content.Shared.Bed.Sleep;

/// <summary>
///     To add SleepingComponent after certain time
/// </summary>
[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause(Dirty = true)]
public sealed partial class PendingSleepingComponent : Component
{
    /// <summary>
    ///     When the component will be added
    /// </summary>
    [DataField]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan SleepTime;
}
