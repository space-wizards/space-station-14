using Robust.Shared.GameStates;

namespace Content.Shared.Projectiles;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedProjectileSystem))]
public sealed partial class BouncyProjectileComponent : Component
{
    [DataField(), ViewVariables(VVAccess.ReadWrite)]
    public int Bounces = 3;
}
