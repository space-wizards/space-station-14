using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Implants;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Mindshield.FakeMindShield;

public sealed class FakeMindShieldSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

    // This tag should be placed on the fake mindshield action so there is a way to easily identify it.
    private static readonly ProtoId<TagPrototype> FakeMindShieldImplantTag = "FakeMindShieldImplant";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FakeMindShieldComponent, FakeMindShieldToggleEvent>(OnToggleMindshield);
        SubscribeLocalEvent<FakeMindShieldComponent, ChameleonControllerOutfitSelectedEvent>(OnChameleonControllerOutfitSelected);
    }
    private void ShowTogglePopup(EntityUid uid, FakeMindShieldComponent comp)
    {
        if (!_net.IsServer)
            return;

        var message = comp.IsEnabled
            ? Loc.GetString("fake-mindshield-enabled")
            : Loc.GetString("fake-mindshield-disabled");

        _popup.PopupEntity(message, uid, uid, PopupType.Small);
    }

    private void OnToggleMindshield(EntityUid uid, FakeMindShieldComponent comp, FakeMindShieldToggleEvent args)
    {
        comp.IsEnabled = !comp.IsEnabled;
        args.Toggle = true;
        args.Handled = true;
        ShowTogglePopup(uid, comp);
        Dirty(uid, comp);
    }

    private void OnChameleonControllerOutfitSelected(EntityUid uid, FakeMindShieldComponent component, ChameleonControllerOutfitSelectedEvent args)
    {
        if (component.IsEnabled == args.ChameleonOutfit.HasMindShield)
            return;

        // This assumes there is only one fake mindshield action per entity (This is currently enforced)
        if (!TryComp<ActionsComponent>(uid, out var actionsComp))
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

            component.IsEnabled = args.ChameleonOutfit.HasMindShield;
            _actions.SetToggled(action, args.ChameleonOutfit.HasMindShield);
            ShowTogglePopup(uid, component);
            Dirty(uid, component);

            if (actionComp.UseDelay != null)
                _actions.SetCooldown(action, actionComp.UseDelay.Value);

            return;
        }

        // If they don't have the action for some reason, still set it correctly.
        if (!actionFound)
        {
            component.IsEnabled = args.ChameleonOutfit.HasMindShield;
            Dirty(uid, component);
        }
    }
}

public sealed partial class FakeMindShieldToggleEvent : InstantActionEvent;
