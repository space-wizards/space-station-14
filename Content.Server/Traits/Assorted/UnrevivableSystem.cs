using Content.Shared.Cloning.Events;
using Content.Shared.Traits.Assorted;

namespace Content.Server.Traits.Assorted;

public sealed class UnrevivableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnrevivableComponent, CloningAttemptEvent>(OnCloningAttempt);
    }

    private void OnCloningAttempt(Entity<UnrevivableComponent> ent, ref CloningAttemptEvent args)
    {
        if (!ent.Comp.Cloneable)
            args.Cancelled = true;
    }
}
