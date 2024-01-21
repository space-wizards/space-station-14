using Robust.Shared.GameStates;

namespace Content.Shared.Execution;

[RegisterComponent, NetworkedComponent]
public sealed partial class ExecutionProjectileComponent : Component
{
    [DataField]
    public EntityUid Target;

    [DataField]
    public float Multiplier;

    [DataField]
    public string FixtureId = "projectile";

    [DataField]
    public bool Clumsy = false;
}
