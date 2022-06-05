using Content.Shared.Follower.Components;

namespace Content.Server.Follower;

/// <summary>
/// This handles updating the position of every following entity, as it no longer uses parenting.
/// </summary>
public sealed class FollowerUpdateSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        foreach (var (follow, xform) in EntityQuery<FollowerComponent, TransformComponent>())
        {
            xform.WorldPosition = Transform(follow.Following).WorldPosition;
        }
    }
}
