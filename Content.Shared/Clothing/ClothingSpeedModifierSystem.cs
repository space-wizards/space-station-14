using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing;

public sealed class ClothingSpeedModifierSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingSpeedModifierComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, GetVerbsEvent<ExamineVerb>>(OnClothingVerbExamine);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnGetState(EntityUid uid, ClothingSpeedModifierComponent component, ref ComponentGetState args)
    {
        args.State = new ClothingSpeedModifierComponentState(component.WalkModifier, component.SprintModifier);
    }

    private void OnHandleState(EntityUid uid, ClothingSpeedModifierComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ClothingSpeedModifierComponentState state)
            return;

        var diff = !MathHelper.CloseTo(component.SprintModifier, state.SprintModifier) ||
                   !MathHelper.CloseTo(component.WalkModifier, state.WalkModifier);

        component.WalkModifier = state.WalkModifier;
        component.SprintModifier = state.SprintModifier;

        // Avoid raising the event for the container if nothing changed.
        // We'll still set the values in case they're slightly different but within tolerance.
        if (diff && _container.TryGetContainingContainer((uid, null, null), out var container))
        {
            _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);
        }
    }

    private void OnRefreshMoveSpeed(EntityUid uid, ClothingSpeedModifierComponent component, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (component.RequireActivated && !_toggle.IsActivated(uid))
            return;

        if (component.Standing != null && !_standing.IsMatchingState(args.Owner, component.Standing.Value))
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

        if (MathHelper.CloseTo(walkModifierPercentage, sprintModifierPercentage, 0.5f))
        {
            if (walkModifierPercentage < 0.0f)
                msg.AddMarkupOrThrow(Loc.GetString("clothing-speed-increase-equal-examine", ("walkSpeed", (int) MathF.Abs(walkModifierPercentage)), ("runSpeed", (int) MathF.Abs(sprintModifierPercentage))));
            else
                msg.AddMarkupOrThrow(Loc.GetString("clothing-speed-decrease-equal-examine", ("walkSpeed", (int) walkModifierPercentage), ("runSpeed", (int) sprintModifierPercentage)));
        }
        else
        {
            if (sprintModifierPercentage < 0.0f)
            {
                msg.AddMarkupOrThrow(Loc.GetString("clothing-speed-increase-run-examine", ("runSpeed", (int) MathF.Abs(sprintModifierPercentage))));
            }
            else if (sprintModifierPercentage > 0.0f)
            {
                msg.AddMarkupOrThrow(Loc.GetString("clothing-speed-decrease-run-examine", ("runSpeed", (int) sprintModifierPercentage)));
            }
            if (walkModifierPercentage != 0.0f && sprintModifierPercentage != 0.0f)
            {
                msg.PushNewline();
            }
            if (walkModifierPercentage < 0.0f)
            {
                msg.AddMarkupOrThrow(Loc.GetString("clothing-speed-increase-walk-examine", ("walkSpeed", (int) MathF.Abs(walkModifierPercentage))));
            }
            else if (walkModifierPercentage > 0.0f)
            {
                msg.AddMarkupOrThrow(Loc.GetString("clothing-speed-decrease-walk-examine", ("walkSpeed", (int) walkModifierPercentage)));
            }
        }

        _examine.AddDetailedExamineVerb(args, component, msg, Loc.GetString("clothing-speed-examinable-verb-text"), "/Textures/Interface/VerbIcons/outfit.svg.192dpi.png", Loc.GetString("clothing-speed-examinable-verb-message"));
    }

    private void OnToggled(Entity<ClothingSpeedModifierComponent> ent, ref ItemToggledEvent args)
    {
        if (!ent.Comp.RequireActivated)
            return;

        // make sentient boots slow or fast too
        _movementSpeed.RefreshMovementSpeedModifiers(ent);

        if (_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
        {
            // inventory system will automatically hook into the event raised by this and update accordingly
            _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);
        }
    }
}
