using Content.Shared.Trigger.Components.Effects;
using Content.Shared.RepulseAttract;

namespace Content.Shared.Trigger.Systems;

public sealed class RepulseAttractOnTriggerSystem : XOnTriggerSystem<RepulseAttractOnTriggerComponent>
{
    [Dependency] private readonly RepulseAttractSystem _repulse = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void OnTrigger(Entity<RepulseAttractOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        var position = _transform.GetMapCoordinates(target);
        _repulse.TryRepulseAttract(position, args.User, ent.Comp.Speed, ent.Comp.Range, ent.Comp.Whitelist, ent.Comp.CollisionMask);

        args.Handled = true;
    }
}
