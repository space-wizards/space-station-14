using Robust.Shared.Serialization;

namespace Content.Shared.TextScreen;

[Serializable, NetSerializable]
public enum TextScreenVisuals : byte
{
    /// <summary>
    ///     Should this show any text? <br/>
    ///     Expects a <see cref="bool"/>.
    /// </summary>
    On,
    /// <summary>
    ///     Is this a timer or a text-screen? <br/>
    ///     Expects a <see cref="TextScreenMode"/>.
    /// </summary>
    Mode,
    /// <summary>
    ///     What text to show? <br/>
    ///     Expects a <see cref="string"/>.
    /// </summary>
    ScreenText,
    /// <summary>
    ///     What is the target time? <br/>
    ///     Expects a <see cref="TimeSpan"/>.
    /// </summary>
    TargetTime
}

[Serializable, NetSerializable]
public enum TextScreenMode : byte
{
    Text,
    Timer
}
