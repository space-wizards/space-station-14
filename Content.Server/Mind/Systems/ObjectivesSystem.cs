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
            if (mind._objectives.Contains(objective))
                return false;
            mind._objectives.Add(objective);
            return true;
        }

        /// <summary>
        /// Removes an objective to this mind.
        /// </summary>
        /// <returns>Returns true if the removal succeeded.</returns>
        public bool TryRemoveObjective(Mind mind, int index)
        {
            if (mind._objectives.Count >= index) return false;

            var objective = mind._objectives[index];
            mind._objectives.Remove(objective);
            return true;
        }
    }
}
