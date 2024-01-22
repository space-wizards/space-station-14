using Robust.Shared.GameStates;

namespace Content.Shared.Execution;

/// <summary>
/// Added to a gun when it's about to fire an execution shot.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveExecutionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Attacker;

    [DataField, AutoNetworkedField]
    public EntityUid Victim;

    [DataField, AutoNetworkedField]
    public bool Clumsy;

    [DataField, AutoNetworkedField]
    public string FixtureId = "projectile";
}
