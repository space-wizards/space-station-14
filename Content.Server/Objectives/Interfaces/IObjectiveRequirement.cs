using Content.Server.Mobs;
using Robust.Shared.Serialization;

namespace Content.Server.Objectives.Interfaces
{
    public interface IObjectiveRequirement : IExposeData
    {

        /// <summary>
        /// Checks whether or not the entity & its surroundings are valid to be given the objective.
        /// </summary>
        /// <returns>Returns true if objective can be given.</returns>
        bool CanBeAssigned(Mind mind);
    }
}
