using Content.Shared.Examine;

namespace Content.Shared.Delivery;

/// <summary>
/// If you're reading this you're gay
/// </summary>
public abstract class SharedDeliverySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeliveryComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<DeliveryComponent> ent, ref ExaminedEvent args)
    {
        var jobTitle = ent.Comp.RecipientJobTitle ?? "Unknown";
        var recipientName = ent.Comp.RecipientName ?? "Unnamed";

        args.PushText(Loc.GetString("delivery-recipient-examine", ("recipient", recipientName), ("job", jobTitle)));
    }
}
