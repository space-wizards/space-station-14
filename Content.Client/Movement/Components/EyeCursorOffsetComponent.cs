using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Client.Movement.Components;

[RegisterComponent]
public sealed partial class EyeCursorOffsetComponent : SharedEyeCursorOffsetComponent
{
    public Vector2 TargetPosition = Vector2.Zero;

    public Vector2 CurrentPosition = Vector2.Zero;
}
