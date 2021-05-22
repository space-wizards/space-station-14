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
    public static class BoxEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed Top(this Fixed[] box)
        {
            return box[Box.Top];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed Bottom(this Fixed[] box)
        {
            return box[Box.Bottom];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed Left(this Fixed[] box)
        {
            return box[Box.Left];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed Right(this Fixed[] box)
        {
            return box[Box.Right];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Top(this int[] box)
        {
            return box[Box.Top];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Bottom(this int[] box)
        {
            return box[Box.Bottom];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Left(this int[] box)
        {
            return box[Box.Left];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Right(this int[] box)
        {
            return box[Box.Right];
        }
    }
}
