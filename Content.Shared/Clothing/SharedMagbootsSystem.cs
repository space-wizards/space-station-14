using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Slippery;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.Clothing;

public sealed class SharedMagbootsSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly ClothingSpeedModifierSystem _clothingSpeedModifier = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedActionsSystem _sharedActions = default!;
    [Dependency] private readonly SharedActionsSystem _actionContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagbootsComponent, GetVerbsEvent<ActivationVerb>>(AddToggleVerb);
        SubscribeLocalEvent<MagbootsComponent, InventoryRelayedEvent<SlipAttemptEvent>>(OnSlipAttempt);
        SubscribeLocalEvent<MagbootsComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<MagbootsComponent, ToggleMagbootsEvent>(OnToggleMagboots);
        SubscribeLocalEvent<MagbootsComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<MagbootsComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MagbootsComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<MagbootsComponent, InventoryRelayedEvent<IsWeightlessEvent>>(OnIsWeightless);
    }

    private void OnMapInit(EntityUid uid, MagbootsComponent component, MapInitEvent args)
    {
        _actionContainer.AddAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
        Dirty(uid, component);
    }

    private void OnGotUnequipped(EntityUid uid, MagbootsComponent component, ref ClothingGotUnequippedEvent args)
    {
        UpdateMagbootEffects(args.Wearer, uid, false, component);
    }

    private void OnGotEquipped(EntityUid uid, MagbootsComponent component, ref ClothingGotEquippedEvent args)
    {
        UpdateMagbootEffects(args.Wearer, uid, true, component);
    }

    private void OnToggleMagboots(EntityUid uid, MagbootsComponent component, ToggleMagbootsEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ToggleMagboots(uid, component);
    }

    private void ToggleMagboots(EntityUid uid, MagbootsComponent magboots)
    {
        magboots.On = !magboots.On;

        if (_sharedContainer.TryGetContainingContainer((uid, Transform(uid)), out var container) &&
            _inventory.TryGetSlotEntity(container.Owner, "shoes", out var entityUid) && entityUid == uid)
        {
            UpdateMagbootEffects(container.Owner, uid, true, magboots);
        }

        if (TryComp<ItemComponent>(uid, out var item))
        {
            _item.SetHeldPrefix(uid, magboots.On ? "on" : null, component: item);
            _clothing.SetEquippedPrefix(uid, magboots.On ? "on" : null);
        }

        _appearance.SetData(uid, ToggleVisuals.Toggled, magboots.On);
        OnChanged(uid, magboots);
        Dirty(uid, magboots);
    }

    public void UpdateMagbootEffects(EntityUid parent, EntityUid uid, bool state, MagbootsComponent? component)
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
            _alerts.ShowAlert(parent, component.MagbootsAlert);
        }
        else
        {
            _alerts.ClearAlert(parent, component.MagbootsAlert);
        }
    }

    private void OnChanged(EntityUid uid, MagbootsComponent component)
    {
        _sharedActions.SetToggled(component.ToggleActionEntity, component.On);
        _clothingSpeedModifier.SetClothingSpeedModifierEnabled(uid, component.On);
    }

    private void AddToggleVerb(EntityUid uid, MagbootsComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        ActivationVerb verb = new()
        {
            Text = Loc.GetString("toggle-magboots-verb-get-data-text"),
            Act = () => ToggleMagboots(uid, component),
            // TODO VERB ICON add toggle icon? maybe a computer on/off symbol?
        };
        args.Verbs.Add(verb);
    }

    private void OnSlipAttempt(EntityUid uid, MagbootsComponent component, InventoryRelayedEvent<SlipAttemptEvent> args)
    {
        if (component.On)
            args.Args.Cancel();
    }

    private void OnGetActions(EntityUid uid, MagbootsComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
    }

    private void OnIsWeightless(Entity<MagbootsComponent> ent, ref InventoryRelayedEvent<IsWeightlessEvent> args)
    {
        if (args.Args.Handled)
            return;

        if (!ent.Comp.On)
            return;

        // do not cancel weightlessness if the person is in off-grid.
        if (ent.Comp.RequiresGrid && !_gravity.EntityOnGravitySupportingGridOrMap(ent.Owner))
            return;

        args.Args.IsWeightless = false;
        args.Args.Handled = true;
    }
}

public sealed partial class ToggleMagbootsEvent : InstantActionEvent;
