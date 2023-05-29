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

        while (query.MoveNext(out var uid, out var comp, out var eyeComp))
        {
            if (!eyeComp.Zoom.Equals(comp.TargetZoom))
            {
                Sawmill.Debug($"set value {comp.TargetZoom} for zoom animation end");
                eyeComp.Zoom = comp.TargetZoom;
                Dirty(eyeComp);
            }
        }
    }
}