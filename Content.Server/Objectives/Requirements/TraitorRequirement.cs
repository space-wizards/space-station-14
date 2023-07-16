using Content.Server.Objectives.Interfaces;
using Content.Server.Roles;
using Content.Server.Traitor;
using JetBrains.Annotations;
using TraitorRole = Content.Server.Roles.TraitorRole;

namespace Content.Server.Objectives.Requirements
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class TraitorRequirement : IObjectiveRequirement
    {
        public bool CanBeAssigned(Mind.Mind mind)
        {
            return mind.HasRole<TraitorRole>();
        }
    }
}
