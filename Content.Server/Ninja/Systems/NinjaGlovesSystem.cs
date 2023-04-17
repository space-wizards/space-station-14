using Content.Server.Communications;
using Content.Server.DoAfter;
using Content.Server.Power.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;

namespace Content.Server.Ninja.Systems;

public sealed class NinjaGlovesSystem : SharedNinjaGlovesSystem
{
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

    protected override bool IsCommsConsole(EntityUid uid)
    {
        return HasComp<CommunicationsConsoleComponent>(uid);
    }
}
