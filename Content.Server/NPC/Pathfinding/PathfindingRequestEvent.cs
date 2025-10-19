// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Map;

namespace Content.Server.NPC.Pathfinding;

public sealed class PathfindingRequestEvent : EntityEventArgs
{
    public EntityCoordinates Start;
    public EntityCoordinates End;

    // TODO: Need stuff like can we break shit, can we pry, collision mask, etc
}
