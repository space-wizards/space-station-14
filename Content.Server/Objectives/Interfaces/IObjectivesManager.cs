namespace Content.Server.Objectives.Interfaces
{
    public interface IObjectivesManager
    {
        /// <summary>
        /// Returns a randomly picked objective the provided mind is valid for.
        /// </summary>
        ObjectivePrototype? GetRandomObjective(Mind.Mind mind, string objectiveGroupProto);
    }
}
