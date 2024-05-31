using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.PowerCell;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing;

public sealed class ClothingSpeedModifierSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ClothingSpeedModifierSystem _clothingSpeedModifier = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingSpeedModifierComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, GetVerbsEvent<ExamineVerb>>(OnClothingVerbExamine);

        SubscribeLocalEvent<ToggleClothingSpeedComponent, GetVerbsEvent<ActivationVerb>>(AddToggleVerb);
        SubscribeLocalEvent<ToggleClothingSpeedComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<ToggleClothingSpeedComponent, ToggleClothingSpeedEvent>(OnToggleSpeed);
        SubscribeLocalEvent<ToggleClothingSpeedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ToggleClothingSpeedComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
    }

    // Public API

    public void SetClothingSpeedModifierEnabled(EntityUid uid, bool enabled, ClothingSpeedModifierComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.Enabled != enabled)
        {
            component.Enabled = enabled;
            Dirty(uid, component);

            // inventory system will automatically hook into the event raised by this and update accordingly
            if (_container.TryGetContainingContainer(uid, out var container))
            {
                _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);
            }
        }
    }

    // Event handlers

    private void OnGetState(EntityUid uid, ClothingSpeedModifierComponent component, ref ComponentGetState args)
    {
        args.State = new ClothingSpeedModifierComponentState(component.WalkModifier, component.SprintModifier, component.Enabled);
    }

    private void OnHandleState(EntityUid uid, ClothingSpeedModifierComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ClothingSpeedModifierComponentState state)
            return;

        var diff = component.Enabled != state.Enabled ||
                   !MathHelper.CloseTo(component.SprintModifier, state.SprintModifier) ||
                   !MathHelper.CloseTo(component.WalkModifier, state.WalkModifier);

        component.WalkModifier = state.WalkModifier;
        component.SprintModifier = state.SprintModifier;
        component.Enabled = state.Enabled;

        // Avoid raising the event for the container if nothing changed.
        // We'll still set the values in case they're slightly different but within tolerance.
        if (diff && _container.TryGetContainingContainer(uid, out var container))
        {
            _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);
        }
    }

    private void OnRefreshMoveSpeed(EntityUid uid, ClothingSpeedModifierComponent component, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (!component.Enabled)
            return;

        args.Args.ModifySpeed(component.WalkModifier, component.SprintModifier);
    }

    private void OnClothingVerbExamine(EntityUid uid, ClothingSpeedModifierComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var walkModifierPercentage = MathF.Round((1.0f - component.WalkModifier) * 100f, 1);
        var sprintModifierPercentage = MathF.Round((1.0f - component.SprintModifier) * 100f, 1);

        if (walkModifierPercentage == 0.0f && sprintModifierPercentage == 0.0f)
            return;

        var msg = new FormattedMessage();

        if (walkModifierPercentage == sprintModifierPercentage)
        {
            if (walkModifierPercentage < 0.0f)
                msg.AddMarkup(Loc.GetString("clothing-speed-increase-equal-examine", ("walkSpeed", MathF.Abs(walkModifierPercentage)), ("runSpeed", MathF.Abs(sprintModifierPercentage))));
            else
                msg.AddMarkup(Loc.GetString("clothing-speed-decrease-equal-examine", ("walkSpeed", walkModifierPercentage), ("runSpeed", sprintModifierPercentage)));
        }
        else
        {
            if (sprintModifierPercentage < 0.0f)
            {
                msg.AddMarkup(Loc.GetString("clothing-speed-increase-run-examine", ("runSpeed", MathF.Abs(sprintModifierPercentage))));
            }
            else if (sprintModifierPercentage > 0.0f)
            {
                msg.AddMarkup(Loc.GetString("clothing-speed-decrease-run-examine", ("runSpeed", sprintModifierPercentage)));
            }
            if (walkModifierPercentage != 0.0f && sprintModifierPercentage != 0.0f)
            {
                msg.PushNewline();
            }
            if (walkModifierPercentage < 0.0f)
            {
                msg.AddMarkup(Loc.GetString("clothing-speed-increase-walk-examine", ("walkSpeed", MathF.Abs(walkModifierPercentage))));
            }
            else if (walkModifierPercentage > 0.0f)
            {
                msg.AddMarkup(Loc.GetString("clothing-speed-decrease-walk-examine", ("walkSpeed", walkModifierPercentage)));
            }
        }

        _examine.AddDetailedExamineVerb(args, component, msg, Loc.GetString("clothing-speed-examinable-verb-text"), "/Textures/Interface/VerbIcons/outfit.svg.192dpi.png", Loc.GetString("clothing-speed-examinable-verb-message"));
    }

    private void OnMapInit(Entity<ToggleClothingSpeedComponent> uid, ref MapInitEvent args)
    {
        _actions.AddAction(uid, ref uid.Comp.ToggleActionEntity, uid.Comp.ToggleAction);
    }

    private void OnToggleSpeed(Entity<ToggleClothingSpeedComponent> uid, ref ToggleClothingSpeedEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        SetSpeedToggleEnabled(uid, !uid.Comp.Enabled, args.Performer);
    }

    private void SetSpeedToggleEnabled(Entity<ToggleClothingSpeedComponent> uid, bool value, EntityUid? user)
    {
        if (uid.Comp.Enabled == value)
            return;

        TryComp<PowerCellDrawComponent>(uid, out var draw);
        if (value && !_powerCell.HasDrawCharge(uid, draw, user: user))
            return;

        uid.Comp.Enabled = value;

        _appearance.SetData(uid, ToggleVisuals.Toggled, uid.Comp.Enabled);
        _actions.SetToggled(uid.Comp.ToggleActionEntity, uid.Comp.Enabled);
        _clothingSpeedModifier.SetClothingSpeedModifierEnabled(uid.Owner, uid.Comp.Enabled);
        _powerCell.SetPowerCellDrawEnabled(uid, uid.Comp.Enabled, draw);
        Dirty(uid, uid.Comp);
    }

    private void AddToggleVerb(Entity<ToggleClothingSpeedComponent> uid, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        ActivationVerb verb = new()
        {
            Text = Loc.GetString("toggle-clothing-verb-text",
                ("entity", Identity.Entity(uid, EntityManager))),
            Act = () => SetSpeedToggleEnabled(uid, !uid.Comp.Enabled, user)
        };
        args.Verbs.Add(verb);
    }

    private void OnGetActions(Entity<ToggleClothingSpeedComponent> uid, ref GetItemActionsEvent args)
    {
        args.AddAction(ref uid.Comp.ToggleActionEntity, uid.Comp.ToggleAction);
    }

    private void OnPowerCellSlotEmpty(Entity<ToggleClothingSpeedComponent> uid, ref PowerCellSlotEmptyEvent args)
    {
        SetSpeedToggleEnabled(uid, false, null);
    }
}
