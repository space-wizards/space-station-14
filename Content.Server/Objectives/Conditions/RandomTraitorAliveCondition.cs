using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Shared.Mind;
using Content.Shared.Objectives.Interfaces;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [DataDefinition]
    public sealed partial class RandomTraitorAliveCondition : IObjectiveCondition
    {
        private EntityUid? _targetMind;

        public IObjectiveCondition GetAssigned(EntityUid mindId, MindComponent mind)
        {
            var entityMgr = IoCManager.Resolve<IEntityManager>();

            var traitors = Enumerable.ToList<(EntityUid Id, MindComponent Mind)>(entityMgr.System<TraitorRuleSystem>().GetOtherTraitorMindsAliveAndConnected(mind));

            if (traitors.Count == 0)
                return new EscapeShuttleCondition(); //You were made a traitor by admins, and are the first/only.
            return new RandomTraitorAliveCondition { _targetMind = IoCManager.Resolve<IRobustRandom>().Pick(traitors).Id };
        }

        public string Title
        {
            get
            {
                var targetName = string.Empty;
                var ents = IoCManager.Resolve<IEntityManager>();
                var jobs = ents.System<SharedJobSystem>();
                var jobName = jobs.MindTryGetJobName(_targetMind);

                if (_targetMind == null)
                    return Loc.GetString("objective-condition-other-traitor-alive-title", ("targetName", targetName), ("job", jobName));

                if (ents.TryGetComponent(_targetMind, out MindComponent? mind) &&
                    mind.OwnedEntity is {Valid: true} owned)
                {
                    targetName = ents.GetComponent<MetaDataComponent>(owned).EntityName;
                }

                return Loc.GetString("objective-condition-other-traitor-alive-title", ("targetName", targetName), ("job", jobName));
            }
        }

        public string Description => Loc.GetString("objective-condition-other-traitor-alive-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ("Objects/Misc/bureaucracy.rsi"), "folder-white");

        public float Progress
        {
            get
            {
                var entityManager = IoCManager.Resolve<EntityManager>();
                var mindSystem = entityManager.System<SharedMindSystem>();
                return !entityManager.TryGetComponent(_targetMind, out MindComponent? mind) ||
                       !mindSystem.IsCharacterDeadIc(mind)
                    ? 1f
                    : 0f;
            }
        }

        public float Difficulty => 1.75f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is RandomTraitorAliveCondition kpc && Equals(_targetMind, kpc._targetMind);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is RandomTraitorAliveCondition alive && alive.Equals(this);
        }

        public override int GetHashCode()
        {
            return _targetMind?.GetHashCode() ?? 0;
        }
    }
}
