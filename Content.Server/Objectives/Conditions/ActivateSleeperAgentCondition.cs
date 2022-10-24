using Content.Server.Objectives.Interfaces;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Server.GameTicking.Rules;

namespace Content.Server.Objectives.Conditions
{
    [DataDefinition]
    public sealed class ActivateSleeperAgentCondition : IObjectiveCondition
    {
        private Mind.Mind? _target;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            var entityMgr = IoCManager.Resolve<IEntityManager>();
            var traitors = EntitySystem.Get<AgentRuleSystem>().Agents;
            List<Agent.AgentRole> removeList = new();

            foreach (var agent in agents)
            {
                if (agent.Mind == null)
                {
                    removeList.Add(agent);
                    continue;
                }
                if (agent.Mind == mind) // shouldn't happen under normal circumstances, but keeping it here since wacky stuff could happen otherwise.
                {
                    removeList.Add(agent);
                    continue;
                }
            }

            foreach (var agent in removeList)
            {
                agents.Remove(agent);
            }

            if (agents.Count == 0) return new EscapeShuttleCondition{}; //There's no sleeper agents on station.
            return new ActivateSleeperAgentCondition { _target = IoCManager.Resolve<IRobustRandom>().Pick(agents).Mind };
        }

        public string Title
        {
            get
            {
                var targetName = string.Empty;
                var jobName = _target?.CurrentJob?.Name ?? "Unknown";

                if (_target == null)
                    return Loc.GetString("objective-condition-activate-sleeper-agent-title", ("targetName", targetName), ("job", jobName));

                if (_target.CharacterName != null)
                    targetName = _target.CharacterName;
                else if (_target.OwnedEntity is {Valid: true} owned)
                    targetName = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(owned).EntityName;

                return Loc.GetString("objective-condition-activate-sleeper-agent-title", ("targetName", targetName), ("job", jobName));
            }
        }

        public string Description => Loc.GetString("objective-condition-activate-sleeper-agent-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Misc/bureaucracy.rsi"), "folder-red");

        public float Difficulty => 1.25f;
        
        public bool Equals(IObjectiveCondition? other)
        {
           return other is ActivateSleeperAgentCondition kpc && Equals(_target, kpc._target);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is RandomTraitorAliveCondition alive && alive.Equals(this);
        }

        public override int GetHashCode()
        {
            return _target?.GetHashCode() ?? 0;
        }
    }
}
