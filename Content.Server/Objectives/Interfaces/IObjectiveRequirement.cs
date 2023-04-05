namespace Content.Server.Objectives.Interfaces
{
    public interface IObjectiveRequirement
    {
        /// <summary>
        /// Checks whether or not the entity & its surroundings are valid to be given the objective.
        /// </summary>
        /// <returns>Returns true if objective can be given.</returns>
        bool CanBeAssigned(Mind.Mind mind);
    }
}
