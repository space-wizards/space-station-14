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

        SubscribeLocalEvent<DeliveryComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<DeliveryComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<DeliveryComponent, ItemSpawnAttemptEvent>(OnSpawnAttempt);
        SubscribeLocalEvent<DeliveryComponent, UseInHandEvent>(OnUseInHand);
    }

    protected virtual void OnMapInit(Entity<DeliveryComponent> ent, ref MapInitEvent args)
    {
        // ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.UpdateInterval;
    }

    private void OnExamine(Entity<DeliveryComponent> ent, ref ExaminedEvent args)
    {
        args.PushText("This one is meant for " + ent.Comp.RecipientName + ", " + ent.Comp.RecipientJob + ".");
    }

    private void OnSpawnAttempt(Entity<DeliveryComponent> ent, ref ItemSpawnAttemptEvent args)
    {
        if (!ent.Comp.Delivered)
            args.Cancel();
    }

    private void OnUseInHand(Entity<DeliveryComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.Delivered)
            return;

        Log.Debug("Checking fingerprint");
        if (!TryComp<FingerprintComponent>(args.User, out var fingerprint))
        {
            _popup.PopupClient("You try to scan your fingerprint but you have no fingers.", args.User, args.User);
            args.Handled = true;
            return;
        }

        Log.Debug("Checking fingerprint validity");
        if (fingerprint.Fingerprint != ent.Comp.RecipientFingerprint)
        {
            _popup.PopupClient("You press your finger against the lock and nothing happens.", args.User, args.User);
            args.Handled = true;
            return;
        }

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
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DeliveryComponent>();
        while (query.MoveNext(out var uid, out var injectComponent))
        {

        }
    }
}
