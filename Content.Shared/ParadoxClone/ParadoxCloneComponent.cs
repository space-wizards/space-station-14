using Robust.Shared.GameStates;

namespace Content.Shared.ParadoxClone;

/// <summary>
/// Added to the paradox clone's ghost form to control its state. Ensures that wandering & listening states work properly.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParadoxCloneComponent : Component
{
    /// <summary>
    ///     The paradox clone's cloned body, safely stored in nullspace until the clone is ready to  materialize.
    /// </summary>
    [DataField]
    public EntityUid ClonedBody;

    /// <summary>
    /// Used to enforce MaxListenTime and MaxWanderTime
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Epoch;

    /// <summary>
    ///     Initial listening time amount, used to display the alert & make sure it gets forced correctly
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxListenTime = 10f;

    /// <summary>
    ///     Initial wandering time amount, used to display the alert & make sure wandering gets forced properly
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxWanderTime = 10f;

    /// <summary>
    /// Tracks wether the paradox clone is currently in wander mode OR if it has already wandered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsWandering = false;
}
