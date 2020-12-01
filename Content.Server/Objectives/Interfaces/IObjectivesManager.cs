using System.Collections.Generic;
using Content.Server.Mobs;

namespace Content.Server.Objectives.Interfaces
{
    public interface IObjectivesManager
    {
        /// <summary>
        /// Returns all objectives the provided mind is valid for.
        /// </summary>
        IReadOnlyList<ObjectivePrototype> GetAllPossibleObjectives(Mind mind);

        /// <summary>
        /// Returns a randomly picked objective the provided mind is valid for.
        /// </summary>
        ObjectivePrototype GetRandomObjective(Mind mind);
    }
}
