using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.Events;
using Content.Shared.DoAfter;
using Content.Shared.Foldable;
using Content.Shared.IdentityManagement;
using Content.Shared.Internals;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class MaskSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timingSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly ClothingSystem _clothingSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaskComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<MaskComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<MaskComponent, ToggleMaskEvent>(OnToggleMaskAction);
        SubscribeLocalEvent<MaskComponent, InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<MaskComponent, ToggleMaskDoAfterEvent>(OnToggleMaskDoAfterEvent);
        SubscribeLocalEvent<MaskComponent, FoldedEvent>(OnFolded);
    }

    private void OnGotUnequipped(Entity<MaskComponent> mask, ref GotUnequippedEvent args)
    {
        ToggleMask(mask, false);
    }

    private void OnGetActions(Entity<MaskComponent> mask, ref GetItemActionsEvent args)
    {
        if (!_inventorySystem.InSlotWithFlags(mask.Owner, SlotFlags.MASK)
            || !mask.Comp.IsToggleable)
            return;

        args.AddAction(ref mask.Comp.ToggleActionEntity, mask.Comp.ToggleAction);
        Dirty(mask);
    }

    private void OnToggleMaskAction(Entity<MaskComponent> mask, ref ToggleMaskEvent args)
    {
        if (args.Handled
            || mask.Comp.ToggleActionEntity == null
            || !mask.Comp.IsToggleable
            || !_inventorySystem.InSlotWithFlags(mask.Owner, SlotFlags.MASK))
            return;

        AttemptToggleMask(mask, args.Performer, args.Performer);

        args.Handled = true;
    }

    private void OnGetInteractionVerbs(Entity<MaskComponent> mask, ref InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>> args)
    {
        var evArgs = args.Args;

        if (!evArgs.CanAccess
            || !evArgs.CanInteract
            || evArgs.Hands is null
            || !mask.Comp.IsToggleable)
            return;

        var dir = mask.Comp.IsToggled ? "up" : "down";

        EquipmentVerb verb = new()
        {
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
            Act = () => AttemptToggleMask(mask, evArgs.User, evArgs.Target),
            Text = Loc.GetString($"verb-name-mask-pull-{dir}"),
            Message = Loc.GetString($"verb-description-mask-pull-{dir}"),
            TextStyleClass = "InteractionVerb",
        };

        evArgs.Verbs.Add(verb);
    }

    /// <summary>
    /// This is called when someone attempts to pull a mask down via a verb.
    /// </summary>
    /// <param name="mask"> The Mask Entity that is being pulled up/down.</param>
    /// <param name="puller"> The person attempting to pull down the mask.</param>
    /// <param name="wearer"> The person wearing the mask.</param>
    /// <param name="state"> The wanted state of the mask. If undefined/null, it simply toggles the mask.</param>
    /// <param name="force"> If true, it forces the mask to be toggled even if it cannot be toggled.</param>
    private void AttemptToggleMask(Entity<MaskComponent> mask, EntityUid puller, EntityUid wearer, bool? state = null, bool force = false)
    {
        TimeSpan delay;
        var dir = mask.Comp.IsToggled ? "up" : "down";

        if (puller == wearer)
        {
            delay = TimeSpan.Zero;

            var message = Loc.GetString($"action-mask-pull-{dir}-popup-message", ("mask", mask));
            _popupSystem.PopupClient(message, wearer, wearer);
        }
        else
        {
            delay = mask.Comp.Delay;

            var message = Loc.GetString($"verb-mask-pull-{dir}-popup-message", ("puller", Identity.Entity(puller, EntityManager)), ("mask", mask));
            _popupSystem.PopupEntity(message, wearer, wearer, PopupType.Medium);
        }

        var doAfterEv = new ToggleMaskDoAfterEvent(state, force, puller != wearer);
        _doAfterSystem.TryStartDoAfter(
            new DoAfterArgs(EntityManager, puller, delay, doAfterEv, mask, target: wearer)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                MovementThreshold = 0.1f,
            });
    }

    private void OnToggleMaskDoAfterEvent(Entity<MaskComponent> mask, ref ToggleMaskDoAfterEvent args)
    {
        if (_timingSystem.ApplyingState || args.Handled || args.Cancelled)
            return;

        ToggleMask(mask, args.State, args.Force);

        if (args.ByOther && args.Target.HasValue)
        {
            var dir = mask.Comp.IsToggled ? "down" : "up";

            var messageWearer =
                Loc.GetString($"verb-mask-other-pulled-{dir}-popup-message", ("puller", Identity.Entity(args.User, EntityManager)), ("mask", mask));
            _popupSystem.PopupEntity(messageWearer, args.Target.Value, args.Target.Value);

            var messagePuller =
                Loc.GetString($"verb-mask-pulled-{dir}-popup-message", ("wearer", Identity.Entity(args.Target.Value, EntityManager)), ("mask", mask));
            _popupSystem.PopupClient(messagePuller, args.User, args.User);
        }

        args.Handled = true;
    }

    private void ToggleMask(Entity<MaskComponent> mask, bool? state = null, bool force = false)
    {
        if (_timingSystem.ApplyingState
            || !force && !mask.Comp.IsToggleable)
            return;

        mask.Comp.IsToggled = state ?? !mask.Comp.IsToggled;

        if (mask.Comp.ToggleActionEntity is { } action)
            _actionSystem.SetToggled(action, mask.Comp.IsToggled);

        // TODO Generalize toggling & clothing prefixes. See also FoldableClothingComponent
        var prefix = mask.Comp.IsToggled ? mask.Comp.EquippedPrefix : null;
        _clothingSystem.SetEquippedPrefix(mask, prefix);

        EntityUid? wearer = null;
        if (_inventorySystem.InSlotWithFlags(mask.Owner, SlotFlags.MASK))
            wearer = Transform(mask).ParentUid;

        var maskEv = new ItemMaskToggledEvent(mask, wearer);
        RaiseLocalEvent(mask, ref maskEv);

        if (wearer != null)
        {
            var wearerEv = new WearerMaskToggledEvent(mask);
            RaiseLocalEvent(wearer.Value, ref wearerEv);
        }

        Dirty(mask);
    }

    private void OnFolded(Entity<MaskComponent> mask, ref FoldedEvent args)
    {
        // See FoldableClothingComponent
        if (!mask.Comp.DisableOnFolded)
            return;

        // While folded, we force the mask to be toggled / pulled down, so that its functionality as a mask is disabled,
        // and we also prevent it from being un-toggled. We also automatically untoggle it when it gets unfolded, so it
        // fully returns to its previous state when folded & unfolded.

        ToggleMask(mask, args.IsFolded, force: true);
        SetToggleable(mask, !args.IsFolded);
    }

    public void SetToggleable(Entity<MaskComponent> mask, bool toggleable)
    {
        if (_timingSystem.ApplyingState
            || mask.Comp.IsToggleable == toggleable)
            return;

        mask.Comp.IsToggleable = toggleable;

        if (mask.Comp.ToggleActionEntity is { } action)
            _actionSystem.SetEnabled(action, toggleable);

        Dirty(mask);
    }
}
