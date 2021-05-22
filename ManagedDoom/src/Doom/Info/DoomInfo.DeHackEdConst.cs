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
        public static class DeHackEdConst
        {
            public static int InitialHealth { get; set; } = 100;
            public static int InitialBullets { get; set; } = 50;
            public static int MaxHealth { get; set; } = 200;
            public static int MaxArmor { get; set; } = 200;
            public static int GreenArmorClass { get; set; } = 1;
            public static int BlueArmorClass { get; set; } = 2;
            public static int MaxSoulsphere { get; set; } = 200;
            public static int SoulsphereHealth { get; set; } = 100;
            public static int MegasphereHealth { get; set; } = 200;
            public static int GodModeHealth { get; set; } = 100;
            public static int IdfaArmor { get; set; } = 200;
            public static int IdfaArmorClass { get; set; } = 2;
            public static int IdkfaArmor { get; set; } = 200;
            public static int IdkfaArmorClass { get; set; } = 2;
            public static int BfgCellsPerShot { get; set; } = 40;
            public static bool MonstersInfight { get; set; } = false;
        }
    }
}
