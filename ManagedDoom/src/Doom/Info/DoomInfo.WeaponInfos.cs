//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//



using System;

namespace ManagedDoom
{
    public static partial class DoomInfo
    {
        public static readonly WeaponInfo[] WeaponInfos = new WeaponInfo[]
        {
            // fist
            new WeaponInfo(
                AmmoType.NoAmmo,
                MobjState.Punchup,
                MobjState.Punchdown,
                MobjState.Punch,
                MobjState.Punch1,
                MobjState.Null
            ),

            // pistol
            new WeaponInfo(
                AmmoType.Clip,
                MobjState.Pistolup,
                MobjState.Pistoldown,
                MobjState.Pistol,
                MobjState.Pistol1,
                MobjState.Pistolflash
            ),

            // shotgun
            new WeaponInfo(
                AmmoType.Shell,
                MobjState.Sgunup,
                MobjState.Sgundown,
                MobjState.Sgun,
                MobjState.Sgun1,
                MobjState.Sgunflash1
            ),

            // chaingun
            new WeaponInfo(
                AmmoType.Clip,
                MobjState.Chainup,
                MobjState.Chaindown,
                MobjState.Chain,
                MobjState.Chain1,
                MobjState.Chainflash1
            ),

            // missile launcher
            new WeaponInfo(
                AmmoType.Missile,
                MobjState.Missileup,
                MobjState.Missiledown,
                MobjState.Missile,
                MobjState.Missile1,
                MobjState.Missileflash1
            ),

            // plasma rifle
            new WeaponInfo(
                AmmoType.Cell,
                MobjState.Plasmaup,
                MobjState.Plasmadown,
                MobjState.Plasma,
                MobjState.Plasma1,
                MobjState.Plasmaflash1
            ),

            // bfg 9000
            new WeaponInfo(
                AmmoType.Cell,
                MobjState.Bfgup,
                MobjState.Bfgdown,
                MobjState.Bfg,
                MobjState.Bfg1,
                MobjState.Bfgflash1
            ),

            // chainsaw
            new WeaponInfo(
                AmmoType.NoAmmo,
                MobjState.Sawup,
                MobjState.Sawdown,
                MobjState.Saw,
                MobjState.Saw1,
                MobjState.Null
            ),

            // // super shotgun
            new WeaponInfo(
                AmmoType.Shell,
                MobjState.Dsgunup,
                MobjState.Dsgundown,
                MobjState.Dsgun,
                MobjState.Dsgun1,
                MobjState.Dsgunflash1
            )
        };
    }
}
