using Content.Shared.Screech;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed partial class ScreechOnTriggerSystem : XOnTriggerSystem<ScreechOnTriggerComponent>
{
    [Dependency] private ScreechSystem _screech = default!;

    protected override void OnTrigger(Entity<ScreechOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _screech.Screech(target, ent.Comp.Range, ent.Comp.Vfx, ent.Comp.ScreechSound, ent.Comp.Effects);
        args.Handled = true;
    }
}
