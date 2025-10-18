using Content.Shared.Implants.Components;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerOnActivateImplantSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnActivateImplantComponent, ActivateImplantEvent>(OnActivateImplant);
    }

    private void OnActivateImplant(Entity<TriggerOnActivateImplantComponent> ent, ref ActivateImplantEvent args)
    {
        _trigger.Trigger(ent.Owner, args.Performer, ent.Comp.KeyOut);
        args.Handled = true;
    }
}
