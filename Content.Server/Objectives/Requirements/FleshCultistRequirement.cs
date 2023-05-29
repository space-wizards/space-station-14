using Content.Server.Flesh;
using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;

namespace Content.Server.Objectives.Requirements
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class FleshCultistRequirement : IObjectiveRequirement
    {
        public bool CanBeAssigned(Mind.Mind mind)
        {
            return mind.HasRole<FleshCultistRole>();
        }
    }
}
