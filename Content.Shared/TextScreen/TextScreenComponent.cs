using Content.Shared.TextScreen.Events;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.TextScreen.Components;

[RegisterComponent]
public sealed partial class TextScreenComponent : Component
{
    /// <summary>
    /// Text to display on the screen after a <see cref="TextScreenTextEvent"/>.
    /// </summary>
    [DataField("label"), ViewVariables]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Sound to play after a timer zeroes.
    /// </summary>
    [DataField("doneSound"), ViewVariables]
    public string? DoneSound;

    // /// <summary>
    // /// MM:SS to display on the screen after a <see cref="TextScreenTimerEvent"/>.
    // /// </summary>
    // [DataField("remaining", customTypeSerializer: typeof(TimeOffsetSerializer))]
    // public TimeSpan? Remaining;
}
