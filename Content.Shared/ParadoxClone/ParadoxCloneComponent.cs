using Robust.Shared.GameStates;

namespace Content.Shared.ParadoxClone;

/// <summary>
/// Added to mind role entities to tag that they are a paradox clone.
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
    ///     The remaining time the paradox clone has before being forced to spawn.
    /// </summary>
    [DataField]
    public float RemainingTime;
}
