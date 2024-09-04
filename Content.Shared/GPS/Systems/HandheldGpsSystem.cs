using Content.Shared.GPS.Components;
using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Shared.GPS.Systems;

public sealed class HandheldGpsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private string _posText = "";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldGPSComponent, ExaminedEvent>(OnExamine);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _gameTiming.CurTime;

        var query = EntityQueryEnumerator<HandheldGPSComponent>();

        while (query.MoveNext(out var uid, out var gps))
        {
            if (Paused(uid) || gps.Deleted)
                continue;

            if (currentTime >= gps.NextCoordUpdate)
            {
                var xform = Transform(uid);

                if (xform.GridUid is not { } gridUid)
                    continue;

                var pos = _transform.GetMapCoordinates(uid, xform);
                var x = (int)pos.X;
                var y = (int)pos.Y;
                var posText = $"({x}, {y})";

                gps.StoredCoords = Loc.GetString("handheld-gps-coordinates-title", ("coordinates", posText));

                gps.NextCoordUpdate = currentTime + gps.UpdateRate;
            }
        }
    }

    private void OnExamine(Entity<HandheldGPSComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(ent.Comp.StoredCoords);
    }
}
