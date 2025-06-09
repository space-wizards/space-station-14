using System.Linq;
using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Radar;

/// <summary>
/// The shape of the radar blip.
/// </summary>
[Serializable, NetSerializable]
public enum RadarBlipShape
{
    /// <summary>Circle shape.</summary>
    Circle,
    /// <summary>Square shape.</summary>
    Square,
    /// <summary>Triangle shape.</summary>
    Triangle,
    /// <summary>Star shape.</summary>
    Star,
    /// <summary>Diamond shape.</summary>
    Diamond,
    /// <summary>Hexagon shape.</summary>
    Hexagon,
    /// <summary>Arrow shape.</summary>
    Arrow
}

/// <summary>
/// Event sent from the server to the client containing radar blip data.
/// </summary>
[Serializable, NetSerializable]
public sealed class GiveBlipsEvent : EntityEventArgs
{
    /// <summary>
    /// Blips are now (grid entity, position, scale, color, shape).
    /// If grid entity is null, position is in world coordinates.
    /// If grid entity is not null, position is in grid-local coordinates.
    /// </summary>
    public readonly List<(Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> Blips;

    /// <summary>
    /// Backwards-compatible constructor for legacy blip format.
    /// </summary>
    /// <param name="blips">List of blips as (position, scale, color).</param>
    public GiveBlipsEvent(List<(Vector2, float, Color)> blips)
    {
        Blips = blips.Select(b => (b.Item1, b.Item2, b.Item3, RadarBlipShape.Circle)).ToList();
    }

    /// <summary>
    /// Constructor for the full blip format.
    /// </summary>
    /// <param name="blips">List of blips as (grid, position, scale, color, shape).</param>
    public GiveBlipsEvent(List<(Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> blips)
    {
        Blips = blips;
    }
}

/// <summary>
/// A request for radar blips around a given entity.
/// Entity must have the RadarConsoleComponent to receive a response.
/// Requests are rate-limited server-side, unhandled messages will not receive a response.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestBlipsEvent : EntityEventArgs
{
    /// <summary>
    /// The radar entity for which blips are being requested.
    /// </summary>
    public readonly NetEntity Radar;

    /// <summary>
    /// Constructor for RequestBlipsEvent.
    /// </summary>
    /// <param name="radar">The radar entity.</param>
    public RequestBlipsEvent(NetEntity radar)
    {
        Radar = radar;
    }
}
