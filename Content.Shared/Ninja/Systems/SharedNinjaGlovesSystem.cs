using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Communications;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Toggleable;
using Robust.Shared.Timing;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// Provides the toggle action and handles examining and unequipping.
/// </summary>
public abstract class SharedNinjaGlovesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] protected readonly SharedInteractionSystem Interaction = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaGlovesComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<NinjaGlovesComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NinjaGlovesComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<NinjaGlovesComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, NinjaGlovesComponent component, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
        Dirty(uid, component);
    }

    /// <summary>
    /// Disable glove abilities and show the popup if they were enabled previously.
    /// </summary>
    public void DisableGloves(EntityUid uid, NinjaGlovesComponent? comp = null)
    {
        // already disabled?
        if (!Resolve(uid, ref comp) || comp.User == null)
            return;

        var user = comp.User.Value;
        comp.User = null;
        Dirty(uid, comp);

        Appearance.SetData(uid, ToggleVisuals.Toggled, false);
        Popup.PopupClient(Loc.GetString("ninja-gloves-off"), user, user);

        RemComp<BatteryDrainerComponent>(user);
        RemComp<EmagProviderComponent>(user);
        RemComp<StunProviderComponent>(user);
        RemComp<ResearchStealerComponent>(user);
        RemComp<CommsHackerComponent>(user);
    }

    /// <summary>
    /// Adds the toggle action when equipped.
    /// </summary>
    private void OnGetItemActions(EntityUid uid, NinjaGlovesComponent comp, GetItemActionsEvent args)
    {
        if (HasComp<SpaceNinjaComponent>(args.User))
            args.AddAction(ref comp.ToggleActionEntity, comp.ToggleAction);
    }

    /// <summary>
    /// Show if the gloves are enabled when examining.
    /// </summary>
    private void OnExamined(EntityUid uid, NinjaGlovesComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(Loc.GetString(comp.User != null ? "ninja-gloves-examine-on" : "ninja-gloves-examine-off"));
    }

    /// <summary>
    /// Disable gloves when unequipped and clean up ninja's gloves reference
    /// </summary>
    private void OnUnequipped(EntityUid uid, NinjaGlovesComponent comp, GotUnequippedEvent args)
    {
        if (comp.User != null)
        {
            var user = comp.User.Value;
            Popup.PopupClient(Loc.GetString("ninja-gloves-off"), user, user);
            DisableGloves(uid, comp);
        }
    }


    // TODO: generic event thing
    /// <summary>
    /// GloveCheck but for abilities stored on the player, skips some checks.
    /// Intended to be more generic, doesn't require the user to be a ninja or have any ninja equipment.
    /// </summary>
    public bool AbilityCheck(EntityUid uid, BeforeInteractHandEvent args, out EntityUid target)
    {
        target = args.Target;
        return _timing.IsFirstTimePredicted
            && !_combatMode.IsInCombatMode(uid)
            && TryComp<HandsComponent>(uid, out var hands)
            && hands.ActiveHandEntity == null
            && Interaction.InRangeUnobstructed(uid, target);
    }
}
