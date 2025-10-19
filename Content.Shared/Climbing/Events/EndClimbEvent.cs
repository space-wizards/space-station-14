// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.Climbing.Events;

/// <summary>
/// Raised on an entity when it ends climbing.
/// </summary>
[ByRefEvent]
public readonly record struct EndClimbEvent;
