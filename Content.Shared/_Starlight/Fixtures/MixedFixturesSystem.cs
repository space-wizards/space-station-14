using Robust.Shared.Physics.Events;

namespace Content.Shared._Starlight.Fixtures;

/// <summary>
/// This handles the mixing of soft and hard fixutres
/// TODO: If the engine PR for this gets merged, delete this file and all related files and use the engine component
/// </summary>
public sealed class MixedFixturesSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MixedFixturesComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnPreventCollide(Entity<MixedFixturesComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled || !args.OurFixture.Hard || !args.OtherFixture.Hard)
            return;

        // Get the mask of the collision itself.
        var collisionMask = args.OurFixture.CollisionLayer & args.OtherFixture.CollisionMask;
        
        if ((ent.Comp.Mask | collisionMask) == ent.Comp.Mask)
            args.Cancelled = true;
            
    }
}