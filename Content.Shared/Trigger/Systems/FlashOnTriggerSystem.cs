using Content.Shared.Flash;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class FlashOnTriggerSystem : XOnTriggerSystem<FlashOnTriggerComponent>
{
    [Dependency] private readonly SharedFlashSystem _flash = default!;

    protected override void OnTrigger(Entity<FlashOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _flash.FlashArea(target, args.User, ent.Comp.Range, ent.Comp.Duration, probability: ent.Comp.Probability);
        args.Handled = true;
    }
}
