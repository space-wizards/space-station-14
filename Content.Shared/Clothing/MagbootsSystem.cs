using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Clothing;

public sealed class SharedMagbootsSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagbootsComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<MagbootsComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MagbootsComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<MagbootsComponent, IsWeightlessEvent>(OnIsWeightless);
        SubscribeLocalEvent<MagbootsComponent, InventoryRelayedEvent<IsWeightlessEvent>>(OnIsWeightless);
    }

    private void OnToggled(Entity<MagbootsComponent> ent, ref ItemToggledEvent args)
    {
        var (uid, comp) = ent;
        // only stick to the floor if being worn in the correct slot
        if (_container.TryGetContainingContainer((uid, null, null), out var container) &&
            _inventory.TryGetSlotEntity(container.Owner, comp.Slot, out var worn)
            && uid == worn)
        {
            UpdateMagbootEffects(container.Owner, ent, args.Activated);
        }

        var prefix = args.Activated ? "on" : null;
        _item.SetHeldPrefix(ent, prefix);
        _clothing.SetEquippedPrefix(ent, prefix);
    }

    private void OnGotUnequipped(Entity<MagbootsComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        UpdateMagbootEffects(args.Wearer, ent, false);
    }

    private void OnGotEquipped(Entity<MagbootsComponent> ent, ref ClothingGotEquippedEvent args)
    {
        UpdateMagbootEffects(args.Wearer, ent, _toggle.IsActivated(ent.Owner));
    }

    public void UpdateMagbootEffects(EntityUid user, Entity<MagbootsComponent> ent, bool state)
    {
        // TODO: public api for this and add access
        if (TryComp<MovedByPressureComponent>(user, out var moved))
            moved.Enabled = !state;

        if (state)
            _alerts.ShowAlert(user, ent.Comp.MagbootsAlert);
        else
            _alerts.ClearAlert(user, ent.Comp.MagbootsAlert);
    }

    private void OnIsWeightless(Entity<MagbootsComponent> ent, ref IsWeightlessEvent args)
    {
        if (args.Handled || !_toggle.IsActivated(ent.Owner))
            return;

        // do not cancel weightlessness if the person is in off-grid.
        if (ent.Comp.RequiresGrid && !_gravity.EntityOnGravitySupportingGridOrMap(ent.Owner))
            return;

        args.IsWeightless = false;
        args.Handled = true;
    }

    private void OnIsWeightless(Entity<MagbootsComponent> ent, ref InventoryRelayedEvent<IsWeightlessEvent> args)
    {
        OnIsWeightless(ent, ref args.Args);
    }
}
