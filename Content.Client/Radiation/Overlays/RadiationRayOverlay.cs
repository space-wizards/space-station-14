using Content.Shared.Radiation.Systems;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;

namespace Content.Client.Radiation.Overlays;

public sealed class RadiationRayOverlay : Overlay
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    public List<RadRayResult>? Rays;

    private readonly Font _font;

    public RadiationRayOverlay()
    {
        IoCManager.InjectDependencies(this);

        var cache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 8);
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        switch (args.Space)
        {
            case OverlaySpace.ScreenSpace:
                DrawScreen(args);
                break;
            case OverlaySpace.WorldSpace:
                DrawWorld(args);
                break;
        }
    }

    private void DrawScreen(OverlayDrawArgs args)
    {
        if (Rays == null)
            return;

        var handle = args.ScreenHandle;
        foreach (var ray in Rays)
        {
            if (ray.MapId != args.MapId)
                continue;

            if (ray.ReachedDestination)
            {
                var screenCenter = _eyeManager.WorldToScreen(ray.DestPos);
                handle.DrawString(_font, screenCenter, ray.ReceivedRads.ToString("F2"), 2f, Color.White);
            }

            foreach (var (blockerPos, rads) in ray.Blockers)
            {
                var screenCenter = _eyeManager.WorldToScreen(blockerPos);
                handle.DrawString(_font, screenCenter, rads.ToString("F2"), 1.5f, Color.White);
            }
        }
    }

    private void DrawWorld(in OverlayDrawArgs args)
    {
        if (Rays == null)
            return;

        var handle = args.WorldHandle;
        foreach (var ray in Rays)
        {
            if (ray.MapId != args.MapId)
                continue;

            var sourcePos = ray.SourcePos;
            var destPos = ray.ReachedDestination ? ray.DestPos : ray.LastPos;

            handle.DrawLine(sourcePos, destPos, Color.Red);

        }
    }
}
