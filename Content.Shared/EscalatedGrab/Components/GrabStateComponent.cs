using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.EscalatedGrab.Components;

/// <summary>
/// Added to a puller when their grab escalates beyond a standard pull.
/// Tracks the current <see cref="GrabStage"/> and target entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GrabStateComponent : Component
{
    [ViewVariables]
    public EntityUid Target;

    [ViewVariables]
    public GrabStage Stage = GrabStage.Aggressive;
}
