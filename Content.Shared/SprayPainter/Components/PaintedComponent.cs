using Robust.Shared.GameStates;

namespace Content.Shared.SprayPainter.Components;

/// <summary>
/// Used to mark an entity that has just been painted.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class PaintedComponent : Component
{
    /// <summary>
    /// Used by the system to record interval-aware time, that is, the time after which the signature will not be displayed.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan RemoveTime;
}
