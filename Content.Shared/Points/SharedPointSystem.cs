using Content.Shared.FixedPoint;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.Points;

/// <summary>
/// This handles modifying point counts for <see cref="PointManagerComponent"/>
/// </summary>
public abstract class SharedPointSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {

    }

    public void AdjustPointValue(NetUserId userId, FixedPoint2 value, EntityUid uid, PointManagerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.Points.TryGetValue(userId, out var current))
            current = 0;

        SetPointValue(userId, current + value, uid, component);
    }

    public void SetPointValue(NetUserId userId, FixedPoint2 value, EntityUid uid, PointManagerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Points.TryGetValue(userId, out var current) && current == value)
            return;

        component.Points[userId] = value;

        var ev = new PlayerPointChangedEvent(userId, value);
        RaiseLocalEvent(uid, ref ev, true);
    }

    public FixedPoint2 GetPointValue(NetUserId userId, EntityUid uid, PointManagerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return FixedPoint2.Zero;

        return component.Points.TryGetValue(userId, out var value)
            ? value
            : FixedPoint2.Zero;
    }

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
