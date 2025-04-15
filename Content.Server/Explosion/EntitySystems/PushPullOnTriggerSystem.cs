using Content.Server.Explosion.Components.OnTrigger;
using Content.Shared.RepulseAttract;
using Content.Shared.Timing;

namespace Content.Server.Explosion.EntitySystems;

public sealed class PushPullOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly RepulseAttractSystem _repulse = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PushPullOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<PushPullOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (!TryComp<RepulseAttractComponent>(ent, out var repulseAttract)
            || !TryComp<UseDelayComponent>(ent, out var useDelay)
            || _delay.IsDelayed((ent, useDelay)))
            return;

        var position = _transform.GetMapCoordinates(ent);
        _repulse.TryRepulseAttract(position, args.User, repulseAttract.Speed, repulseAttract.Range, repulseAttract.Whitelist, repulseAttract.CollisionMask);
    }
}
