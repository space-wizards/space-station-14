using Content.Shared.Actions;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Slippery;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.Clothing;

public abstract class SharedMagbootsSystem : EntitySystem
{
    [Dependency] private readonly ClothingSpeedModifierSystem _clothingSpeedModifier = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedActionsSystem _sharedActions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagbootsComponent, GetVerbsEvent<ActivationVerb>>(AddToggleVerb);
        SubscribeLocalEvent<MagbootsComponent, InventoryRelayedEvent<SlipAttemptEvent>>(OnSlipAttempt);
        SubscribeLocalEvent<MagbootsComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<MagbootsComponent, ToggleActionEvent>(OnToggleAction);
    }

    private void OnToggleAction(EntityUid uid, MagbootsComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        component.On = !component.On;

        if (_sharedContainer.TryGetContainingContainer(uid, out var container) &&
            _inventory.TryGetSlotEntity(container.Owner, "shoes", out var entityUid) && entityUid == component.Owner)
            UpdateMagbootEffects(container.Owner, uid, true, component);

        if (TryComp<ItemComponent>(uid, out var item))
        {
            _item.SetHeldPrefix(uid, component.On ? "on" : null, item);
            _clothing.SetEquippedPrefix(uid, component.On ? "on" : null);
        }

        _appearance.SetData(uid, ToggleVisuals.Toggled, component.Owner);
        OnChanged(component);
        Dirty(component);
    }

    protected virtual void UpdateMagbootEffects(EntityUid parent, EntityUid uid, bool state, MagbootsComponent? component) { }

    protected void OnChanged(MagbootsComponent component)
    {
        _sharedActions.SetToggled(component.ToggleAction, component.On);
        _clothingSpeedModifier.SetClothingSpeedModifierEnabled(component.Owner, component.On);
    }

    private void AddToggleVerb(EntityUid uid, MagbootsComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        ActivationVerb verb = new();
        verb.Text = Loc.GetString("toggle-magboots-verb-get-data-text");
        verb.Act = () => component.On = !component.On;
        // TODO VERB ICON add toggle icon? maybe a computer on/off symbol?
        args.Verbs.Add(verb);
    }

    private void OnSlipAttempt(EntityUid uid, MagbootsComponent component, InventoryRelayedEvent<SlipAttemptEvent> args)
    {
        if (component.On)
            args.Args.Cancel();
    }

    private void OnGetActions(EntityUid uid, MagbootsComponent component, GetItemActionsEvent args)
    {
        args.Actions.Add(component.ToggleAction);
    }
}
