using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Server.Objectives.Interfaces
{
    public interface IObjectiveRequirement : IExposeData
    {
        /// <summary>
        /// Checks whether or not the entity & its surroundings are valid to be given the objective.
        /// Returns true if objective can be given.
        /// Returns false if objective cannot be given.
        /// </summary>
        bool CanBeAssigned(IEntity entity);
    }
}
