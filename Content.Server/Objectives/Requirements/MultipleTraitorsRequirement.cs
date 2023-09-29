using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;

namespace Content.Server.Objectives.Requirements
{
    [DataDefinition]
    public sealed partial class MultipleTraitorsRequirement : IObjectiveRequirement
    {
        [DataField("traitors")]
        private int _requiredTraitors = 2;

        public bool CanBeAssigned(EntityUid mindId, MindComponent mind)
        {
            return EntitySystem.Get<TraitorRuleSystem>().GetOtherTraitorMindsAliveAndConnected(mind).Count >= _requiredTraitors;
        }
    }
}
