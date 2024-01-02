using Content.Shared.ProximityDetection.Components;
using Content.Shared.ProximityDetection.Systems;

namespace Content.Server.ProximityDetectionSystem;

public sealed class ProximityDetectionSystem : SharedProximityDetectionSystem
{
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ProximityDetectorComponent>();
        while (query.MoveNext(out var owner, out var detector))
        {
            if (!detector.Enabled)
                continue;
            detector.AccumulatedFrameTime += frameTime;
            if (detector.AccumulatedFrameTime < detector.UpdateRate)
                continue;
            detector.AccumulatedFrameTime -= detector.UpdateRate;
            RunUpdate_Internal(owner, detector);
        }
    }
}
