using Content.Server.Explosion.Components.OnTrigger;
using Content.Shared.RepulseAttract;

namespace Content.Server.Explosion.EntitySystems;

public sealed class PushPullOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly RepulseAttractSystem _repulse = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PushPullOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<PushPullOnTriggerComponent> ent, ref TriggerEvent args)  //why the heck is this not triggerin :c
    {
        if (!TryComp<RepulseAttractComponent>(ent, out var repulseAttract))
            return;

        var position = _transform.GetMapCoordinates(ent);
        _repulse.TryRepulseAttract(position, args.User, repulseAttract.Speed, repulseAttract.Range, repulseAttract.Whitelist, repulseAttract.CollisionMask);
    }
}
