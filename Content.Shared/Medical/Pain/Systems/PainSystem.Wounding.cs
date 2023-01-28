using Content.Shared.Body.Components;
using Content.Shared.Medical.Pain.Components;
using Content.Shared.Medical.Wounds.Systems;

namespace Content.Shared.Medical.Pain.Systems;

public sealed partial class PainSystem
{
    private void OnWoundAdded(EntityUid target, BodyComponent component, ref WoundAddedEvent args)
    {
        if (!TryComp<PainInflicterComponent>(args.WoundEntity, out var inflicter) || !TryComp<PainReceiverComponent>(target, out var receiver))
            return;
        PainLocalModifierComponent? modifier = null;
        Resolve(args.WoundEntity, ref modifier, false);
        InflictPain(target, args.WoundEntity, receiver, inflicter, modifier);
    }

    private void OnWoundRemoved(EntityUid target, BodyComponent component, ref WoundRemovedEvent args)
    {
        if (!TryComp<PainInflicterComponent>(args.WoundEntity, out var inflicter) || !TryComp<PainReceiverComponent>(target, out var receiver))
            return;
        PainLocalModifierComponent? modifier = null;
        Resolve(args.WoundEntity, ref modifier, false);
        RemovePain(target, args.WoundEntity, receiver, inflicter, modifier);
    }
}
