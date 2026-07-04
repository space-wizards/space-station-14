using Content.Shared.Trigger.Components.Effects;
using Content.Shared.RepulseAttract;

namespace Content.Shared.Trigger.Systems;

public sealed partial class RepulseAttractOnTriggerSystem : XOnTriggerSystem<RepulseAttractOnTriggerComponent>
{
    [Dependency] private RepulseAttractSystem _repulse = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void OnTrigger(Entity<RepulseAttractOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        var position = _transform.GetMapCoordinates(target);
        _repulse.TryRepulseAttract(position, args.User, ent.Comp.Speed, ent.Comp.Range, ent.Comp.Whitelist, ent.Comp.CollisionMask);

        args.Handled = true;
    }
}
