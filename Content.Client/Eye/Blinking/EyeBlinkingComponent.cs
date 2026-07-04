using Robust.Client.GameObjects;
using Content.Shared.Eye.Blinking;

namespace Content.Client.Eye.Blinking;

/// <summary>
/// A client component that manages eyelid states (e.g., when they need to be opened, closed, etc.). Attached to an entity after the <see cref="EyeBlinkingComponent"> component is initialized.
/// </summary>

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class EyeBlinkingClientComponent : Component
{
    /// <summary>
    /// for when the component is paused, this is the offset to apply to the next blink time and next open eyes time to account for the pause duration.
    /// </summary>
    [AutoPausedField]
    public TimeSpan PausedOffset;

    /// <summary>
    /// List of all EyelidState objects for the entity. Each EyelidState represents the state of a single eyelid layer, including whether it is closed, whether it is a complete blink, and the scheduled times for closing and opening.
    /// </summary>
    [ViewVariables]
    public List<EyelidState> Eyelids = new();
}

/// <summary>
/// Represents the state of an eyelid layer, including whether it is closed, whether it is a complete blink, and the scheduled times for closing and opening.
/// Can be extended in the future to include additional properties related to eyelid behavior, such as blink speed or eyelid color, or force closing the eyelid by health eye, or other factors.
/// </summary>
public sealed partial class EyelidState
{
    /// <summary>
    /// The sprite layer associated with this eyelid state.
    /// </summary>
    public ISpriteLayer Layer;

    /// <summary>
    /// Indicates whether the eyelid is currently closed.
    /// </summary>
    [ViewVariables] public bool IsClosed;

    /// <summary>
    /// Indicate if curently this eyelid is in a complete blink state, meaning it has fully closed and is scheduled to open.
    /// </summary>
    [ViewVariables] public bool IsCompleteBlink;

    /// <summary>
    /// The scheduled time for the eyelid to close, in seconds.
    /// </summary>
    [ViewVariables] public TimeSpan ScheduledCloseTime;

    /// <summary>
    /// The scheduled time for the eyelid to open, in seconds.
    /// </summary>
    [ViewVariables] public TimeSpan ScheduledOpenTime;

    public EyelidState(ISpriteLayer layer)
    {
        Layer = layer;
        IsClosed = false;
        IsCompleteBlink = false;
        ScheduledCloseTime = default;
        ScheduledOpenTime = default;
    }
}
