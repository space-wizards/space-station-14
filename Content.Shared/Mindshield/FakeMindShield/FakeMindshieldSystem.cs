using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Implants;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mindshield.FakeMindShield;

/// <summary>
/// This system is responsible for handling the fake mindshield implant.
/// </summary>
public sealed partial class FakeMindShieldSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TagSystem _tag = default!;

    /// <summary>
    /// This tag should be placed on the fake mindshield action for identification.
    /// </summary>
    private static readonly ProtoId<TagPrototype> FakeMindShieldImplantTag = "FakeMindShieldImplant";

    /// <summary>
    /// Displays a popup to inform the player of activation or deactivation.
    /// </summary>
    /// <param name="ent">
    /// An associated tuple. The state in <see cref="FakeMindShieldComponent"/> will be used
    /// to display the appropriate popup.
    /// </param>
    private void ShowTogglePopup(Entity<FakeMindShieldComponent> ent)
    {
        var message = ent.Comp.IsEnabled
            ? Loc.GetString("fake-mindshield-enabled")
            : Loc.GetString("fake-mindshield-disabled");

        _popup.PopupEntity(message, ent, ent);
    }

    /// <summary>
    /// Run when <see cref="FakeMindShieldToggleEvent"/> is raised. Toggles the component and the action indication.
    /// </summary>
    /// <param name="ent">An associated tuple. The state in <see cref="FakeMindShieldComponent"/> will be toggled.</param>
    /// <param name="args">The event arguments. Will be marked as handled.</param>
    [SubscribeLocalEvent]
    private void OnToggleMindshield(Entity<FakeMindShieldComponent> ent, ref FakeMindShieldToggleEvent args)
    {
        ent.Comp.IsEnabled = !ent.Comp.IsEnabled;
        args.Toggle = true;
        args.Handled = true;
        ShowTogglePopup(ent);
        Dirty(ent);
    }

    /// <summary>
    /// Handles implant interactions with chameleon outfits.
    /// (De)Activates the mindshield if the chameleon outfit requires it. Displays a popup if state changes.
    /// </summary>
    /// <param name="ent">An associated tuple. The state in <see cref="FakeMindShieldComponent"/> will be set if changed.</param>
    /// <param name="args">The event arguments.</param>
    [SubscribeLocalEvent]
    private void OnChameleonControllerOutfitSelected(Entity<FakeMindShieldComponent> ent, ref ChameleonControllerOutfitSelectedEvent args)
    {
        if (ent.Comp.IsEnabled == args.ChameleonOutfit.HasMindShield)
            return;

        // This assumes there is only one fake mindshield action per entity (This is currently enforced)
        if (!TryComp<ActionsComponent>(ent, out var actionsComp))
            return;

        ent.Comp.IsEnabled = args.ChameleonOutfit.HasMindShield;

        foreach (var action in actionsComp.Actions)
        {
            if (!_tag.HasTag(action, FakeMindShieldImplantTag))
                continue;

            if (!TryComp<ActionComponent>(action, out var actionComp))
                continue;

            _actions.SetToggled(action, args.ChameleonOutfit.HasMindShield);
            ShowTogglePopup(ent);

            if (actionComp.UseDelay != null)
                _actions.SetCooldown(action, actionComp.UseDelay.Value);

            break;
        }

        Dirty(ent);
    }
}

/// <summary>
/// Generic <see cref="InstantActionEvent"/>.
/// </summary>
public sealed partial class FakeMindShieldToggleEvent : InstantActionEvent;
