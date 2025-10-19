// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Hands.Components;
using Content.Shared.Whitelist;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    private bool CheckWhitelists(Entity<HandsComponent?> ent, string handId, EntityUid toTest)
    {
        if (!TryGetHand(ent, handId, out var hand))
            return false;

        return _entityWhitelist.CheckBoth(toTest, hand.Value.Blacklist, hand.Value.Whitelist);
    }
}
