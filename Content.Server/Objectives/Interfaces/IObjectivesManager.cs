using System.Collections.Generic;

namespace Content.Server.Objectives.Interfaces
{
    public interface IObjectivesManager
    {
        /// <summary>
        /// Returns all objectives the provided mind is valid for.
        /// </summary>
        IEnumerable<ObjectivePrototype> GetAllPossibleObjectives(Mind.Mind mind);

        /// <summary>
        /// Returns a randomly picked objective the provided mind is valid for.
        /// </summary>
        ObjectivePrototype? GetRandomObjective(Mind.Mind mind);
    }
}
