using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MobCollisionComponent : Component
{
    /// <summary>
    /// Flags this component as being handled this tick to avoid receiving 10 trillion messages.
    /// </summary>
    [ViewVariables]
    public bool HandledThisTick;

    [DataField, AutoNetworkedField]
    public bool Colliding;

    [DataField, AutoNetworkedField]
    public float BufferTime = 0.3f;

    [DataField, AutoNetworkedField]
    public float SpeedModifier = 1f;

    /// <summary>
    /// Shape to give this entity for mob collisions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public IPhysShape Shape = new PhysShapeCircle(radius: 0.35f);

    [DataField, AutoNetworkedField]
    public float Strength = 2.5f;
}
