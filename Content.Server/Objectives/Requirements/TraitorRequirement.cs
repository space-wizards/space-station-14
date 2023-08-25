using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;
using TraitorRole = Content.Server.Roles.TraitorRole;

namespace Content.Server.Objectives.Requirements
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class TraitorRequirement : IObjectiveRequirement
    {
        public bool CanBeAssigned(Mind.Mind mind)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mindSystem = entityManager.System<MindSystem>();
            return mindSystem.HasRole<TraitorRole>(mind);
        }
    }
}
