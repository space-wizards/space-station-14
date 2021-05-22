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
using System.Runtime.CompilerServices;

namespace ManagedDoom
{
    public static partial class Trig
    {
        public const int FineAngleCount = 8192;
        public const int FineMask = FineAngleCount - 1;
        public const int AngleToFineShift = 19;

        private const int fineCosineOffset = FineAngleCount / 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed Tan(Angle anglePlus90)
        {
            return new Fixed(fineTangent[anglePlus90.Data >> AngleToFineShift]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed Tan(int fineAnglePlus90)
        {
            return new Fixed(fineTangent[fineAnglePlus90]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed Sin(Angle angle)
        {
            return new Fixed(fineSine[angle.Data >> AngleToFineShift]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed Sin(int fineAngle)
        {
            return new Fixed(fineSine[fineAngle]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed Cos(Angle angle)
        {
            return new Fixed(fineSine[(angle.Data >> AngleToFineShift) + fineCosineOffset]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed Cos(int fineAngle)
        {
            return new Fixed(fineSine[fineAngle + fineCosineOffset]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Angle TanToAngle(uint tan)
        {
            return new Angle(tanToAngle[tan]);
        }
    }
}
