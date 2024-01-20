using Robust.Shared.GameStates;

namespace Content.Shared.Execution;

/// <summary>
/// Added to a gun when it's about to fire an execution shot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveExecutionComponent : Component
{
    [DataField]
    public EntityUid Attacker;

    [DataField]
    public EntityUid Victim;
}
