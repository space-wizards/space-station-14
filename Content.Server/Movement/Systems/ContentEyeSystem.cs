using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server.Movement.Systems;

public sealed class ContentEyeSystem : SharedContentEyeSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var eyesSharedComponents = AllEntityQuery<SharedEyeComponent>();

        while (eyesSharedComponents.MoveNext(out var uid, out var eyeComp))
        {

            Vector2 targetZoom;
            if (HasGhostZoom(null, uid) is ContentEyeComponent ghostContent)
                targetZoom = ghostContent.TargetZoom;
            else
                targetZoom = eyeComp.TargetZoom;

            // set new value if they dont equals
            if (eyeComp.Zoom.Equals(targetZoom))
                continue;

            var diff = targetZoom - eyeComp.Zoom;

            if (diff.LengthSquared < 0.000001f)
            {
                eyeComp.Zoom = targetZoom;
                Dirty(eyeComp);
                return;
            }

            var change = diff * 5f * frameTime;

            eyeComp.Zoom += change;
            Dirty(eyeComp);
        }
    }
}
