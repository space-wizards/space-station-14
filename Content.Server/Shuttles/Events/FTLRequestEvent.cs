// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised by a shuttle when it has requested an FTL.
/// </summary>
[ByRefEvent]
public record struct FTLRequestEvent(EntityUid MapUid);
