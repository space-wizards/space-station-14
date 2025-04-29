using Robust.Shared.GameStates;

namespace Content.Shared.SprayPainter.Components;

/// <summary>
/// Used to mark an entity that has just been painted.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PaintedComponent : Component
{
    /// <summary>
    /// The value through which the field will drop from the entity description.
    /// </summary>
    [DataField]
    public TimeSpan RemovalInterval = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Used by the system to record interval-aware time, that is, the time after which the signature will not be displayed.
    /// </summary>
    [DataField]
    public TimeSpan RemoveTime;
}
