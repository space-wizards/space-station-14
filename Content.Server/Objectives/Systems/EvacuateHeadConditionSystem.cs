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
        [Dependency] private readonly KillPersonConditionSystem _killPersonConditionSystem = default!;

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
            _killPersonConditionSystem.OnPersonAssigned(uid, comp, ref args);
        }

        private void OnHeadAssigned(EntityUid uid, PickRandomHeadComponent comp, ref ObjectiveAssignedEvent args)
        {
            _killPersonConditionSystem.OnHeadAssigned(uid, comp, ref args);
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

