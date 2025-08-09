using Content.Server.Polymorph.Components;
using Content.Shared.ImmovableRod;
using Robust.Shared.Physics.Events;

namespace Content.Server.ImmovableRod;

public sealed class ImmovableRodSystem : SharedImmovableRodSystem
{
    protected override void OnCollide(Entity<ImmovableRodComponent> ent, ref StartCollideEvent args)
    {
        // Save thyself from the wrath of Rod.
        if (TryComp<PolymorphedEntityComponent>(ent, out var polymorphed) && polymorphed.Parent == args.OtherEntity)
            return;

        base.OnCollide(ent, ref args);
    }
}
