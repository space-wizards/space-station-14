using Content.Shared.Follower.Components;

namespace Content.Client.Follower;

/// <summary>
/// This handles updating the position of every following entity, as it no longer uses parenting.
/// </summary>
public sealed class FollowerUpdateSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void FrameUpdate(float frameTime)
    {
        foreach (var (follow, xform) in EntityQuery<FollowerComponent, TransformComponent>())
        {
            // pvs shenanigans
            if (!Exists(follow.Following))
                continue;

            xform.WorldPosition = Transform(follow.Following).WorldPosition;
        }
    }
}
