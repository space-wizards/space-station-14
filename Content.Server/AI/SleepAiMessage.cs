using Content.Server.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.AI
{
    /// <summary>
    ///     Indicates whether an AI should be updated by the AiSystem or not.
    ///     Useful to sleep AI when they die or otherwise should be inactive.
    /// </summary>
    internal sealed class SleepAiMessage : EntityEventArgs
    {
        /// <summary>
        ///     Sleep or awake.
        /// </summary>
        public bool Sleep { get; }
        public AiControllerComponent Component { get; }

        public SleepAiMessage(AiControllerComponent component, bool sleep)
        {
            Component = component;
            Sleep = sleep;
        }
    }
}
