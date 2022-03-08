using Content.Server.Objectives.Interfaces;
using Content.Server.Traitor;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

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
