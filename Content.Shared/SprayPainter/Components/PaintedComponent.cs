using Robust.Shared.GameStates;

namespace Content.Shared.SprayPainter.Components;

/// <summary>
/// Used to mark an entity that has been repainted.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PaintedComponent : Component
{
    /// <summary>
    /// The time after which the entity is dried and does not appear as "freshly painted".
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan DryTime;
}
