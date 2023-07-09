using Content.Server.Communications;
using Content.Server.DoAfter;
using Content.Server.Ninja.Systems;
using Content.Server.Power.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Toggleable;

namespace Content.Server.Ninja.Systems;

public sealed class NinjaGlovesSystem : SharedNinjaGlovesSystem
{
    [Dependency] private readonly EmagProviderSystem _emagProvider = default!;
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

        var enabling = comp.User == null;
        Appearance.SetData(uid, ToggleVisuals.Toggled, enabling);
        var message = Loc.GetString(enabling ? "ninja-gloves-on" : "ninja-gloves-off");
        Popup.PopupEntity(message, user, user);

        if (enabling)
        {
            comp.User = user;
            _ninja.AssignGloves(ninja, uid);
            // set up interaction relay for handling glove abilities, comp.User is used to see the actual user of the events
            // TODO: remove, bad
            Interaction.SetRelay(user, uid, EnsureComp<InteractionRelayComponent>(user));
            Dirty(comp);

            var drainer = EnsureComp<BatteryDrainerComponent>(user);
            if (_ninja.GetNinjaBattery(user, out var battery, out var _))
                drainer.BatteryUid = battery;

            var emag = EnsureComp<EmagProviderComponent>(user);
            _emagProvider.SetWhitelist(user, comp.DoorjackWhitelist, emag);
        }
        else
        {
            DisableGloves(uid, comp);
        }
    }

    /// <inheritdoc/>
    protected override void OnDownloadDoAfter(EntityUid uid, NinjaDownloadComponent comp, DownloadDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.User;
        var target = args.Target;

        if (!TryComp<SpaceNinjaComponent>(user, out var ninja)
            || !TryComp<TechnologyDatabaseComponent>(target, out var database))
            return;

        var gained = _ninja.Download(user, database.UnlockedTechnologies);
        var str = gained == 0
            ? Loc.GetString("ninja-download-fail")
            : Loc.GetString("ninja-download-success", ("count", gained), ("server", target));

        Popup.PopupEntity(str, user, user, PopupType.Medium);
    }

    /// <inheritdoc/>
    protected override void OnTerror(EntityUid uid, NinjaTerrorComponent comp, InteractionAttemptEvent args)
    {
        if (!GloveCheck(uid, args, out var gloves, out var user, out var target)
            || !_ninja.GetNinjaRole(user, out var role)
            || !HasComp<CommunicationsConsoleComponent>(target))
            return;

        // can only do it once
        if (role.CalledInThreat)
        {
            Popup.PopupEntity(Loc.GetString("ninja-terror-already-called"), user, user);
            return;
        }

        var doAfterArgs = new DoAfterArgs(user, comp.TerrorTime, new TerrorDoAfterEvent(), target: target, used: uid, eventTarget: uid)
        {
            BreakOnDamage = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.5f,
            CancelDuplicate = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Cancel();
    }

    /// <inheritdoc/>
    protected override void OnTerrorDoAfter(EntityUid uid, NinjaTerrorComponent comp, TerrorDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        _ninja.CallInThreat(args.User);
    }
}
