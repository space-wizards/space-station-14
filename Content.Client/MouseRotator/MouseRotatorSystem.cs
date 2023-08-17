using Content.Shared.MouseRotator;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.MouseRotator;

/// <inheritdoc/>
public sealed class MouseRotatorSystem : SharedMouseRotatorSystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEyeManager _eye = default!;

    private const double Precision = 0.001d;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted || !_input.MouseScreenPosition.IsValid)
            return;

        var player = _player.LocalPlayer?.ControlledEntity;

        if (player == null || !HasComp<MouseRotatorComponent>(player))
            return;

        var xform = Transform(player.Value);

        // Get mouse loc and convert to angle based on player location
        var coords = _input.MouseScreenPosition;
        var mapPos = _eye.PixelToMap(coords);

        if (mapPos.MapId == MapId.Nullspace)
            return;

        var angle = (mapPos.Position - xform.MapPosition.Position).ToWorldAngle();

        var curRot = xform.LocalRotation;

        // Don't raise event if mouse ~hasn't moved
        if (curRot.EqualsApprox(angle, Precision))
        {
            return;
        }

        RaisePredictiveEvent(new RequestMouseRotatorRotationEvent
        {
            Rotation = angle
        });
    }
}
