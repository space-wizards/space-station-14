using System.Numerics;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Displaces SS14 eye data when given to an entity.
/// </summary>
[RegisterComponent]
public sealed partial class EyeCursorOffsetComponent : Component
{
    //[DataField("targetPosition"), AutoNetworkedField]
    public Vector2 TargetPosition = Vector2.Zero;

    //[AutoNetworkedField]
    public Vector2 CurrentPosition = Vector2.Zero;

    public Vector2 SavedOffset = Vector2.Zero;
}
