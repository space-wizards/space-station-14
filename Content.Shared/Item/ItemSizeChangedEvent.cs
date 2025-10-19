// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.Item;

/// <summary>
/// Raised directed on an entity when its item size / shape changes.
/// </summary>
[ByRefEvent]
public struct ItemSizeChangedEvent(EntityUid Entity)
{
    public EntityUid Entity = Entity;
}
