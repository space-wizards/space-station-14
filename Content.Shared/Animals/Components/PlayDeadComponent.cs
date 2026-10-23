using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Animals.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PlayDeadComponent : Component
{
    /// <summary>
    /// How long should we play dead for when attacked by something
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PlayDeadDuration = TimeSpan.FromSeconds(15.0);

    /// <summary>
    /// Whether they will automatically wake up from the current "death"
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool AutoWake = false;

    /// <summary>
    /// When to stop playing dead.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan StopPlayingDeadTime = TimeSpan.Zero;
}
