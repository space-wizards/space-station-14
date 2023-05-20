using Content.Shared.Physics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;

namespace Content.Client.Physics;

/// <summary>
/// Draws a texture on top of a joint.
/// </summary>
public sealed class JointVisualsOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private IEntityManager _entManager;

    private HashSet<Joint> _drawn = new();

    public JointVisualsOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        _drawn.Clear();
        var worldHandle = args.WorldHandle;

        var spriteSystem = _entManager.System<SpriteSystem>();
        var xformSystem = _entManager.System<SharedTransformSystem>();
        var joints = _entManager.EntityQueryEnumerator<JointVisualsComponent, JointComponent, TransformComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        while (joints.MoveNext(out var uid, out var visuals, out var jointComp, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            foreach (var (id, joint) in jointComp.GetJoints)
            {
                if (!_drawn.Add(joint))
                    continue;

                switch (joint)
                {
                    case DistanceJoint distance:
                        var other = joint.BodyAUid == uid ? joint.BodyBUid : joint.BodyAUid;
                        var otherXform = xformQuery.GetComponent(joint.BodyBUid);

                        if (xform.MapID != otherXform.MapID)
                            continue;

                        var texture = spriteSystem.Frame0(visuals.Sprite);
                        var width = texture.Width / (float) EyeManager.PixelsPerMeter;

                        var coordsA = xform.Coordinates;
                        var coordsB = otherXform.Coordinates;

                        coordsA = coordsA.Offset(visuals.OffsetA);
                        coordsB = coordsB.Offset(visuals.OffsetB);

                        var posA = coordsA.ToMapPos(_entManager, xformSystem);
                        var posB = coordsB.ToMapPos(_entManager, xformSystem);
                        var diff = (posB - posA);
                        var length = diff.Length;

                        var midPoint = diff / 2f + posA;
                        var angle = (posB - posA).ToWorldAngle();
                        var box = new Box2(-width / 2f, -length / 2f, width / 2f, length / 2f);
                        var rotate = new Box2Rotated(box.Translated(midPoint), angle, midPoint);

                        worldHandle.DrawTextureRect(texture, rotate);

                        break;
                    default:
                        continue;
                }
            }
        }
    }
}
