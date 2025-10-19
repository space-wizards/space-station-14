// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Server.Administration.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.Administration.Systems;

public sealed class AdminGunSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<AdminMinigunComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
    }

    private void OnGunRefreshModifiers(Entity<AdminMinigunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        args.FireRate = 15;
    }
}
