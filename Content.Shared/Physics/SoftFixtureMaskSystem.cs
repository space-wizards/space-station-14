using Robust.Shared.Physics.Events;

namespace Content.Shared.Physics;

/// <summary>
/// Prevents collision if the colliding fixture layers are completely contained within the
/// <see cref="SoftFixtureMaskComponent"/> mask.
/// </summary>
public sealed class SoftFixtureMaskSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SoftFixtureMaskComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnPreventCollide(Entity<SoftFixtureMaskComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled || !args.OurFixture.Hard || !args.OtherFixture.Hard)
            return;

        // Get the mask of the collision itself.
        var collisionMask = args.OurFixture.CollisionLayer & args.OtherFixture.CollisionMask;

        if ((ent.Comp.Mask | collisionMask) == ent.Comp.Mask)
            args.Cancelled = true;

    }
}
