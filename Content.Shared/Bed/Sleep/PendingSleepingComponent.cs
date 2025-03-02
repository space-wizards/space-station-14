using Robust.Shared.GameStates;

namespace Content.Shared.Bed.Sleep;

/// <summary>
///     Makes an entity wait before falling asleep
/// </summary>
[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause(Dirty = true)]
public sealed partial class PendingSleepingComponent : Component
{
    /// <summary>
    ///     Time in seconds to wait
    /// </summary>
    [DataField]
    public float PendingTime;

    [DataField]
    [AutoNetworkedField, AutoPausedField, Access(typeof(SleepingSystem))]
    public TimeSpan FallAsleepTime;
}
