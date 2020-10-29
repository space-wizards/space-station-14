using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Objectives.Interfaces
{
    public interface IObjectivesManager
    {
        /// <summary>
        /// Returns all objectives the provided entity is valid for.
        /// </summary>
        ObjectivePrototype[] GetAllPossibleObjectives(IEntity entity);

        /// <summary>
        /// Returns a randomly picked (no pop) collection of objectives the provided entity is valid for.
        /// </summary>
        ObjectivePrototype[] GetRandomObjectives(IEntity entity, float maxDifficulty = 3f);

        /// <summary>
        /// Assigns a objective to the entity.
        /// </summary>
        public void AssignObjective(IEntity entity, ObjectivePrototype objective)
        {
            AssignObjectives(entity, new[] {objective});
        }

        /// <summary>
        /// Assigns the objectives to the entity.
        /// </summary>
        void AssignObjectives(IEntity entity, ObjectivePrototype[] objectives);
    }
}
