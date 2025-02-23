using Content.Shared.Cargo.Components;
using Content.Shared.Examine;
using Content.Shared.Forensics.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Storage.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Deliveries;

/// <summary>
/// If you're reading this you're gay
/// </summary>
public abstract class SharedDeliveriesSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeliveryComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<DeliveryComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnExamine(Entity<DeliveryComponent> ent, ref ExaminedEvent args)
    {
        args.PushText(Loc.GetString("delivery-recipient-examine", ("recipient", ent.Comp.RecipientName), ("job", ent.Comp.RecipientJob)));
    }

    private void OnUseInHand(Entity<DeliveryComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.Delivered)
            return;

        Log.Debug("Granting reward");
        ent.Comp.Delivered = true;
        GrantSpesoReward(ent);
        _popup.PopupClient("Money manifests on the station, wow!", args.User, args.User);
        Dirty(ent);

        args.Handled = true;
    }

    protected virtual void GrantSpesoReward(EntityUid uid, DeliveryComponent? comp = null)
    {

    }
    /*public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DeliveryComponent>();
        while (query.MoveNext(out var uid, out var injectComponent))
        {

        }
    }*/
}
