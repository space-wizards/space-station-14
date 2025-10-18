using Content.Shared.FixedPoint;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.Points;

/// <summary>
/// This handles modifying point counts for <see cref="PointManagerComponent"/>
/// </summary>
public abstract class SharedPointSystem : EntitySystem
{
    /// <summary>
    /// Adds the specified point value to a player.
    /// </summary>
    public void AdjustPointValue(NetUserId userId, FixedPoint2 value, EntityUid uid, PointManagerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.Points.TryGetValue(userId, out var current))
            current = 0;

        SetPointValue(userId, current + value, uid, component);
    }

    /// <summary>
    /// Sets the amount of points for a player
    /// </summary>
    public void SetPointValue(NetUserId userId, FixedPoint2 value, EntityUid uid, PointManagerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Points.TryGetValue(userId, out var current) && current == value)
            return;

        component.Points[userId] = value;
        component.Scoreboard = GetScoreboard(uid, component);
        Dirty(uid, component);

        var ev = new PlayerPointChangedEvent(userId, value);
        RaiseLocalEvent(uid, ref ev, true);
    }

    /// <summary>
    /// Gets the amount of points for a given player
    /// </summary>
    public FixedPoint2 GetPointValue(NetUserId userId, EntityUid uid, PointManagerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return FixedPoint2.Zero;

        return component.Points.TryGetValue(userId, out var value)
            ? value
            : FixedPoint2.Zero;
    }

    /// <summary>
    /// Ensures that a player is being tracked by the PointManager, giving them a default score of 0.
    /// </summary>
    public void EnsurePlayer(NetUserId userId, EntityUid uid, PointManagerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Points.ContainsKey(userId))
            return;
        SetPointValue(userId, FixedPoint2.Zero, uid, component);
    }

    /// <summary>
    /// Returns a formatted message containing a ranking of all the currently online players and their scores.
    /// </summary>
    public virtual FormattedMessage GetScoreboard(EntityUid uid, PointManagerComponent? component = null)
    {
        return new FormattedMessage();
    }
}

/// <summary>
/// Event raised on the point manager entity and broadcasted whenever a player's points change.
/// </summary>
/// <param name="Player"></param>
/// <param name="Points"></param>
[ByRefEvent]
public readonly record struct PlayerPointChangedEvent(NetUserId Player, FixedPoint2 Points);
