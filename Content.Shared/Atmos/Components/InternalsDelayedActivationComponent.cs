using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// This entity will try to toggle their internals at the specified time
/// This component is automatically created and deleted by InternalsSystem
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class InternalsDelayedActivationComponent : Component
{
    /// <summary>
    /// The entity that will toggle internals
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Entity;

    /// <summary>
    /// The server time when the internals will be toggled
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Time;
}

