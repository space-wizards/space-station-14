using Content.Server.Atmos.Components;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereWeatherDeviceSystem : EntitySystem
{
    private TimeSpan _lastChange;
    private readonly TimeSpan _maxWait = TimeSpan.FromSeconds(15);

    [Dependency] private readonly IGameTiming _time = default!;

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        var diff = _time.CurTime - _lastChange;
        if (diff < _maxWait)
        {
            return;
        }

        var query = EntityQueryEnumerator<WeatherDeviceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var device, out var transform))
        {
            var gridUid = transform.GridUid;
            EnsureComp<GridAtmosphereComponent>(gridUid!.Value, out var comp);
            foreach (var tileAtmosphere in comp.MapTiles)
            {
                if (tileAtmosphere.Air != null)
                    tileAtmosphere.Air.Temperature += 5;
            }
        }

        _lastChange = _time.CurTime;
    }
}
