using Content.Shared.Explosion.Components.OnTrigger;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.RepulseAttract;
using Content.Shared.Timing;

namespace Content.Server.Explosion.EntitySystems;

public sealed class RepulseAttractOnTriggerSystem : SharedRepulseAttractOnTriggerSystem
{
    [Dependency] private readonly RepulseAttractSystem _repulse = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedRepulseAttractOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<SharedRepulseAttractOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (_delay.IsDelayed(ent.Owner))
            return;

        var position = _transform.GetMapCoordinates(ent);
        _repulse.TryRepulseAttract(position, args.User, ent.Comp.Speed, ent.Comp.Range, ent.Comp.Whitelist, ent.Comp.CollisionMask);
    }
}
