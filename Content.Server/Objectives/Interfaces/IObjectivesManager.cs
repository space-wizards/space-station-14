using Content.Server.Mobs;

namespace Content.Server.Objectives.Interfaces
{
    public interface IObjectivesManager
    {
        /// <summary>
        /// Returns all objectives the provided mind is valid for.
        /// </summary>
        ObjectivePrototype[] GetAllPossibleObjectives(Mind mind);

        /// <summary>
        /// Returns a randomly picked (no pop) collection of objectives the provided mind is valid for.
        /// </summary>
        ObjectivePrototype[] GetRandomObjectives(Mind mind, float maxDifficulty = 3f);
    }
}
