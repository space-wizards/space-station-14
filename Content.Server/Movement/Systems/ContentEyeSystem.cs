using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server.Movement.Systems;

public sealed class ContentEyeSystem : SharedContentEyeSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<EndOfTargetZoomAnimation>(OnZoomAnimationEnd);
    }

    private void OnZoomAnimationEnd(EndOfTargetZoomAnimation msg)
    {
        var query = AllEntityQuery<ContentEyeComponent, SharedEyeComponent>();

        Logger.Debug($"on end animation event! +++++++++++++++++++++++++++++++++++");
        while (query.MoveNext(out var uid, out var comp, out var eyeComp))
        {
            if (!eyeComp.Zoom.Equals(comp.TargetZoom))
            {
                Logger.Debug($"set zoom target in {comp.TargetZoom}");
                eyeComp.Zoom = comp.TargetZoom;
                Dirty(eyeComp);
            }
        }
    }
}