using Content.Shared.FixedPoint;
using Robust.Shared.Network;

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

    public void SetPointValue(NetUserId userId, FixedPoint2 value, EntityUid uid, PointManagerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Points[userId] = value;
    }
}
