using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Clothing;

/// <summary>
/// A system for enabling and disabling the effects of magboots.
/// The boots "force" gravity for the wearing entity when enabled and on a grid.
/// </summary>
public sealed partial class SharedMagbootsSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private ClothingSystem _clothing = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;

    [Dependency] private EntityQuery<MovedByPressureComponent> _movedByPressureQuery;

    [SubscribeLocalEvent]
    private void OnToggled(Entity<MagbootsComponent> ent, ref ItemToggledEvent args)
    {
        if (_clothing.IsEquipped(ent.Owner)
            && _container.TryGetContainingContainer((ent.Owner, null, null), out var container))
        {
            UpdateMagbootEffects(container.Owner, ent, args.Activated);
        }
    }

    [SubscribeLocalEvent]
    private void OnGotUnequipped(Entity<MagbootsComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        UpdateMagbootEffects(args.Wearer, ent, false);
    }

    [SubscribeLocalEvent]
    private void OnGotEquipped(Entity<MagbootsComponent> ent, ref ClothingGotEquippedEvent args)
    {
        UpdateMagbootEffects(args.Wearer, ent, _toggle.IsActivated(ent.Owner));
    }

    public void UpdateMagbootEffects(EntityUid user, Entity<MagbootsComponent> ent, bool state)
    {
        // TODO: public api for this and add access
        if (_movedByPressureQuery.TryComp(user, out var moved))
            moved.Enabled = !state;

        _gravity.RefreshWeightless(user);

        if (state)
            _alerts.ShowAlert(user, ent.Comp.MagbootsAlert);
        else
            _alerts.ClearAlert(user, ent.Comp.MagbootsAlert);
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
    private void OnIsWeightless(Entity<MagbootsComponent> ent, ref InventoryRelayedEvent<IsWeightlessEvent> args)
    {
        OnIsWeightless(ent, ref args.Args);
    }
}
