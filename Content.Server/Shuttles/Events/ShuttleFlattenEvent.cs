// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised broadcast whenever a shuttle FTLs
/// </summary>
[ByRefEvent]
public readonly record struct ShuttleFlattenEvent(EntityUid MapUid, List<Box2> AABBs);
