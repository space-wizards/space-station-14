using Content.Shared.Smoking;
using Content.Shared.Light.Components;
using Content.Server.Atmos.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.Light.EntitySystems;

namespace Content.Server.Light.EntitySystems;

public sealed class MatchstickSystem : SharedMatchstickSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MatchstickComponent>();

        while (query.MoveNext(out var uid, out var match))
        {
            if (match.CurrentState != SmokableState.Lit)
                continue;

            var xform = Transform(uid);

            if (xform.GridUid is not { } gridUid)
                continue;

            var position = _transformSystem.GetGridOrMapTilePosition(uid, xform);

            _atmosphereSystem.HotspotExpose(gridUid, position, 400, 50, uid, true);

            // Check if the match has expired.
            var burnoutTime = match.TimeMatchWillBurnOut;
            if (burnoutTime != null && _timing.CurTime > burnoutTime)
            {
                SetState(uid, match, SmokableState.Burnt);
                match.TimeMatchWillBurnOut = null;
            }
        }
    }
}
