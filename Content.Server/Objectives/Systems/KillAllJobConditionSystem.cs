using System;
using System.Linq;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Mind.Filters;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Configuration;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Evaluates progress for <see cref="KillAllJobConditionComponent"/> by checking all alive minds with the target job.
/// 100% when no target job minds remain alive/escaping based on the requirement flags.
/// </summary>
public sealed class KillAllJobConditionSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KillAllJobConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, KillAllJobConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        // Gather all living humans and filter by job
        var minds = _mind.GetAliveHumans();
        var filter = new JobMindFilter { Job = comp.Job };
        _mind.FilterMinds(minds, filter);

        if (minds.Count == 0)
        {
            args.Progress = 1f; // no targets exist
            return;
        }

        // Handle shuttle disabled edge-case mirroring KillPersonConditionSystem.
        var requireDead = comp.RequireDead;
        var requireMaroon = comp.RequireMaroon;
        if (_cfg.GetCVar(CCVars.EmergencyShuttleEnabled) == false && requireMaroon)
        {
            requireDead = true;
            requireMaroon = false;
        }

        var total = 0;
        var satisfied = 0;

        foreach (var mind in minds)
        {
            total++;

            // Deleted/gibbed counts as dead
            if (!_mind.IsCharacterDeadIc(mind.Comp))
            {
                // alive. If require dead -> not satisfied.
                if (requireDead)
                    continue;

                // If require maroon check shuttle status windows
                if (requireMaroon)
                {
                    // Always 0 before shuttle arrives
                    if (!_emergencyShuttle.EmergencyShuttleArrived)
                        continue;

                    // If shuttle hasn't left yet, being off shuttle is partial; for aggregate, we won't grant partial per-target.
                    // We will consider as satisfied only after shuttles left and target not escaping.
                    if (_emergencyShuttle.ShuttlesLeft)
                    {
                        var escaping = mind.Comp.OwnedEntity != null && _emergencyShuttle.IsTargetEscaping(mind.Comp.OwnedEntity.Value);
                        if (!escaping || _mind.IsCharacterUnrevivableIc(mind.Comp))
                            satisfied++;
                    }

                    continue;
                }

                // neither requireDead nor requireMaroon => kill-all only satisfied if everyone dead; since this mind alive, not satisfied
                continue;
            }

            // dead
            satisfied++;
        }

        // If requireMaroon and shuttle hasn't left yet, allow partial overall progress similar to KillPersonCondition.
        if (requireMaroon && !_emergencyShuttle.ShuttlesLeft)
        {
            // If none of the targets are on the shuttle yet, grant 0.5 if all currently marooned/ dead.
            // For simplicity, compute if any alive target exists. If any alive target exists progress=0, else 0.5
            var anyAlive = minds.Any(m => !_mind.IsCharacterDeadIc(m.Comp));
            args.Progress = anyAlive ? 0f : 0.5f;
            return;
        }

        var progress = total == 0 ? 1f : (float) satisfied / total;
        // Only consider complete when all satisfied
        if (progress < 1f)
            progress = MathF.Min(progress, 0.99f);
        args.Progress = progress;
    }
}
