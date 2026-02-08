using System.Numerics;
using Content.Shared.DirectionalSigns.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;

namespace Content.Client.DirectionalSigns;

/// <summary>
/// Maintains the orientation of directional signs
/// </summary>
/// <remarks>
/// Maintains the orientation of directional signs relative to the player's 'eye'
/// while keeping directional signs snapped to GRID-space cardinal directions.
/// Keep changes to client-side sprite to avoid unnecessary network traffic.
/// </remarks>
public sealed class DirectionalSignSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    // Cached references to the previous loop used to check for changes
    private IEye? _lastEye;
    private float _lastBaseEyeRotationDeg = float.NaN;


    public override void FrameUpdate(float frameTime)
    {
        // Get current eye rotation
        // Must be made relative to GRID-space inside the while loop as some signs
        // will be on a different grid than the player's controlled entity
        var eye = _eyeManager.CurrentEye;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (eye is null)
            return;
        var baseEyeRotationDeg = (float)eye.Rotation.Reduced().Degrees;

        // Escape early if the eye attachment or its rotation haven't changed
        // Avoids unnecessary sprite updates while allowing use of FrameUpdate to maintain smooth animation
        // I don't know an obvious way to do this via an event, which would be ideal
        const float epsilonDeg = 0.001f;
        if (ReferenceEquals(eye, _lastEye) && MathF.Abs(baseEyeRotationDeg - _lastBaseEyeRotationDeg) < epsilonDeg)
            return;

        _lastEye = eye;
        _lastBaseEyeRotationDeg = baseEyeRotationDeg;

        // Loop through all directional signs and update the sprite rotation and offset
        var query = EntityQueryEnumerator<DirectionalSignComponent, TransformComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var signXform, out var sprite))
        {
            // Escape early if the sign isn't on a grid, or its grid can't be resolved
            var gridUid = signXform.GridUid;
            if (gridUid is null || gridUid.Value == EntityUid.Invalid)
                continue;

            // Force NoRotation, we will control rotation manually in this system
            if (!sprite.NoRotation)
                sprite.NoRotation = true;

            // Define initial positions
            var spriteCenter = signXform.LocalPosition;
            var tileCenter = new Vector2(
                MathF.Floor(spriteCenter.X) + 0.5f,
                MathF.Floor(spriteCenter.Y) + 0.5f);
            var spriteTileCenterOffset = spriteCenter - tileCenter;

            // Correct eyeRotation for gridRotation
            if (!TryComp(gridUid.Value, out TransformComponent? gridXform))
                continue;
            var gridRotationDeg = (float)gridXform.LocalRotation.Reduced().Degrees;
            var adjustedEyeRotationDeg = baseEyeRotationDeg + gridRotationDeg;

            // Define caseID based on eyeRotation: 0deg>0, 90deg>1, 180deg>2, 270deg>3
            var caseID = ((int)MathF.Floor((adjustedEyeRotationDeg + 45) / 90) % 4 + 4) % 4;
            var nearestCardinalDeg = caseID * 90;

            // Smoothly counter-rotate within the quadrant; jump at 45° because 'nearestCardinalDeg' flips.
            // This makes the sprites snap to GRID-space cardinals instead of following eye rotation
            // while the eye rotation animation is in progress
            var eyeDeltaNearestCardinalRad = Angle.FromDegrees(adjustedEyeRotationDeg - nearestCardinalDeg);

            // Calculate the sprite offset vector in GRID-space
            var o = spriteTileCenterOffset;
            var newSpriteTileCenterOffset = caseID switch
            {
                0 => new Vector2( o.X,  o.Y),   // 0°
                1 => new Vector2( o.Y, -o.X),   // 90°
                2 => new Vector2(-o.X, -o.Y),   // 180°
                3 => new Vector2(-o.Y,  o.X),   // 270°
                _ => Vector2.Zero,
            };

            // GRID-space vector from current sprite center to desired sprite center.
            var spriteOffsetGrid = newSpriteTileCenterOffset - spriteTileCenterOffset;
            // Convert GRID-space vector into CAMERA-space vector
            var spriteOffsetCamera = Angle.FromDegrees(adjustedEyeRotationDeg).RotateVec(spriteOffsetGrid);

            // Push the values to the sprite
            _sprite.SetRotation((uid, sprite), eyeDeltaNearestCardinalRad);
            _sprite.SetOffset((uid, sprite), spriteOffsetCamera);

        }
    }
}
