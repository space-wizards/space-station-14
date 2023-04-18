using Content.Server.Communications;
using Content.Server.DoAfter;
using Content.Server.Ninja.Systems;
using Content.Server.Power.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.Research.Components;

namespace Content.Server.Ninja.Systems;

public sealed class NinjaGlovesSystem : SharedNinjaGlovesSystem
{
    [Dependency] private readonly new NinjaSystem _ninja = default!;

    protected override void OnDrain(EntityUid uid, NinjaDrainComponent comp, InteractionAttemptEvent args)
    {
        if (!GloveCheck(uid, args, out var gloves, out var user, out var target)
            || !HasComp<PowerNetworkBatteryComponent>(target))
            return;

        // nicer for spam-clicking to not open apc ui, and when draining starts, so cancel the ui action
        args.Cancel();

        var doAfterArgs = new DoAfterArgs(user, comp.DrainTime, new DrainDoAfterEvent(), target: target, used: uid, eventTarget: uid)
        {
            BreakOnUserMove = true,
            MovementThreshold = 0.5f,
            CancelDuplicate = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    protected override void OnDownloadDoAfter(EntityUid uid, NinjaDownloadComponent comp, DownloadDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.User;
        var target = args.Target;

        if (!TryComp<NinjaComponent>(user, out var ninja)
            || !TryComp<TechnologyDatabaseComponent>(target, out var database))
            return;

        var gained = _ninja.Download(uid, database.TechnologyIds);
        var str = gained == 0
            ? Loc.GetString("ninja-download-fail")
            : Loc.GetString("ninja-download-success", ("count", gained), ("server", target));

        Popups.PopupEntity(str, user, user, PopupType.Medium);
    }

    protected override void OnTerror(EntityUid uid, NinjaTerrorComponent comp, InteractionAttemptEvent args)
    {
        if (!GloveCheck(uid, args, out var gloves, out var user, out var target)
            || !_ninja.GetNinjaRole(user, out var role)
            || !HasComp<CommunicationsConsoleComponent>(target))
            return;


        // can only do it once
        if (role.CalledInThreat)
        {
            Popups.PopupEntity(Loc.GetString("ninja-terror-already-called"), user, user);
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
        // FIXME: doesnt work, don't show the console popup
        args.Cancel();
    }

    protected override void OnTerrorDoAfter(EntityUid uid, NinjaTerrorComponent comp, TerrorDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        _ninja.CallInThreat(args.User);
    }
}
