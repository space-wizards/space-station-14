using Content.Server.Mobs;
using Content.Server.Mobs.Roles.Traitor;
using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Objectives.Requirements
{
    [UsedImplicitly]
    [DataDefinition]
    public class TraitorRequirement : IObjectiveRequirement
    {
        public bool CanBeAssigned(Mind mind)
        {
            return mind.HasRole<TraitorRole>();
        }
    }
}
