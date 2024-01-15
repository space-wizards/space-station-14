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
    ///     What text to show? <br/>
    ///     Expects a <see cref="string?[]"/>.
    /// </summary>
    ScreenText,

    /// <summary>
    ///     What is the target time? <br/>
    ///     Expects a <see cref="TimeSpan"/>.
    /// </summary>
    TargetTime
}
