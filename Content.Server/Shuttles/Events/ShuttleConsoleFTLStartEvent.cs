// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised when shuttle console approved FTL
/// </summary>
[ByRefEvent]
public record struct ShuttleConsoleFTLTravelStartEvent(EntityUid Uid)
{
    public EntityUid Uid = Uid;
}
