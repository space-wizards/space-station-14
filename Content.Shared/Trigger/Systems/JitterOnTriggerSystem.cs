using Content.Shared.Jittering;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class JitterOnTriggerSystem : XOnTriggerSystem<JitterOnTriggerComponent>
{
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;

    protected override void OnTrigger(Entity<JitterOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _jittering.CreateJitter(target, ent.Comp.Jitter, ent.Comp.Time, ent.Comp.Refresh);
        args.Handled = true;
    }
}
