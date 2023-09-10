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
    public sealed partial class RandomTraitorProgressCondition : IObjectiveCondition
    {
        // TODO ecs all of this
        private EntityUid? _targetMind;

        public IObjectiveCondition GetAssigned(EntityUid mindId, MindComponent mind)
        {
            //todo shit of a fuck
            var entityMgr = IoCManager.Resolve<IEntityManager>();

            var traitors = entityMgr.System<TraitorRuleSystem>().GetOtherTraitorMindsAliveAndConnected(mind).ToList();
            List<EntityUid> removeList = new();

            foreach (var traitor in traitors)
            {
                foreach (var objective in traitor.Mind.AllObjectives)
                {
                    foreach (var condition in objective.Conditions)
                    {
                        if (condition is RandomTraitorProgressCondition)
                        {
                            removeList.Add(traitor.Id);
                        }
                    }
                }
            }

            foreach (var traitor in removeList)
            {
                traitors.RemoveAll(t => t.Id == traitor);
            }

            if (traitors.Count == 0) return new EscapeShuttleCondition{}; //You were made a traitor by admins, and are the first/only.
            return new RandomTraitorProgressCondition { _targetMind = IoCManager.Resolve<IRobustRandom>().Pick(traitors).Id };
        }

        public string Title
        {
            get
            {
                var targetName = string.Empty;
                var entities = IoCManager.Resolve<IEntityManager>();
                var jobs = entities.System<SharedJobSystem>();
                var jobName = jobs.MindTryGetJobName(_targetMind);

                if (_targetMind == null)
                    return Loc.GetString("objective-condition-other-traitor-progress-title", ("targetName", targetName), ("job", jobName));

                if (entities.TryGetComponent(_targetMind, out MindComponent? mind) &&
                    mind.OwnedEntity is {Valid: true} owned)
                {
                    targetName = entities.GetComponent<MetaDataComponent>(owned).EntityName;
                }

                return Loc.GetString("objective-condition-other-traitor-progress-title", ("targetName", targetName), ("job", jobName));
            }
        }

        public string Description => Loc.GetString("objective-condition-other-traitor-progress-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ("Objects/Misc/bureaucracy.rsi"), "folder-white");

        public float Progress
        {
            get
            {
                float total = 0f; // how much progress they have
                float max = 0f; // how much progress is needed for 100%

                if (_targetMind == null)
                {
                    Logger.Error("Null target on RandomTraitorProgressCondition.");
                    return 1f;
                }

                var entities = IoCManager.Resolve<IEntityManager>();
                if (entities.TryGetComponent(_targetMind, out MindComponent? mind))
                {
                    foreach (var objective in mind.AllObjectives)
                    {
                        foreach (var condition in objective.Conditions)
                        {
                            max++; // things can only be up to 100% complete yeah
                            total += condition.Progress;
                        }
                    }
                }

                if (max == 0f)
                {
                    Logger.Error("RandomTraitorProgressCondition assigned someone with no objectives to be helped.");
                    return 1f;
                }

                var completion = total / max;

                if (completion >= 0.5f)
                    return 1f;
                else
                    return completion / 0.5f;
            }
        }

        public float Difficulty => 2.5f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is RandomTraitorProgressCondition kpc && Equals(_targetMind, kpc._targetMind);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is RandomTraitorProgressCondition alive && alive.Equals(this);
        }

        public override int GetHashCode()
        {
            return _targetMind?.GetHashCode() ?? 0;
        }
    }
}
