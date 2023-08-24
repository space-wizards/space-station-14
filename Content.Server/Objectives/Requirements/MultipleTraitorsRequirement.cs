using Content.Server.Objectives.Interfaces;
using Content.Server.GameTicking.Rules;

namespace Content.Server.Objectives.Requirements
{
    [DataDefinition]
    public sealed partial class MultipleTraitorsRequirement : IObjectiveRequirement
    {
        [DataField("traitors")]
        private int _requiredTraitors = 2;

        public bool CanBeAssigned(Mind.Mind mind)
        {
            return EntitySystem.Get<TraitorRuleSystem>().GetOtherTraitorsAliveAndConnected(mind).Count >= _requiredTraitors;
        }
    }
}
