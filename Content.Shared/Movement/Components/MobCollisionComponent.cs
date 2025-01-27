using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
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

    // TODO: I hate this but also I couldn't quite figure out a way to avoid having to dirty it every tick.
    // The issue is it's a time target that changes constantly so we can't just use a timespan.
    // However that doesn't mean it should be modified every tick if we're still colliding.

    /// <summary>
    /// Buffer time for <see cref="SpeedModifier"/> to keep applying after the entities are no longer colliding.
    /// Without this you will get jittering unless you are very specific with your values.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BufferAccumulator = SharedMobCollisionSystem.BufferTime;

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
    public float Strength = 0.5f;
}
