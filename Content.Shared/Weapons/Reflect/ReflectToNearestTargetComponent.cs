using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Reflect;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReflectToNearestTargetComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool IncludeShooter = false;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float MaxDistance = 30f;
}
