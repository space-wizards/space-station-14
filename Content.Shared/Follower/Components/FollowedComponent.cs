using System.Collections.Generic;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Follower.Components;

/// <summary>
///     Attached to entities that are currently being followed by a ghost.
/// </summary>
[RegisterComponent, Friend(typeof(FollowerSystem))]
public sealed class FollowedComponent : Component
{
    public HashSet<EntityUid> Following = new();
}
