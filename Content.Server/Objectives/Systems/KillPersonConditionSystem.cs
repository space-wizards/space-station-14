using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Configuration;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles kill person condition logic and picking random kill targets.
/// </summary>
public sealed class KillPersonConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillPersonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, KillPersonConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value, comp.RequireDead, comp.RequireMaroon);
    }

    private float GetProgress(EntityUid target, bool requireDead, bool requireMaroon)
    {
        // deleted or gibbed or something, counts as dead
        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
            return 1f;

        var targetDead = _mind.IsCharacterDeadIc(mind);
        var targetOnShuttle = _emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value);
        if (!_config.GetCVar(CCVars.EmergencyShuttleEnabled) && requireMaroon)
        {
            requireDead = true;
            requireMaroon = false;
        }

        if (requireDead && !targetDead)
            return 0f;

        // Always failed if the target needs to be marooned and the shuttle hasn't even arrived yet
        if (requireMaroon && !_emergencyShuttle.EmergencyShuttleArrived)
            return 0f;

        // If the shuttle hasn't left, give 50% progress if the target isn't on the shuttle as a "almost there!"
        if (requireMaroon && !_emergencyShuttle.ShuttlesLeft)
            return targetOnShuttle ? 0f : 0.5f;

        // If the shuttle has already left, and the target isn't on it, 100%
        if (requireMaroon && _emergencyShuttle.ShuttlesLeft)
            return targetOnShuttle ? 0f : 1f;

        return 1f; // Good job you did it woohoo
    }
}
