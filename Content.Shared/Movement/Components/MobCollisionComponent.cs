using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MobCollisionComponent : Component
{
    public const float BufferTime = 0.5f;

    /// <summary>
    /// When to end the collision on the client.
    /// This is to avoid the client leaving collision for a frame then re-colliding causing jerkiness.
    /// </summary>
    [ViewVariables]
    public float EndAccumulator;

    /// <summary>
    /// Shape to give this entity for mob collisions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public IPhysShape Shape = new PhysShapeCircle(radius: 0.35f);

    [DataField, AutoNetworkedField]
    public float Strength = 1f;

    [DataField, AutoNetworkedField]
    public bool Colliding = false;
}
