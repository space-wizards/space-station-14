using Robust.Shared.Serialization;

namespace Content.Shared.TextScreen
{
    [Serializable, NetSerializable]
    public enum TextScreenVisuals
    {
        On, //Should this show any text?
        Mode, //Is this a timer or a text-screen?
        ScreenText, //What text to show?
        TargetTime //What is the target time?
    }

    [Serializable, NetSerializable]
    public enum TextScreenMode
    {
        Text,
        Timer
    }
}
