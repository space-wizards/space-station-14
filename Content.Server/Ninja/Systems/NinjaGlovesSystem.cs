using Content.Server.Communications;
using Content.Server.DoAfter;
using Content.Server.Mind;
using Content.Server.Ninja.Events;
using Content.Server.Power.Components;
using Content.Server.Roles;
using Content.Shared.Communications;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Toggleable;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Handles the toggle gloves action.
/// </summary>
public sealed class NinjaGlovesSystem : SharedNinjaGlovesSystem
{
    [Dependency] private readonly EmagProviderSystem _emagProvider = default!;
    [Dependency] private readonly CommsHackerSystem _commsHacker = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedStunProviderSystem _stunProvider = default!;
    [Dependency] private readonly SpaceNinjaSystem _ninja = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaGlovesComponent, ToggleActionEvent>(OnToggleAction);
    }

    /// <summary>
    /// Toggle gloves, if the user is a ninja wearing a ninja suit.
    /// </summary>
    private void OnToggleAction(EntityUid uid, NinjaGlovesComponent comp, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var user = args.Performer;
        // need to wear suit to enable gloves
        if (!TryComp<SpaceNinjaComponent>(user, out var ninja)
            || ninja.Suit == null
            || !HasComp<NinjaSuitComponent>(ninja.Suit.Value))
        {
            Popup.PopupEntity(Loc.GetString("ninja-gloves-not-wearing-suit"), user, user);
            return;
        }

        // show its state to the user
        var enabling = comp.User == null;
        Appearance.SetData(uid, ToggleVisuals.Toggled, enabling);
        var message = Loc.GetString(enabling ? "ninja-gloves-on" : "ninja-gloves-off");
        Popup.PopupEntity(message, user, user);

        if (enabling)
        {
            EnableGloves(uid, comp, user, ninja);
        }
        else
        {
            DisableGloves(uid, comp);
        }
    }

    private void EnableGloves(EntityUid uid, NinjaGlovesComponent comp, EntityUid user, SpaceNinjaComponent ninja)
    {
        // can't use abilities if suit is not equipped, this is checked elsewhere but just making sure to satisfy nullability
        if (ninja.Suit == null)
            return;

        comp.User = user;
        Dirty(uid, comp);
        _ninja.AssignGloves(user, uid, ninja);

        var drainer = EnsureComp<BatteryDrainerComponent>(user);
        var stun = EnsureComp<StunProviderComponent>(user);
        _stunProvider.SetNoPowerPopup(user, "ninja-no-power", stun);
        if (_ninja.GetNinjaBattery(user, out var battery, out var _))
        {
            var ev = new NinjaBatteryChangedEvent(battery.Value, ninja.Suit.Value);
            RaiseLocalEvent(user, ref ev);
        }

        var emag = EnsureComp<EmagProviderComponent>(user);
        _emagProvider.SetWhitelist(user, comp.DoorjackWhitelist, emag);

        EnsureComp<ResearchStealerComponent>(user);
        // prevent calling in multiple threats by toggling gloves after
        if (_mind.TryGetRole<NinjaRoleComponent>(user, out var role) && !role.CalledInThreat)
        {
            var hacker = EnsureComp<CommsHackerComponent>(user);
            var rule = _ninja.NinjaRule(user);
            if (rule != null)
                _commsHacker.SetThreats(user, rule.Threats, hacker);
        }
    }
}
