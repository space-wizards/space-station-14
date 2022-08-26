using System.Linq;
using Content.Shared.Radiation.Systems;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Radiation.Overlays;

public sealed class RadiationRayOverlay : Overlay
{
    public List<RadRayResult>? Rays;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Rays == null)
            return;

        var handle = args.WorldHandle;
        foreach (var ray in Rays)
        {
            if (ray.MapId != args.MapId)
                continue;

            var sourcePos = ray.SourcePos;
            var destPos = ray.ReachedDestination ? ray.DestPos : ray.Blockers.Last();

            handle.DrawLine(sourcePos, destPos, Color.Red);

        }
    }
}
