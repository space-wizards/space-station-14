using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Content.Server.Traitor;
using JetBrains.Annotations;

namespace Content.Server.Objectives.Requirements
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class TraitorRequirement : IObjectiveRequirement
    {
        public bool CanBeAssigned(Mind.Mind mind)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mindSystem = entityManager.System<MindSystem>();
            return mindSystem.HasRole<TraitorRole>(mind);
        }
    }
}
