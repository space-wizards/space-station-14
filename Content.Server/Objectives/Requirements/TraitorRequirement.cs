using Content.Server.Mobs;
using Content.Server.Mobs.Roles.Suspicion;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Objectives.Requirements
{
    public class TraitorRequirement : IObjectiveRequirement
    {
        public void ExposeData(ObjectSerializer serializer){}

        public bool CanBeAssigned(Mind mind)
        {
            return mind.HasRole<SuspicionTraitorRole>();
        }
    }
}
