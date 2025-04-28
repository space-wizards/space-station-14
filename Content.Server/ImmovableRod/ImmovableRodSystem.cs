using Content.Server.Polymorph.Components;
using Content.Shared.ImmovableRod;
using Robust.Shared.Physics.Events;

namespace Content.Server.ImmovableRod;

public sealed class ImmovableRodSystem : SharedImmovableRodSystem
{
    protected override void OnCollide(EntityUid uid, ImmovableRodComponent component, ref StartCollideEvent args)
    {
        // Save thyself from the wrath of Rod.
        if (TryComp<PolymorphedEntityComponent>(uid, out var polymorphed) && polymorphed.Parent == args.OtherEntity)
            return;

        base.OnCollide(uid, component, ref args);
    }
}
