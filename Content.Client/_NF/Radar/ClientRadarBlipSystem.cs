using System.Numerics;
using Content.Shared._NF.Radar;
using Robust.Shared.Timing;

namespace Content.Client._NF.Radar;

/// <summary>
/// A system for requesting, receiving, and caching radar blips.
/// Sends off ad hoc requests for blips, caches them for a period of time, and draws them when requested.
/// </summary>
/// <remarks>
/// Ported from Monolith's RadarBlipsSystem.
/// Imp notes - Removed all functionality surrounding blips on grids. you've got the crew monitor for that.
/// </remarks>
public sealed partial class ClientRadarBlipSystem : EntitySystem
{
    private const double BlipStaleSeconds = 3.0;
    private static readonly List<(Vector2, float, Color, RadarBlipShape)> EmptyBlipList = new();

    private static readonly List<(Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> EmptyRawBlipList =
        new();

    private TimeSpan _lastRequestTime = TimeSpan.Zero;

    // Minimum time between requests.  Slightly larger than the server-side value.
    private static readonly TimeSpan RequestThrottle = TimeSpan.FromMilliseconds(1250);

    // Maximum distance for blips to be considered visible
    private const float MaxBlipRenderDistance = 256f;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private TimeSpan _lastUpdatedTime;
    private List<(Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> _blips = new();
    private Vector2 _radarWorldPosition;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<GiveBlipsEvent>(HandleReceiveBlips);
    }

    /// <summary>
    /// Handles receiving blip data from the server.
    /// </summary>
    private void HandleReceiveBlips(GiveBlipsEvent ev, EntitySessionEventArgs args)
    {
        if (ev.Blips.Count == 0)
        {
            _blips = EmptyRawBlipList;
            return;
        }

        _blips = ev.Blips;
        _lastUpdatedTime = _timing.CurTime;
    }

    /// <summary>
    /// Requests blip data from the server for the given radar console, throttled to avoid spamming.
    /// </summary>
    public void RequestBlips(EntityUid console)
    {
        if (!Exists(console))
            return;

        if (_timing.CurTime - _lastRequestTime < RequestThrottle)
            return;

        _lastRequestTime = _timing.CurTime;

        // Cache the radar position for distance culling
        _radarWorldPosition = _xform.GetWorldPosition(console);

        var netConsole = GetNetEntity(console);
        var ev = new RequestBlipsEvent(netConsole);
        RaiseNetworkEvent(ev);
    }

    /// <summary>
    /// Gets the current blips as world positions with their scale, color and shape.
    /// This is needed for the legacy radar display that expects world coordinates.
    /// </summary>
    public List<(Vector2, float, Color, RadarBlipShape)> GetCurrentBlips()
    {
        if (_timing.CurTime.TotalSeconds - _lastUpdatedTime.TotalSeconds > BlipStaleSeconds)
            return EmptyBlipList;

        var result = new List<(Vector2, float, Color, RadarBlipShape)>(_blips.Count);
        foreach (var blip in _blips)
        {
            var worldPosition = blip.Position;

            // Distance culling for world position blips
            if (Vector2.DistanceSquared(worldPosition, _radarWorldPosition) >
                MaxBlipRenderDistance * MaxBlipRenderDistance)
                continue;

            result.Add((worldPosition, blip.Scale, blip.Color, blip.Shape));
        }

        return result;
    }

    /// <summary>
    /// Gets the raw blips data which includes grid information for more accurate rendering.
    /// </summary>
    public List<(Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> GetRawBlips()
    {
        if (_timing.CurTime.TotalSeconds - _lastUpdatedTime.TotalSeconds > BlipStaleSeconds)
            return EmptyRawBlipList;

        if (_blips.Count == 0)
            return _blips;

        var filteredBlips = new List<(Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)>(_blips.Count);

        foreach (var blip in _blips)
        {
            // For non-grid blips, do direct distance check
            if (Vector2.DistanceSquared(blip.Position, _radarWorldPosition) <=
                MaxBlipRenderDistance * MaxBlipRenderDistance)
            {
                filteredBlips.Add(blip);
            }
        }

        return filteredBlips;
    }
}
