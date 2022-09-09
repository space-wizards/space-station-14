using Content.Server.Objectives.Interfaces;
using Content.Server.GameTicking.Rules;

namespace Content.Server.Objectives.Requirements
{

    [DataDefinition]
    public sealed class NotRoleRequirement : IObjectiveRequirement
    {
        [DataField("roleId")]
        private string roleId = "";

        /// <summary>
        /// This requirement is met if the traitor is NOT the roleId, and fails if they are.
        /// </summary>
        public bool CanBeAssigned(Mind.Mind mind)
        {
            if (mind.CurrentJob == null) // no job no problems
                return true;

            return (mind.CurrentJob.Prototype.ID != roleId);
        }
    }
}
