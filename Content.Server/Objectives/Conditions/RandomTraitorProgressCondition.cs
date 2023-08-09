using System.Linq;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Server.GameTicking.Rules;

namespace Content.Server.Objectives.Conditions
{
    [DataDefinition]
    public sealed class RandomTraitorProgressCondition : IObjectiveCondition
    {
        private Mind.Mind? _target;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            var entityMgr = IoCManager.Resolve<IEntityManager>();
            var traitors = entityMgr.EntitySysManager.GetEntitySystem<TraitorRuleSystem>().GetOtherTraitorsAliveAndConnected(mind).ToList();
            List<Traitor.TraitorRole> removeList = new();

            foreach (var traitor in traitors)
            {
                foreach (var objective in traitor.Mind.AllObjectives)
                {
                    foreach (var condition in objective.Conditions)
                    {
                        if (condition.GetType() == typeof(RandomTraitorProgressCondition))
                        {
                            removeList.Add(traitor);
                        }
                    }
                }
            }

            foreach (var traitor in removeList)
            {
                traitors.Remove(traitor);
            }

            if (traitors.Count == 0) return new EscapeShuttleCondition{}; //You were made a traitor by admins, and are the first/only.
            return new RandomTraitorProgressCondition { _target = IoCManager.Resolve<IRobustRandom>().Pick(traitors).Mind };
        }

        public string Title
        {
            get
            {
                var targetName = string.Empty;
                var jobName = _target?.CurrentJob?.Name ?? "Unknown";

                if (_target == null)
                    return Loc.GetString("objective-condition-other-traitor-progress-title", ("targetName", targetName), ("job", jobName));

                if (_target.OwnedEntity is {Valid: true} owned)
                    targetName = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(owned).EntityName;

                return Loc.GetString("objective-condition-other-traitor-progress-title", ("targetName", targetName), ("job", jobName));
            }
        }

        public string Description => Loc.GetString("objective-condition-other-traitor-progress-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Misc/bureaucracy.rsi"), "folder-white");

        public float Progress
        {
            get {
                var entMan = IoCManager.Resolve<IEntityManager>();

                float total = 0f; // how much progress they have
                float max = 0f; // how much progress is needed for 100%

                if (_target == null)
                {
                    Logger.Error("Null target on RandomTraitorProgressCondition.");
                    return 1f;
                }

                foreach (var objective in _target.AllObjectives)
                {
                    foreach (var condition in objective.Conditions)
                    {
                        max++; // things can only be up to 100% complete yeah
                        total += condition.Progress;
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
            return other is RandomTraitorProgressCondition kpc && Equals(_target, kpc._target);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is RandomTraitorProgressCondition alive && alive.Equals(this);
        }

        public override int GetHashCode()
        {
            return _target?.GetHashCode() ?? 0;
        }
    }
}
