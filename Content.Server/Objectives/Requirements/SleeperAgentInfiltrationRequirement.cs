using Content.Server.Objectives.Interfaces;
using Content.Server.GameTicking.Rules;

namespace Content.Server.Objectives.Requirements
{
    [DataDefinition]
    public sealed class SleeperAgentInfiltrationRequirement : IObjectiveRequirement
    {
        [DataField("agents")]
        private readonly int _requiredAgents = 1;

        public bool CanBeAssigned(Mind.Mind mind)
        {
            return EntitySystem.Get<TraitorRuleSystem>().TotalTraitors >= _requiredTraitors;
        }
    }
}
