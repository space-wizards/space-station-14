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
        if (args.Target == null || !TryComp<NinjaGlovesComponent>(uid, out var gloves) || gloves.User == null)
            return;

        var user = gloves.User.Value;
        var target = args.Target.Value;
        if (!HasComp<PowerNetworkBatteryComponent>(target))
            return;

        // nicer for spam-clicking to not open apc ui, and when draining starts, so cancel the ui action
        args.Cancel();
        if (gloves.Busy)
            return;

        var doafterArgs = new DoAfterEventArgs(user, comp.DrainTime, target: target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.5f
        };

        _doafter.DoAfter(doafterArgs);
        gloves.Busy = true;
    }

    protected override bool IsCommsConsole(EntityUid uid)
    {
        return HasComp<CommunicationsConsoleComponent>(uid);
    }
}
