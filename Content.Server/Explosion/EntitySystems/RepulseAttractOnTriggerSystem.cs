using Content.Server.Explosion.Components.OnTrigger;
using Content.Shared.RepulseAttract;
using Content.Shared.Timing;

namespace Content.Server.Explosion.EntitySystems;

public sealed class RepulseAttractOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly RepulseAttractSystem _repulse = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RepulseAttractOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<RepulseAttractOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (TryComp<UseDelayComponent>(ent, out var useDelay)
            && _delay.IsDelayed((ent, useDelay)))
            return;

        var position = _transform.GetMapCoordinates(ent);
        _repulse.TryRepulseAttract(position, args.User, ent.Comp.Speed, ent.Comp.Range, ent.Comp.Whitelist, ent.Comp.CollisionMask);
    }
}
