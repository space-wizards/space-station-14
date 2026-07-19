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
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    // This tag should be placed on the fake mindshield action so there is a way to easily identify it.
    private static readonly ProtoId<TagPrototype> FakeMindShieldImplantTag = "FakeMindShieldImplant";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FakeMindShieldComponent, FakeMindShieldToggleEvent>(OnToggleMindshield);
        SubscribeLocalEvent<FakeMindShieldComponent, ChameleonControllerOutfitSelectedEvent>(OnChameleonControllerOutfitSelected);
    }

    private void ShowTogglePopup(Entity<FakeMindShieldComponent> ent)
    {
        var message = ent.Comp.IsEnabled
            ? Loc.GetString("fake-mindshield-enabled")
            : Loc.GetString("fake-mindshield-disabled");

        _popup.PopupEntity(message, ent, ent, PopupType.Small);
    }

    private void OnToggleMindshield(Entity<FakeMindShieldComponent> ent, ref FakeMindShieldToggleEvent args)
    {
        ent.Comp.IsEnabled = !ent.Comp.IsEnabled;
        args.Toggle = true;
        args.Handled = true;
        ShowTogglePopup(ent);
        Dirty(ent);
    }

    private void OnChameleonControllerOutfitSelected(Entity<FakeMindShieldComponent> ent, ref ChameleonControllerOutfitSelectedEvent args)
    {
        if (ent.Comp.IsEnabled == args.ChameleonOutfit.HasMindShield)
            return;

        // This assumes there is only one fake mindshield action per entity (This is currently enforced)
        if (!TryComp<ActionsComponent>(ent, out var actionsComp))
            return;

        // In case the fake mindshield ever doesn't have an action.
        var actionFound = false;

        foreach (var action in actionsComp.Actions)
        {
            if (!_tag.HasTag(action, FakeMindShieldImplantTag))
                continue;

            if (!TryComp<ActionComponent>(action, out var actionComp))
                continue;

            actionFound = true;

            if (_actions.IsCooldownActive(actionComp, _timing.CurTime))
                continue;

            ent.Comp.IsEnabled = args.ChameleonOutfit.HasMindShield;
            _actions.SetToggled(action, args.ChameleonOutfit.HasMindShield);
            ShowTogglePopup(ent);
            Dirty(ent);

            if (actionComp.UseDelay != null)
                _actions.SetCooldown(action, actionComp.UseDelay.Value);

            return;
        }

        // If they don't have the action for some reason, still set it correctly.
        if (!actionFound)
        {
            ent.Comp.IsEnabled = args.ChameleonOutfit.HasMindShield;
            Dirty(ent);
        }
    }
}

public sealed partial class FakeMindShieldToggleEvent : InstantActionEvent;
