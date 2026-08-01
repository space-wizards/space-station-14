using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Projectiles;

/// <summary>
/// Keeps an authoritative projectile visible to observers until it reaches the client-reported hit point.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PredictedProjectileHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityCoordinates Origin;

    [DataField, AutoNetworkedField]
    public float Distance;
}
