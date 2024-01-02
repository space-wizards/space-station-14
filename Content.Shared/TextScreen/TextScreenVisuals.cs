using Robust.Shared.Serialization;

namespace Content.Shared.TextScreen;

[Serializable, NetSerializable]
public enum TextScreenVisuals : byte
{
    /// <summary>
    ///     What text to show? <br/>
    ///     Expects a <see cref="string?[]"/>.
    /// </summary>
    ScreenText,

    /// <summary>
    ///     What is the target time? <br/>
    ///     Expects a <see cref="TimeSpan"/>.
    /// </summary>
    TargetTime,

    /// <summary>
    ///     Change text color on the entire screen
    ///     Expects a <see cref="Color"/>.
    /// </summary>
    Color
}
