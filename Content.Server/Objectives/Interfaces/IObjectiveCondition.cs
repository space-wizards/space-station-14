using Content.Server.Mobs;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Interfaces
{
    public interface IObjectiveCondition : IExposeData
    {
        /// <summary>
        /// Returns the title of the condition.
        /// </summary>
        string GetTitle();

        /// <summary>
        /// Returns the description of the condition.
        /// </summary>
        string GetDescription();

        /// <summary>
        /// Returns a SpriteSpecifier to be used as an icon for the condition.
        /// </summary>
        SpriteSpecifier GetIcon();

        /// <summary>
        /// Returns the current progress of the condition in %.
        /// </summary>
        /// <returns>Current progress in %.</returns>
        float GetProgress(Mind mind);

        /// <summary>
        /// Returns a difficulty of the condition.
        /// </summary>
        float GetDifficulty();
    }
}
