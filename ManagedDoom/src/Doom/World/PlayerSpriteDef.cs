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
    public sealed class PlayerSpriteDef
    {
        private MobjStateDef state;
        private int tics;
        private Fixed sx;
        private Fixed sy;

        public void Clear()
        {
            state = null;
            tics = 0;
            sx = Fixed.Zero;
            sy = Fixed.Zero;
        }

        public MobjStateDef State
        {
            get => state;
            set => state = value;
        }

        public int Tics
        {
            get => tics;
            set => tics = value;
        }

        public Fixed Sx
        {
            get => sx;
            set => sx = value;
        }

        public Fixed Sy
        {
            get => sy;
            set => sy = value;
        }
    }
}
