using Robust.Shared.GameStates;

namespace Content.Shared.ParadoxClone;

/// <summary>
/// Added to the paradox clone's ghost form to control its state. Ensures that wandering & listening states work properly.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ParadoxCloneComponent : Component
{
    /// <summary>
    ///     The paradox clone's cloned body, safely stored in nullspace until the clone is ready to  materialize.
    /// </summary>
    [DataField]
    public EntityUid ClonedBody;

    /// <summary>
    ///     The remaining time the paradox clone has before being forced to wander.
    /// </summary>
    [DataField]
    public float ListenTime = 10f;

    /// <summary>
    ///     The remaining time the paradox clone has before being forced to spawn.
    /// </summary>
    [DataField]
    public float WanderTime = 10f;

    /// <summary>
    /// Tracks wether the paradox clone is currently in wander mode OR if it has already wandered.
    /// </summary>
    [DataField]
    public bool IsWandering = false;
}
