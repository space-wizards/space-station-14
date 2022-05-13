using Robust.Shared.Utility;

namespace Content.Server.Objectives.Interfaces
{
    public interface IObjectiveCondition : IEquatable<IObjectiveCondition>
    {
        /// <summary>
        /// Returns a copy of the IObjectiveCondition which is assigned to the mind.
        /// </summary>
        /// <param name="mind">Mind to assign to.</param>
        /// <returns>The new IObjectiveCondition.</returns>
        IObjectiveCondition GetAssigned(Mind.Mind mind);

        /// <summary>
        /// Returns the title of the condition.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Returns the description of the condition.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Returns a SpriteSpecifier to be used as an icon for the condition.
        /// </summary>
        SpriteSpecifier Icon { get; }

        /// <summary>
        /// Returns the current progress of the condition in % from 0 to 1.
        /// </summary>
        /// <returns>Current progress in %.</returns>
        float Progress { get; }

        /// <summary>
        /// Returns a difficulty of the condition.
        /// </summary>
        float Difficulty { get; }
    }
}
