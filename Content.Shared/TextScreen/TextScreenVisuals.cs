using Robust.Shared.Serialization;

namespace Content.Shared.TextScreen
{
    [Serializable, NetSerializable]
    public enum TextScreenVisuals
    {
        /// <summary>
        /// Should this show any text?
        /// </summary>
        On,
        /// <summary>
        /// Is this a timer or a text-screen?
        /// </summary>
        Mode,
        /// <summary>
        /// What text to show?
        /// </summary>
        ScreenText,
        /// <summary>
        /// What is the target time?
        /// </summary>
        TargetTime
    }

    [Serializable, NetSerializable]
    public enum TextScreenMode
    {
        Text,
        Timer
    }
}
