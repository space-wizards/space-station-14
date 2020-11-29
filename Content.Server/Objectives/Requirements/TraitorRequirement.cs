using Content.Server.Mobs;
using Content.Server.Mobs.Roles.Traitor;
using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Server.Objectives.Requirements
{
    [UsedImplicitly]
    public class TraitorRequirement : IObjectiveRequirement
    {
        public void ExposeData(ObjectSerializer serializer){}

        public bool CanBeAssigned(Mind mind)
        {
            return mind.HasRole<TraitorRole>();
        }
    }
}
