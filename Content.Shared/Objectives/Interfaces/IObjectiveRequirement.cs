using Content.Shared.Mind;

namespace Content.Shared.Objectives.Interfaces
{
    // TODO refactor all of this to be ecs
    public interface IObjectiveRequirement
    {
        /// <summary>
        /// Checks whether or not the entity & its surroundings are valid to be given the objective.
        /// </summary>
        /// <returns>Returns true if objective can be given.</returns>
        bool CanBeAssigned(EntityUid mindId, MindComponent mind);
    }
}
