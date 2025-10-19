// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

/// <summary>
/// Debug message containing a pathfinding route.
/// </summary>
[Serializable, NetSerializable]
public sealed class PathRouteMessage : EntityEventArgs
{
    public List<DebugPathPoly> Path;
    public Dictionary<DebugPathPoly, float> Costs;

    public PathRouteMessage(List<DebugPathPoly> path, Dictionary<DebugPathPoly, float> costs)
    {
        Path = path;
        Costs = costs;
    }
}
