using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Implants;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Mindshield.FakeMindShield;

public sealed partial class FakeMindShieldSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TagSystem _tag = default!;

    // This tag should be placed on the fake mindshield action so there is a way to easily identify it.
    private static readonly ProtoId<TagPrototype> FakeMindShieldImplantTag = "FakeMindShieldImplant";

    private void ShowTogglePopup(Entity<FakeMindShieldComponent> ent)
    {
        var message = ent.Comp.IsEnabled
            ? Loc.GetString("fake-mindshield-enabled")
            : Loc.GetString("fake-mindshield-disabled");

        _popup.PopupEntity(message, ent);
    }

    [SubscribeLocalEvent]
    private void OnToggleMindshield(Entity<FakeMindShieldComponent> ent, ref FakeMindShieldToggleEvent args)
    {
        ent.Comp.IsEnabled = !ent.Comp.IsEnabled;
        args.Toggle = true;
        args.Handled = true;
        ShowTogglePopup(ent);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnChameleonControllerOutfitSelected(Entity<FakeMindShieldComponent> ent, ref ChameleonControllerOutfitSelectedEvent args)
    {
        if (ent.Comp.IsEnabled == args.ChameleonOutfit.HasMindShield)
            return;

        // This assumes there is only one fake mindshield action per entity (This is currently enforced)
        if (!TryComp<ActionsComponent>(ent, out var actionsComp))
            return;

        foreach (var action in actionsComp.Actions)
        {
            if (!_tag.HasTag(action, FakeMindShieldImplantTag))
                continue;

            if (!TryComp<ActionComponent>(action, out var actionComp))
                continue;

            if (_actions.IsCooldownActive(actionComp, _timing.CurTime))
                continue;

            _actions.SetToggled(action, args.ChameleonOutfit.HasMindShield);
            ShowTogglePopup(ent);

            if (actionComp.UseDelay != null)
                _actions.SetCooldown(action, actionComp.UseDelay.Value);
        }

        ent.Comp.IsEnabled = args.ChameleonOutfit.HasMindShield;
        Dirty(ent);
    }
}

public sealed partial class FakeMindShieldToggleEvent : InstantActionEvent;
