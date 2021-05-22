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
    public sealed class FireFlicker : Thinker
    {
        private World world;

        private Sector sector;
        private int count;
        private int maxLight;
        private int minLight;

        public FireFlicker(World world)
        {
            this.world = world;
        }

        public override void Run()
        {
            if (--count > 0)
            {
                return;
            }

            var amount = (world.Random.Next() & 3) * 16;

            if (sector.LightLevel - amount < minLight)
            {
                sector.LightLevel = minLight;
            }
            else
            {
                sector.LightLevel = maxLight - amount;
            }

            count = 4;
        }

        public Sector Sector
        {
            get => sector;
            set => sector = value;
        }

        public int Count
        {
            get => count;
            set => count = value;
        }

        public int MaxLight
        {
            get => maxLight;
            set => maxLight = value;
        }

        public int MinLight
        {
            get => minLight;
            set => minLight = value;
        }
    }
}
