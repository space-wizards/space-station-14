using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server.Movement.Systems;

public sealed class ContentEyeSystem : SharedContentEyeSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var eyeQuery = GetEntityQuery<SharedEyeComponent>();

        foreach (var (_, comp) in EntityQuery<ActiveContentEyeComponent, ContentEyeComponent>(true))
        {
            var uid = comp.Owner;

            // Use a separate query jjuussstt in case any actives mistakenly hang around.
            if (!eyeQuery.TryGetComponent(comp.Owner, out var eyeComp) ||
                eyeComp.Zoom.Equals(comp.TargetZoom))
            {
                RemComp<ActiveContentEyeComponent>(comp.Owner);
                continue;
            }

            UpdateEye(uid, comp, eyeComp, frameTime);
        }
    }
}
