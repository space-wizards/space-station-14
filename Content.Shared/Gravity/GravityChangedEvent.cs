// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.Gravity
{
    [ByRefEvent]
    public readonly record  struct GravityChangedEvent(EntityUid ChangedGridIndex, bool HasGravity);
}
