using Content.Server.Objectives;
using Robust.Shared.GameObjects;

namespace Content.Server.Mind.Systems
{
    public class ObjectivesSystem : EntitySystem
    {
        /// <summary>
        /// Adds an objective to this mind.
        /// </summary>
        public bool TryAddObjective(Mind mind, ObjectivePrototype objectivePrototype)
        {
            if (!objectivePrototype.CanBeAssigned(mind))
                return false;
            var objective = objectivePrototype.GetObjective(mind);
            if (mind.Objectives.Contains(objective))
                return false;
            mind.Objectives.Add(objective);
            return true;
        }

        /// <summary>
        /// Removes an objective to this mind.
        /// </summary>
        /// <returns>Returns true if the removal succeeded.</returns>
        public bool TryRemoveObjective(Mind mind, int index)
        {
            if (mind.Objectives.Count >= index) return false;

            var objective = mind.Objectives[index];
            mind.Objectives.Remove(objective);
            return true;
        }
    }
}
