// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.Throwing;

/// <summary>
/// Raised on someone when they try to catch an item.
/// </summary>
[ByRefEvent]
public record struct CatchAttemptEvent(EntityUid Item, float CatchChance, bool Cancelled = false);
