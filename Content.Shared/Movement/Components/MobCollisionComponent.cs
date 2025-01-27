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

    /// <summary>
    /// Is this mob currently colliding? Used for SpeedModifier.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Colliding;

    /// <summary>
    /// Buffer time for <see cref="SpeedModifier"/> to keep applying after the entities are no longer colliding.
    /// Without this you will get jittering unless you are very specific with your values.
    /// </summary>
    [ViewVariables]
    public float BufferAccumulator = 0.3f;

    /// <summary>
    /// The speed modifier for mobs currently pushing.
    /// By setting this low you can ensure you don't have to set the push-strength too high if you can push static entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 0.60f;

    /// <summary>
    /// Shape to give this entity for mob collisions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public IPhysShape Shape = new PhysShapeCircle(radius: 0.35f);

    /// <summary>
    /// Strength of the pushback for entities. This is combined between the 2 entities being pushed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Strength = 2.5f;
}
