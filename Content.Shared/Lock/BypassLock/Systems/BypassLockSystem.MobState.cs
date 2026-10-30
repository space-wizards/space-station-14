using Content.Shared.Lock.BypassLock.Components;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Lock.BypassLock.Systems;

public sealed partial class BypassLockSystem
{
    private void InitializeMobStateLockSystem()
    {
        SubscribeLocalEvent<BypassLockRequiresMobStateComponent, ForceOpenLockAttemptEvent>(OnForceOpenLockAttempt);
        SubscribeLocalEvent<BypassLockRequiresMobStateComponent, CheckBypassLockVerbRequirements>(OnGetVerb);
    }

    private void OnForceOpenLockAttempt(Entity<BypassLockRequiresMobStateComponent> target, ref ForceOpenLockAttemptEvent args)
    {
        if (!TryComp<MobStateComponent>(target, out var mobState))
            return;

        args.CanForceOpen &= target.Comp.RequiredMobState.Contains(mobState.CurrentState);
    }

    private void OnGetVerb(Entity<BypassLockRequiresMobStateComponent> target, ref CheckBypassLockVerbRequirements args)
    {
        if (!TryComp<MobStateComponent>(target, out var mobState))
            return;

        // Only show disabled verb on a too healthy target when they have the right tool.
        if (!target.Comp.RequiredMobState.Contains(mobState.CurrentState) && args.RightTool)
        {
            args.Verb.Disabled = true;
            args.Verb.Message = Loc.GetString("bypass-lock-disabled-healthy");
        }
        // Show verb of using the wrong tool when the target is critical.
        else if (target.Comp.RequiredMobState.Contains(mobState.CurrentState) && !args.RightTool)
        {
            args.ShowVerb = true;
            args.Verb.Disabled = true;
            args.Verb.Message = Loc.GetString("bypass-lock-disabled-wrong-tool", ("quality", args.ToolQuality.ToString().ToLower()));
        }
    }
}
