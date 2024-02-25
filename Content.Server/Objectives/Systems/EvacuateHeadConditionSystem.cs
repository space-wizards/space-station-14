using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Mind;
using Content.Server.GameTicking.Rules;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using System.Linq;
using Content.Shared.Cuffs.Components;

namespace Content.Server.Objectives.Systems
{

    public sealed class EvacuateHeadConditionSystem : EntitySystem
    {
        [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedJobSystem _job = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly TargetObjectiveSystem _target = default!;
        [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RandomHeadEvacuateComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        }

        private void OnGetProgress(EntityUid uid, RandomHeadEvacuateComponent comp, ref ObjectiveGetProgressEvent args)
        {
            if (!_target.GetTarget(uid, out var target))
                return;

            args.Progress = GetProgress(target.Value);
        }

        private void OnPersonAssigned(EntityUid uid, PickRandomPersonComponent comp, ref ObjectiveAssignedEvent args)
        {
            if (!TryComp<TargetObjectiveComponent>(uid, out var target))
            {
                args.Cancelled = true;
                return;
            }

            // target already assigned
            if (target.Target != null)
                return;
        
            // no other humans
            var allHumans = _mind.GetAliveHumansExcept(args.MindId);
            if (allHumans.Count == 0)
            {
                args.Cancelled = true;
                return;
            }

            _target.SetTarget(uid, _random.Pick(allHumans), target);
        }

        private void OnHeadAssigned(EntityUid uid, PickRandomHeadComponent comp, ObjectiveAssignedEvent args)
        {
            if (!TryComp<TargetObjectiveComponent>(uid, out var target))
            {
                args.Cancelled = true;
                return;
            }
            // target already assigned
            if (target.Target != null)
                return;
            // no other humans
            var allHumans = _mind.GetAliveHumansExcept(args.MindId);
            if (allHumans.Count == 0)
            {
                args.Cancelled = true;
                return;
            }
            // new list
            var allHeads = new List<EntityUid>();
            foreach (var mind in allHumans)
            {
                if (_job.MindTryGetJob(mind, out _, out var prototype) && prototype.RequireAdminNotify)
                    allHeads.Add(mind);
            }
            
            if (allHeads.Count == 0)
                return; // not the head=person because it causes errors

            _target.SetTarget(uid, _random.Pick(allHeads), target);
        }
        
        private float GetProgress(EntityUid mindId)
        {
            if (!TryComp<MindComponent>(mindId, out var mind))
                return 0f;

            // they will not evacuate if they are restrained
            if (TryComp<CuffableComponent>(mind.OwnedEntity, out var cuffed) && cuffed.CuffedHandCount > 0)
                return 0f;

            // any emergency shuttle counts for this objective, but not pods.
            if (mind.OwnedEntity != null && _emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value))
                return 1f;

            // evacuation went without purpose, loss
            if (_emergencyShuttle.ShuttlesLeft)
                return 0f;
            
            // dead is loss ...
            if (_mind.IsCharacterDeadIc(mind))
                return 0f;
            else
                return 0.5f;
        }
    }
}

