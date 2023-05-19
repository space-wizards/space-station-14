using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;

namespace Content.Client.Physics;

/// <summary>
/// Draws a texture on top of a joint.
/// </summary>
public sealed class JointOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private IEntityManager _entManager;

    private HashSet<Joint> _drawn = new();

    public JointOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        _drawn.Clear();
        var worldHandle = args.WorldHandle;

        var joints = _entManager.EntityQueryEnumerator<JointComponent, TransformComponent>();

        while (joints.MoveNext(out var uid, out var jointComp, out var xform))
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

                        break;
                    default:
                        continue;
                }
            }
        }
    }
}
