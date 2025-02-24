using Content.Shared.Examine;
using Robust.Shared.Prototypes;

namespace Content.Shared.Deliveries;

/// <summary>
/// If you're reading this you're gay
/// </summary>
public abstract class SharedDeliveriesSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeliveryComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<DeliveryComponent> ent, ref ExaminedEvent args)
    {
        args.PushText(Loc.GetString("delivery-recipient-examine", ("recipient", ent.Comp.RecipientName), ("job", ent.Comp.RecipientJobTitle)));
    }
}
