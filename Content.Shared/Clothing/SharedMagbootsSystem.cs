using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.Clothing;

public abstract class SharedMagbootsSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagbootsComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<MagbootsComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<MagbootsComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        // let magboots themselves get stuck to the floor :)
        SubscribeLocalEvent<MagbootsComponent, CheckGravityEvent>(OnCheckGravity);
        SubscribeLocalEvent<MagbootsComponent, InventoryRelayedEvent<CheckGravityEvent>>(OnCheckGravity);
    }

    private void OnToggled(Entity<MagbootsComponent> ent, ref ItemToggledEvent args)
    {
        // only stick to the floor if being worn in the correct slot
        if (_container.TryGetContainingContainer(ent.Owner, out var container) &&
            _inventory.TryGetSlotEntity(container.Owner, "shoes", out var shoes)
            && ent == shoes)
        {
            UpdateMagbootEffects(ent, container.Owner, args.Activated);
        }

        var prefix = args.Activated ? "on" : null;
        _item.SetHeldPrefix(ent, prefix);
        _clothing.SetEquippedPrefix(ent, prefix);
    }

    private void OnUnequipped(Entity<MagbootsComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        UpdateMagbootEffects(ent, args.Wearer, false);
    }

    private void OnEquipped(Entity<MagbootsComponent> ent, ref ClothingGotEquippedEvent args)
    {
        UpdateMagbootEffects(ent, args.Wearer, _toggle.IsActivated(ent.Owner));
    }

    protected virtual void UpdateMagbootEffects(Entity<MagbootsComponent> ent, EntityUid user, bool on)
    {
        if (on)
            _alerts.ShowAlert(user, ent.Comp.MagbootsAlert);
        else
            _alerts.ClearAlert(user, ent.Comp.MagbootsAlert);
    }

    private void OnCheckGravity(Entity<MagbootsComponent> ent, ref CheckGravityEvent args)
    {
        if (_toggle.IsActivated(ent.Owner))
            args.Handled = true;
    }

    private void OnCheckGravity(Entity<MagbootsComponent> ent, ref InventoryRelayedEvent<CheckGravityEvent> args)
    {
        OnCheckGravity(ent, ref args.Args);
    }
}
