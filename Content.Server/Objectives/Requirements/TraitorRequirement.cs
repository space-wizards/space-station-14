using Content.Server.Objectives.Interfaces;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Objectives.Requirements
{
    public class TraitorRequirement : IObjectiveRequirement
    {
        public void ExposeData(ObjectSerializer serializer){}

        public bool CanBeAssigned(IEntity entity)
        {
            return true; //todo detect if traitor
        }
    }
}
