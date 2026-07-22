// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Alert;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Blink;

public abstract class SharedBlinkSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlinkItemComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<BlinkItemComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<BlinkItemComponent, GotEquippedHandEvent>(OnEquippedHand);
        SubscribeLocalEvent<BlinkItemComponent, GotUnequippedHandEvent>(OnUnequippedHand);
        SubscribeLocalEvent<ToggleBlinkViewAlertEvent>(OnToggle);
    }

    private void OnEquipped(Entity<BlinkItemComponent> ent, ref GotEquippedEvent args)
    {
        if (!ent.Comp.NeedHand)
            _alerts.ShowAlert(args.Equipee, ent.Comp.CooldownAlert, autoRemove: false);
    }

    private void OnUnequipped(Entity<BlinkItemComponent> ent, ref GotUnequippedEvent args)
    {
        if (!ent.Comp.NeedHand)
            RefreshAlert(args.Equipee, ent.Comp.CooldownAlert, ent.Owner);
    }

    private void OnEquippedHand(Entity<BlinkItemComponent> ent, ref GotEquippedHandEvent args)
    {
        if (ent.Comp.NeedHand)
            _alerts.ShowAlert(args.User, ent.Comp.CooldownAlert, autoRemove: false);
    }

    private void OnUnequippedHand(Entity<BlinkItemComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (ent.Comp.NeedHand)
            RefreshAlert(args.User, ent.Comp.CooldownAlert, ent.Owner);
    }

    private void OnToggle(ToggleBlinkViewAlertEvent args)
    {
        if (!TryFindItem(args.User, out var item, out var blink))
            return;

        blink.Targeting = !blink.Targeting;
        Dirty(item, blink);
        args.Handled = true;
    }

    private void RefreshAlert(EntityUid user, ProtoId<AlertPrototype> alert, EntityUid ignored)
    {
        if (TryFindItem(user, out _, out _, ignored))
            return;

        _alerts.ClearAlert(user, alert);
    }

    protected bool TryFindItem(EntityUid user, out EntityUid item, out BlinkItemComponent component, EntityUid? ignored = null)
    {
        var query = EntityQueryEnumerator<BlinkItemComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (uid == ignored)
                continue;

            var usable = comp.NeedHand
                ? _hands.IsHolding(user, uid)
                : _inventory.TryGetContainingSlot(uid, out _) && Transform(uid).ParentUid == user;
            if (!usable)
                continue;

            item = uid;
            component = comp;
            return true;
        }

        item = default;
        component = default!;
        return false;
    }
}
