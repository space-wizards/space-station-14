using Content.Server.Atmos.Components;
using Content.Shared.Alert;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Toggleable;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using static Content.Shared.Clothing.MagbootsComponent;

namespace Content.Server.Clothing;

public sealed class MagbootsSystem : SharedMagbootsSystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagbootsComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MagbootsComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<MagbootsComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<MagbootsComponent, ComponentGetState>(OnGetState);
    }

    private void UpdateMagbootEffects(EntityUid parent, EntityUid uid, bool state, MagbootsComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;
        state = state && component.On;

        if (TryComp(parent, out MovedByPressureComponent? movedByPressure))
        {
            movedByPressure.Enabled = !state;
        }

        if (state)
        {
            _alertsSystem.ShowAlert(parent, AlertType.Magboots);
        }
        else
        {
            _alertsSystem.ClearAlert(parent, AlertType.Magboots);
        }
    }

    private void OnToggleAction(EntityUid uid, MagbootsComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        component.On = !component.On;

        if (_sharedContainer.TryGetContainingContainer(uid, out var container) &&
            _inventory.TryGetSlotEntity(container.Owner, "shoes", out var entityUid) && entityUid == component.Owner)
            UpdateMagbootEffects(container.Owner, component.Owner, true, component);

        if (TryComp<SharedItemComponent>(component.Owner, out var item))
            item.EquippedPrefix = component.On ? "on" : null;

        if (TryComp<SpriteComponent>(component.Owner, out var sprite))
            sprite.LayerSetState(0, component.On ? "icon-on" : "icon");

        OnChanged(component);
        Dirty(component);
    }

    private void OnGotUnequipped(EntityUid uid, MagbootsComponent component, GotUnequippedEvent args)
    {
        if (args.Slot == "shoes")
        {
            UpdateMagbootEffects(args.Equipee, uid, false, component);
        }
    }

    private void OnGotEquipped(EntityUid uid, MagbootsComponent component, GotEquippedEvent args)
    {
        if (args.Slot == "shoes")
        {
            UpdateMagbootEffects(args.Equipee, uid, true, component);
        }
    }

    private void OnGetState(EntityUid uid, MagbootsComponent component, ref ComponentGetState args)
    {
        args.State = new MagbootsComponentState(component.On);
    }
}
