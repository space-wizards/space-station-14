using Robust.Shared.GameStates;

namespace Content.Shared.Execution;

[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveExecutionComponent : Component
{
    [DataField]
    public EntityUid Attacker;

    [DataField]
    public EntityUid Victim;
}
