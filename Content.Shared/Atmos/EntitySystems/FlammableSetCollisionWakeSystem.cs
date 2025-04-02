using Content.Shared.Atmos.Components;

namespace Content.Shared.Atmos.EntitySystems;

/// <summary>
/// Implements <see cref="FlammableSetCollisionWakeComponent"/>.
/// </summary>
public sealed class FlammableSetCollisionWakeSystem : EntitySystem
{
    [Dependency]
    private readonly CollisionWakeSystem _collisionWake = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlammableSetCollisionWakeComponent, FlammableExtinguished>(HandleExtinguished);
        SubscribeLocalEvent<FlammableSetCollisionWakeComponent, FlammableIgnited>(HandleIgnited);
    }

    private void HandleExtinguished(Entity<FlammableSetCollisionWakeComponent> ent, ref FlammableExtinguished args)
    {
        _collisionWake.SetEnabled(ent, true);
    }

    private void HandleIgnited(Entity<FlammableSetCollisionWakeComponent> ent, ref FlammableIgnited args)
    {
        _collisionWake.SetEnabled(ent, false);
    }
}
