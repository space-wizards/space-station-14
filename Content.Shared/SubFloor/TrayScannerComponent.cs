using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SubFloor;

[RegisterComponent, NetworkedComponent]
public sealed partial class TrayScannerComponent : Component
{
    /// <summary>
    ///     Whether the scanner is currently on.
    /// </summary>
    [DataField]
    public bool Enabled;

    /// <summary>
    ///     Radius in which the scanner will reveal entities. Centered on the <see cref="LastLocation"/>.
    /// </summary>
    [DataField]
    public float Range = 4f;

    /// <summary>
    ///     Whether the scanner should be disabled when it's held or in a container.
    ///     If this is true, it can only be used if it's on the user's entity itself (e.g. built-in).
    /// </summary>
    [DataField]
    public bool DisableContained;
}

[Serializable, NetSerializable]
public sealed class TrayScannerState : ComponentState
{
    public bool Enabled;
    public float Range;
    public bool DisableContained;

    public TrayScannerState(bool enabled, float range, bool disableContained)
    {
        Enabled = enabled;
        Range = range;
        DisableContained = disableContained;
    }
}

/// <summary>
///     Event raised when the T-Ray scanner action is used.
/// </summary>
public sealed partial class TrayScannerActionEvent : InstantActionEvent
{

}
