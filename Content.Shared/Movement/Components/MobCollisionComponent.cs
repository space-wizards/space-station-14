using System.Numerics;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Handles mobs pushing against each other.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class MobCollisionComponent : Component
{
    // If you want to tweak the feel of the pushing use SpeedModifier and Strength.
    // Strength goes both ways and affects how much the other mob is pushed by so controls static pushing a lot.
    // Speed mod affects your own mob primarily.

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
    public float SpeedModifier = 1f;

    [DataField, AutoNetworkedField]
    public float MinimumSpeedModifier = 0.35f;

    /// <summary>
    /// Strength of the pushback for entities. This is combined between the 2 entities being pushed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Strength = 50f;

    // Yes I know, I will deal with it if I ever refactor collision layers due to misuse.
    // If anything it probably needs some assurance on mobcollisionsystem for it.
    /// <summary>
    /// Fixture to listen to for mob collisions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string FixtureId = "flammable";

    [DataField, AutoNetworkedField]
    public Vector2 Direction;
}
