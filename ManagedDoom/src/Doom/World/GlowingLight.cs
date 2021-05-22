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
    public sealed class GlowingLight : Thinker
    {
        private static readonly int glowSpeed = 8;

        private World world;

        private Sector sector;
        private int minLight;
        private int maxLight;
        private int direction;

        public GlowingLight(World world)
        {
            this.world = world;
        }

        public override void Run()
        {
            switch (direction)
            {
                case -1:
                    // Down.
                    sector.LightLevel -= glowSpeed;
                    if (sector.LightLevel <= minLight)
                    {
                        sector.LightLevel += glowSpeed;
                        direction = 1;
                    }
                    break;

                case 1:
                    // Up.
                    sector.LightLevel += glowSpeed;
                    if (sector.LightLevel >= maxLight)
                    {
                        sector.LightLevel -= glowSpeed;
                        direction = -1;
                    }
                    break;
            }
        }

        public Sector Sector
        {
            get => sector;
            set => sector = value;
        }

        public int MinLight
        {
            get => minLight;
            set => minLight = value;
        }

        public int MaxLight
        {
            get => maxLight;
            set => maxLight = value;
        }

        public int Direction
        {
            get => direction;
            set => direction = value;
        }
    }
}
