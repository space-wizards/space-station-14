using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Configuration;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles the changeling absorb target objective.
/// Half of this is just a copy of the basic traitor kill target objective, except marooning doesn't count.
/// You must absorb the body that the *target mind* is attached to, meaning if the mind swaps bodies then their new body is your new target.
/// This is consistent with how teach a lesson works, although this may cause confusion about who to consume if, say, they are cloned.
/// </summary>
public sealed class AbsorbTargetConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbsorbTargetConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, AbsorbTargetConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value, comp.RequireDead, comp.Absorbed);
    }

    private float GetProgress(EntityUid target, bool requireDead, bool absorbed)
    {
        float progress = 0f;
        progress += GetKillProgress(target, requireDead);
        progress += GetAbsorbProgress(absorbed);
        return progress;
    }

    private float GetKillProgress(EntityUid target, bool requireDead)
    {
        // deleted or gibbed or something, counts as dead
        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
            return 0.5f;

        // dead is success
        if (_mind.IsCharacterDeadIc(mind))
            return 0.5f;

        // if the target has to be dead dead then don't check evac stuff
        if (requireDead)
            return 0f;

        // if evac is disabled then they really do have to be dead
        if (!_config.GetCVar(CCVars.EmergencyShuttleEnabled))
            return 0f;

        // target is escaping so you fail
        if (_emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value))
            return 0f;

        // evac has left without the target, greentext since the target is afk in space with a full oxygen tank and coordinates off.
        if (_emergencyShuttle.ShuttlesLeft)
            return 0.5f;

        // if evac is still here and target hasn't boarded, show 50% to give you an indicator that you are doing good
        return _emergencyShuttle.EmergencyShuttleArrived ? 0.25f : 0f;
    }

    private float GetAbsorbProgress(bool absorbed)
    {
        // Will be set when you absorb your target. Won't be undone if they are revived, but you'll need them dead for the other half.
        if (absorbed)
            return 0.5f;
        return 0f;
    }
}

