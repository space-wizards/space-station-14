using Robust.Shared.GameObjects;

namespace Content.Server.AI.Utility.Actions
{
    public interface IAiUtility
    {
        /// <summary>
        ///     NPC this action is attached to.
        /// </summary>
        EntityUid Owner { get; set; }

        /// <summary>
        ///     Highest possible score for this action.
        /// </summary>
        float Bonus { get; }
    }
}
