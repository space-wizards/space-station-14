// using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

// namespace Content.Shared.TextScreen.Components;

// [RegisterComponent]
// public sealed partial class TextScreenComponent : Component
// {
//     public const int Rows = 2;
//     /// <summary>
//     /// Text to display on the screen after a <see cref="TextScreenTextEvent"/>.
//     /// </summary>
//     [DataField("text"), ViewVariables]
//     public string?[] Text { get; set; } = new string[Rows];

//     /// <summary>
//     /// Sound to play after a timer zeroes.
//     /// </summary>
//     [DataField("doneSound"), ViewVariables]
//     public string? DoneSound;

//     // /// <summary>
//     // /// MM:SS to display on the screen after a <see cref="TextScreenTimerEvent"/>.
//     // /// </summary>
//     // [DataField("remaining", customTypeSerializer: typeof(TimeOffsetSerializer))]
//     // public TimeSpan? Remaining;
// }
