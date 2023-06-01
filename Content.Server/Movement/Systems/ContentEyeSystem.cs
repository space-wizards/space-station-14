using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server.Movement.Systems;

public sealed class ContentEyeSystem : SharedContentEyeSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<ContentEyeComponent, SharedEyeComponent>();

        while (query.MoveNext(out var uid, out var comp, out var eyeComp))
        {
            if (eyeComp.Zoom.Equals(comp.TargetZoom))
                continue;

            UpdateEye(uid, comp, eyeComp, frameTime);
        }
    }
}
