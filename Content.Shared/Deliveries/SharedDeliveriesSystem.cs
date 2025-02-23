using Content.Shared.Examine;
using Robust.Shared.Prototypes;

namespace Content.Shared.Deliveries;

/// <summary>
/// If you're reading this you're gay
/// </summary>
public abstract class SharedDeliveriesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeliveryComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<DeliveryComponent> ent, ref ExaminedEvent args)
    {
        if (!_prototype.TryIndex(ent.Comp.RecipientJob, out var proto))
            return;
        // TODO: if we can't get a valid recipient, change the popup

        var job = Loc.GetString(proto.LocalizedName);
        args.PushText(Loc.GetString("delivery-recipient-examine", ("recipient", ent.Comp.RecipientName), ("job", job)));
    }
}
